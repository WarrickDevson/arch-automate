namespace ArchAutomate.Data.Entities;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid OwnerUserId { get; set; }

    // Site parameters
    public double SiteAreaM2 { get; set; }
    public string ZoningScheme { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string Erf { get; set; } = string.Empty;

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    // Path in Supabase Storage bucket "ifc-models": {tenantId}/{projectId}/model.ifc
    public string? IfcPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RejectionComment> RejectionComments { get; set; } = [];
    public ICollection<Stakeholder> Stakeholders { get; set; } = [];
}

public enum ProjectStatus
{
    Draft,
    InProgress,
    SubmittedToCouncil,
    Approved,
    Rejected,
    Revised
}
