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

//          string cubegenScript = """
//
//                                 # Generate orbital cube files only if formchk succeeded
//                                 if [ "$FORMCHK_STATUS\" -eq 0 ] && [ -s chkfile.fchk ]; then
//                                     echo \"Generating HOMO/LUMO cube files...\" >> \"$WORKDIR/$LOGFILE\"
//                                     
//                                     cubegen 0 MO=HOMO chkfile.fchk \"$SCRATCHDIR/homo.cube\" -2 h
//                                         HOMO_STATUS=$?
//                                         
//                                     cubegen 0 MO=LUMO chkfile.fchk \"$SCRATCHDIR/lumo.cube\" -2 h
//                                     LUMO_STATUS=$?
//                                                 
//                                     echo \"HOMO cubegen exit code: $HOMO_STATUS\" >> \"$WORKDIR/$LOGFILE\"
//                                     echo \"LUMO cubegen exit code: $LUMO_STATUS\" >> \"$WORKDIR/$LOGFILE\"
//                                 else
//                                     echo \"WARNING: formchk failed; skipping cube generation.\" >> \"$WORKDIR/$LOGFILE\"
//                                 fi
//                                 """;
//
//          string staticTextTop = """
//                              FileBasename=$(basename $GaussianInputFilename)
//                              GaussianOutputFilename="${FileBasename%.*}.$PBS_JOBID.log"
//
//                              user=`whoami`
//                               
//                              #make unique scratch directory on GPFS filesystem
//                              SCRATCHDIR=/scratch_gs/$USER/$PBS_JOBID
//                              mkdir -p "$SCRATCHDIR"
//                               
//                              #load Gaussian Environment and set scratch directory
//                              module load Gaussian/EXPERIMENTAL/g16_intel
//                              export GAUSS_SCRDIR=$SCRATCHDIR
//                               
//                              #some (useful?) output
//                              #LOGFILE=$PBS_O_WORKDIR/$PBS_JOBNAME"."$PBS_JOBID".lg"
//                              LOGFILE=${FileBasename%.*}"."$PBS_JOBID".lg"
//
//                              cd $WORKDIR
//                               
//                              echo "$PBS_JOBID ($PBS_JOBNAME) @ `hostname` at `date` in "$WORKDIR" START" > $LOGFILE
//                              echo "`date +"%d.%m.%Y-%T"`" >> $LOGFILE
//                               
//                              echo >> $LOGFILE
//                              echo "GLOBAL PARAMETERS">> $LOGFILE
//                              echo "---------------------------" >> $LOGFILE
//                              echo "Node       : "$HOSTNAME >> $LOGFILE
//                              echo "Arch       : "$ARCH >> $LOGFILE
//                              echo "---------------------------" >> $LOGFILE
//                              echo "RunDir     : "$WORKDIR >> $LOGFILE
//                              echo "InputFile  : "$GaussianInputFilename >> $LOGFILE
//                              echo "OutputFile : "$GaussianOutputFilename >> $LOGFILE
//                              echo "ScratchDir : "$GAUSS_SCRDIR >> $LOGFILE
//                              echo "GaussianDir: "$GAUSS_EXEDIR >> $LOGFILE
//                               
//                              #execute gaussian IN the (fast) scratch directory
//                              cp *.chk $SCRATCHDIR
//                              cd $SCRATCHDIR
//                              g16 < $WORKDIR/$GaussianInputFilename > $WORKDIR/$GaussianOutputFilename
//                              formchk gauss.chk chkfile.fchk
//                              #rwfdump gauss.rwf overlap 514r
//                              #rwfdump gauss.rwf density 633r
//
//                              ls -l >> $LOGFILE
//
//                              #copy files back from scratch directory
//                              rm gauss.rwf
//                              
//                              """;
//          
//          string staticTextBottom = """
//                                    cp -r "$SCRATCHDIR"/* $WORKDIR/.
//                                    cd $WORKDIR
//                                    mv chkfile.fchk ${FileBasename%.*}"."$PBS_JOBID.fchk
//                                    #mv overlap ${FileBasename%.*}"."$PBS_JOBID.overlap
//                                    #mv density ${FileBasename%.*}"."$PBS_JOBID.desity
//                                     
//                                    #print the last known statistics of the job (memory usage, cpu time, etc...)
//                                    echo >> $LOGFILE
//                                    qstat -f $PBS_JOBID >> $LOGFILE
//                                     
//                                    echo "$PBS_JOBID ($PBS_JOBNAME) @ `hostname` at `date` in "$RUNDIR" END" >> $LOGFILE
//                                    echo "`date +"%d.%m.%Y-%T"`" >> $LOGFILE
//                                    """;
         
        string staticTextTop = """
    FileBasename=$(basename "$GaussianInputFilename")
    GaussianOutputFilename="${FileBasename%.*}.$PBS_JOBID.log"

    # Create unique scratch directory
    SCRATCHDIR=/scratch_gs/$USER/$PBS_JOBID
    mkdir -p "$SCRATCHDIR" || exit 1

    # Load Gaussian environment
    module load Gaussian/g16.C02_pgi
    export GAUSS_SCRDIR="$SCRATCHDIR"

    # Log file
    LOGFILE="${FileBasename%.*}.$PBS_JOBID.lg"

    cd "$WORKDIR" || exit 1

    echo "$PBS_JOBID ($PBS_JOBNAME) @ $(hostname) at $(date) in $WORKDIR START" > "$LOGFILE"
    echo "$(date +"%d.%m.%Y-%T")" >> "$LOGFILE"

    echo >> "$LOGFILE"
    echo "GLOBAL PARAMETERS" >> "$LOGFILE"
    echo "---------------------------" >> "$LOGFILE"
    echo "Node       : $HOSTNAME" >> "$LOGFILE"
    echo "Arch       : $ARCH" >> "$LOGFILE"
    echo "RunDir     : $WORKDIR" >> "$LOGFILE"
    echo "InputFile  : $GaussianInputFilename" >> "$LOGFILE"
    echo "OutputFile : $GaussianOutputFilename" >> "$LOGFILE"
    echo "ScratchDir : $GAUSS_SCRDIR" >> "$LOGFILE"
    echo "GaussianDir: $GAUSS_EXEDIR" >> "$LOGFILE"
    echo "cubegen    : $(command -v cubegen)" >> "$LOGFILE"

    # Copy checkpoint files to scratch, if present
    cp ./*.chk "$SCRATCHDIR/" 2>/dev/null || true

    cd "$SCRATCHDIR" || exit 1
    
    echo "Working directory: $(pwd)" >> "$WORKDIR/$LOGFILE"
    echo "Input file: $WORKDIR/$GaussianInputFilename" >> "$WORKDIR/$LOGFILE"
    ls -lh "$WORKDIR/$GaussianInputFilename" >> "$WORKDIR/$LOGFILE" 2>&1
    echo "g16 path: $(command -v g16)" >> "$WORKDIR/$LOGFILE"

    # Execute Gaussian
    g16 < "$WORKDIR/$GaussianInputFilename" > "$WORKDIR/$GaussianOutputFilename"
    GAUSSIAN_STATUS=$?

    echo "Gaussian exit code: $GAUSSIAN_STATUS" >> "$WORKDIR/$LOGFILE"

    # Generate formatted checkpoint
    rm -f chkfile.fchk
    if [ "$GAUSSIAN_STATUS" -eq 0 ] &&
       grep -q "Normal termination of Gaussian" "$WORKDIR/$GaussianOutputFilename" &&
       [ -s gauss.chk ]; then
        formchk gauss.chk chkfile.fchk >> "$WORKDIR/$LOGFILE" 2>&1
        FORMCHK_STATUS=$?
    else
        FORMCHK_STATUS=1
    fi

    echo "formchk exit code: $FORMCHK_STATUS" >> "$WORKDIR/$LOGFILE"

    """;
        
        string cubegenScript = """
                               # Generate orbital cube files for geometry optimizations
                               if [ "$GAUSSIAN_STATUS" -eq 0 ] &&
                                  [ "$FORMCHK_STATUS" -eq 0 ] &&
                                  [ -s chkfile.fchk ]; then

                                   echo "Starting HOMO cube generation..." >> "$WORKDIR/$LOGFILE"
                                   cubegen 0 MO=HOMO chkfile.fchk homo.cube \
                                       -2 h >> "$WORKDIR/$LOGFILE" 2>&1
                                   HOMO_STATUS=$?
                                   echo "HOMO cubegen exit code: $HOMO_STATUS" >> "$WORKDIR/$LOGFILE"

                                   echo "Starting LUMO cube generation..." >> "$WORKDIR/$LOGFILE"
                                   cubegen 0 MO=LUMO chkfile.fchk lumo.cube \
                                       -2 h >> "$WORKDIR/$LOGFILE" 2>&1
                                   LUMO_STATUS=$?
                                   echo "LUMO cubegen exit code: $LUMO_STATUS" >> "$WORKDIR/$LOGFILE"

                                   if [ -s homo.cube ]; then
                                       echo "HOMO cube created successfully." >> "$WORKDIR/$LOGFILE"
                                   else
                                       echo "WARNING: homo.cube missing or empty." >> "$WORKDIR/$LOGFILE"
                                   fi

                                   if [ -s lumo.cube ]; then
                                       echo "LUMO cube created successfully." >> "$WORKDIR/$LOGFILE"
                                   else
                                       echo "WARNING: lumo.cube missing or empty." >> "$WORKDIR/$LOGFILE"
                                   fi
                               else
                                   echo "WARNING: Gaussian or formchk failed; skipping cubes." >> "$WORKDIR/$LOGFILE"
                               fi

                               """;
        
        string staticTextBottom = """
                                  # List scratch files before copying
                                  echo "Files in scratch directory:" >> "$WORKDIR/$LOGFILE"
                                  ls -lh >> "$WORKDIR/$LOGFILE" 2>&1

                                  # Copy results back from scratch
                                  rm -f gauss.rwf
                                  cp -r "$SCRATCHDIR"/* "$WORKDIR"/.

                                  cd "$WORKDIR" || exit 1

                                  # Rename formatted checkpoint to the expected job-specific filename
                                  if [ -s chkfile.fchk ]; then
                                      mv chkfile.fchk "${FileBasename%.*}.$PBS_JOBID.fchk"
                                  fi

                                  # Record final status
                                  echo >> "$LOGFILE"
                                  echo "Final files:" >> "$LOGFILE"
                                  ls -lh >> "$LOGFILE" 2>&1

                                  echo >> "$LOGFILE"
                                  qstat -f "$PBS_JOBID" >> "$LOGFILE" 2>&1

                                  echo "$PBS_JOBID ($PBS_JOBNAME) @ $(hostname) at $(date) in $WORKDIR END" >> "$LOGFILE"
                                  echo "$(date +"%d.%m.%Y-%T")" >> "$LOGFILE"
                                  """;

         sb.AppendLine(staticTextTop);

         if (_calculation.GaussianParameters.Jobtype == JobTypes.GeometryOptimization)
         {
             sb.AppendLine(cubegenScript);
         }
         
         sb.AppendLine(staticTextBottom);
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