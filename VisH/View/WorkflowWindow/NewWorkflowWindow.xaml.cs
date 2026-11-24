using System.Windows;
using VisH.ViewModel;

namespace VisH.View.WorkflowWindow;

public partial class NewWorkflowWindow : Window
{
    public NewWorkflowWindow()
    {
        InitializeComponent();
        DataContext = new WorkflowViewmodel();
    }
}