using System.IO;
using VisH.Model.CalculationProperties;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.PostRun;

namespace VisH.Model.CalculationObject;

public static class CalculationBuilder
{
    
    public static Calculation GeometryOptimization(
        BundledConstructionParameters bundledParameters,
        SshService sshService)
    {
        var metaData = new MetaData();
        var gaussianParameters = new GaussianParameters(bundledParameters);
        var molecule = new Molecule(bundledParameters, bundledParameters.GeometryAtoms);
        
        var calculation = new Calculation(sshService);
        
        calculation.AddMetaData(metaData);
        calculation.AddGaussianParameters(gaussianParameters);
        calculation.AddMolecule(molecule);
        
        calculation.AddPaths();
        calculation.GaussianParameters.AddKeywords(bundledParameters);

        return calculation;
    }

    public static Calculation TimeDependant(
        BundledConstructionParameters bundledConstructionParameters,
        string geometryOptimizationJobId,
        string geometryOptimizationChkPath,
        SshService sshService,
        bool useExistingCheckpoint = false)
    {
        if (string.IsNullOrWhiteSpace(geometryOptimizationJobId))
        {
            throw new ArgumentException("Geometry optimization job id is required for a time-dependent calculation.", nameof(geometryOptimizationJobId));
        }

        if (string.IsNullOrWhiteSpace(geometryOptimizationChkPath))
        {
            throw new ArgumentException("The geometry optimization checkpoint path is required for a time-dependent calculation.", nameof(geometryOptimizationChkPath));
        }

        var molecule = new Molecule(
            bundledConstructionParameters,
            bundledConstructionParameters.GeometryAtoms);
        var metaData = new MetaData();
        var gaussianParameters = new GaussianParameters(bundledConstructionParameters)
        {
            OldChkPath = useExistingCheckpoint ? null : geometryOptimizationChkPath,
            DependencyJobId = useExistingCheckpoint ? null : geometryOptimizationJobId
        };

        var calculation = new Calculation(sshService);
        calculation.AddGaussianParameters(gaussianParameters);
        calculation.AddMolecule(molecule);
        calculation.AddMetaData(metaData);
        calculation.AddPaths();
        
        calculation.GaussianParameters.AddKeywords(bundledConstructionParameters);

        return calculation;
    }

    public static Calculation SinglePoint(
        BundledConstructionParameters bundledParameters,
        string geometryOptimizationJobId,
        SshService sshService)
    {
        if (string.IsNullOrWhiteSpace(geometryOptimizationJobId))
        {
            throw new ArgumentException(
                "A geometry optimization job id is required for a single-point calculation.",
                nameof(geometryOptimizationJobId));
        }

        var molecule = new Molecule(
            bundledParameters,
            bundledParameters.GeometryAtoms);
        var metaData = new MetaData();
        var gaussianParameters = new GaussianParameters(bundledParameters)
        {
            OldChkPath = null
        };

        var calculation = new Calculation(sshService);
        calculation.AddGaussianParameters(gaussianParameters);
        calculation.AddMolecule(molecule);
        calculation.AddMetaData(metaData);
        calculation.AddPaths();

        calculation.GaussianParameters.AddKeywords(bundledParameters);

        return calculation;
    }

    public static (Calculation TimeDependent, Calculation GroundState) ElectronDensityDifference(
        BundledConstructionParameters bundledParameters,
        Calculation sourceOptimization,
        SshService sshService,
        bool sourceIsAlreadyComplete)
    {
        if (bundledParameters.State is not (State.S1 or State.S2 or State.S3))
        {
            throw new ArgumentException("EDD calculations require state S1, S2, or S3.", nameof(bundledParameters));
        }

        if (sourceOptimization.Paths?.ChkFile == null ||
            sourceOptimization.Paths.RelativeDirectory == null)
        {
            throw new InvalidOperationException("The source optimization has no checkpoint path.");
        }

        var sourcePath = sourceOptimization.Paths.RelativeDirectory.RelativePath.ToArray();
        var sharedPath = sourcePath[..^1]
            .Append(sourcePath[^1] + "_edd")
            .ToArray();
        var tdPath = sharedPath.Append("td").ToArray();
        var spPath = sharedPath.Append("sp").ToArray();

        var tdParameters = bundledParameters with { JobType = JobTypes.ElectronDensityDifference };
        var td = new Calculation(sshService);
        td.AddGaussianParameters(new GaussianParameters(tdParameters)
        {
            DependencyJobId = sourceIsAlreadyComplete ? null : sourceOptimization.MetaData?.JobId,
            IsEddDensityCalculation = true
        });
        td.AddMolecule(new Molecule(tdParameters, sourceOptimization.Molecule?.Atoms));
        td.AddMetaData(new MetaData
        {
            JobName = $"{sourceOptimization.Molecule!.MoleculeName}_td"
        });
        td.AddPaths(string.Join(Path.DirectorySeparatorChar, tdPath));
        td.GaussianParameters.AddKeywords(tdParameters);

        var spParameters = bundledParameters with { JobType = JobTypes.SinglePoint };
        var sp = new Calculation(sshService);
        sp.AddGaussianParameters(new GaussianParameters(spParameters)
        {
            DependencyJobId = sourceIsAlreadyComplete ? null : sourceOptimization.MetaData?.JobId,
            IsEddDensityCalculation = true
        });
        sp.AddMolecule(new Molecule(spParameters, sourceOptimization.Molecule?.Atoms));
        sp.AddMetaData(new MetaData());
        sp.AddPaths(string.Join(Path.DirectorySeparatorChar, spPath));
        sp.GaussianParameters.AddKeywords(spParameters);

        var sharedDirectory = new DirectoryExtension(sharedPath, sshService);
        td.EddSharedDirectory = sharedDirectory.GetPath(PathType.Cluster);
        sp.EddSharedDirectory = td.EddSharedDirectory;
        sp.EddPartnerDirectory = td.Paths.RelativeDirectory.GetPath(PathType.Cluster);
        if (!sourceIsAlreadyComplete)
        {
            td.EddCheckpointSourceDirectory =
                sourceOptimization.Paths.RelativeDirectory.GetPath(PathType.Cluster);
            sp.EddCheckpointSourceDirectory = td.EddCheckpointSourceDirectory;
        }

        return (td, sp);
    }

    public static Calculation EddPostProcessing(
        BundledConstructionParameters bundledParameters,
        Calculation timeDependent,
        Calculation groundState,
        string dependencyJobIds,
        SshService sshService)
    {
        var sharedPath = timeDependent.Paths!.RelativeDirectory.RelativePath
            .Take(timeDependent.Paths.RelativeDirectory.RelativePath.Count - 1)
            .ToArray();
        var postParameters = bundledParameters with { JobType = JobTypes.EddPostProcessing };
        var post = new Calculation(sshService);
        post.AddGaussianParameters(new GaussianParameters(postParameters)
        {
            DependencyJobId = dependencyJobIds
        });
        post.AddMolecule(new Molecule(postParameters, timeDependent.Molecule?.Atoms));
        post.AddMetaData(new MetaData
        {
            JobName = $"{bundledParameters.MoleculeName}_edd_post"
        });
        post.AddPaths(string.Join(Path.DirectorySeparatorChar, sharedPath));
        post.EddSharedDirectory = new DirectoryExtension(sharedPath, sshService)
            .GetPath(PathType.Cluster);
        post.EddTdDirectory = timeDependent.Paths.RelativeDirectory.GetPath(PathType.Cluster);
        post.EddSpDirectory = groundState.Paths.RelativeDirectory.GetPath(PathType.Cluster);
        return post;
    }
}
