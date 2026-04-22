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
    public short? MunicipalityId { get; set; }          // FK to public.municipalities
    public string Erf { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    // Building dimensions (IFC-extracted, user-overridable)
    public double ProposedGfaM2 { get; set; } = 0;
    public double FootprintM2 { get; set; } = 0;
    public int NumberOfStoreys { get; set; } = 1;
    public double BuildingHeightM { get; set; } = 0;

    // Setbacks (always manual — not extractable from IFC)
    public double FrontSetbackM { get; set; } = 0;
    public double RearSetbackM { get; set; } = 0;
    public double SideSetbackM { get; set; } = 0;

    // Parking
    public int ParkingBays { get; set; } = 0;
    public double GlaForParkingM2 { get; set; } = 0;

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
