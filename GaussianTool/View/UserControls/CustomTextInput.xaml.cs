using System.Windows;
using System.Windows.Controls;

namespace GaussianTool.View.UserControls;

public partial class CustomTextInput : UserControl
{
    public CustomTextInput()
    {
        InitializeComponent();
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        TextInput.Clear();
        TbPlaceholder.Visibility = Visibility.Visible;
        TextInput.Focus();
    }
    
    private void TextInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        TbPlaceholder.Visibility = string.IsNullOrEmpty(TextInput.Text) ? Visibility.Visible : Visibility.Collapsed;
    }
}