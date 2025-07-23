using System.Diagnostics;
using System.Windows;

namespace XML_Extractor
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Logo_Click(object sender, RoutedEventArgs e)
        {
            Open("https://github.com/NoahDomingues/XML-Extractor");
        }

        private void DiscordIcon_Click(object sender, RoutedEventArgs e)
        {
            Open("https://discord.gg/Z88NnTgpWU");
        }

        private void GitHubIcon_Click(object sender, RoutedEventArgs e)
        {
            Open("https://github.com/NoahDomingues/XML-Extractor");
        }

        private void YouTubeIcon_Click(object sender, RoutedEventArgs e)
        {
            Open("https://www.youtube.com/@NoahDomingues?sub_confirmation=1");
        }

        private void Open(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
