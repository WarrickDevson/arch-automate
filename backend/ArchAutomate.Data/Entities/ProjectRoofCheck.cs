namespace ArchAutomate.Data.Entities;

/// <summary>
/// Stores the roof callout checklist for a project (SANS 10400-L / K).
/// One row per project — upserted on each evaluation.
/// </summary>
public class ProjectRoofCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON of the RoofCheckRequest submitted by the user.</summary>
    public string InputJson { get; set; } = "{}";

    /// <summary>JSON of the RoofCheckResult returned by the engine.</summary>
    public string ResultsJson { get; set; } = "[]";

    public bool OverallPass { get; set; }
    public string RoofType { get; set; } = string.Empty;

    public Project Project { get; set; } = null!;
}
