using System.Text.RegularExpressions;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.GeneralUtils;

public static class QstatParser
{
    public static Dictionary<String, JobState> ParseQstat(string qstatOutput)
    {
        var result = new Dictionary<String, JobState>();
        
        var matches = GaussianRegex.JobStateRegex.Matches(qstatOutput);

        foreach (Match match in matches)
        {
            var jobId = match.Groups[1].Value;
            var stateChar = match.Groups[2].Value;

            var jobState = stateChar switch
            {
                "Q" => JobState.Queue,
                "R" => JobState.Running,
                "H" => JobState.Held,
                "B" => JobState.Begun,
                _ => JobState.Unknown
            };

            result[jobId] = jobState;
        }

        return result;
    }
}