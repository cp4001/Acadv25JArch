namespace DuctSizing;

internal sealed record AuxiliaryValues(
    double Area,             // A  [m²]
    double Velocity,         // V  [m/s]
    double DynamicPressure,  // Δp [mmAq]
    double ActualResistance); // R′ [mmAq/m]

internal static class AuxiliaryCalculator
{
    public static AuxiliaryValues Calculate(double q, double deMm)
    {
        // A  = π · De² / 4 / 10^6                       [m²]
        var area = Math.PI * deMm * deMm / 4.0 / 1_000_000.0;
        // V  = 1.2732 · Q / (3600 · De²) · 10^6         [m/s]
        var velocity = 1.2732 * q / (3600.0 * deMm * deMm) * 1_000_000.0;
        // Δp = 0.061 · V²                               [mmAq]
        var dynamic = 0.061 * velocity * velocity;
        // R′ = 0.00119 · V^1.9 · (De/1000)^-1.22        [mmAq/m]
        var actual = 0.00119 * Math.Pow(velocity, 1.9) * Math.Pow(deMm / 1000.0, -1.22);
        return new AuxiliaryValues(area, velocity, dynamic, actual);
    }
}
