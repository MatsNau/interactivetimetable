using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timetable.Domain.People;
using Timetable.Domain.Planning;

namespace Timetable.App.ViewModels;

/// <summary>Eine Zeile der Personenliste im Bearbeitungsdialog.</summary>
public sealed partial class PersonEditItem : ObservableObject
{
    public Guid Id { get; }

    [ObservableProperty]
    private string _shortCode = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    public PersonEditItem() => Id = Guid.NewGuid();

    public PersonEditItem(Person person)
    {
        Id = person.Id;
        _shortCode = person.ShortCode;
        _displayName = person.DisplayName;
    }
}

/// <summary>
/// Bearbeitungszustand der Personenverwaltung inkl. Validierung.
/// Schreibt erst bei <see cref="ApplyTo"/> in die Domäne zurück,
/// Abbrechen lässt den Plan daher unberührt.
/// </summary>
public sealed partial class PeopleEditorViewModel : ObservableObject
{
    private readonly ProjectPlan _plan;

    public ObservableCollection<PersonEditItem> People { get; }

    [ObservableProperty]
    private PersonEditItem? _selectedPerson;

    public PeopleEditorViewModel(ProjectPlan plan)
    {
        _plan = plan;
        People = new ObservableCollection<PersonEditItem>(plan.People.Select(p => new PersonEditItem(p)));
        SelectedPerson = People.FirstOrDefault();
    }

    [RelayCommand]
    private void Add()
    {
        var item = new PersonEditItem();
        People.Add(item);
        SelectedPerson = item;
    }

    public void RemoveSelected()
    {
        if (SelectedPerson is not { } selected)
            return;

        var index = People.IndexOf(selected);
        People.Remove(selected);
        SelectedPerson = People.Count == 0 ? null : People[Math.Min(index, People.Count - 1)];
    }

    /// <summary>Wie oft die Person im Plan eingetragen ist (Lead, Beteiligte, Task-Zuordnungen).</summary>
    public int CountReferences(Guid personId)
    {
        var count = 0;
        foreach (var project in _plan.Projects)
        {
            if (project.LeadId == personId)
                count++;
            count += project.ParticipantIds.Count(id => id == personId);

            foreach (var milestone in project.Milestones)
            {
                if (milestone.LeadId == personId)
                    count++;
                count += milestone.ParticipantIds.Count(id => id == personId);

                foreach (var task in milestone.Tasks)
                    count += task.AssigneeIds.Count(id => id == personId);
            }
        }

        return count;
    }

    /// <summary>Fehlermeldung oder null, wenn alle Eingaben gültig sind.</summary>
    public string? Validate()
    {
        if (People.Any(p => string.IsNullOrWhiteSpace(p.ShortCode)))
            return "Bitte für jede Person ein Kürzel angeben.";

        var duplicate = People
            .GroupBy(p => p.ShortCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        return duplicate is null ? null : $"Das Kürzel \"{duplicate.Key}\" ist mehrfach vergeben.";
    }

    /// <summary>
    /// Übernimmt die Liste in den Plan und bereinigt alle Einträge
    /// (Lead, Beteiligte, Task-Zuordnungen), die auf entfernte Personen zeigen.
    /// </summary>
    public void ApplyTo(ProjectPlan plan)
    {
        var keptIds = People.Select(p => p.Id).ToHashSet();
        foreach (var project in plan.Projects)
        {
            if (project.LeadId is { } projectLead && !keptIds.Contains(projectLead))
                project.LeadId = null;
            project.ParticipantIds.RemoveAll(id => !keptIds.Contains(id));

            foreach (var milestone in project.Milestones)
            {
                if (milestone.LeadId is { } milestoneLead && !keptIds.Contains(milestoneLead))
                    milestone.LeadId = null;
                milestone.ParticipantIds.RemoveAll(id => !keptIds.Contains(id));

                foreach (var task in milestone.Tasks)
                    task.AssigneeIds.RemoveAll(id => !keptIds.Contains(id));
            }
        }

        var existing = plan.People.ToDictionary(p => p.Id);
        plan.People = People
            .Select(item =>
            {
                if (existing.TryGetValue(item.Id, out var person))
                {
                    person.ShortCode = item.ShortCode.Trim();
                    person.DisplayName = item.DisplayName.Trim();
                    return person;
                }

                return new Person
                {
                    Id = item.Id,
                    ShortCode = item.ShortCode.Trim(),
                    DisplayName = item.DisplayName.Trim(),
                };
            })
            .ToList();
    }
}
