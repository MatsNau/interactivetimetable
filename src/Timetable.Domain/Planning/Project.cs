namespace Timetable.Domain.Planning;

/// <summary>Ein übergeordnetes Projekt, das Meilensteine gruppiert.</summary>
public sealed class Project
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public Guid? LeadId { get; set; }

    public List<Guid> ParticipantIds { get; set; } = [];

    public List<Milestone> Milestones { get; set; } = [];
}
