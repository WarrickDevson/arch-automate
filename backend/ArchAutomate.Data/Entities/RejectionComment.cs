namespace ArchAutomate.Data.Entities;

public class RejectionComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string SourceDocument { get; set; } = string.Empty;
    public string ClauseReference { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public string ParsedAction { get; set; } = string.Empty;
    public RejectionCategory Category { get; set; }
    public RejectionStatus Status { get; set; } = RejectionStatus.Open;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

public enum RejectionCategory
{
    Zoning,
    BuildingLines,
    Parking,
    Accessibility,
    StructuralDocumentation,
    FireCompliance,
    Drainage,
    Other
}

public enum RejectionStatus
{
    Open,
    InProgress,
    Resolved,
    Disputed
}
