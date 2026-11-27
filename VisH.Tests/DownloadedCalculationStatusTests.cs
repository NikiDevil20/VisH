using System.Text.Json;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationProperties;
using VisH.Model.Enums;
using VisH.Model.PostRun;
using Xunit;

namespace VisH.Tests;

public class DownloadedCalculationStatusTests
{
    [Theory]
    [InlineData(true, JobState.Successful)]
    [InlineData(false, JobState.Failed)]
    public void UpdateFromDownloadedFiles_UpdatesJobStateFromNormalTermination(
        bool normalTermination,
        JobState expectedState)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var resultPath = Path.Combine(directory, "result.json");
            var lgPath = Path.Combine(directory, "job.lg");
            File.WriteAllText(
                resultPath,
                JsonSerializer.Serialize(new Results
                {
                    NormalTermination = normalTermination
                }));
            File.WriteAllText(lgPath, string.Empty);

            var calculation = new Calculation
            {
                MetaData = new MetaData { JobState = JobState.InPreparation }
            };

            calculation.UpdateFromDownloadedFiles(resultPath, lgPath);

            Assert.Equal(expectedState, calculation.MetaData!.JobState);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void UpdateFromDownloadedFiles_PreservesJobStateWhenTerminationIsUnknown()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var resultPath = Path.Combine(directory, "result.json");
            var lgPath = Path.Combine(directory, "job.lg");
            File.WriteAllText(resultPath, JsonSerializer.Serialize(new Results()));
            File.WriteAllText(lgPath, string.Empty);

            var calculation = new Calculation
            {
                MetaData = new MetaData { JobState = JobState.InPreparation }
            };

            calculation.UpdateFromDownloadedFiles(resultPath, lgPath);

            Assert.Equal(JobState.InPreparation, calculation.MetaData!.JobState);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
