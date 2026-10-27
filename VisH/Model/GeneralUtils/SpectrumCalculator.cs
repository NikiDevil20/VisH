namespace VisH.Model.GeneralUtils;


// taken from https://gaussian.com/uvvisplot/

public class SpectrumCalculator
{
    private double firstConstant = 1.3062974 * Math.Pow(10.0, 8.0);
    private double secondConstant = Math.Pow(10.0, 7.0) / 3099.6;
    private double thirdConstant = 1 / 3099.6;

    // private double WavelengthToWavenumber(double wavelength)
    // {
    //     return 0;
    // }

    private double GetPeakFromWavelengthAndOscillator(
        double variablewavelength,
        double wavelengthAtPeak,
        double oscillatorStrength)
    {
        if (oscillatorStrength <= 0 || variablewavelength <= 0 || wavelengthAtPeak <= 0)
        {
            return 0;
        }

        double oscFraction = oscillatorStrength / secondConstant;
        double outsideExp = firstConstant * oscFraction;

        double insideSquare = (1 / variablewavelength - 1 / wavelengthAtPeak) / thirdConstant;

        double insideExp = -Math.Pow(insideSquare, 2);

        double exp = Math.Exp(insideExp);
        return outsideExp * exp;
    }

    public Dictionary<double, double> CalculateSpectrum(
        double[] wavelengths,
        double[] wavelengthsAtPeak,
        double[] oscillatorStrengths)
    {
        var spectrum = new Dictionary<double, double>();

        foreach (var wavelength in wavelengths)
        {
            for (int i = 0; i < wavelengthsAtPeak.Length; i++)
            {
                spectrum.TryGetValue(wavelength, out var currentValue);
                spectrum[wavelength] = currentValue +
                    GetPeakFromWavelengthAndOscillator(wavelength, wavelengthsAtPeak[i], oscillatorStrengths[i]);
            }
        }

        return spectrum;
    }
}