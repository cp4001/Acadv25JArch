namespace DuctSizing.Core;

public static class RectCombinationFinder
{
    // Mode B: De_eq ∈ [deMin, deMax] & a/b ≤ aspectMax 인 모든 (b, a) 조합
    public static List<RectCombination> FindAll(double deMin, double deMax, double aspectMax, IReadOnlyList<int> sizes)
    {
        var results = new List<RectCombination>();
        foreach (var b in sizes)
        {
            foreach (var a in sizes)
            {
                if (a < b) continue;
                if ((double)a / b > aspectMax) break;
                var deEq = HuebscherFormula.Calculate(b, a);
                if (deEq < deMin) continue;
                if (deEq > deMax) break;
                results.Add(new RectCombination
                {
                    B = b,
                    A = a,
                    DeEq = HuebscherFormula.CalculateRounded(b, a),
                    Aspect = (double)a / b,
                    Area = (double)b * a / 1_000_000.0,
                });
            }
        }
        return results;
    }
}
