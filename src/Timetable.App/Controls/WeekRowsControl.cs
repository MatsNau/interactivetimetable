using System.Windows;
using System.Windows.Media;
using Timetable.Application.Timeline;

namespace Timetable.App.Controls;

/// <summary>
/// Zeichnet den Inhaltsbereich der Wochenansicht: Tagesraster mit
/// Wochenend-Schattierung, Ferienbänder, Heute-Spalte, Projekt-Gruppenzeilen,
/// Meilenstein-Marker und Aufgabenbalken mit Personen-Kürzeln.
/// </summary>
public sealed class WeekRowsControl : FrameworkElement
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(WeekModel), typeof(WeekRowsControl),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Brush WeekendFill = CreateFrozen("#F0F0F0");

    public WeekModel? Model
    {
        get => (WeekModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => Model is null
        ? new Size(0, 0)
        : new Size(Model.Days.Count * TimelineMetrics.DayWidth, Model.Rows.Count * TimelineMetrics.RowHeight);

    protected override void OnRender(DrawingContext dc)
    {
        if (Model is null)
            return;

        var dw = TimelineMetrics.DayWidth;
        var rh = TimelineMetrics.RowHeight;
        var width = Model.Days.Count * dw;
        var height = Model.Rows.Count * rh;
        var ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

        for (var column = 0; column < Model.Days.Count; column++)
        {
            if (Model.Days[column].IsWeekend)
                dc.DrawRectangle(WeekendFill, null, new Rect(column * dw, 0, dw, height));
        }

        foreach (var band in Model.Holidays)
        {
            dc.DrawRectangle(TimetablePalette.HolidayFill, null,
                new Rect(band.StartColumn * dw, 0, band.ColumnCount * dw, height));
        }

        if (Model.TodayColumn is int today)
            dc.DrawRectangle(TimetablePalette.TodayFill, null, new Rect(today * dw, 0, dw, height));

        for (var column = 0; column <= Model.Days.Count; column++)
            dc.DrawLine(TimetablePalette.GridPen, new Point(column * dw, 0), new Point(column * dw, height));

        for (var index = 0; index < Model.Rows.Count; index++)
        {
            var y = index * rh;

            switch (Model.Rows[index])
            {
                case WeekProjectRow:
                    dc.DrawRectangle(TimetablePalette.ProjectRowFill, null, new Rect(0, y, width, rh));
                    break;
                case WeekMilestoneRow { MarkerStartColumn: int start } milestone:
                    DrawMilestoneMarker(dc, milestone, start, y);
                    break;
                case WeekTaskRow task:
                    DrawTaskBar(dc, task, y, ppd);
                    break;
            }

            dc.DrawLine(TimetablePalette.RowPen, new Point(0, y + rh), new Point(width, y + rh));
        }

        if (Model.TodayColumn is int todayLine)
        {
            var x = todayLine * dw + dw / 2;
            dc.DrawLine(TimetablePalette.TodayPen, new Point(x, 0), new Point(x, height));
        }
    }

    private static void DrawMilestoneMarker(DrawingContext dc, WeekMilestoneRow row, int startColumn, double y)
    {
        var dw = TimelineMetrics.DayWidth;
        var rh = TimelineMetrics.RowHeight;
        var brush = TimetablePalette.StatusBrush(row.Status);

        var rect = new Rect(startColumn * dw + 1, y + 6, row.MarkerColumnCount * dw - 2, rh - 12);
        var translucent = brush.Clone();
        translucent.Opacity = 0.35;
        translucent.Freeze();
        dc.DrawRoundedRectangle(translucent, new Pen(brush, 1), rect, 3, 3);
    }

    private static void DrawTaskBar(DrawingContext dc, WeekTaskRow row, double y, double ppd)
    {
        var dw = TimelineMetrics.DayWidth;
        var rh = TimelineMetrics.RowHeight;
        var brush = TimetablePalette.StatusBrush(row.Status);

        // Läuft die Aufgabe über die Woche hinaus, endet der Balken bündig am Rand (keine Rundung).
        var left = row.StartColumn * dw + (row.ContinuesBefore ? 0 : 2);
        var right = (row.StartColumn + row.ColumnCount) * dw - (row.ContinuesAfter ? 0 : 2);
        var rect = new Rect(left, y + 3, right - left, rh - 6);

        var radiusLeft = row.ContinuesBefore ? 0 : 3;
        var radiusRight = row.ContinuesAfter ? 0 : 3;
        var geometry = new RectangleGeometry(rect, Math.Max(radiusLeft, radiusRight), Math.Max(radiusLeft, radiusRight));
        dc.DrawGeometry(brush, null, geometry);

        if (string.IsNullOrEmpty(row.Assignees))
            return;

        var text = TimetablePalette.Text(row.Assignees, 10, Brushes.White, ppd, bold: true);
        dc.PushClip(new RectangleGeometry(rect));
        dc.DrawText(text, new Point(
            rect.X + Math.Max((rect.Width - text.Width) / 2, 4),
            rect.Y + (rect.Height - text.Height) / 2));
        dc.Pop();
    }

    private static Brush CreateFrozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
