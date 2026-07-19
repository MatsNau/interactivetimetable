using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Timetable.App.Controls;
using Timetable.Domain.Planning;

namespace Timetable.App;

public sealed class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MilestoneStatus status ? TimetablePalette.StatusBrush(status) : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
