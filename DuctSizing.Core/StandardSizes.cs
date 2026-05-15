namespace DuctSizing.Core;

public static class StandardSizes
{
    public static readonly IReadOnlyList<int> Default = Build(100, 2000, 50);

    public static int NextAtLeast(double value)
    {
        foreach (var v in Default) if (v >= value) return v;
        return Default[^1];
    }

    private static int[] Build(int min, int max, int step)
    {
        var list = new List<int>();
        for (int v = min; v <= max; v += step) list.Add(v);
        return list.ToArray();
    }
}
