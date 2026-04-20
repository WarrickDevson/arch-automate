namespace ArchAutomate.BIM.Models;

public class ComplianceResult
{
    public bool IsCompliant => Violations.Count == 0;
    public List<ComplianceViolation> Violations { get; set; } = [];
    public List<ComplianceCheck> Checks { get; set; } = [];

    // Calculated metrics
    public double SiteCoveragePercent { get; set; }
    public double FloorAreaRatio { get; set; }
    public double ParkingRatio { get; set; }
}

public class ComplianceCheck
{
    public string Rule { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ProvidedValue { get; set; }
    public double RequiredValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool Passed { get; set; }
}

public class ComplianceViolation
{
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ClauseReference { get; set; } = string.Empty;
    public ViolationSeverity Severity { get; set; }
}

public enum ViolationSeverity
{
    Advisory,
    Warning,
    NonCompliant
}
