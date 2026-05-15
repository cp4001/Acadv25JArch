namespace DuctSizing.Core;

public static class EquivalentDiameter
{
    // De = ROUND( ( (3.295e-10 · Q^1.9 / R)^0.199 · 1000 ) / α , 0 )   [mm]
    // 출처: DUCT MEASURE!D18
    public static double Calculate(double q, double r, double alpha)
    {
        var inner = Math.Pow(3.295e-10 * Math.Pow(q, 1.9) / r, 0.199);
        return Math.Round(inner * 1000.0 / alpha, MidpointRounding.AwayFromZero);
    }
}
