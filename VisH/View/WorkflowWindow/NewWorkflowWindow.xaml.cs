using System.Windows;
using VisH.Model.PostRun;
using VisH.ViewModel;

namespace VisH.View.WorkflowWindow;

public partial class NewWorkflowWindow : Window
{
    public NewWorkflowWindow(JobFinder? jobFinder = null)
    {
        InitializeComponent();
        DataContext = new WorkflowViewmodel(jobFinder);
    }
}