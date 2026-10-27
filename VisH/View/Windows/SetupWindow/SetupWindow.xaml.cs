using System.Windows;
using VisH.ViewModel;

namespace VisH.View.Windows.SetupWindow;

public partial class SetupWindow : Window
{
    public SetupConfiguration? Result => ((SetupViewModel)DataContext).Result;

    public SetupWindow()
    {
        InitializeComponent();
        var viewModel = new SetupViewModel();
        DataContext = viewModel;
        viewModel.RequestClose += result => DialogResult = result;
    }
}
