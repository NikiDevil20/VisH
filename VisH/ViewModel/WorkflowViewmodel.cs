using System.Windows.Input;
using System.Windows.Media;
using VisH.Model.GeneralUtils.Enums;

namespace VisH.ViewModel;

public class WorkflowViewmodel : ViewModelBase
{
    private int _workflowProgress;
    private Workflows? _selectedWorkflow;

    public WorkflowViewmodel()
    {
        BackButtonCommand = new RelayCommand(
            _ => PreviousStep(),
            _ => WorkflowProgress > 0);

        NextButtonCommand = new RelayCommand(
            _ => NextStep(),
            _ => WorkflowProgress < 4 &&
                 (WorkflowProgress != 0 || SelectedWorkflow.HasValue));
    }

    public RelayCommand BackButtonCommand { get; }
    public RelayCommand NextButtonCommand { get; }

    public Workflows? SelectedWorkflow
    {
        get => _selectedWorkflow;
        set
        {
            if (!SetProperty(ref _selectedWorkflow, value))
                return;

            OnPropertyChanged(nameof(IsFluorescenceSelected));
            OnPropertyChanged(nameof(IsElectronAffinitySelected));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsFluorescenceSelected
    {
        get => SelectedWorkflow == Workflows.Fluorescence;
        set
        {
            if (value)
                SelectedWorkflow = Workflows.Fluorescence;
        }
    }

    public bool IsElectronAffinitySelected
    {
        get => SelectedWorkflow == Workflows.ElectronAffinity;
        set
        {
            if (value)
                SelectedWorkflow = Workflows.ElectronAffinity;
        }
    }

    public int WorkflowProgress
    {
        get => _workflowProgress;
        set
        {
            if (!SetProperty(ref _workflowProgress, value))
                return;

            OnPropertyChanged(nameof(SelectWorkflowIndicator));
            OnPropertyChanged(nameof(ChooseMoleculeIndicator));
            OnPropertyChanged(nameof(MoleculeSettingsIndicator));
            OnPropertyChanged(nameof(MethodSettingsIndicator));
            OnPropertyChanged(nameof(OverviewIndicator));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public void NextStep()
    {
        if (WorkflowProgress < 4 &&
            (WorkflowProgress != 0 || SelectedWorkflow.HasValue))
        {
            WorkflowProgress++;
        }
    }

    public void PreviousStep()
    {
        if (WorkflowProgress > 0)
        {
            WorkflowProgress--;
        }
    }

    public Brush SelectWorkflowIndicator => GetIndicatorBrush(0);
    public Brush ChooseMoleculeIndicator => GetIndicatorBrush(1);
    public Brush MoleculeSettingsIndicator => GetIndicatorBrush(2);
    public Brush MethodSettingsIndicator => GetIndicatorBrush(3);
    public Brush OverviewIndicator => GetIndicatorBrush(4);

    private Brush GetIndicatorBrush(int step)
    {
        if (step < WorkflowProgress)
            return new SolidColorBrush(Color.FromRgb(0, 106, 179));

        if (step == WorkflowProgress)
            return new SolidColorBrush(Colors.Orange);

        return new SolidColorBrush(Colors.LightGray);
    }
}