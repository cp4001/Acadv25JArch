namespace DuctSizing.Core;

public enum DuctType
{
    Return = 0,  // 환기(리턴) — R = 0.08 mmAq/m
    Supply = 1,  // 급기(서플라이) — R = 0.10 mmAq/m
}

public static class DuctResistance
{
    public const double Return = 0.08;
    public const double Supply = 0.10;

    public static double Of(DuctType type) => type switch
    {
        DuctType.Return => Return,
        DuctType.Supply => Supply,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
