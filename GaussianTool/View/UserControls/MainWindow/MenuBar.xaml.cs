using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GaussianTool.ViewModel;

namespace GaussianTool.View.UserControls;

public partial class MenuBar : UserControl
{
    public MenuBar()
    {
        InitializeComponent();
    }

    private void MenuBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Window.GetWindow(this)?.DragMove();
    }

    private void MinimizeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)?.WindowState = WindowState.Minimized;
    }

    private void MaximizeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this)?.WindowState == WindowState.Maximized)
        {
            Window.GetWindow(this)?.WindowState = WindowState.Normal;
        }
        else
        {
            Window.GetWindow(this)?.WindowState = WindowState.Maximized;
        }
    }

    private void CloseBtn_OnClick(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)?.Close();
    }
}