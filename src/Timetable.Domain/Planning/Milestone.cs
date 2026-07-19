using Timetable.Domain.Time;

namespace Timetable.Domain.Planning;

/// <summary>Ein Meilenstein/Event innerhalb eines übergeordneten Projekts.</summary>
public sealed class Milestone
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public FuzzyDate PlannedDate { get; set; }

    public MilestoneStatus Status { get; set; } = MilestoneStatus.Open;

    /// <summary>Verantwortliche Person ("Lead"-Spalte).</summary>
    public Guid? LeadId { get; set; }

    /// <summary>Weitere Beteiligte ("mit wem"-Spalte).</summary>
    public List<Guid> ParticipantIds { get; set; } = [];

    public string Note { get; set; } = string.Empty;

    public List<TaskItem> Tasks { get; set; } = [];
}
