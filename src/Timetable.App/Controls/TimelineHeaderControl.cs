using System.Windows;
using System.Windows.Media;
using Timetable.Application.Timeline;

namespace Timetable.App.Controls;

/// <summary>
/// Kopf der Zeitachse: Monatsgruppen, KW-Nummern sowie je eine Zeile
/// für Ferienbänder und externe Events.
/// </summary>
public sealed class TimelineHeaderControl : FrameworkElement
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(TimelineModel), typeof(TimelineHeaderControl),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public TimelineModel? Model
    {
        get => (TimelineModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(Model is null ? 0 : Model.Weeks.Count * TimelineMetrics.CellWidth, TimelineMetrics.HeaderHeight);

    protected override void OnRender(DrawingContext dc)
    {
        if (Model is null)
            return;

        var cw = TimelineMetrics.CellWidth;
        var width = Model.Weeks.Count * cw;
        var ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        dc.DrawRectangle(TimetablePalette.HeaderFill, null, new Rect(0, 0, width, TimelineMetrics.HeaderHeight));

        // Zeile 1: Monatsgruppen
        var monthRow = new Rect(0, 0, width, TimelineMetrics.MonthRowHeight);
        foreach (var month in Model.Months)
        {
            var rect = new Rect(month.StartColumn * cw, 0, month.ColumnCount * cw, TimelineMetrics.MonthRowHeight);
            dc.DrawLine(TimetablePalette.MonthPen, rect.TopLeft, rect.BottomLeft);
            DrawCentered(dc, TimetablePalette.Text(month.Label, 11, TimetablePalette.TextBrush, ppd, bold: true), rect);
        }
        dc.DrawLine(TimetablePalette.HeaderBorderPen, monthRow.BottomLeft, monthRow.BottomRight);

        // Zeile 2: KW-Nummern
        var weekTop = TimelineMetrics.MonthRowHeight;
        for (var column = 0; column < Model.Weeks.Count; column++)
        {
            var rect = new Rect(column * cw, weekTop, cw, TimelineMetrics.SubRowHeight);
            dc.DrawLine(TimetablePalette.GridPen, rect.TopLeft, rect.BottomLeft);
            DrawCentered(dc, TimetablePalette.Text(
                Model.Weeks[column].Week.ToString(), 9.5, TimetablePalette.MutedTextBrush, ppd), rect);
        }
        dc.DrawLine(TimetablePalette.HeaderBorderPen,
            new Point(0, weekTop + TimelineMetrics.SubRowHeight),
            new Point(width, weekTop + TimelineMetrics.SubRowHeight));

        // Zeile 3: Ferien, Zeile 4: Events
        DrawBands(dc, Model.Holidays, weekTop + TimelineMetrics.SubRowHeight, highlightable: false, ppd);
        DrawBands(dc, Model.Events, weekTop + 2 * TimelineMetrics.SubRowHeight, highlightable: true, ppd);

        if (Model.TodayColumn is int today)
        {
            var x = today * cw + cw / 2;
            dc.DrawLine(TimetablePalette.TodayPen, new Point(x, TimelineMetrics.MonthRowHeight), new Point(x, TimelineMetrics.HeaderHeight));
        }
    }

    private static void DrawBands(
        DrawingContext dc, IReadOnlyList<BandSegment> bands, double top, bool highlightable, double ppd)
    {
        foreach (var band in bands)
        {
            var rect = new Rect(
                band.StartColumn * TimelineMetrics.CellWidth, top,
                band.ColumnCount * TimelineMetrics.CellWidth, TimelineMetrics.SubRowHeight);

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
