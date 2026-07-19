using CommunityToolkit.Mvvm.ComponentModel;
using Timetable.Domain.Planning;

namespace Timetable.App.ViewModels;

/// <summary>
/// Bearbeitungszustand des Projekt-Dialogs inkl. Validierung.
/// Schreibt erst bei <see cref="ApplyTo"/> in die Domäne zurück,
/// Abbrechen lässt den Plan daher unberührt.
/// </summary>
public sealed partial class ProjectEditorViewModel : ObservableObject
{
    public IReadOnlyList<LeadChoice> LeadChoices { get; }

    public IReadOnlyList<PersonSelection> Participants { get; }

    public string WindowTitle { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private LeadChoice _selectedLead;

    public ProjectEditorViewModel(ProjectPlan plan, Project? existing)
    {
        WindowTitle = existing is null
            ? "Neues Projekt"
            : $"Projekt bearbeiten — {existing.Name}";

        LeadChoices = LeadChoice.ListFor(plan);

        var participantIds = existing?.ParticipantIds ?? [];
        Participants = plan.People
            .Select(p => new PersonSelection(p, participantIds.Contains(p.Id)))
            .ToList();

        _selectedLead = LeadChoices[0];

        if (existing is null)
            return;

        _name = existing.Name;
        _selectedLead = LeadChoices.FirstOrDefault(c => c.Id == existing.LeadId) ?? LeadChoices[0];
    }

    /// <summary>Fehlermeldung oder null, wenn alle Eingaben gültig sind.</summary>
    public string? Validate() =>
        string.IsNullOrWhiteSpace(Name) ? "Bitte einen Projektnamen angeben." : null;

    public void ApplyTo(Project project)
    {
        project.Name = Name.Trim();
        project.LeadId = SelectedLead.Id;
        project.ParticipantIds = Participants.Where(p => p.IsSelected).Select(p => p.Person.Id).ToList();
    }
}
