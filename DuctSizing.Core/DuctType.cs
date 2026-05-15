namespace DuctSizing.Core;

public enum DuctType
{
    Suction = 0,   // 흡입 — R = 0.08 mmAq/m
    Discharge = 1, // 토출 — R = 0.10 mmAq/m
}

public static class DuctResistance
{
    public const double Suction = 0.08;
    public const double Discharge = 0.10;

    public static double Of(DuctType type) => type switch
    {
        DuctType.Suction => Suction,
        DuctType.Discharge => Discharge,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
