namespace ArchAutomate.Data.Entities;

public class Municipality
{
    public short Id { get; set; }
    public short ProvinceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // A / B / C
    public string ZoningScheme { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
