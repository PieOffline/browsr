using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProPilot.ViewModels;

namespace ProPilot.Views;

public partial class AssignmentsView : UserControl
{
    public AssignmentsView()
    {
        InitializeComponent();
    }

    private void AssignmentCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Models.Assignment assignment)
        {
            if (DataContext is AssignmentsViewModel vm)
            {
                vm.SelectedAssignment = assignment;
            }
        }
    }

    private void ScreenshotDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void ScreenshotDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0)
            {
                try
                {
                    var data = File.ReadAllBytes(files[0]);
                    if (DataContext is AssignmentsViewModel vm)
                        vm.SetScreenshotFromClipboard(data);
                }
                catch { }
            }
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null && DataContext is AssignmentsViewModel vm)
                {
                    using var ms = new MemoryStream();
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                    encoder.Save(ms);
                    vm.SetScreenshotFromClipboard(ms.ToArray());
                }
            }
        }
        base.OnPreviewKeyDown(e);
    }
}
