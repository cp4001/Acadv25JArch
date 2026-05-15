namespace DuctSizing.Core;

public static class DuctSizingCalculator
{
    public const double DeUpperRatio = 1.05;

    // Mode A: 단변 b 지정 → De_eq ∈ [De, De×1.05] 범위 내 모든 장변 a 후보
    public static DuctSizingResult ModeA(double q, DuctType type, double alpha, int b)
    {
        var ctx = ComputeContext(q, type, alpha);
        var combos = RectDuctSelector
            .FindLongSidesInRange(ctx.de, ctx.deMax, b, StandardSizes.Default)
            .Select(a => BuildCombo(b, a))
            .ToList();
        return new DuctSizingResult(ctx.de, ctx.d, ctx.aux, combos);
    }

    // Mode B: 모든 (b, a) 조합 — b 오름차순, De_eq ∈ [De, De×1.05], a/b ≤ aspectMax
    public static DuctSizingResult ModeB(double q, DuctType type, double alpha, double aspectMax)
    {
        var ctx = ComputeContext(q, type, alpha);
        var combos = RectCombinationFinder.FindAll(ctx.de, ctx.deMax, aspectMax, StandardSizes.Default);
        return new DuctSizingResult(ctx.de, ctx.d, ctx.aux, combos);
    }

    // Mode C: Mode B 결과 중 De_eq 가장 작은(가장 적게 과대설계된) 1개
    public static DuctSizingResult ModeC(double q, DuctType type, double alpha, double aspectMax)
    {
        var b = ModeB(q, type, alpha, aspectMax);
        var best = b.Combinations
            .OrderBy(c => c.DeEq)
            .ThenBy(c => c.Area)
            .FirstOrDefault();
        var combos = best != null
            ? new List<RectCombination> { best }
            : new List<RectCombination>();
        return new DuctSizingResult(b.De, b.D, b.Aux, combos);
    }

    private static (double de, int d, double deMax, AuxiliaryValues aux) ComputeContext(double q, DuctType type, double alpha)
    {
        var r = DuctResistance.Of(type);
        var de = EquivalentDiameter.Calculate(q, r, alpha);
        var d = StandardSizes.NextAtLeast(de);
        var aux = AuxiliaryCalculator.Calculate(q, de);
        var deMax = de * DeUpperRatio;
        return (de, d, deMax, aux);
    }

    private static RectCombination BuildCombo(int b, int a) => new()
    {
        B = b,
        A = a,
        DeEq = HuebscherFormula.CalculateRounded(b, a),
        Aspect = (double)a / b,
        Area = (double)b * a / 1_000_000.0,
    };
}
