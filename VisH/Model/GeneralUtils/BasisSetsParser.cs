using System.Globalization;
using System.Windows;
using System.Windows.Data;
using VisH.Model.Enums;

namespace VisH.Model.Enums;

public static class BasisSetsParser
{
    public static string ToString(BasisSets basisSet)
    {
        return basisSet.ToString().Replace('_', '-');
    }

    public static string ToGaussianString(this BasisSets basisSet)
    {
        return basisSet.ToString().Replace('_', '-');
    }

    public static string Parse(BasisSets basisSet)
    {
        return ToString(basisSet);
    }

    public static BasisSets Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or empty", nameof(value));
        }

        var enumString = value.Replace('-', '_');
        return Enum.Parse<BasisSets>(enumString, ignoreCase: true);
    }

    public static bool TryParse(string? value, out BasisSets result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        var enumString = value.Replace('-', '_');
        return Enum.TryParse<BasisSets>(enumString, ignoreCase: true, out result);
    }

    public static BasisSets FromString(string value)
    {
        return Parse(value);
    }
}

public static class BasisSetParser
{
    public static string ToString(BasisSets basisSet) => BasisSetsParser.ToString(basisSet);
    public static string ToGaussianString(this BasisSets basisSet) => BasisSetsParser.ToGaussianString(basisSet);
    public static string Parse(BasisSets basisSet) => BasisSetsParser.Parse(basisSet);
    public static BasisSets Parse(string value) => BasisSetsParser.Parse(value);
    public static bool TryParse(string? value, out BasisSets result) => BasisSetsParser.TryParse(value, out result);
    public static BasisSets FromString(string value) => BasisSetsParser.FromString(value);
}

public static class BasisSetsHelper
{
    public static string ToString(BasisSets basisSet) => BasisSetsParser.ToString(basisSet);
    public static string ToGaussianString(this BasisSets basisSet) => BasisSetsParser.ToGaussianString(basisSet);
    public static string Parse(BasisSets basisSet) => BasisSetsParser.Parse(basisSet);
    public static BasisSets Parse(string value) => BasisSetsParser.Parse(value);
    public static bool TryParse(string? value, out BasisSets result) => BasisSetsParser.TryParse(value, out result);
    public static BasisSets FromString(string value) => BasisSetsParser.FromString(value);
}

[ValueConversion(typeof(BasisSets), typeof(string))]
public class BasisSetToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BasisSets basisSet)
        {
            return BasisSetsParser.ToString(basisSet);
        }

        if (value is string str && BasisSetsParser.TryParse(str, out var parsed))
        {
            return BasisSetsParser.ToString(parsed);
        }

        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && BasisSetsParser.TryParse(str, out var basisSet))
        {
            return basisSet;
        }

        if (value is BasisSets b)
        {
            return b;
        }

        return DependencyProperty.UnsetValue;
    }
}

[ValueConversion(typeof(BasisSets), typeof(string))]
public class BasisSetsConverter : BasisSetToStringConverter
{
}
