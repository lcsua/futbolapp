namespace PublicWeb.Helpers;

public static class TeamDisplayHelper
{
    public static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    public static bool IsFinished(string? status)
    {
        var s = (status ?? string.Empty).Trim().ToUpperInvariant();
        return s is "COMPLETED" or "PLAYED" or "FINISHED" or "SUSPENDED";
    }
}
