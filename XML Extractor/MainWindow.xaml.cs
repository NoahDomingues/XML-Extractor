using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;               // WPF OpenFileDialog
using WinForms = System.Windows.Forms; // alias for FolderBrowserDialog
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

            // WinForms.ShowDialog() returns DialogResult, not bool
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
            ExtractionProgress.Visibility = Visibility.Visible;

            if (!string.IsNullOrEmpty(_singleFilePath))
                await ExtractFromFileAsync(_singleFilePath);
            else if (!string.IsNullOrEmpty(_folderPath))
                await ExtractFromFolderAsync(_folderPath);

            ExtractionProgress.Visibility = Visibility.Collapsed;
            Log("✅ Extraction complete.");
            BtnSelectFile.IsEnabled = true;
            BtnSelectFolder.IsEnabled = true;
        }

        private async Task ExtractFromFolderAsync(string folder)
        {
            var files = Directory.GetFiles(folder);
            Log($"Scanning folder: found {files.Length} files.");
            foreach (var file in files)
                await ExtractFromFileAsync(file);
        }

        private async Task ExtractFromFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                Log($"🔍 Processing {Path.GetFileName(filePath)}");

                byte[] data = File.ReadAllBytes(filePath);
                string text = System.Text.Encoding.UTF8.GetString(data);
                var matches = Regex.Matches(text, @"<\?xml version=""1\.0""\?>");

                if (matches.Count == 0)
                {
                    Log("❌ No XML headers found.");
                    return;
                }

                Log($"Found {matches.Count} XML headers.");

                int count = 0;
                foreach (Match m in matches)
                {
                    int start = m.Index;
                    string snippet = text.Substring(start);
                    var rootMatch = Regex.Match(
                        snippet,
                        @"<\?xml version=""1\.0""\?>\s*<(\w+)"
                    );

                    if (!rootMatch.Success)
                    {
                        Log($"⚠️ Root tag not found at index {start}");
                        continue;
                    }

                    string rootTag = rootMatch.Groups[1].Value;
                    string closing = $"</{rootTag}>";
                    int endIdx = snippet.IndexOf(closing, StringComparison.Ordinal);
                    if (endIdx < 0)
                    {
                        Log($"⚠️ No closing tag </{rootTag}> for block starting at {start}");
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

                        string outputDir = Path.Combine(
                            Path.GetDirectoryName(filePath)!,
                            "Extracted_XMLs"
                        );
                        Directory.CreateDirectory(outputDir);

                        count++;
                        string filename = $"{rootTag}_{count:00}.xml";
                        string outPath = Path.Combine(outputDir, filename);
                        using var writer = XmlWriter.Create(outPath, settings);
                        doc.Save(writer);

                        Log($"📄 Saved {filename}");
                    }
                    catch (Exception ex)
                    {
                        Log($"❌ Failed formatting block: {ex.Message}");
                    }
                }

                Log($"🏁 Extracted {count} XML(s) from {Path.GetFileName(filePath)}");
            });
        }
    }
}
