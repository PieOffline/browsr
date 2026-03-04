using System.Windows.Controls;
using System.Windows.Input;

namespace ProPilot.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private void MessageBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            var vm = DataContext as ProPilot.ViewModels.ChatViewModel;
            if (vm?.SendCommand.CanExecute(null) == true)
            {
                vm.SendCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
