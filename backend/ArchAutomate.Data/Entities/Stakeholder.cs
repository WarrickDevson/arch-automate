namespace ArchAutomate.Data.Entities;

public class Stakeholder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Organisation { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public StakeholderRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum StakeholderRole
{
    Architect,
    Client,
    StructuralEngineer,
    ElectricalEngineer,
    PlumbingEngineer,
    QuantitySurveyor,
    CouncilOfficer,
    Contractor,
    Other
}
