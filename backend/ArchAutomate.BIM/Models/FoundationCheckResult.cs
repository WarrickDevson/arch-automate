namespace ArchAutomate.BIM.Models;

public class FoundationCheckResult
{
    public bool OverallPass { get; set; }
    public bool EngineerRequired { get; set; }
    public int NumberOfStoreys { get; set; }
    public string FoundationType { get; set; } = string.Empty;
    public List<FoundationCheckItem> Checks { get; set; } = [];
    public string SansReference { get; set; } = "SANS 10400-H:2010";
}

public class FoundationCheckItem
{
    public string Rule { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClauseReference { get; set; } = string.Empty;
    public double? ProvidedValue { get; set; }
    public double? RequiredValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public bool IsMandatory { get; set; } = true;
    public string? Note { get; set; }
    public string CheckType { get; set; } = "Numeric"; // "Numeric" | "Boolean"
}
