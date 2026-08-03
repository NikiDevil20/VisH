using System.IO;
using System.Text;
using GaussianTool.Model.Configs;
using System.Text.Json;
using System.Windows;
using GaussianTool.Model.FileHandling;
using GaussianTool.Model.Hilbert;

namespace GaussianTool.Model;

public class Calculation
{
    public Molecule Molecule { get; init; }
    public CalcParameters Parameters { get; init; }
    public CalcParameters? Link { get; init; }

    public string? JobId { get; set; }

    public CalcStatus? Status { get; set; }
    private string UniqueName { get; set; }
    public PathObject GjfPath { get; set; }
    public PathObject GstartPath { get; set; }




    public Calculation(Molecule molecule, CalcParameters parameters, CalcParameters? link = null)
    {
        Molecule = molecule;
        Parameters = parameters;
        Link = link;

        UniqueName = GetName();
        var paths = GetPaths();
        GjfPath = paths["gjf"];
        GstartPath = paths["gstart"];

        if (!Directory.Exists(GjfPath.WindowsFolder))
        {
            Console.WriteLine($"Creating directory: {GjfPath.WindowsFolder}");
            Directory.CreateDirectory(GjfPath.WindowsFolder);
        }
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
        }

        return sb.ToString();
    }

    public string ToGstart()
    {
        var sb = new StringBuilder();
        sb.AppendLine("#!/bin/bash");
        sb.AppendLine($"#PBS -l select=1:ncpus={Parameters.Proc}:mem={int.Parse(Parameters.Ram) + 2}GB");
        sb.AppendLine($"#PBS -l walltime={Parameters.Time}");
        sb.AppendLine("#PBS -r n");
        sb.AppendLine($"#PBS -N {Molecule.Name}P");
        sb.AppendLine("#PBS -A OC1M\n");

        sb.AppendLine($"GaussianInputFilename={UniqueName}.gjf");
        sb.AppendLine($"WORKDIR={GstartPath.ClusterFolder}");

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

    private Dictionary<string, PathObject> GetPaths()
    {
        Config cfg = Config.Load();
        Dictionary<string, PathObject> paths = new Dictionary<string, PathObject>();


        string suffix = Path.Combine(Molecule.Name, Parameters.State, UniqueName);
        string localPath = Path.Combine(cfg.LocalRechnungenPath, suffix);
        
        
        while (Directory.Exists(localPath))
        {
            string baseName = Path.GetFileName(localPath);
            UniqueName = GetUniqueName(baseName);
            localPath = Path.GetDirectoryName(localPath);
            localPath = Path.Combine(localPath, UniqueName);

        }
        paths["gjf"] = new PathObject(localPath + $"\\{UniqueName}.gjf");
        paths["gstart"] = new PathObject(localPath + "\\gstart");
        return paths;
    }

    private string GetName()
    {
        string baseName = Molecule.Name;
        string suffix = "";

        switch (Parameters.CalcType)
        {
            case "OPT":
                suffix = "_opt";
                break;
            case "TD":
                if (Parameters.State == "S0")
                {
                    suffix = "_abs";
                    break;
                }

                if (Parameters.State.StartsWith("S"))
                {
                    suffix = "_flu";
                    break;
                }

                suffix = "_pho";
                break;

        }

        string fullName = baseName + suffix;

        return fullName;
    }

    private string GetUniqueName(string nonUniqueName)
    {
        if (nonUniqueName.EndsWith("abs") || nonUniqueName.EndsWith("flu") ||
            nonUniqueName.EndsWith("pho") || nonUniqueName.EndsWith("opt"))
        {
            // hat noch kein inkrement
            return nonUniqueName + "_1";
        }

        int index = nonUniqueName.LastIndexOf("_");
        string baseName = nonUniqueName.Substring(0, index);
        string incrementString = nonUniqueName.Substring(index + 1);
        int currentIncrement = int.Parse(incrementString);

        return baseName + $"_{currentIncrement + 1}";
    }

    public void WriteFiles()
    {
        string gjf = ToGjf();
        string gstart = ToGstart();
        
        File.WriteAllText(GjfPath.WindowsPath, gjf);
        File.WriteAllText(GstartPath.WindowsPath, gstart);
    }

    public void SaveCalculation()
    {
        if (JobId == null)
        {
            MessageBox.Show("No job ID available for saving.");
            return;
        }

        string jsonString = JsonSerializer.Serialize(this);
        string path = Path.Combine(GjfPath.WindowsFolder, $"{UniqueName}.config.json");
        File.WriteAllText(path, jsonString);
    }
}
    

