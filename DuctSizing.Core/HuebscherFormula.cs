namespace DuctSizing.Core;

public static class HuebscherFormula
{
    // De_eq(b, a) = 1.3 · (b·a)^0.625 / (b + a)^0.25
    public static double Calculate(double b, double a)
        => 1.3 * Math.Pow(b * a, 0.625) / Math.Pow(b + a, 0.25);

    public static double CalculateRounded(double b, double a)
        => Math.Round(Calculate(b, a), MidpointRounding.AwayFromZero);
}
