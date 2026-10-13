using System.IO;
using System.Text;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationProperties;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils;

namespace VisH.Model.CalculationUtils;

public class Filewriter
{
    private readonly Calculation _calculation;

    private static string NormalizeUnixLineEndings(string content) =>
        content.Replace("\r\n", "\n").Replace("\r", "\n");
    
    public Filewriter(Calculation calculation)
    {
        _calculation = calculation;
    }

    private string GetGaussianInputText()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine(_calculation.GaussianParameters.ToString().TrimEnd());
        if (_calculation.GaussianParameters.Jobtype == JobTypes.GeometryOptimization)
        {
            sb.AppendLine(_calculation.Molecule.ToString().TrimEnd());
        }
        else
        {
            var multiplicity = _calculation.Molecule.State switch
            {
                State.S0 or State.S1 or State.S2 or State.S3 => "1",
                State.T1 or State.T2 or State.T3 => "3",
                _ => throw new ArgumentOutOfRangeException(nameof(State), _calculation.Molecule.State.ToString(), null)
            };
            
            sb.AppendLine($"\n{_calculation.Molecule.MoleculeName}\n\n {_calculation.Molecule.Charge} {multiplicity}");
            
        }
        sb.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(_calculation.GaussianParameters.LinkKeywords))
        {
            sb.AppendLine("--Link1--");
            sb.AppendLine(_calculation.GaussianParameters.ToLink().TrimEnd());
            if (_calculation.GaussianParameters.Jobtype == JobTypes.GeometryOptimization)
            {
                sb.AppendLine();
                var multiplicity = _calculation.Molecule.State switch
                {
                    State.S0 or State.S1 or State.S2 or State.S3 => "1",
                    State.T1 or State.T2 or State.T3 => "3",
                    _ => throw new ArgumentOutOfRangeException(nameof(State), _calculation.Molecule.State.ToString(), null)
                };
            
                sb.AppendLine($"{_calculation.Molecule.MoleculeName}\n\n{_calculation.Molecule.Charge} {multiplicity}");
                sb.AppendLine();
            }
        }
        else if (!string.IsNullOrWhiteSpace(_calculation.GaussianParameters.ScanContext))
        {
            sb.AppendLine(_calculation.GaussianParameters.GetScanContext().TrimEnd());
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GetGstartText()
    {
        var sb = new StringBuilder();
        var cfg = Config.Load();
        
        string walltime =
            $"{(int)_calculation.GaussianParameters.MaxWalltime.TotalHours:00}" +
            $":{_calculation.GaussianParameters.MaxWalltime.Minutes:00}" +
            $":{_calculation.GaussianParameters.MaxWalltime.Seconds:00}";
        
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine($"#PBS -l select=1:ncpus={_calculation.GaussianParameters.NProc}" +
                       $":mem={_calculation.GaussianParameters.Memory + 2}GB");
         sb.AppendLine($"#PBS -l walltime={walltime}");
         sb.AppendLine("#PBS -r n");
         if (!string.IsNullOrWhiteSpace(_calculation.GaussianParameters.DependencyJobId))
         {
             sb.AppendLine($"#PBS -W depend=afterok:{_calculation.GaussianParameters.DependencyJobId}");
         }
         sb.AppendLine($"#PBS -N {_calculation.MetaData.JobName}P");
         sb.AppendLine("#PBS -A OC1M\n");

         sb.AppendLine($"GaussianInputFilename={_calculation.Paths.GaussianInputFile.GetFileName()}");
         sb.AppendLine($"WORKDIR=/home/{cfg.ClusterUsername}/{_calculation.Paths.RelativeDirectory.GetPath(PathType.Cluster)}");

         string staticText = """
                             FileBasename=$(basename $GaussianInputFilename)
                             GaussianOutputFilename="${FileBasename%.*}.$PBS_JOBID.log"

                             user=`whoami`
                              
                             #make unique scratch directory on GPFS filesystem
                             SCRATCHDIR=/scratch_gs/$USER/$PBS_JOBID
                             mkdir -p "$SCRATCHDIR"
                              
                             #load Gaussian Environment and set scratch directory
                             module load Gaussian/EXPERIMENTAL/g16_intel
                             export GAUSS_SCRDIR=$SCRATCHDIR
                              
                             #some (useful?) output
                             #LOGFILE=$PBS_O_WORKDIR/$PBS_JOBNAME"."$PBS_JOBID".lg"
                             LOGFILE=${FileBasename%.*}"."$PBS_JOBID".lg"

                             cd $WORKDIR
                              
                             echo "$PBS_JOBID ($PBS_JOBNAME) @ `hostname` at `date` in "$WORKDIR" START" > $LOGFILE
                             echo "`date +"%d.%m.%Y-%T"`" >> $LOGFILE
                              
                             echo >> $LOGFILE
                             echo "GLOBAL PARAMETERS">> $LOGFILE
                             echo "---------------------------" >> $LOGFILE
                             echo "Node       : "$HOSTNAME >> $LOGFILE
                             echo "Arch       : "$ARCH >> $LOGFILE
                             echo "---------------------------" >> $LOGFILE
                             echo "RunDir     : "$WORKDIR >> $LOGFILE
                             echo "InputFile  : "$GaussianInputFilename >> $LOGFILE
                             echo "OutputFile : "$GaussianOutputFilename >> $LOGFILE
                             echo "ScratchDir : "$GAUSS_SCRDIR >> $LOGFILE
                             echo "GaussianDir: "$GAUSS_EXEDIR >> $LOGFILE
                              
                             #execute gaussian IN the (fast) scratch directory
                             cp *.chk $SCRATCHDIR
                             cd $SCRATCHDIR
                             g16 < $WORKDIR/$GaussianInputFilename > $WORKDIR/$GaussianOutputFilename
                             formchk gauss.chk chkfile.fchk
                             #rwfdump gauss.rwf overlap 514r
                             #rwfdump gauss.rwf density 633r

                             ls -l >> $LOGFILE

                             #copy files back from scratch directory
                             rm gauss.rwf
                             cp -r "$SCRATCHDIR"/* $WORKDIR/.
                             cd $WORKDIR
                             mv chkfile.fchk ${FileBasename%.*}"."$PBS_JOBID.fchk
                             #mv overlap ${FileBasename%.*}"."$PBS_JOBID.overlap
                             #mv density ${FileBasename%.*}"."$PBS_JOBID.desity
                              
                             #print the last known statistics of the job (memory usage, cpu time, etc...)
                             echo >> $LOGFILE
                             qstat -f $PBS_JOBID >> $LOGFILE
                              
                             echo "$PBS_JOBID ($PBS_JOBNAME) @ `hostname` at `date` in "$RUNDIR" END" >> $LOGFILE
                             echo "`date +"%d.%m.%Y-%T"`" >> $LOGFILE
                             """;
         sb.AppendLine(staticText);
         string content = sb.ToString();
         content = NormalizeUnixLineEndings(content);
         return content;
    }

    public void WriteGaussianInputFile()
    {
        var content = GetGaussianInputText();
        content = NormalizeUnixLineEndings(content);
        var path = _calculation.Paths.GaussianInputFile.GetPath();
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public void WriteGstartFile()
    {
        var content = GetGstartText();
        var path = _calculation.Paths.GstartFile.GetPath();

        File.WriteAllText(path, content);
    }
}