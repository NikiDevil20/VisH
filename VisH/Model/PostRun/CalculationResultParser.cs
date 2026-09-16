using System.Globalization;
using System.IO;
using System.Text.Json;
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
