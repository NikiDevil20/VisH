namespace GaussianTool.Objects;

public class Calculation(Molecule molecule, CalcParameters parameters)
{
    public Molecule molecule { get; } =  molecule;
    public CalcParameters parameters { get; } =  parameters;
}