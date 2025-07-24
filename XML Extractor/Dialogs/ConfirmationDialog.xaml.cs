using System.Windows;

namespace XML_Extractor.Dialogs
{
    public partial class ConfirmationDialog : Window
    {
        public bool? Result { get; private set; }

        public ConfirmationDialog(string message, string title)
        {
            InitializeComponent();
            Title = title;
            MessageTextBlock.Text = message;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }
    }
}
