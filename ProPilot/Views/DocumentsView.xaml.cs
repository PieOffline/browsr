using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ProPilot.ViewModels;

namespace ProPilot.Views;

public partial class DocumentsView : UserControl
{
    public DocumentsView()
    {
        InitializeComponent();
    }

    private void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Documents (*.pdf;*.docx)|*.pdf;*.docx|PDF Files (*.pdf)|*.pdf|Word Documents (*.docx)|*.docx",
            Title = "Select a document to upload"
        };

        if (dialog.ShowDialog() == true)
        {
            if (DataContext is DocumentsViewModel vm)
            {
                vm.UploadFile(dialog.FileName);
            }
        }
    }
}
