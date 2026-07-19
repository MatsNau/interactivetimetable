using System.Windows;
using System.Windows.Media;
using Timetable.Application.Timeline;

namespace Timetable.App.Controls;

/// <summary>
/// Zeichnet den Inhaltsbereich der Zeitachse: KW-Gitter, Ferienbänder,
/// Heute-Linie, Projekt-Gruppenzeilen und Meilenstein-Marker.
/// Ein einzelnes OnRender statt vieler Elemente — bei der Zeilenzahl eines
/// Projektplans schnell genug und deutlich einfacher.
/// </summary>
public sealed class TimelineRowsControl : FrameworkElement
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(TimelineModel), typeof(TimelineRowsControl),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public TimelineModel? Model
    {
        get => (TimelineModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => Model is null
        ? new Size(0, 0)
        : new Size(Model.Weeks.Count * TimelineMetrics.CellWidth, Model.Rows.Count * TimelineMetrics.RowHeight);

    protected override void OnRender(DrawingContext dc)
    {
        if (Model is null)
            return;

        var cw = TimelineMetrics.CellWidth;
        var rh = TimelineMetrics.RowHeight;
        var width = Model.Weeks.Count * cw;
        var height = Model.Rows.Count * rh;
        var ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

        // Ferienbänder über die volle Höhe
        foreach (var band in Model.Holidays)
        {
            dc.DrawRectangle(TimetablePalette.HolidayFill, null,
                new Rect(band.StartColumn * cw, 0, band.ColumnCount * cw, height));
        }

        // Heute-Spalte
        if (Model.TodayColumn is int today)
            dc.DrawRectangle(TimetablePalette.TodayFill, null, new Rect(today * cw, 0, cw, height));

        // KW-Gitter, Monatsgrenzen kräftiger
        var monthStarts = Model.Months.Select(m => m.StartColumn).ToHashSet();
        for (var column = 0; column <= Model.Weeks.Count; column++)
        {
            var pen = monthStarts.Contains(column) || column == Model.Weeks.Count
                ? TimetablePalette.MonthPen
                : TimetablePalette.GridPen;
            dc.DrawLine(pen, new Point(column * cw, 0), new Point(column * cw, height));
        }

        for (var index = 0; index < Model.Rows.Count; index++)
        {
            var y = index * rh;

            if (Model.Rows[index] is ProjectHeaderRow)
                dc.DrawRectangle(TimetablePalette.ProjectRowFill, null, new Rect(0, y, width, rh));

            dc.DrawLine(TimetablePalette.RowPen, new Point(0, y + rh), new Point(width, y + rh));

            if (Model.Rows[index] is MilestoneRow { Marker: { } marker } milestone)
                DrawMarker(dc, marker, milestone, y, ppd);
        }

        if (Model.TodayColumn is int todayLine)
        {
            var x = todayLine * cw + cw / 2;
            dc.DrawLine(TimetablePalette.TodayPen, new Point(x, 0), new Point(x, height));
        }
    }

    private static void DrawMarker(DrawingContext dc, MilestoneMarker marker, MilestoneRow row, double y, double ppd)
    {
        var cw = TimelineMetrics.CellWidth;
        var rh = TimelineMetrics.RowHeight;
        var brush = TimetablePalette.StatusBrush(row.Status);

        if (marker.Kind == MarkerKind.MonthSpan)
        {
            var rect = new Rect(marker.StartColumn * cw + 1, y + 6, marker.ColumnCount * cw - 2, rh - 12);
            var translucent = brush.Clone();
            translucent.Opacity = 0.35;
            translucent.Freeze();
            dc.DrawRoundedRectangle(translucent, new Pen(brush, 1), rect, 3, 3);
            return;
        }

        var cell = new Rect(marker.StartColumn * cw + 2, y + 3, cw - 4, rh - 6);
        dc.DrawRoundedRectangle(brush, null, cell, 3, 3);

        var text = TimetablePalette.Text(marker.Label, 10, Brushes.White, ppd, bold: true);
        dc.DrawText(text, new Point(
            cell.X + (cell.Width - text.Width) / 2,
            cell.Y + (cell.Height - text.Height) / 2));
    }
}
