using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timetable.Domain.Calendar;
using Timetable.Domain.Planning;

namespace Timetable.App.ViewModels;

/// <summary>Eine Zeile der Ferien- oder Event-Liste; Datumsangaben als Text "TT.MM.JJJJ".</summary>
public sealed partial class CalendarEntryEditItem : ObservableObject
{
    private static readonly string[] DateFormats = ["dd.MM.yyyy", "d.M.yyyy"];

    public Guid Id { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _startText;

    [ObservableProperty]
    private string _endText;

    [ObservableProperty]
    private bool _isHighlighted;

    public CalendarEntryEditItem()
    {
        Id = Guid.NewGuid();
        var today = DateTime.Today.ToString("dd.MM.yyyy");
        _startText = today;
        _endText = today;
    }

    public CalendarEntryEditItem(Guid id, string name, DateOnly start, DateOnly end, bool isHighlighted)
    {
        Id = id;
        _name = name;
        _startText = start.ToString("dd.MM.yyyy");
        _endText = end.ToString("dd.MM.yyyy");
        _isHighlighted = isHighlighted;
    }

    public static bool TryParseDate(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}

/// <summary>
/// Bearbeitungszustand der Ferien- und Event-Verwaltung inkl. Validierung.
/// Schreibt erst bei <see cref="ApplyTo"/> in die Domäne zurück,
/// Abbrechen lässt den Plan daher unberührt.
/// </summary>
public sealed partial class CalendarEditorViewModel : ObservableObject
{
    public ObservableCollection<CalendarEntryEditItem> Holidays { get; }

    public ObservableCollection<CalendarEntryEditItem> Events { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveHolidayCommand))]
    private CalendarEntryEditItem? _selectedHoliday;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveEventCommand))]
    private CalendarEntryEditItem? _selectedEvent;

    public CalendarEditorViewModel(ProjectPlan plan)
    {
        Holidays = new ObservableCollection<CalendarEntryEditItem>(
            plan.Holidays.Select(h => new CalendarEntryEditItem(h.Id, h.Name, h.Start, h.End, isHighlighted: false)));
        Events = new ObservableCollection<CalendarEntryEditItem>(
            plan.ExternalEvents.Select(e => new CalendarEntryEditItem(e.Id, e.Name, e.Start, e.End, e.IsHighlighted)));
        SelectedHoliday = Holidays.FirstOrDefault();
        SelectedEvent = Events.FirstOrDefault();
    }

    [RelayCommand]
    private void AddHoliday()
    {
        var item = new CalendarEntryEditItem();
        Holidays.Add(item);
        SelectedHoliday = item;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveHoliday))]
    private void RemoveHoliday() => SelectedHoliday = RemoveFrom(Holidays, SelectedHoliday!);

    private bool CanRemoveHoliday() => SelectedHoliday is not null;

    [RelayCommand]
    private void AddEvent()
    {
        var item = new CalendarEntryEditItem();
        Events.Add(item);
        SelectedEvent = item;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveEvent))]
    private void RemoveEvent() => SelectedEvent = RemoveFrom(Events, SelectedEvent!);

    private bool CanRemoveEvent() => SelectedEvent is not null;

    /// <summary>Entfernt den Eintrag und liefert den Nachbarn als neue Auswahl.</summary>
    private static CalendarEntryEditItem? RemoveFrom(
        ObservableCollection<CalendarEntryEditItem> items, CalendarEntryEditItem selected)
    {
        var index = items.IndexOf(selected);
        items.Remove(selected);
        return items.Count == 0 ? null : items[Math.Min(index, items.Count - 1)];
    }

    /// <summary>Fehlermeldung oder null, wenn alle Eingaben gültig sind.</summary>
    public string? Validate() =>
        ValidateList(Holidays, "Ferien") ?? ValidateList(Events, "Externe Events");

    private static string? ValidateList(IEnumerable<CalendarEntryEditItem> items, string listName)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                return $"Bitte für jeden Eintrag unter \"{listName}\" einen Namen angeben.";
            if (!CalendarEntryEditItem.TryParseDate(item.StartText, out var start))
                return $"\"{item.Name}\" ({listName}): Bitte \"Von\" im Format TT.MM.JJJJ angeben.";
            if (!CalendarEntryEditItem.TryParseDate(item.EndText, out var end))
                return $"\"{item.Name}\" ({listName}): Bitte \"Bis\" im Format TT.MM.JJJJ angeben.";
            if (end < start)
                return $"\"{item.Name}\" ({listName}): \"Bis\" liegt vor \"Von\".";
        }

        return null;
    }

    /// <summary>Setzt Validierung per <see cref="Validate"/> voraus.</summary>
    public void ApplyTo(ProjectPlan plan)
    {
        plan.Holidays = Holidays
            .Select(item => new HolidayPeriod
            {
                Id = item.Id,
                Name = item.Name.Trim(),
                Start = ParseDate(item.StartText),
                End = ParseDate(item.EndText),
            })
            .ToList();

        plan.ExternalEvents = Events
            .Select(item => new ExternalEvent
            {
                Id = item.Id,
                Name = item.Name.Trim(),
                Start = ParseDate(item.StartText),
                End = ParseDate(item.EndText),
                IsHighlighted = item.IsHighlighted,
            })
            .ToList();
    }

    private static DateOnly ParseDate(string text)
    {
        CalendarEntryEditItem.TryParseDate(text, out var date);
        return date;
    }
}
