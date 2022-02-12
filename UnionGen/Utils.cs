namespace UnionGen;

public static class Utils
{
    public static string CommarLink(this IEnumerable<string> str)
        => string.Join(", ", str);

    public static string CollonLink(this IEnumerable<string> str)
        => string.Join("; ", str);

    public static IEnumerable<string> AddCommars(this IEnumerable<string> lines)
    {
        var count = lines.Count();

        return lines.Select((l, i) => (i + 1) == count ? l : l + ",");
    }
}

