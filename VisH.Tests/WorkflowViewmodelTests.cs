using System.Text.Json;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationProperties;
using VisH.Model.GeneralUtils.Enums;
using VisH.Model.Enums;
using VisH.Model.PostRun;
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
        var molecule = new WorkflowMoleculeViewModel("Mol", "C", null)
        {
            IsNewGeometrySelected = true
        };
        viewModel.MoleculeSelection.Molecules.Add(molecule);

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
        viewModel.MoleculeSelection.Molecules.Add(
            new WorkflowMoleculeViewModel("Mol", "C", null)
            {
                IsNewGeometrySelected = true
            });

        settings.SelectedMultiplicity = 1;
        settings.SelectedCharge = "-2";
        viewModel.NextStep();
        viewModel.NextStep();
        viewModel.PreviousStep();

        Assert.Equal(1, viewModel.WorkflowProgress);
        Assert.Equal(1, viewModel.WorkflowOptions.MoleculeStates[1].SelectedMultiplicity);
        Assert.Equal("-2", viewModel.WorkflowOptions.MoleculeStates[1].SelectedCharge);
    }

    [Fact]
    public void LocalGeometryCatalogExcludesArchivedAndInvalidJobs()
    {
        var root = Path.Combine(Path.GetTempPath(), "vish-molecule-tests", Guid.NewGuid().ToString("N"));
        try
        {
            WriteCalculation(root, ["Mol", "S0", "valid"], "job-1", JobState.Successful, true);
            WriteCalculation(root, ["Mol", "S0", "failed"], "job-2", JobState.Failed, true);
            WriteCalculation(root, ["Mol", "S0", "invalid"], "job-3", JobState.Successful, false);
            WriteCalculation(
                root,
                ["AlteRechnungen", "Mol", "S0", "old"],
                "job-old",
                JobState.Successful,
                true);

            var finder = new JobFinder(null!, null!);
            var calculations = finder.GetSuccessfulLocalGeometryCalculations(root);

            var calculation = Assert.Single(calculations);
            Assert.Equal("job-1", calculation.MetaData!.JobId);
            Assert.Equal("Mol", calculation.Molecule!.MoleculeName);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void MoleculeTileSupportsMultipleExistingGeometriesOrOneNewGeometry()
    {
        var firstCalculation = CreateGeometryCalculation("job-1");
        var secondCalculation = CreateGeometryCalculation("job-2");
        var firstGeometry = new WorkflowGeometryViewModel(firstCalculation);
        var secondGeometry = new WorkflowGeometryViewModel(secondCalculation);
        var molecule = new WorkflowMoleculeViewModel(
            "Mol",
            "C",
            null,
            [firstGeometry, secondGeometry]);

        firstGeometry.IsSelected = true;
        secondGeometry.IsSelected = true;
        Assert.Equal(2, molecule.GetSelectedGeometries().Count());

        molecule.IsNewGeometrySelected = true;
        var selectedNew = Assert.Single(molecule.GetSelectedGeometries());
        Assert.Null(selectedNew.ExistingCalculation);
        Assert.False(firstGeometry.IsSelected);
        Assert.False(secondGeometry.IsSelected);

        molecule.IsNewGeometrySelected = false;
        secondGeometry.IsSelected = true;
        Assert.False(molecule.IsNewGeometrySelected);
        Assert.Single(molecule.GetSelectedGeometries());
    }

    private static void WriteCalculation(
        string root,
        string[] directoryParts,
        string jobId,
        JobState status,
        bool geometryValid)
    {
        var directory = Path.Combine(directoryParts.Prepend(root).ToArray());
        Directory.CreateDirectory(directory);

        var calculation = CreateGeometryCalculation(jobId, status, geometryValid);
        File.WriteAllText(
            Path.Combine(directory, "calculation.json"),
            JsonSerializer.Serialize(calculation));
    }

    private static Calculation CreateGeometryCalculation(
        string jobId,
        JobState state = JobState.Successful,
        bool geometryValid = true) =>
        new()
        {
            MetaData = new MetaData { JobId = jobId, JobState = state },
            GaussianParameters = new GaussianParameters
            {
                Jobtype = JobTypes.GeometryOptimization
            },
            Molecule = new Molecule
            {
                MoleculeName = "Mol",
                SmilesString = "C",
                Atoms = []
            },
            Results = new Results { GeometryValid = geometryValid }
        };
}
