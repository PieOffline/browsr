using System.Windows;
using ProPilot.ViewModels;

namespace ProPilot;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}