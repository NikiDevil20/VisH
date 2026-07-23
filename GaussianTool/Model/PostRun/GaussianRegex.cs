using System.Globalization;
using System.Text.RegularExpressions;

namespace GaussianTool.Model.PostRun;

public static class GaussianRegex
{
    public static double? MatchDouble(string text, string pattern)
    {
        var match = Regex.Match(text, pattern);

        if (!match.Success)
            return null;

        return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    public static int? MatchInt(string text, string pattern)
    {
        var match = Regex.Match(text, pattern);

        if (!match.Success)
            return null;

        return int.Parse(match.Groups[1].Value);
    }
    
    public static string? MatchString(string text, string pattern)
    {
        var match = Regex.Match(text, pattern);

        if (!match.Success)
            return null;
        return match.Groups[1].Value;
    }
}