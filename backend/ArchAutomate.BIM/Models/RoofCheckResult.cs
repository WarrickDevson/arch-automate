namespace ArchAutomate.BIM.Models;

public class RoofCheckResult
{
    public bool OverallPass { get; set; }
    public string RoofType { get; set; } = string.Empty;
    public List<RoofCheckItem> Items { get; set; } = [];
    public string SansReference { get; set; } = "SANS 10400-L:2011, SANS 10400-K:2011";
    public int PassCount => Items.Count(i => i.Passed);
    public int TotalMandatoryCount => Items.Count(i => i.IsMandatory);
}

public class RoofCheckItem
{
    public string Category { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public string ClauseReference { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public bool IsMandatory { get; set; } = true;
    public string? ProvidedValue { get; set; }
    public string? Note { get; set; }
}
