namespace ArchAutomate.CAD.Models;

public class CouncilTableData
{
    public string ProjectName { get; set; } = string.Empty;
    public string Erf { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string ZoningScheme { get; set; } = string.Empty;
    public string Architect { get; set; } = string.Empty;
    public string DrawnBy { get; set; } = string.Empty;
    public string CheckedBy { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public string Scale { get; set; } = "1:100";
    public string DrawingNumber { get; set; } = string.Empty;
    public string Revision { get; set; } = "P1";

    public List<SiteStatRow> SiteStatistics { get; set; } = [];
    public List<AreaScheduleRow> AreaSchedule { get; set; } = [];
}

public class SiteStatRow
{
    public string Description { get; set; } = string.Empty;
    public string Permitted { get; set; } = string.Empty;
    public string Proposed { get; set; } = string.Empty;
    public bool Compliant { get; set; } = true;
}

public class AreaScheduleRow
{
    public string SpaceName { get; set; } = string.Empty;
    public double AreaM2 { get; set; }
    public string Level { get; set; } = "Ground Floor";
}
