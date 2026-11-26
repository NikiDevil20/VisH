using VisH.Model.GeneralUtils.Enums;
using VisH.ViewModel;
using Xunit;

namespace VisH.Tests;

public sealed class WorkflowViewmodelTests
{
    [Fact]
    public void WorkflowMustBeSelectedBeforeLeavingFirstStep()
    {
        var viewModel = new WorkflowViewmodel();

        Assert.False(viewModel.NextButtonCommand.CanExecute(null));

        viewModel.IsFluorescenceSelected = true;

        Assert.Equal(Workflows.Fluorescence, viewModel.SelectedWorkflow);
        Assert.True(viewModel.IsFluorescenceSelected);
        Assert.True(viewModel.NextButtonCommand.CanExecute(null));
    }

    [Fact]
    public void SelectingAnotherWorkflowUpdatesRadioButtonProperties()
    {
        var viewModel = new WorkflowViewmodel
        {
            IsFluorescenceSelected = true
        };

        viewModel.IsElectronAffinitySelected = true;

        Assert.Equal(Workflows.ElectronAffinity, viewModel.SelectedWorkflow);
        Assert.False(viewModel.IsFluorescenceSelected);
        Assert.True(viewModel.IsElectronAffinitySelected);
    }

    [Fact]
    public void WorkflowSelectionIsPreservedAcrossWizardSteps()
    {
        var viewModel = new WorkflowViewmodel
        {
            IsElectronAffinitySelected = true
        };

        viewModel.NextStep();
        viewModel.NextStep();
        viewModel.PreviousStep();

        Assert.Equal(1, viewModel.WorkflowProgress);
        Assert.Equal(Workflows.ElectronAffinity, viewModel.SelectedWorkflow);
    }
}
