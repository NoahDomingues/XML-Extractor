using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Xml;
using System.Diagnostics;
using Microsoft.Win32;                   // WPF OpenFileDialog
using WinForms = System.Windows.Forms;   // alias for FolderBrowserDialog

namespace XML_Extractor
{
    public partial class MainWindow : Window
    {
        private string _singleFilePath;
        private string _folderPath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                LogBox.ScrollToEnd();
            });
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select File to Scan",
                Filter = "All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                _singleFilePath = dlg.FileName;
                _folderPath = null;
                Log($"Selected file: {_singleFilePath}");
                BtnExtract.IsEnabled = true;
            }
        }

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new WinForms.FolderBrowserDialog
            {
                Description = "Select folder to scan",
                UseDescriptionForTitle = true
            };

            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
            {
                _folderPath = dlg.SelectedPath;
                _singleFilePath = null;
                Log($"Selected folder: {_folderPath}");
                BtnExtract.IsEnabled = true;
            }
        }

        private async void BtnExtract_Click(object sender, RoutedEventArgs e)
        {
            BtnExtract.IsEnabled = false;
            BtnSelectFile.IsEnabled = false;
            BtnSelectFolder.IsEnabled = false;

            if (!string.IsNullOrEmpty(_singleFilePath))
                await ExtractSingleFileAsync(_singleFilePath);
            else if (!string.IsNullOrEmpty(_folderPath))
                await ExtractFolderAsync(_folderPath);

            Log("✅ All done.");
            BtnSelectFile.IsEnabled = true;
            BtnSelectFolder.IsEnabled = true;
            BtnExtract.IsEnabled = false;
        }

        // ─────────────────────────────────────────────────
        // Single‐file XML extraction: proper prologs only,
        // any version, safe try/catch
        // ─────────────────────────────────────────────────
        private async Task ExtractSingleFileAsync(string filePath)
        {
            try
            {
                // 1) Read file & detect BOM
                byte[] data = File.ReadAllBytes(filePath);
                Encoding enc = DetectEncoding(data, out int bomLen);
                string text = enc.GetString(data, bomLen, data.Length - bomLen);

                // 2) Find all <?xml version="..."?> occurrences (any version)
                var regex = new Regex(@"<\?xml\s+version\s*=\s*['""](?<ver>[^'""]+)['""].*?\?>",
                                         RegexOptions.IgnoreCase);
                var matches = regex.Matches(text);
                int total = matches.Count;

                if (total == 0)
                {
                    Log("❌ No XML prologs found.");
                    return;
                }
                Log($"Found {total} XML prolog(s) (any version).");

                // 3) Prepare per-file output folder
                string outputDir = PrepareOutputFolder(filePath);
                if (outputDir == null) return;  // user skipped

                // 4) Setup progress bar
                Dispatcher.Invoke(() =>
                {
                    ExtractionProgress.Minimum = 0;
                    ExtractionProgress.Maximum = total;
                    ExtractionProgress.Value = 0;
                    ExtractionProgress.IsIndeterminate = false;
                    ExtractionProgress.Visibility = Visibility.Visible;
                });

                int written = 0;

                // 5) Iterate each prolog match
                await Task.Run(() =>
                {
                    for (int i = 0; i < total; i++)
                    {
                        int start = matches[i].Index;
                        string snippet = text.Substring(start);

                        // 6) Extract root tag name after prolog
                        var rootMatch = Regex.Match(
                          snippet,
                          @"<\?xml\b.*?\?>\s*<(?<tag>[A-Za-z_:][\w:\-\.]*)",
                          RegexOptions.Singleline);
                        if (!rootMatch.Success)
                        {
                            Log($"⚠️ Could not find root tag in prolog #{i + 1}");
                            Dispatcher.Invoke(() => ExtractionProgress.Value++);
                            continue;
                        }

                        string rootTag = rootMatch.Groups["tag"].Value;
                        string closing = $"</{rootTag}>";

                        int endIdx = snippet.IndexOf(closing, StringComparison.Ordinal);
                        if (endIdx < 0)
                        {
                            Log($"⚠️ No closing </{rootTag}> for prolog #{i + 1}");
                            Dispatcher.Invoke(() => ExtractionProgress.Value++);
                            continue;
                        }

                        string rawXml = snippet.Substring(0, endIdx + closing.Length);

                        try
                        {
                            var doc = new XmlDocument();
                            doc.LoadXml(rawXml);

                            var settings = new XmlWriterSettings
                            {
                                Indent = true,
                                IndentChars = "    ",
                                OmitXmlDeclaration = false,
                                NewLineChars = "\n",
                                NewLineHandling = NewLineHandling.Replace
                            };

                            written++;
                            string filename = $"{rootTag}_{written:00}.xml";
                            string outPath = Path.Combine(outputDir, filename);
                            using var writer = XmlWriter.Create(outPath, settings);
                            doc.Save(writer);

                            Log($"📄 Saved {filename}");
                        }
                        catch (Exception exBlock)
                        {
                            Log($"❌ XML parse error in block #{i + 1}: {exBlock.Message}");
                        }
                        finally
                        {
                            Dispatcher.Invoke(() => ExtractionProgress.Value++);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"❌ Unexpected error: {ex.Message}");
            }
        }

        // Folder‐mode: just calls single‐file extractor for each file
        private async Task ExtractFolderAsync(string folder)
        {
            var files = Directory.GetFiles(folder);
            int total = files.Length;

            Dispatcher.Invoke(() =>
            {
                ExtractionProgress.Minimum = 0;
                ExtractionProgress.Maximum = total;
                ExtractionProgress.Value = 0;
                ExtractionProgress.IsIndeterminate = false;
                ExtractionProgress.Visibility = Visibility.Visible;
            });

            foreach (var file in files)
            {
                await ExtractSingleFileAsync(file);
                Dispatcher.Invoke(() => ExtractionProgress.Value++);
            }
        }

        // BOM detection helper (UTF-8/16LE/16BE/32)
        private static Encoding DetectEncoding(byte[] data, out int bomLength)
        {
            bomLength = 0;
            if (data.Length >= 3 &&
                data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                bomLength = 3;
                return Encoding.UTF8;
            }
            if (data.Length >= 2 &&
                data[0] == 0xFF && data[1] == 0xFE)
            {
                bomLength = 2;
                return Encoding.Unicode;  // UTF-16 LE
            }
            if (data.Length >= 2 &&
                data[0] == 0xFE && data[1] == 0xFF)
            {
                bomLength = 2;
                return Encoding.BigEndianUnicode; // UTF-16 BE
            }
            if (data.Length >= 4 &&
                data[0] == 0xFF && data[1] == 0xFE &&
                data[2] == 0x00 && data[3] == 0x00)
            {
                bomLength = 4;
                return Encoding.UTF32;
            }
            return Encoding.UTF8;  // default
        }

        // Prepares & prompts per‐file output folder
        private string PrepareOutputFolder(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath)!;
            string fileName = Path.GetFileName(filePath);
            int dotIndex = fileName.LastIndexOf('.');
            string folderName = dotIndex >= 0
                ? fileName.Substring(0, dotIndex) + "_" + fileName.Substring(dotIndex + 1)
                : fileName;
            string outputDir = Path.Combine(dir, folderName);

            if (Directory.Exists(outputDir))
            {
                var dlg = new Dialogs.ConfirmationDialog(
                    $"Output folder '{outputDir}' already exists.\n\nOverwrite?",
                    "Folder Exists"
                );
                dlg.Owner = this;
                dlg.ShowDialog();
                
                if (dlg.Result == true)
                    Directory.Delete(outputDir, true);
                else
                {
                    Log($"⚠️ Skipped '{fileName}'");
                    return null;
                }
            }

            Directory.CreateDirectory(outputDir);
            return outputDir;
        }

        // Link handlers
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void AboutLink_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow { Owner = this }.ShowDialog();
        }
    }
}
