namespace ArchAutomate.Data.Entities;

/// <summary>
/// Stores the IFC-extracted electrical/fixture tally for a project.
/// One row per project (upserted on each extraction).
/// The tally JSON column holds a serialised array of TallyItem records
/// grouped by category (Lighting, Electrical, Sanitary, HVAC, Fire, Other).
/// </summary>
public class ProjectTally
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON array of TallyItemDto records.</summary>
    public string TallyJson { get; set; } = "[]";

    public int LightingCount { get; set; }
    public int ElectricalCount { get; set; }
    public int SanitaryCount { get; set; }
    public int HvacCount { get; set; }
    public int FireCount { get; set; }
    public int OtherCount { get; set; }
    public int TotalCount { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
}
