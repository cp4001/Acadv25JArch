// Quick smoke test runner — temporary file, not part of WinForm app.
// Build: dotnet run --project Tests/SmokeTest.csproj
using System;
using System.Globalization;
using DuctSizeApp;

internal static class SmokeTest
{
    private static void Main()
    {
        var ci = CultureInfo.InvariantCulture;

        var case1 = DuctDesign.OptionA(30, 13000, 0.08, 1.0);
        Console.WriteLine($"[Case1] L=30 Q=13000 R=0.08 mmAq/m  SF=1.0");
        Console.WriteLine($"  → A×B = {case1.A} × {case1.B}  De={case1.De.ToString("F1", ci)}  V={case1.V.ToString("F2", ci)}  R={case1.R.ToString("F4", ci)}  ΔP={case1.DeltaPActual.ToString("F2", ci)}  ASP={case1.Aspect.ToString("F2", ci)}");

        var case2 = DuctDesign.OptionA(30, 13000, 0.08, 1.05);
        Console.WriteLine($"[Case2] SF=1.05");
        Console.WriteLine($"  → A×B = {case2.A} × {case2.B}  De={case2.De.ToString("F1", ci)}");

        var listD = DuctDesign.OptionD(30, 13000, 0.08, 1.0);
        Console.WriteLine($"[OptionD] {listD.Count} candidates");
        foreach (var r in listD)
            Console.WriteLine($"  {r.A} × {r.B}  De={r.De.ToString("F1", ci)}  V={r.V.ToString("F2", ci)}  R={r.R.ToString("F4", ci)}  ΔP={r.DeltaPActual.ToString("F2", ci)}  ASP={r.Aspect.ToString("F2", ci)}");
    }
}
