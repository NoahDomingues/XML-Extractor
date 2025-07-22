using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;                   // WPF OpenFileDialog
using WinForms = System.Windows.Forms;   // alias for FolderBrowserDialog
using System.Xml;

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
            ExtractionProgress.Visibility = Visibility.Collapsed;
            BtnSelectFile.IsEnabled = true;
            BtnSelectFolder.IsEnabled = true;
            BtnExtract.IsEnabled = false;
        }

        // Single‐file mode: track XML blocks
        private async Task ExtractSingleFileAsync(string filePath)
        {
            string outputDir = PrepareOutputFolder(filePath);
            if (outputDir == null) return;

            // Read blob, find headers
            byte[] data = File.ReadAllBytes(filePath);
            string text = System.Text.Encoding.UTF8.GetString(data);
            var matches = Regex.Matches(text, @"<\?xml version=""1\.0""\?>");
            int total = matches.Count;

            Dispatcher.Invoke(() =>
            {
                ExtractionProgress.Minimum = 0;
                ExtractionProgress.Maximum = total;
                ExtractionProgress.Value = 0;
                ExtractionProgress.IsIndeterminate = total == 0;
                ExtractionProgress.Visibility = Visibility.Visible;
            });

            int written = 0;
            await Task.Run(() =>
            {
                for (int i = 0; i < total; i++)
                {
                    int start = matches[i].Index;
                    string snippet = text.Substring(start);
                    var rootMatch = Regex.Match(
                        snippet,
                        @"<\?xml version=""1\.0""\?>\s*<(\w+)"
                    );
                    if (!rootMatch.Success)
                    {
                        Log($"⚠️ Unable to find root tag in block {i + 1}");
                        Dispatcher.Invoke(() => ExtractionProgress.Value++);
                        continue;
                    }

                    string rootTag = rootMatch.Groups[1].Value;
                    string closing = $"</{rootTag}>";
                    int endIdx = snippet.IndexOf(closing, StringComparison.Ordinal);
                    if (endIdx < 0)
                    {
                        Log($"⚠️ No closing </{rootTag}> in block {i + 1}");
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
                    catch (Exception ex)
                    {
                        Log($"❌ Error formatting block {i + 1}: {ex.Message}");
                    }
                    finally
                    {
                        Dispatcher.Invoke(() => ExtractionProgress.Value++);
                    }
                }
            });
        }

        // Folder‐mode: track file count
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

        // Prepares (and prompts) the output subfolder. Returns null on cancel.
        private string PrepareOutputFolder(string filePath)
        {
            // 1. Base directory and original filename
            string dir = Path.GetDirectoryName(filePath)!;
            string fileName = Path.GetFileName(filePath);      // e.g. "OFDR.exe"

            // 2. Replace only the last '.' with '_'
            int dotIndex = fileName.LastIndexOf('.');
            string folderName = dotIndex >= 0
                ? fileName.Substring(0, dotIndex)
                  + "_"
                  + fileName.Substring(dotIndex + 1)        // e.g. "OFDR_exe"
                : fileName;                                 // no extension case

            string outputDir = Path.Combine(dir, folderName);

            // 3. Prompt if folder already exists
            if (Directory.Exists(outputDir))
            {
                var result = MessageBox.Show(
                    $"Output folder '{outputDir}' already exists.\n\nOverwrite?",
                    "Folder Exists",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    Directory.Delete(outputDir, recursive: true);
                }
                else
                {
                    Log($"⚠️ Skipped '{fileName}'");
                    return null;
                }
            }

            // 4. Create & return the new folder path
            Directory.CreateDirectory(outputDir);
            return outputDir;
        }
    }
}
