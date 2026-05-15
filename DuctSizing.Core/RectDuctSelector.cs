namespace DuctSizing.Core;

public static class RectDuctSelector
{
    // Mode A: 단변 b 고정 시 De_eq ∈ [deMin, deMax] 만족하는 모든 장변 a (a ≥ b)
    // De_eq는 a에 대해 단조증가 → deMax 초과 시 break.
    public static List<int> FindLongSidesInRange(double deMin, double deMax, int b, IReadOnlyList<int> sizes)
    {
        var result = new List<int>();
        foreach (var a in sizes)
        {
            if (a < b) continue;
            var deEq = HuebscherFormula.Calculate(b, a);
            if (deEq < deMin) continue;
            if (deEq > deMax) break;
            result.Add(a);
        }
        return result;
    }
}
