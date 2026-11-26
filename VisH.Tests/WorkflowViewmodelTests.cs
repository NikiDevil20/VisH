using VisH.Model.GeneralUtils.Enums;
using VisH.Model.Enums;
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

    [Fact]
    public void FluorescenceProvidesSharedMethodAndPerCalculationResources()
    {
        var viewModel = new WorkflowViewmodel
        {
            IsFluorescenceSelected = true
        };
        var options = Assert.IsType<WorkflowOptionsViewModel>(viewModel.WorkflowOptions);

        Assert.Single(options.MoleculeStates);
        Assert.Single(options.SharedMethodOptions);
        Assert.Equal(2, options.Calculations.Count);
        Assert.All(options.Calculations, calculation =>
            Assert.False(calculation.IncludesMethodSelection));
        Assert.Equal("S1 optimization resources", options.Calculations[0].Title);
        Assert.Equal("TD calculation resources", options.Calculations[1].Title);
    }

    [Fact]
    public void ElectronAffinityProvidesNeutralAndAnionMethodsAndResources()
    {
        var viewModel = new WorkflowViewmodel
        {
            IsElectronAffinitySelected = true
        };
        var options = Assert.IsType<WorkflowOptionsViewModel>(viewModel.WorkflowOptions);

        Assert.Equal(2, options.MoleculeStates.Count);
        Assert.Equal("0", options.MoleculeStates[0].SelectedCharge);
        Assert.Equal("-1", options.MoleculeStates[1].SelectedCharge);
        Assert.Empty(options.SharedMethodOptions);
        Assert.Equal(2, options.Calculations.Count);
        Assert.All(options.Calculations, calculation =>
            Assert.True(calculation.IncludesMethodSelection));
    }

    [Fact]
    public void SelectionChangesUpdateValuesInWorkflowOptionsViewModel()
    {
        var viewModel = new WorkflowViewmodel
        {
            IsElectronAffinitySelected = true
        };
        var settings = viewModel.WorkflowOptions!.MoleculeStates[1];

        settings.SelectedMultiplicity = 1;
        settings.SelectedCharge = "-2";
        viewModel.NextStep();
        viewModel.NextStep();
        viewModel.PreviousStep();

        Assert.Equal(1, viewModel.WorkflowProgress);
        Assert.Equal(1, viewModel.WorkflowOptions.MoleculeStates[1].SelectedMultiplicity);
        Assert.Equal("-2", viewModel.WorkflowOptions.MoleculeStates[1].SelectedCharge);
    }
}
