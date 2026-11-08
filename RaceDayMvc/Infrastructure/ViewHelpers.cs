using System.Globalization;
using RaceDayMvc.Models;

namespace RaceDayMvc.Infrastructure;

public static class ViewHelpers
{
    public static string Ordinal(int n)
    {
        var lastTwo = n % 100;
        if (lastTwo is >= 11 and <= 13) return $"{n}th";

        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }

    public static string FormatTime(TimeSpan t) =>
        $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";

    public static string EventDate(DateTime d)
    {
        var text = d.ToString("ddd, d MMM yyyy", CultureInfo.InvariantCulture);
        return d.TimeOfDay == TimeSpan.Zero ? text : $"{text} · {d.ToString("HH:mm", CultureInfo.InvariantCulture)}";
    }

    public static string ShortDate(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    public static string StatusClass(string status) => status.ToLowerInvariant() switch
    {
        "confirmed" => "status-confirmed",
        "cancelled" => "status-cancelled",
        _ => "status-pending"
    };

    public static string StatusIcon(string status) => status.ToLowerInvariant() switch
    {
        "confirmed" => "bi-check-circle-fill",
        "cancelled" => "bi-x-circle-fill",
        _ => "bi-hourglass-split"
    };

    public static string TypeClass(string type) => $"type-{type.ToLowerInvariant()}";

    public static string TypeIcon(string type) => type.ToLowerInvariant() switch
    {
        "walk" => "bi-person-walking",
        "cycle" => "bi-bicycle",
        _ => "bi-lightning-charge-fill"
    };

    public static string AgeRange(int? min, int? max) => (min, max) switch
    {
        (null, null) => "All ages",
        (int lo, null) => $"{lo}+",
        (null, int hi) => $"Up to {hi}",
        (int lo, int hi) => $"{lo}–{hi}"
    };

    public static string CategoryAges(CategoryItem c) => AgeRange(c.MinAge, c.MaxAge);

    public static string PodiumClass(int position) => position switch
    {
        1 => "podium-gold",
        2 => "podium-silver",
        3 => "podium-bronze",
        _ => ""
    };

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
