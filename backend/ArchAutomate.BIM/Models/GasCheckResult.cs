namespace ArchAutomate.BIM.Models;

public class GasCheckResult
{
    public bool OverallPass { get; set; }
    public bool HasGasInstallation { get; set; }
    public List<GasCheckItem> Items { get; set; } = [];
    public string SansReference { get; set; } = "SANS 10087-7:2013";
    public int PassCount => Items.Count(i => i.Passed);
    public int TotalMandatoryCount => Items.Count(i => i.IsMandatory);
}

public class GasCheckItem
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
