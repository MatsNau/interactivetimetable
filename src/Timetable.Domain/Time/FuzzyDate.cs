namespace Timetable.Domain.Time;

public enum FuzzyDatePrecision
{
    /// <summary>Tag steht fest, z. B. 22.06.2027.</summary>
    Day,

    /// <summary>Nur die Kalenderwoche steht fest.</summary>
    Week,

    /// <summary>Nur der Monat steht fest, z. B. "xx.03.2027".</summary>
    Month,
}

/// <summary>
/// Ein Termin, der tagesgenau, wochengenau oder nur monatsgenau feststehen kann.
/// Intern wird ein Ankerdatum gehalten (der Tag selbst, der Montag der Woche
/// bzw. der Monatserste), aus dem sich alle Darstellungen ableiten.
/// </summary>
public readonly record struct FuzzyDate : IComparable<FuzzyDate>
{
    public FuzzyDatePrecision Precision { get; }

    /// <summary>Ankerdatum; dient zugleich als Sortierschlüssel.</summary>
    public DateOnly Anchor { get; }

    private FuzzyDate(FuzzyDatePrecision precision, DateOnly anchor)
    {
        Precision = precision;
        Anchor = anchor;
    }

    public static FuzzyDate FromDay(DateOnly day) => new(FuzzyDatePrecision.Day, day);

    public static FuzzyDate FromWeek(IsoWeek week) => new(FuzzyDatePrecision.Week, week.Monday);

    public static FuzzyDate FromWeek(int isoYear, int week) => FromWeek(new IsoWeek(isoYear, week));

    public static FuzzyDate FromMonth(int year, int month) =>
        new(FuzzyDatePrecision.Month, new DateOnly(year, month, 1));

    /// <summary>Das exakte Datum, sofern tagesgenau bekannt.</summary>
    public DateOnly? ExactDay => Precision == FuzzyDatePrecision.Day ? Anchor : null;

    /// <summary>
    /// Alle Kalenderwochen, die dieser Termin auf der Zeitachse belegt:
    /// bei Tag/Woche genau eine, bei Monat alle Wochen, die den Monat berühren.
    /// </summary>
    public IReadOnlyList<IsoWeek> CoveredWeeks()
    {
        if (Precision != FuzzyDatePrecision.Month)
            return [IsoWeek.FromDate(Anchor)];

        var lastOfMonth = new DateOnly(Anchor.Year, Anchor.Month, DateTime.DaysInMonth(Anchor.Year, Anchor.Month));
        return IsoWeek.Range(IsoWeek.FromDate(Anchor), IsoWeek.FromDate(lastOfMonth)).ToList();
    }

    /// <summary>Darstellung wie im bisherigen Excel-Plan, z. B. "22.06.2027", "KW 23/2026", "xx.03.2027".</summary>
    public string ToDisplayString() => Precision switch
    {
        FuzzyDatePrecision.Day => Anchor.ToString("dd.MM.yyyy"),
        FuzzyDatePrecision.Week => IsoWeek.FromDate(Anchor).ToString(),
        FuzzyDatePrecision.Month => $"xx.{Anchor.Month:00}.{Anchor.Year}",
        _ => throw new InvalidOperationException($"Unbekannte Präzision: {Precision}"),
    };

    public int CompareTo(FuzzyDate other) => Anchor.CompareTo(other.Anchor);

    public static bool operator <(FuzzyDate left, FuzzyDate right) => left.CompareTo(right) < 0;
    public static bool operator >(FuzzyDate left, FuzzyDate right) => left.CompareTo(right) > 0;
    public static bool operator <=(FuzzyDate left, FuzzyDate right) => left.CompareTo(right) <= 0;
    public static bool operator >=(FuzzyDate left, FuzzyDate right) => left.CompareTo(right) >= 0;

    public override string ToString() => ToDisplayString();
}
