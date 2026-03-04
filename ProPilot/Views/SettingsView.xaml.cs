using System.Windows;
using System.Windows.Controls;
using ProPilot.ViewModels;

namespace ProPilot.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset all data? This cannot be undone.",
            "Reset All Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.ResetAllData();
            }
        }
    }
}
