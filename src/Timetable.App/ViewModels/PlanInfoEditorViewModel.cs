using CommunityToolkit.Mvvm.ComponentModel;
using Timetable.Domain.Planning;

namespace Timetable.App.ViewModels;

/// <summary>
/// Bearbeitungszustand der Plan-Kopfdaten (Titel und "Stand"-Datum).
/// Schreibt erst bei <see cref="ApplyTo"/> in die Domäne zurück.
/// </summary>
public sealed partial class PlanInfoEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private DateTime? _asOfDate;

    public PlanInfoEditorViewModel(ProjectPlan plan)
    {
        _title = plan.Title;
        _asOfDate = plan.AsOfDate.ToDateTime(TimeOnly.MinValue);
    }

    /// <summary>Fehlermeldung oder null, wenn alle Eingaben gültig sind.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return "Bitte einen Titel angeben.";

        return AsOfDate is null ? "Bitte ein Stand-Datum auswählen." : null;
    }

    public void ApplyTo(ProjectPlan plan)
    {
        plan.Title = Title.Trim();
        plan.AsOfDate = DateOnly.FromDateTime(AsOfDate!.Value);
    }
}
