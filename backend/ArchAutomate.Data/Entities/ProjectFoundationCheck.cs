namespace ArchAutomate.Data.Entities;

/// <summary>
/// Stores the foundation compliance check for a project (SANS 10400-H).
/// One row per project — upserted on each evaluation.
/// </summary>
public class ProjectFoundationCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON of the FoundationRequest submitted by the user.</summary>
    public string InputJson { get; set; } = "{}";

    /// <summary>JSON of the FoundationCheckResult returned by the engine.</summary>
    public string ResultsJson { get; set; } = "[]";

    public bool OverallPass { get; set; }
    public int NumberOfStoreys { get; set; } = 1;

    public Project Project { get; set; } = null!;
}
