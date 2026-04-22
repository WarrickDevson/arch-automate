namespace ArchAutomate.Data.Entities;

/// <summary>
/// Stores the IFC-extracted material strings and the compiled specification for a project.
/// One row per project (upserted on each extraction or manual re-compile).
/// </summary>
public class ProjectSpec
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompiledAt { get; set; }

    /// <summary>JSON array of raw material strings collected from the IFC model.</summary>
    public string MaterialsJson { get; set; } = "[]";

    /// <summary>JSON of the CompiledSpec returned by SpecEngine.Compile().</summary>
    public string SpecJson { get; set; } = "{}";

    public int ClauseCount { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
}
