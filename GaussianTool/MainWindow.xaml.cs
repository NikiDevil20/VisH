using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GaussianTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

    }

    private void DebugBtn_OnClick(object sender, RoutedEventArgs e)
    {
        // Atom atom1 = new Atom("C", 0, 0, 0) ;
        // Atom atom2 = new Atom("O", 1, 1, 1);
        // Atom[] atoms = [atom1, atom2];
        //
        // Molecule molecule = new Molecule("BeispielMolekuel", atoms, 0, 1);
        // CalcParameters parameters = new CalcParameters("td", 16, 32, "wb97xd",
        //     "def2tzvp", "S0", "dichloromethane", "71:00:00", ["opt", "freq"]);
        // CalcParameters link = parameters;
        //
        // Calculation calc = new Calculation(molecule, parameters, link);
        //
        // calc.WriteFiles();
        
    }
}