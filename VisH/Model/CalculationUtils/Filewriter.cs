using System.IO;
using System.Text;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationProperties;
using VisH.Model.Enums;

namespace VisH.Model.CalculationUtils;

public class Filewriter
{
    private readonly Calculation _calculation;
    
    public Filewriter(Calculation calculation)
    {
        _calculation = calculation;
    }

    private string GetGaussianInputText()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine(_calculation.GaussianParameters.ToString());
        sb.AppendLine(_calculation.Molecule.ToString());
        
        if (_calculation.GaussianParameters.LinkKeywords != null)
        {
            sb.AppendLine(_calculation.GaussianParameters.ToLink());
        }
        else if (_calculation.GaussianParameters.ScanContext != null)
        {
            sb.AppendLine(_calculation.GaussianParameters.GetScanContext());
        }

        return sb.ToString();
    }

    private string GetGstartText()
    {
        var sb = new StringBuilder();
        
        string walltime =
            $"{(int)_calculation.GaussianParameters.MaxWalltime.TotalHours:00}" +
            $":{_calculation.GaussianParameters.MaxWalltime.Minutes:00}" +
            $":{_calculation.GaussianParameters.MaxWalltime.Seconds:00}";
        
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine($"#PBS -l select=1:ncpus={_calculation.GaussianParameters.NProc}" +
                       $":mem={_calculation.GaussianParameters.Memory + 2}GB");
         sb.AppendLine($"#PBS -l walltime={walltime}");
         sb.AppendLine("#PBS -r n");
         sb.AppendLine($"#PBS -N {_calculation.MetaData.JobName}P");
         sb.AppendLine("#PBS -A OC1M\n");

         sb.AppendLine($"GaussianInputFilename={_calculation.Paths.GaussianInputFile.GetFileName()}");
         sb.AppendLine($"WORKDIR={_calculation.Paths.RelativeDirectory.GetPath(PathType.Cluster)}");

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
         content = content.Replace("\r\n", "\n");
         return content;
    }

    public void WriteGaussianInputFile()
    {
        var content = GetGaussianInputText();
        var path = _calculation.Paths.GaussianInputFile.GetPath();
        File.WriteAllText(path, content);
    }

    public void WriteGstartFile()
    {
        var content = GetGstartText();
        var path = _calculation.Paths.GstartFile.GetPath();

        File.WriteAllText(path, content);
    }
}