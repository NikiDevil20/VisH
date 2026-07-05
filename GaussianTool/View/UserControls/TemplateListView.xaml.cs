using System.Windows;
using System.Windows.Controls;
using GaussianTool.Objects;

namespace GaussianTool.View.UserControls;

public partial class TemplateListView : UserControl
{
    private static CalcParameters _parameters = new CalcParameters("B3LYP", "6-31G(d)");
    private static CalcTemplate _template = new CalcTemplate("Langer Name", _parameters);
    
    public TemplateListView()
    {
        InitializeComponent();
    }

    private void AddTemplateBtn_OnClick(object sender, RoutedEventArgs e)
    {
        TemplateLv.Items.Add(_template);
    }

    private void RemoveTemplateBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var item = TemplateLv.SelectedItem;
        
        string message = $"Do you really want to delete template {item}? \n This action cannot be undone.";
        
        var answer = MessageBox.Show(message, "Delete Template", MessageBoxButton.YesNo, 
            MessageBoxImage.Warning);
        
        if (answer == MessageBoxResult.Yes)
            TemplateLv.Items.Remove(item);
    }
}