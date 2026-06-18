using System.Windows;

namespace CassaEventiAI.Views.Shared;

public partial class ReceiptPreviewWindow : Window
{
    public ReceiptPreviewWindow(string previewContent, string title = "Anteprima scontrino")
    {
        InitializeComponent();
        Title = title;
        PreviewText.Text = previewContent;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
