namespace DuctSizing.Core;

public sealed class DuctSizingResult
{
    public double De { get; }
    public int D { get; }
    public AuxiliaryValues Aux { get; }
    public IReadOnlyList<RectCombination> Combinations { get; }

    public DuctSizingResult(double de, int d, AuxiliaryValues aux, IReadOnlyList<RectCombination> combinations)
    {
        De = de;
        D = d;
        Aux = aux;
        Combinations = combinations;
    }
}
