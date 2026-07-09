using System.Windows;
using System.Windows.Controls;
using GaussianTool.Model;

namespace GaussianTool.View.UserControls;
    
 

public partial class TemplateListView : UserControl
{
    
    
    public TemplateListView()
    {
        InitializeComponent();
    }

    private void AddTemplateBtn_OnClick(object sender, RoutedEventArgs e)
    {
        CalcParameters parameters = new CalcParameters("Opt", 8, 16, "B3LYP", "def2svp", 
            "S0", "dichloromethane", "71:00:00",["opt", "freq"]);
        CalcTemplate template = new CalcTemplate("GeoOpt Grundzustand", parameters);
        
        TemplateLv.Items.Add(template);
    }

    private void RemoveTemplateBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var item = TemplateLv.SelectedItem;
        
        string message = $"Do you really want to delete template {item.ToString()}? \n This action cannot be undone.";
        
        var answer = MessageBox.Show(message, "Delete Template", MessageBoxButton.YesNo, 
            MessageBoxImage.Warning);
        
        if (answer == MessageBoxResult.Yes)
            TemplateLv.Items.Remove(item);
    }
}