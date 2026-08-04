using System.Windows;
using System.Windows.Controls;

namespace VisH.View.UserControls;

public partial class CustomTextInput : UserControl
{
    
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(CustomTextInput),
            new PropertyMetadata("Enter text..."));
    
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }
    
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CustomTextInput),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
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
