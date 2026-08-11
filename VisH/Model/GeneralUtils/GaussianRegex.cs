using System.Globalization;
using System.Text.RegularExpressions;

namespace VisH.Model.GeneralUtils;

public class GaussianRegex
{
    public static readonly Regex JobIdRegex =
        new(@"Job Id:\s*(\S+)", RegexOptions.Compiled);

    public static readonly Regex FunctionalRegex =
        new(@"#p\s+(\S+)", RegexOptions.Compiled);

    public static readonly Regex BasisSetRegex =
        new(@"#p\s+\S+\s+(\S+)", RegexOptions.Compiled);

    public static readonly Regex ChargeRegex =
        new(@"Charge\s*=\s*(-?\d+)", RegexOptions.Compiled);

    public static readonly Regex MultiplicityRegex =
        new(@"Multiplicity\s*=\s*(\d+)", RegexOptions.Compiled);

    public static readonly Regex WalltimeRegex =
        new(@"resources_used\.walltime\s*=\s*([0-9:]+)", RegexOptions.Compiled);

    public static readonly Regex UsedMemRegex =
        new(@"resources_used\.mem\s*=\s*(\d+)kb", RegexOptions.Compiled);

    public static readonly Regex UsedCpuRegex =
        new(@"resources_used\.cpupercent\s*=\s*(\d+)", RegexOptions.Compiled);

    public static readonly Regex RamRegex =
        new(@"%mem\s*=\s*(\d+)\s*([KMGT]?B)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex NCpuRegex =
        new(@"%NProcShared\s*=\s*(\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public static readonly Regex JobIdLogFile =
        new(@"(\d+\.hpc-batch)", RegexOptions.Compiled);
    
    public static readonly Regex JobNameRegex =
        new(@"Job_Name\s*=\s*(\S+)", RegexOptions.Compiled);

    public static double? MatchDouble(string text, Regex pattern)
    {
        var match = pattern.Match(text);

        if (!match.Success)
            return null;

        return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    public static int? MatchInt(string text, Regex pattern)
    {
        var match = pattern.Match(text);

        if (!match.Success)
            return null;

        return int.Parse(match.Groups[1].Value);
    }
    
    public static string? MatchString(string text, Regex pattern)
    {
        var match = pattern.Match(text);

        if (!match.Success)
            return null;
        return match.Groups[1].Value;
    }
}
