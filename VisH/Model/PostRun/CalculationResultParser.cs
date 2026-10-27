using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using VisH.Model.CalculationProperties;
using VisH.Model.GeneralUtils;

namespace VisH.Model.PostRun;

public static class CalculationResultParser
{
    public static Results ApplyLogFile(Results results, string logPath)
    {
        var json = File.ReadAllText(logPath);
        return JsonSerializer.Deserialize<Results>(json) ?? results;
    }

    public static IReadOnlyList<Peak> ParseExcitedStates(string logPath)
    {
        var text = File.ReadAllText(logPath);
        var stateRegex = new Regex(
            @"^\s*Excited\s+State\s+\d+\s*:.*?\s(?<energy>[+-]?\d+(?:\.\d+)?)\s*eV\s+(?<wavelength>[+-]?\d+(?:\.\d+)?)\s*nm\s+f\s*=\s*(?<oscillator>[+-]?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?)",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        var lists = new List<List<Peak>>();
        List<Peak>? current = null;
        var previousStateNumber = 0;

        foreach (Match match in stateRegex.Matches(text))
        {
            var stateNumberText = Regex.Match(match.Value, @"Excited\s+State\s+(?<number>\d+)", RegexOptions.IgnoreCase)
                .Groups["number"].Value;
            if (!int.TryParse(stateNumberText, out var stateNumber) ||
                !double.TryParse(match.Groups["wavelength"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var wavelength) ||
                !double.TryParse(match.Groups["oscillator"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var oscillatorStrength))
            {
                continue;
            }

            if (stateNumber == 1 && current is { Count: > 0 })
            {
                lists.Add(current);
                current = null;
                previousStateNumber = 0;
            }

            current ??= [];
            if (stateNumber > previousStateNumber || previousStateNumber == 0)
            {
                current.Add(new Peak
                {
                    Wavelength = wavelength,
                    OscillatorStrength = oscillatorStrength
                });
            }

            previousStateNumber = stateNumber;
        }

        if (current is { Count: > 0 })
        {
            lists.Add(current);
        }

        return lists.LastOrDefault() ?? [];
    }

    public static MetaData ApplyLgFile(MetaData metaData, string lgPath)
    {
        var text = File.ReadAllText(lgPath);

        var walltime = GaussianRegex.MatchString(text, GaussianRegex.WalltimeRegex);
        if (walltime != null && TimeSpan.TryParseExact(walltime, @"h\:mm\:ss", CultureInfo.InvariantCulture, out var parsedWalltime))
        {
            metaData.Ressources.Walltime = parsedWalltime;
        }

        var cpu = GaussianRegex.MatchString(text, GaussianRegex.UsedCpuRegex);
        if (cpu != null && float.TryParse(cpu, NumberStyles.Float, CultureInfo.InvariantCulture, out var usedCpu))
        {
            metaData.Ressources.UsedCpu = usedCpu;
        }

        var mem = GaussianRegex.MatchString(text, GaussianRegex.UsedMemRegex);
        if (mem != null && float.TryParse(mem, NumberStyles.Float, CultureInfo.InvariantCulture, out var usedMem))
        {
            metaData.Ressources.UsedMemory = usedMem;
        }

        return metaData;
    }
}
