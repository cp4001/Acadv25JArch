using System;
using System.Collections.Generic;
using System.Linq;

namespace DuctSizeApp;

public sealed record DuctDesignResult(
    int A, int B, double De, double V, double R, double DeltaPActual, double Aspect);

public static class DuctTables
{
    // §5.3 표준 사이즈 시리즈 (mm) — 장변·단변 공통, 69개
    public static readonly int[] StandardSizes =
    {
        50, 100, 150, 200, 250, 300, 350, 400, 450, 500,
        550, 600, 650, 700, 750, 800, 850, 900, 950, 1000,
        1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400, 1450, 1500,
        1550, 1600, 1650, 1700, 1750, 1800,
        1900, 2000, 2050, 2100,
        2200, 2300, 2400, 2500, 2600, 2700, 2800, 2900, 3000,
        3100, 3200, 3300, 3400, 3500, 3600, 3700, 3800, 3900, 4000,
        4100, 4200, 4300, 4400, 4500, 4600, 4700, 4800, 4900, 5000
    };

    public static int SnapToStandard(double x)
    {
        int best = StandardSizes[0];
        double bestErr = Math.Abs(StandardSizes[0] - x);
        for (int i = 1; i < StandardSizes.Length; i++)
        {
            double err = Math.Abs(StandardSizes[i] - x);
            if (err < bestErr)
            {
                bestErr = err;
                best = StandardSizes[i];
            }
        }
        return best;
    }

    public static int? NextSizeUp(int s)
    {
        int idx = Array.IndexOf(StandardSizes, s);
        if (idx < 0 || idx >= StandardSizes.Length - 1) return null;
        return StandardSizes[idx + 1];
    }
}

public static class DuctMath
{
    public const double Rho = 1.2;            // kg/m³
    public const double Mu = 1.81e-5;         // Pa·s
    public const double DefaultEpsM = 9e-5;   // m  (갈바나 강판 clean)
    public const double G = 9.80665;          // m/s²

    // Huebscher 등가원형 직경 (mm)
    public static double Huebscher(double a, double b)
        => 1.30 * Math.Pow(a * b, 0.625) / Math.Pow(a + b, 0.25);

    // 단위 마찰손실 R [mmAq/m]  (Q [m³/s], D [mm])
    public static double Friction(double q, double dMm, double epsM = DefaultEpsM)
    {
        double dM = dMm * 1e-3;
        double area = Math.PI * dM * dM / 4.0;
        double v = q / area;
        double re = Rho * v * dM / Mu;

        double f;
        if (re < 2300.0)
        {
            f = 64.0 / re;
        }
        else
        {
            f = 0.11 * Math.Pow(68.0 / re + epsM / dM, 0.25);
            if (f < 0.018) f = 0.85 * f + 0.0028;
        }

        double dpdLPa = f * Rho * v * v / (2.0 * dM);
        return dpdLPa / G;
    }

    // (Q, R) → D 이분법 역산 (mm)
    public static double SolveDiameter(double q, double rTarget, double epsM = DefaultEpsM)
    {
        double dLo = 50.0, dHi = 5000.0, dMid = 0.0;
        for (int i = 0; i < 60; i++)
        {
            dMid = 0.5 * (dLo + dHi);
            double rMid = Friction(q, dMid, epsM);
            if (rMid > rTarget) dLo = dMid;     // D 작을수록 R 큼
            else dHi = dMid;
            if (Math.Abs(dHi - dLo) < 0.01) break;
        }
        return dMid;
    }

    // Huebscher 역산: a 고정, De → b
    public static double SolveBHuebscher(double a, double deTarget)
    {
        double bLo = 1.0, bHi = a, bMid = 0.0;
        for (int i = 0; i < 60; i++)
        {
            bMid = 0.5 * (bLo + bHi);
            double de = Huebscher(a, bMid);
            if (de < deTarget) bLo = bMid;
            else bHi = bMid;
            if (Math.Abs(bHi - bLo) < 0.001) break;
        }
        return bMid;
    }
}

public static class DuctDesign
{
    public static DuctDesignResult OptionA(
        double lengthM, double flowCMH, double rMmAqPerM, double safetyFactor,
        double roughnessM = DuctMath.DefaultEpsM)
    {
        Validate(lengthM, flowCMH, rMmAqPerM, safetyFactor);

        double q = flowCMH / 3600.0;
        double rTarget = rMmAqPerM;

        double de0 = DuctMath.SolveDiameter(q, rTarget, roughnessM);
        double de = de0 * Math.Sqrt(safetyFactor);

        // a 초기 추정: 정사각 가정 (Huebscher: 정사각 De ≈ 1.087 a)
        int a = DuctTables.SnapToStandard(de / 1.087);
        int b = DuctTables.SnapToStandard(DuctMath.SolveBHuebscher(a, de));
        if (a < b) (a, b) = (b, a);

        int guard = 0;
        while ((double)a / b > 3.0)
        {
            if (++guard > 30)
                throw new InvalidOperationException("ASP ≤ 3 제약을 만족할 수 없습니다.");
            int? next = DuctTables.NextSizeUp(a);
            if (next is null)
                throw new InvalidOperationException("표준 시리즈 상한 초과 — 입력 재검토 필요.");
            a = next.Value;
            b = DuctTables.SnapToStandard(DuctMath.SolveBHuebscher(a, de));
            if (a < b) (a, b) = (b, a);
        }

        return BuildResult(q, lengthM, a, b, roughnessM);
    }

    public static IReadOnlyList<DuctDesignResult> OptionD(
        double lengthM, double flowCMH, double rMmAqPerM, double safetyFactor,
        double tolerance = 0.05, double aspectMax = 3.0,
        double roughnessM = DuctMath.DefaultEpsM)
    {
        Validate(lengthM, flowCMH, rMmAqPerM, safetyFactor);
        if (tolerance <= 0 || tolerance > 0.5)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "tolerance ∈ (0, 0.5]");
        if (aspectMax < 1.0 || aspectMax > 10.0)
            throw new ArgumentOutOfRangeException(nameof(aspectMax), "aspectMax ∈ [1, 10]");

        double q = flowCMH / 3600.0;
        double rTarget = rMmAqPerM;
        double de = DuctMath.SolveDiameter(q, rTarget, roughnessM) * Math.Sqrt(safetyFactor);

        var list = new List<DuctDesignResult>();
        var sizes = DuctTables.StandardSizes;
        for (int ai = 0; ai < sizes.Length; ai++)
        {
            int a = sizes[ai];
            for (int bi = 0; bi <= ai; bi++)
            {
                int b = sizes[bi];
                double deAb = DuctMath.Huebscher(a, b);
                double err = Math.Abs(deAb - de) / de;
                double asp = (double)a / b;
                if (err <= tolerance && asp <= aspectMax)
                    list.Add(BuildResult(q, lengthM, a, b, roughnessM));
            }
        }

        return list
            .OrderBy(r => r.Aspect)
            .ThenBy(r => Math.Abs(r.De - de))
            .ToList();
    }

    private static DuctDesignResult BuildResult(double q, double lengthM, int a, int b, double epsM)
    {
        double deActual = DuctMath.Huebscher(a, b);
        double areaM2 = a * b * 1e-6;
        double v = q / areaM2;
        double rActual = DuctMath.Friction(q, deActual, epsM);
        double dpActual = rActual * lengthM;
        return new DuctDesignResult(a, b, deActual, v, rActual, dpActual, (double)a / b);
    }

    private static void Validate(double L, double Q, double R, double SF)
    {
        if (L <= 0 || L > 1000)
            throw new ArgumentOutOfRangeException(nameof(L), "덕트 길이 L ∈ (0, 1000] m");
        if (Q <= 0 || Q > 500_000)
            throw new ArgumentOutOfRangeException(nameof(Q), "풍량 Q ∈ (0, 500,000] CMH");
        if (R <= 0 || R > 5)
            throw new ArgumentOutOfRangeException(nameof(R), "허용 손실율 R ∈ (0, 5] mmAq/m");
        if (SF < 0.5 || SF > 1.5)
            throw new ArgumentOutOfRangeException(nameof(SF), "여유율 SF ∈ [0.5, 1.5]");
    }
}
