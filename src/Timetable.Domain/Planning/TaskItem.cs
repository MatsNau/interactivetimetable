namespace Timetable.Domain.Planning;

/// <summary>
/// Ein Arbeitspaket unterhalb eines Meilensteins. Wird erst in der
/// Wochenansicht sichtbar und ist tagesgenau terminiert.
/// </summary>
public sealed class TaskItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public MilestoneStatus Status { get; set; } = MilestoneStatus.Open;

    public DateOnly Start { get; set; }

    public DateOnly End { get; set; }

    public List<Guid> AssigneeIds { get; set; } = [];
}
