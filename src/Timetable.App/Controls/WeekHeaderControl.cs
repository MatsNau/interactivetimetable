using System.Windows;
using System.Windows.Media;
using Timetable.Application.Timeline;

namespace Timetable.App.Controls;

/// <summary>
/// Kopf der Wochenansicht: Tagesspalten (z. B. "Mo 20.07.") sowie je eine
/// Zeile für Ferienbänder und externe Events — gleiche Gesamthöhe wie der
/// Kopf der Jahresansicht, damit das Fensterlayout unverändert bleibt.
/// </summary>
public sealed class WeekHeaderControl : FrameworkElement
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(WeekModel), typeof(WeekHeaderControl),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public WeekModel? Model
    {
        get => (WeekModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(Model is null ? 0 : Model.Days.Count * TimelineMetrics.DayWidth, TimelineMetrics.HeaderHeight);

    protected override void OnRender(DrawingContext dc)
    {
        if (Model is null)
            return;

        var dw = TimelineMetrics.DayWidth;
        var width = Model.Days.Count * dw;
        var ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        dc.DrawRectangle(TimetablePalette.HeaderFill, null, new Rect(0, 0, width, TimelineMetrics.HeaderHeight));

        // Zeile 1: Tage
        for (var column = 0; column < Model.Days.Count; column++)
        {
            var rect = new Rect(column * dw, 0, dw, TimelineMetrics.DayRowHeight);
            dc.DrawLine(TimetablePalette.GridPen, rect.TopLeft, rect.BottomLeft);
            DrawCentered(dc, TimetablePalette.Text(
                Model.Days[column].Label, 11, TimetablePalette.TextBrush, ppd, bold: true), rect);
        }
        dc.DrawLine(TimetablePalette.HeaderBorderPen,
            new Point(0, TimelineMetrics.DayRowHeight), new Point(width, TimelineMetrics.DayRowHeight));

        // Zeile 2: Ferien, Zeile 3: Events
        DrawBands(dc, Model.Holidays, TimelineMetrics.DayRowHeight, highlightable: false, ppd);
        DrawBands(dc, Model.Events, TimelineMetrics.DayRowHeight + TimelineMetrics.SubRowHeight, highlightable: true, ppd);

        if (Model.TodayColumn is int today)
        {
            var x = today * dw + dw / 2;
            dc.DrawLine(TimetablePalette.TodayPen,
                new Point(x, TimelineMetrics.DayRowHeight), new Point(x, TimelineMetrics.HeaderHeight));
        }
    }

    private static void DrawBands(
        DrawingContext dc, IReadOnlyList<DayBandSegment> bands, double top, bool highlightable, double ppd)
    {
        foreach (var band in bands)
        {
            var rect = new Rect(
                band.StartColumn * TimelineMetrics.DayWidth, top,
                band.ColumnCount * TimelineMetrics.DayWidth, TimelineMetrics.SubRowHeight);

            var fill = highlightable
                ? band.IsHighlighted ? TimetablePalette.HighlightedEventFill : TimetablePalette.EventFill
                : TimetablePalette.HolidayFill;
            dc.DrawRectangle(fill, null, rect);

            var text = TimetablePalette.Text(band.Name, 9.5, TimetablePalette.TextBrush, ppd);
            dc.PushClip(new RectangleGeometry(rect));
            dc.DrawText(text, new Point(rect.X + 2, rect.Y + (rect.Height - text.Height) / 2));
            dc.Pop();
        }
    }

    private static void DrawCentered(DrawingContext dc, FormattedText text, Rect rect)
    {
        dc.PushClip(new RectangleGeometry(rect));
        dc.DrawText(text, new Point(rect.X + (rect.Width - text.Width) / 2, rect.Y + (rect.Height - text.Height) / 2));
        dc.Pop();
    }
}
