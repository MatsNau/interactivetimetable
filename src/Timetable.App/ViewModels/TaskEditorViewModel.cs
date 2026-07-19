using CommunityToolkit.Mvvm.ComponentModel;
using Timetable.Domain.Planning;

namespace Timetable.App.ViewModels;

/// <summary>
/// Bearbeitungszustand des Aufgaben-Dialogs inkl. Validierung.
/// Schreibt erst bei <see cref="ApplyTo"/> in die Domäne zurück,
/// Abbrechen lässt den Plan daher unberührt.
/// </summary>
public sealed partial class TaskEditorViewModel : ObservableObject
{
    public IReadOnlyList<StatusChoice> StatusChoices { get; } =
    [
        new(MilestoneStatus.Open, "Offen"),
        new(MilestoneStatus.InProgress, "In Arbeit"),
        new(MilestoneStatus.Done, "Erledigt"),
        new(MilestoneStatus.Blocked, "Blockiert"),
    ];

    public IReadOnlyList<PersonSelection> Assignees { get; }

    public string WindowTitle { get; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private StatusChoice _selectedStatus;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    public TaskEditorViewModel(ProjectPlan plan, TaskItem? existing, string milestoneTitle, DateOnly defaultStart, DateOnly defaultEnd)
    {
        WindowTitle = existing is null
            ? $"Neue Aufgabe — {milestoneTitle}"
            : $"Aufgabe bearbeiten — {milestoneTitle}";

        var assigneeIds = existing?.AssigneeIds ?? [];
        Assignees = plan.People
            .Select(p => new PersonSelection(p, assigneeIds.Contains(p.Id)))
            .ToList();

        _selectedStatus = StatusChoices[0];
        _startDate = defaultStart.ToDateTime(TimeOnly.MinValue);
        _endDate = defaultEnd.ToDateTime(TimeOnly.MinValue);

        if (existing is null)
            return;

        _title = existing.Title;
        _selectedStatus = StatusChoices.First(c => c.Value == existing.Status);
        _startDate = existing.Start.ToDateTime(TimeOnly.MinValue);
        _endDate = existing.End.ToDateTime(TimeOnly.MinValue);
    }

    /// <summary>Fehlermeldung oder null, wenn alle Eingaben gültig sind.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return "Bitte einen Titel angeben.";
        if (StartDate is null)
            return "Bitte ein Start-Datum auswählen.";
        if (EndDate is null)
            return "Bitte ein Ende-Datum auswählen.";
        if (EndDate < StartDate)
            return "Das Ende liegt vor dem Start.";
        return null;
    }

    /// <summary>Setzt Validierung per <see cref="Validate"/> voraus.</summary>
    public void ApplyTo(TaskItem task)
    {
        task.Title = Title.Trim();
        task.Status = SelectedStatus.Value;
        task.Start = DateOnly.FromDateTime(StartDate!.Value);
        task.End = DateOnly.FromDateTime(EndDate!.Value);
        task.AssigneeIds = Assignees.Where(p => p.IsSelected).Select(p => p.Person.Id).ToList();
    }
}
