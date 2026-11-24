using Xunit;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.Workflow;

namespace VisH.Tests;

public sealed class WorkflowManagerTests
{
    [Fact]
    public void StartSubmitsOnlyReadyJobs()
    {
        using var fixture = new WorkflowFixture();
        var workflow = fixture.CreateManager().Create(
            "emission-001",
            "Emission",
            [
                new WorkflowNode
                {
                    NodeId = "optimization",
                    Role = "S1Optimization",
                    JobType = JobTypes.GeometryOptimization,
                    Cpus = 16,
                    MemoryGb = 64
                },
                new WorkflowNode
                {
                    NodeId = "td",
                    Role = "EmissionSpectrum",
                    JobType = JobTypes.TimeDependant,
                    Cpus = 4,
                    MemoryGb = 16,
                    DependsOn = ["optimization"]
                }
            ]);

        fixture.CreateManager().Start(workflow);

        Assert.Single(fixture.Submissions);
        Assert.Equal("optimization", fixture.Submissions[0].NodeId);
        Assert.Empty(fixture.Submissions[0].DependencyJobIds);
        Assert.Equal(
            WorkflowNodeState.WaitingForDependencies,
            workflow.Nodes.Single(node => node.NodeId == "td").State);
        Assert.Equal(WorkflowState.Running, workflow.State);
        Assert.Equal(0, workflow.CompletedNodeCount);

        fixture.Statuses["job-1"] = JobState.Successful;
        var manager = fixture.CreateManager();
        manager.Refresh("emission-001");
        Assert.Equal("td", fixture.Submissions[1].NodeId);
        Assert.Equal(["job-1"], fixture.Submissions[1].DependencyJobIds);
    }

    [Fact]
    public void RefreshPropagatesStatusesAndCompletesWorkflow()
    {
        using var fixture = new WorkflowFixture();
        var manager = fixture.CreateManager();
        manager.Create(
            "emission-002",
            "Emission",
            [
                new WorkflowNode
                {
                    NodeId = "optimization",
                    Role = "S1Optimization",
                    JobType = JobTypes.GeometryOptimization,
                    Cpus = 16,
                    MemoryGb = 64
                },
                new WorkflowNode
                {
                    NodeId = "td",
                    Role = "EmissionSpectrum",
                    JobType = JobTypes.TimeDependant,
                    Cpus = 4,
                    MemoryGb = 16,
                    DependsOn = ["optimization"]
                }
            ]);
        manager.Start(fixture.Repository.Load("emission-002"));

        fixture.Statuses["job-1"] = JobState.Successful;
        fixture.Statuses["job-2"] = JobState.Running;
        var running = manager.Refresh("emission-002");

        Assert.Equal(WorkflowState.PartiallyCompleted, running.State);
        Assert.Equal(1, running.CompletedNodeCount);
        Assert.Equal(50, running.CompletionPercentage);

        fixture.Statuses["job-2"] = JobState.Successful;
        var completed = manager.Refresh("emission-002");

        Assert.Equal(WorkflowState.Successful, completed.State);
        Assert.Equal(2, completed.CompletedNodeCount);
        Assert.Equal(100, completed.CompletionPercentage);
    }

    [Fact]
    public void WorkflowCanBeLoadedAfterManagerRestart()
    {
        using var fixture = new WorkflowFixture();
        var manager = fixture.CreateManager();
        manager.Create(
            "emission-003",
            "Emission",
            [
                new WorkflowNode
                {
                    NodeId = "optimization",
                    Role = "S1Optimization",
                    JobType = JobTypes.GeometryOptimization,
                    Cpus = 16,
                    MemoryGb = 64
                },
                new WorkflowNode
                {
                    NodeId = "td",
                    Role = "EmissionSpectrum",
                    JobType = JobTypes.TimeDependant,
                    Cpus = 4,
                    MemoryGb = 16,
                    DependsOn = ["optimization"]
                }
            ]);
        manager.Start(fixture.Repository.Load("emission-003"));

        var restartedManager = fixture.CreateManager();
        fixture.Statuses["job-1"] = JobState.Successful;
        var loaded = restartedManager.Refresh("emission-003");

        Assert.Equal("emission-003", loaded.WorkflowId);
        Assert.Equal("job-1", loaded.Nodes.Single(node =>
            node.NodeId == "optimization").JobId);
        Assert.Equal("job-2", loaded.Nodes.Single(node =>
            node.NodeId == "td").JobId);
        Assert.Equal(2, fixture.Submissions.Count);
    }

    [Fact]
    public void FailedDependencyBlocksDependentNode()
    {
        using var fixture = new WorkflowFixture();
        var manager = fixture.CreateManager();
        manager.Create(
            "emission-004",
            "Emission",
            [
                new WorkflowNode
                {
                    NodeId = "optimization",
                    Role = "S1Optimization",
                    JobType = JobTypes.GeometryOptimization,
                    Cpus = 16,
                    MemoryGb = 64
                },
                new WorkflowNode
                {
                    NodeId = "td",
                    Role = "EmissionSpectrum",
                    JobType = JobTypes.TimeDependant,
                    Cpus = 4,
                    MemoryGb = 16,
                    DependsOn = ["optimization"]
                }
            ]);
        manager.Start(fixture.Repository.Load("emission-004"));

        fixture.Statuses["job-1"] = JobState.Failed;
        var failed = manager.Refresh("emission-004");

        Assert.Equal(WorkflowState.Failed, failed.State);
        Assert.Equal(WorkflowNodeState.Failed, failed.Nodes.Single(node =>
            node.NodeId == "td").State);
        Assert.Single(fixture.Submissions);
    }

    [Fact]
    public void RejectsCyclicWorkflow()
    {
        using var fixture = new WorkflowFixture();

        Assert.Throws<ArgumentException>(() => fixture.CreateManager().Create(
            "cycle-001",
            "Invalid",
            [
                new WorkflowNode { NodeId = "a", DependsOn = ["b"] },
                new WorkflowNode { NodeId = "b", DependsOn = ["a"] }
            ]));
    }

    [Fact]
    public void CalculationFactoryDispatchesByJobTypeAndReceivesDependencies()
    {
        var factory = new DelegateWorkflowCalculationFactory();
        IReadOnlyList<WorkflowNode>? receivedDependencies = null;
        factory.Register(
            JobTypes.TimeDependant,
            (_, dependencies) =>
            {
                receivedDependencies = dependencies;
                return new VisH.Model.CalculationObject.Calculation();
            });

        var dependency = new WorkflowNode
        {
            NodeId = "optimization",
            JobId = "job-1",
            State = WorkflowNodeState.Successful
        };
        var request = new WorkflowCalculationRequest(
            "emission-005",
            "td",
            "EmissionSpectrum",
            JobTypes.TimeDependant,
            4,
            16,
            ["job-1"],
            [dependency]);

        var calculation = factory.Create(request, request.DependencyNodes!);

        Assert.NotNull(calculation);
        Assert.Same(dependency, Assert.Single(receivedDependencies!));
    }

    [Fact]
    public void GaussianFactoryRequiresCheckpointForDependentCalculation()
    {
        var factory = new GaussianWorkflowCalculationFactory(
            null!,
            new Dictionary<string, BundledConstructionParameters>
            {
                ["td"] = CreateParameters(JobTypes.TimeDependant)
            });
        var request = new WorkflowCalculationRequest(
            "emission-006",
            "td",
            "EmissionSpectrum",
            JobTypes.TimeDependant,
            4,
            16,
            ["job-1"]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.Create(
                request,
                [
                    new WorkflowNode
                    {
                        NodeId = "optimization",
                        JobId = "job-1"
                    }
                ]));

        Assert.Contains("checkpoint", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmissionDefinitionCreatesConfiguredTwoNodePlan()
    {
        var optimization = CreateParameters(JobTypes.GeometryOptimization) with
        {
            Memory = 64,
            NProcs = 16
        };
        var timeDependent = CreateParameters(JobTypes.TimeDependant) with
        {
            Memory = 16,
            NProcs = 4
        };

        var plan = EmissionWorkflowDefinition.Create(
            "emission-007",
            optimization,
            timeDependent);

        Assert.Equal("Emission", plan.Workflow.WorkflowType);
        Assert.Equal(2, plan.Workflow.Nodes.Count);
        Assert.Equal(16, plan.Workflow.Nodes[0].Cpus);
        Assert.Equal(64, plan.Workflow.Nodes[0].MemoryGb);
        Assert.Equal(
            ["excited-state-optimization"],
            plan.Workflow.Nodes[1].DependsOn);
        Assert.Equal(4, plan.Workflow.Nodes[1].Cpus);
        Assert.Equal(16, plan.Workflow.Nodes[1].MemoryGb);
    }

    [Fact]
    public void SubmissionResultStoresCalculationArtifactsOnWorkflowNode()
    {
        using var fixture = new WorkflowFixture();
        fixture.SubmissionService.AddArtifacts = true;
        var manager = fixture.CreateManager();
        var workflow = manager.Create(
            "emission-008",
            "Emission",
            [
                new WorkflowNode
                {
                    NodeId = "optimization",
                    Role = "S1Optimization",
                    JobType = JobTypes.GeometryOptimization,
                    Cpus = 16,
                    MemoryGb = 64
                },
                new WorkflowNode
                {
                    NodeId = "td",
                    Role = "EmissionSpectrum",
                    JobType = JobTypes.TimeDependant,
                    Cpus = 4,
                    MemoryGb = 16,
                    DependsOn = ["optimization"]
                }
            ]);

        manager.Start(workflow);

        var optimization = workflow.Nodes.Single(node => node.NodeId == "optimization");
        Assert.Equal("optimization-calculation", optimization.CalculationPath);
        Assert.Equal("Checkpoint", Assert.Single(optimization.Artifacts).Type);
    }

    [Fact]
    public void ProcessingNodeRunsAfterCalculationDependenciesAndPublishesResult()
    {
        using var fixture = new WorkflowFixture();
        fixture.Processor = new EnergyDifferenceProcessor();
        fixture.SubmissionService.AddEnergyArtifacts = true;
        var manager = fixture.CreateManager();
        var workflow = manager.Create(
            "edd-001",
            "ElectronDensityDifference",
            [
                new WorkflowNode
                {
                    NodeId = "ground",
                    Role = "GroundState",
                    JobType = JobTypes.SinglePoint
                },
                new WorkflowNode
                {
                    NodeId = "excited",
                    Role = "ExcitedState",
                    JobType = JobTypes.SinglePoint
                },
                new WorkflowNode
                {
                    NodeId = "difference",
                    Role = "EnergyDifference",
                    Kind = WorkflowNodeKind.Processing,
                    ProcessorType = EnergyDifferenceProcessor.ProcessorType,
                    DependsOn = ["ground", "excited"]
                }
            ]);

        manager.Start(workflow);
        fixture.Statuses["job-1"] = JobState.Successful;
        fixture.Statuses["job-2"] = JobState.Successful;
        var completed = manager.Refresh("edd-001");

        var result = Assert.Single(completed.Nodes.Single(node =>
            node.NodeId == "difference").Artifacts);
        Assert.Equal("EnergyDifference", result.Type);
        Assert.Equal(0, result.NumericValue);
        Assert.Equal(WorkflowState.Successful, completed.State);
        Assert.Equal(100, completed.CompletionPercentage);
    }

    [Fact]
    public void EddDefinitionCreatesThreeCalculationsAndPostprocessingNode()
    {
        var source = CreateParameters(JobTypes.GeometryOptimization);
        var density = CreateParameters(JobTypes.ElectronDensityDifference) with
        {
            State = State.S1,
            NProcs = 8,
            Memory = 32
        };
        var ground = CreateParameters(JobTypes.SinglePoint);

        var plan = EddWorkflowDefinition.Create("edd-002", source, density, ground);

        Assert.Equal("ElectronDensityDifference", plan.Workflow.WorkflowType);
        Assert.Equal(4, plan.Workflow.Nodes.Count);
        Assert.Equal(
            JobTypes.EddPostProcessing,
            plan.Workflow.Nodes.Single(node => node.NodeId == "edd-postprocessing").JobType);
        Assert.Equal(
            ["source-optimization"],
            plan.Workflow.Nodes.Single(node => node.NodeId == "density-calculation").DependsOn);
        Assert.Equal(
            ["density-calculation", "ground-state-calculation"],
            plan.Workflow.Nodes.Single(node => node.NodeId == "edd-postprocessing").DependsOn);
    }

    private static BundledConstructionParameters CreateParameters(JobTypes jobType) =>
        new(
            "TestMolecule",
            null,
            "0",
            State.S0,
            16,
            4,
            Functionals.B3LYP,
            BasisSets.def2svp,
            jobType,
            Solvents.None,
            TimeSpan.FromHours(1),
            null,
            [],
            null,
            null,
            null);

    private sealed class WorkflowFixture : IDisposable
    {
        public FakeSubmissionService SubmissionService { get; } = new();
        public FakeStatusService StatusService { get; } = new();
        public string RepositoryDirectory { get; } = Path.Combine(
            Path.GetTempPath(),
            "vish-workflow-tests",
            Guid.NewGuid().ToString("N"));
        public JsonWorkflowRepository Repository { get; }

        public WorkflowFixture()
        {
            Repository = new JsonWorkflowRepository(RepositoryDirectory);
        }

        public Dictionary<string, JobState> Statuses => StatusService.Statuses;
        public List<WorkflowCalculationRequest> Submissions => SubmissionService.Submissions;
        public IWorkflowProcessor? Processor { get; set; }

        public WorkflowManager CreateManager() =>
            new(SubmissionService, StatusService, Repository, Processor);

        public void Dispose()
        {
            if (Directory.Exists(RepositoryDirectory))
                Directory.Delete(RepositoryDirectory, recursive: true);
        }
    }

    private sealed class FakeSubmissionService : IWorkflowSubmissionService
    {
        public List<WorkflowCalculationRequest> Submissions { get; } = [];
        public bool AddArtifacts { get; set; }
        public bool AddEnergyArtifacts { get; set; }

        public WorkflowSubmissionResult Submit(WorkflowCalculationRequest request)
        {
            Submissions.Add(request);
            var artifacts = new List<WorkflowArtifact>();
            if (AddArtifacts)
            {
                artifacts.Add(new WorkflowArtifact
                {
                    ArtifactId = $"{request.NodeId}-checkpoint",
                    Type = "Checkpoint",
                    ProducerNodeId = request.NodeId,
                    RelativePath = $"{request.NodeId}/gauss.chk"
                });
            }
            if (AddEnergyArtifacts)
            {
                artifacts.Add(new WorkflowArtifact
                {
                    ArtifactId = $"{request.NodeId}-energy",
                    Type = "Energy",
                    ProducerNodeId = request.NodeId,
                    NumericValue = 10,
                    Unit = "Hartree"
                });
            }
            return new WorkflowSubmissionResult(
                $"job-{Submissions.Count}",
                $"{request.NodeId}-calculation",
                artifacts.Count == 0 ? null : artifacts);
        }
    }

    private sealed class FakeStatusService : IWorkflowStatusService
    {
        public Dictionary<string, JobState> Statuses { get; } = [];

        public JobState GetStatus(string jobId) =>
            Statuses.TryGetValue(jobId, out var state) ? state : JobState.Queue;
    }
}
