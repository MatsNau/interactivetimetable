using CommunityToolkit.Mvvm.ComponentModel;
using Timetable.Domain.People;
using Timetable.Domain.Planning;
using Timetable.Domain.Time;

namespace Timetable.App.ViewModels;

public sealed record StatusChoice(MilestoneStatus Value, string Label);

public sealed record LeadChoice(Guid? Id, string Label)
{
    /// <summary>Auswahlliste "(niemand)" plus alle Personen des Plans.</summary>
    public static IReadOnlyList<LeadChoice> ListFor(ProjectPlan plan) =>
    [
        new LeadChoice(null, "(niemand)"),
        .. plan.People.Select(p => new LeadChoice(p.Id,
            string.IsNullOrEmpty(p.DisplayName) ? p.ShortCode : $"{p.ShortCode} — {p.DisplayName}")),
    ];
}

public sealed partial class PersonSelection(Person person, bool isSelected) : ObservableObject
{
    public Person Person { get; } = person;

    [ObservableProperty]
    private bool _isSelected = isSelected;
}

/// <summary>
/// Bearbeitungszustand des Meilenstein-Dialogs inkl. Validierung.
/// Schreibt erst bei <see cref="ApplyTo"/> in die Domäne zurück,
/// Abbrechen lässt den Plan daher unberührt.
/// </summary>
public sealed partial class MilestoneEditorViewModel : ObservableObject
{
    public IReadOnlyList<string> Months { get; } =
        ["Januar", "Februar", "März", "April", "Mai", "Juni",
         "Juli", "August", "September", "Oktober", "November", "Dezember"];

    public IReadOnlyList<StatusChoice> StatusChoices { get; } =
    [
        new(MilestoneStatus.Open, "Offen"),
        new(MilestoneStatus.InProgress, "In Arbeit"),
        new(MilestoneStatus.Done, "Erledigt"),
        new(MilestoneStatus.Blocked, "Blockiert"),
    ];

    public IReadOnlyList<LeadChoice> LeadChoices { get; }

    public IReadOnlyList<PersonSelection> Participants { get; }

    public string WindowTitle { get; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private StatusChoice _selectedStatus;

    [ObservableProperty]
    private LeadChoice _selectedLead;

    [ObservableProperty]
    private bool _isDayPrecision;

    [ObservableProperty]
    private bool _isWeekPrecision;

    [ObservableProperty]
    private bool _isMonthPrecision;

    [ObservableProperty]
    private DateTime? _dayDate;

    [ObservableProperty]
    private string _weekNumberText;

    [ObservableProperty]
    private string _weekYearText;

    [ObservableProperty]
    private int _selectedMonthIndex;

    [ObservableProperty]
    private string _monthYearText;

    public MilestoneEditorViewModel(ProjectPlan plan, Milestone? existing, string projectName)
    {
        WindowTitle = existing is null
            ? $"Neuer Meilenstein — {projectName}"
            : $"Meilenstein bearbeiten — {projectName}";

        LeadChoices = LeadChoice.ListFor(plan);

        var participantIds = existing?.ParticipantIds ?? [];
        Participants = plan.People
            .Select(p => new PersonSelection(p, participantIds.Contains(p.Id)))
            .ToList();

        // Standardwerte: heute, aktuelle KW, aktueller Monat
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentWeek = IsoWeek.FromDate(today);
        _dayDate = DateTime.Today;
        _weekNumberText = currentWeek.Week.ToString();
        _weekYearText = currentWeek.Year.ToString();
        _selectedMonthIndex = today.Month - 1;
        _monthYearText = today.Year.ToString();
        _selectedStatus = StatusChoices[0];
        _selectedLead = LeadChoices[0];
        _isDayPrecision = true;

        if (existing is null)
            return;

        _title = existing.Title;
        _note = existing.Note;
        _selectedStatus = StatusChoices.First(c => c.Value == existing.Status);
        _selectedLead = LeadChoices.FirstOrDefault(c => c.Id == existing.LeadId) ?? LeadChoices[0];

        var date = existing.PlannedDate;
        switch (date.Precision)
        {
            case FuzzyDatePrecision.Day:
                _dayDate = date.Anchor.ToDateTime(TimeOnly.MinValue);
                break;
            case FuzzyDatePrecision.Week:
                var week = IsoWeek.FromDate(date.Anchor);
                _isDayPrecision = false;
                _isWeekPrecision = true;
                _weekNumberText = week.Week.ToString();
                _weekYearText = week.Year.ToString();
                break;
            case FuzzyDatePrecision.Month:
                _isDayPrecision = false;
                _isMonthPrecision = true;
                _selectedMonthIndex = date.Anchor.Month - 1;
                _monthYearText = date.Anchor.Year.ToString();
                break;
        }
    }

    // Die Radiobuttons schließen sich gegenseitig aus — auch auf ViewModel-Ebene,
    // damit die Validierung nie zwei aktive Genauigkeiten sieht.
    partial void OnIsDayPrecisionChanged(bool value)
    {
        if (value) { IsWeekPrecision = false; IsMonthPrecision = false; }
    }

    partial void OnIsWeekPrecisionChanged(bool value)
    {
        if (value) { IsDayPrecision = false; IsMonthPrecision = false; }
    }

    partial void OnIsMonthPrecisionChanged(bool value)
    {
        if (value) { IsDayPrecision = false; IsWeekPrecision = false; }
    }

    /// <summary>Fehlermeldung oder null, wenn alle Eingaben gültig sind.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return "Bitte einen Titel angeben.";

        if (IsDayPrecision)
            return DayDate is null ? "Bitte ein Datum auswählen." : null;

        if (IsWeekPrecision)
        {
            if (!int.TryParse(WeekYearText, out var year) || year is < 2000 or > 2100)
                return "Bitte ein gültiges Jahr für die Kalenderwoche angeben (2000–2100).";
            if (!int.TryParse(WeekNumberText, out var week) || week < 1 || week > IsoWeek.WeeksInYear(year))
                return $"Das Jahr {year} hat {IsoWeek.WeeksInYear(year)} Kalenderwochen.";
            return null;
        }

        if (IsMonthPrecision)
        {
            return !int.TryParse(MonthYearText, out var year) || year is < 2000 or > 2100
                ? "Bitte ein gültiges Jahr für den Monat angeben (2000–2100)."
                : null;
        }

        return "Bitte eine Termin-Genauigkeit wählen.";
    }

    /// <summary>Setzt Validierung per <see cref="Validate"/> voraus.</summary>
    public FuzzyDate BuildDate()
    {
        if (IsDayPrecision)
            return FuzzyDate.FromDay(DateOnly.FromDateTime(DayDate!.Value));
        if (IsWeekPrecision)
            return FuzzyDate.FromWeek(int.Parse(WeekYearText), int.Parse(WeekNumberText));
        return FuzzyDate.FromMonth(int.Parse(MonthYearText), SelectedMonthIndex + 1);
    }

    public void ApplyTo(Milestone milestone)
    {
        milestone.Title = Title.Trim();
        milestone.Note = Note.Trim();
        milestone.Status = SelectedStatus.Value;
        milestone.LeadId = SelectedLead.Id;
        milestone.ParticipantIds = Participants.Where(p => p.IsSelected).Select(p => p.Person.Id).ToList();
        milestone.PlannedDate = BuildDate();
    }
}
