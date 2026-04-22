namespace ArchAutomate.Data.Entities;

/// <summary>
/// Stores the LPG gas installation checklist for a project (SANS 10087-7).
/// One row per project — upserted on each evaluation.
/// </summary>
public class ProjectGasCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON of the GasCheckRequest submitted by the user.</summary>
    public string InputJson { get; set; } = "{}";

    /// <summary>JSON of the GasCheckResult returned by the engine.</summary>
    public string ResultsJson { get; set; } = "[]";

    public bool OverallPass { get; set; }
    public bool HasGasInstallation { get; set; }

    public Project Project { get; set; } = null!;
}
