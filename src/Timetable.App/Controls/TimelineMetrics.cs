using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Timetable.Domain.Planning;

namespace Timetable.App.Controls;

/// <summary>Gemeinsame Maße, damit linke Tabelle und Zeitachse zeilengenau fluchten.</summary>
internal static class TimelineMetrics
{
    public const double CellWidth = 22;
    public const double RowHeight = 24;
    public const double MonthRowHeight = 22;
    public const double SubRowHeight = 18;

    /// <summary>Monate + KW-Nummern + Ferienzeile + Eventzeile.</summary>
    public const double HeaderHeight = MonthRowHeight + 3 * SubRowHeight;

    /// <summary>Breite einer Tagesspalte in der Wochenansicht.</summary>
    public const double DayWidth = 110;

    /// <summary>Tageszeile der Wochenansicht; so gewählt, dass der Kopf insgesamt <see cref="HeaderHeight"/> hoch bleibt.</summary>
    public const double DayRowHeight = HeaderHeight - 2 * SubRowHeight;
}

/// <summary>Zentrale Farb- und Textdefinitionen für die Zeitachse.</summary>
internal static class TimetablePalette
{
    public static readonly Brush ProjectRowFill = Freeze("#D9D9D9");
    public static readonly Brush HolidayFill = Freeze("#E9E9E9");
    public static readonly Brush EventFill = Freeze("#F4E8D7");
    public static readonly Brush HighlightedEventFill = Freeze("#F7E96E");
    public static readonly Brush TodayFill = Freeze("#33E53935");
    public static readonly Brush HeaderFill = Freeze("#F5F5F5");
    public static readonly Brush TextBrush = Brushes.Black;
    public static readonly Brush MutedTextBrush = Freeze("#666666");

    public static readonly Pen GridPen = FreezePen("#E4E4E4", 1);
    public static readonly Pen MonthPen = FreezePen("#ABABAB", 1);
    public static readonly Pen RowPen = FreezePen("#E0E0E0", 1);
    public static readonly Pen TodayPen = FreezePen("#E53935", 1.5);
    public static readonly Pen HeaderBorderPen = FreezePen("#ABABAB", 1);

    private static readonly Typeface Typeface = new("Segoe UI");
    private static readonly Typeface BoldTypeface =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    public static Brush StatusBrush(MilestoneStatus status) => status switch
    {
        MilestoneStatus.Done => StatusDone,
        MilestoneStatus.InProgress => StatusInProgress,
        MilestoneStatus.Blocked => StatusBlocked,
        _ => StatusOpen,
    };

    public static readonly Brush StatusDone = Freeze("#4C9A52");
    public static readonly Brush StatusInProgress = Freeze("#2E7FBE");
    public static readonly Brush StatusBlocked = Freeze("#D64545");
    public static readonly Brush StatusOpen = Freeze("#8A8A8A");

    public static FormattedText Text(string value, double size, Brush brush, double pixelsPerDip, bool bold = false) =>
        new(value, CultureInfo.GetCultureInfo("de-DE"), FlowDirection.LeftToRight,
            bold ? BoldTypeface : Typeface, size, brush, pixelsPerDip);

    private static Brush Freeze(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private static Pen FreezePen(string hex, double thickness)
    {
        var pen = new Pen(Freeze(hex), thickness);
        pen.Freeze();
        return pen;
    }
}
