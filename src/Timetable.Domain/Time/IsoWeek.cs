using System.Globalization;

namespace Timetable.Domain.Time;

/// <summary>
/// Eine Kalenderwoche nach ISO 8601. Achtung: Das ISO-Jahr kann am Jahresanfang/-ende
/// vom Kalenderjahr abweichen (z. B. gehört der 01.01.2027 zur KW 53/2026).
/// </summary>
public readonly record struct IsoWeek : IComparable<IsoWeek>
{
    public int Year { get; }
    public int Week { get; }

    public IsoWeek(int year, int week)
    {
        if (week < 1 || week > ISOWeek.GetWeeksInYear(year))
            throw new ArgumentOutOfRangeException(nameof(week),
                $"Das ISO-Jahr {year} hat {ISOWeek.GetWeeksInYear(year)} Wochen, KW {week} ist ungültig.");

        Year = year;
        Week = week;
    }

    public static IsoWeek FromDate(DateOnly date) =>
        new(ISOWeek.GetYear(date), ISOWeek.GetWeekOfYear(date));

    public static int WeeksInYear(int year) => ISOWeek.GetWeeksInYear(year);

    public DateOnly Monday => ISOWeek.ToDateOnly(Year, Week, DayOfWeek.Monday);

    public DateOnly Sunday => Monday.AddDays(6);

    public bool Contains(DateOnly date) => FromDate(date) == this;

    public IsoWeek Next() => FromDate(Monday.AddDays(7));

    public IsoWeek Previous() => FromDate(Monday.AddDays(-7));

    /// <summary>Alle Wochen von <paramref name="from"/> bis einschließlich <paramref name="to"/>.</summary>
    public static IEnumerable<IsoWeek> Range(IsoWeek from, IsoWeek to)
    {
        if (from.CompareTo(to) > 0)
            throw new ArgumentException("Der Startwert liegt nach dem Endwert.", nameof(from));

        for (var week = from; ; week = week.Next())
        {
            yield return week;
            if (week == to)
                yield break;
        }
    }

    public int CompareTo(IsoWeek other)
    {
        var byYear = Year.CompareTo(other.Year);
        return byYear != 0 ? byYear : Week.CompareTo(other.Week);
    }

    public static bool operator <(IsoWeek left, IsoWeek right) => left.CompareTo(right) < 0;
    public static bool operator >(IsoWeek left, IsoWeek right) => left.CompareTo(right) > 0;
    public static bool operator <=(IsoWeek left, IsoWeek right) => left.CompareTo(right) <= 0;
    public static bool operator >=(IsoWeek left, IsoWeek right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"KW {Week}/{Year}";
}
