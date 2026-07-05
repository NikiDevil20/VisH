using System.IO;
using System.Text;

namespace GaussianTool.Objects;

public class Calculation
{
    public Molecule Molecule { get; init;  }
    public CalcParameters Parameters { get; init; }
    public CalcParameters? Link { get; init; }
    
    public string JobId { get; set; }
    public string LocalPath { get; set; }
    public string ClusterPath { get; set; }
    public CalcStatus Status { get; set; }
    public string UniqueName { get; set; }
    private string GjfPath { get; set; }

    public Calculation(Molecule molecule, CalcParameters parameters, CalcParameters? link = null)
    {
        Molecule = molecule;
        Parameters = parameters;
        Link = link;
    }

    public string ToGjf()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Parameters.ToString());
        sb.AppendLine(Molecule.ToString());

        if (Link != null)
        {
            sb.AppendLine("\n" + "--link1--");
            sb.AppendLine(Link.ToString());
            sb.AppendLine(Molecule.Name + "\n");
            sb.AppendLine($"{Molecule.Charge} {Molecule.Multiplicity}");
        }
        
        return sb.ToString();
    }

    public string ToGstart()
    {
        var sb = new StringBuilder();
        sb.AppendLine("#!/bin/bash");
        sb.AppendLine($"#PBS -l select=1:ncpus={Parameters.Proc}:mem={Parameters.Ram + 2}GB");
        sb.AppendLine($"#PBS -l walltime={Parameters.Time}");
        sb.AppendLine("#PBS -r m");
        sb.AppendLine($"#PBS -N {Molecule.Name}P");
        sb.AppendLine("#PBS -A OC1M\n");

        sb.AppendLine($"GaussianInputFilename={GjfPath}");
        sb.AppendLine($"WORKDIR={ClusterPath}");
        
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

        return sb.ToString();
    }
    
}

