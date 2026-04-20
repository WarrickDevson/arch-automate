using ArchAutomate.BIM.Models;

namespace ArchAutomate.BIM.Engines;

/// <summary>
/// Calculates zoning compliance based on South African municipal zoning schemes.
/// Rules are indicative for common Residential and General Residential zones.
/// </summary>
public class ZoningEngine
{
    private static readonly Dictionary<string, ZoningRules> KnownZones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Residential 1"] = new(MaxCoverage: 0.50, MaxFar: 1.0, MaxHeightM: 8.5, FrontSetbackM: 4.5, RearSetbackM: 3.0, SideSetbackM: 1.5),
        ["Residential 2"] = new(MaxCoverage: 0.60, MaxFar: 1.5, MaxHeightM: 11.0, FrontSetbackM: 3.0, RearSetbackM: 2.0, SideSetbackM: 1.5),
        ["General Residential 1"] = new(MaxCoverage: 0.60, MaxFar: 1.5, MaxHeightM: 11.0, FrontSetbackM: 3.0, RearSetbackM: 2.0, SideSetbackM: 1.5),
        ["General Residential 2"] = new(MaxCoverage: 0.70, MaxFar: 2.5, MaxHeightM: 18.0, FrontSetbackM: 3.0, RearSetbackM: 2.0, SideSetbackM: 1.5),
        ["Special Residential"] = new(MaxCoverage: 0.40, MaxFar: 0.8, MaxHeightM: 8.5, FrontSetbackM: 5.0, RearSetbackM: 4.0, SideSetbackM: 2.0),
        ["Business 1"] = new(MaxCoverage: 0.80, MaxFar: 3.0, MaxHeightM: 30.0, FrontSetbackM: 0.0, RearSetbackM: 0.0, SideSetbackM: 0.0),
        ["Industrial 1"] = new(MaxCoverage: 0.70, MaxFar: 2.0, MaxHeightM: 20.0, FrontSetbackM: 6.0, RearSetbackM: 3.0, SideSetbackM: 3.0),
    };

    public ComplianceResult Evaluate(ZoningParameters parameters)
    {
        var result = new ComplianceResult();

        result.SiteCoveragePercent = parameters.SiteAreaM2 > 0
            ? (parameters.FootprintM2 / parameters.SiteAreaM2) * 100.0
            : 0;

        result.FloorAreaRatio = parameters.SiteAreaM2 > 0
            ? parameters.ProposedGfaM2 / parameters.SiteAreaM2
            : 0;

        if (!KnownZones.TryGetValue(parameters.ZoningScheme, out var rules))
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "ZoningScheme",
                Message = $"Zoning scheme '{parameters.ZoningScheme}' is not recognised. Verify against the applicable municipal zoning scheme.",
                ClauseReference = "Municipal Zoning Scheme",
                Severity = ViolationSeverity.Warning
            });
            return result;
        }

        Check(result, "SiteCoverage", "Maximum site coverage",
            result.SiteCoveragePercent, rules.MaxCoverage * 100,
            "%", isMaximum: true,
            clauseRef: "Municipal Zoning Scheme – Coverage");

        Check(result, "FAR", "Floor Area Ratio (FAR)",
            result.FloorAreaRatio, rules.MaxFar,
            "FAR", isMaximum: true,
            clauseRef: "Municipal Zoning Scheme – FAR");

        Check(result, "BuildingHeight", "Maximum building height",
            parameters.BuildingHeightM, rules.MaxHeightM,
            "m", isMaximum: true,
            clauseRef: "Municipal Zoning Scheme – Height");

        Check(result, "FrontSetback", "Front building line setback",
            parameters.FrontSetbackM, rules.FrontSetbackM,
            "m", isMaximum: false,
            clauseRef: "Municipal Zoning Scheme – Building Lines");

        Check(result, "RearSetback", "Rear building line setback",
            parameters.RearSetbackM, rules.RearSetbackM,
            "m", isMaximum: false,
            clauseRef: "Municipal Zoning Scheme – Building Lines");

        Check(result, "SideSetback", "Side building line setback",
            parameters.SideSetbackM, rules.SideSetbackM,
            "m", isMaximum: false,
            clauseRef: "Municipal Zoning Scheme – Building Lines");

        return result;
    }

    private static void Check(
        ComplianceResult result,
        string rule,
        string description,
        double provided,
        double required,
        string unit,
        bool isMaximum,
        string clauseRef)
    {
        bool passed = isMaximum ? provided <= required : provided >= required;

        result.Checks.Add(new ComplianceCheck
        {
            Rule = rule,
            Description = description,
            ProvidedValue = Math.Round(provided, 2),
            RequiredValue = required,
            Unit = unit,
            Passed = passed
        });

        if (!passed)
        {
            string direction = isMaximum ? "exceeds maximum" : "is less than required";
            result.Violations.Add(new ComplianceViolation
            {
                Rule = rule,
                Message = $"{description} {direction}: provided {provided:F2}{unit}, required {required:F2}{unit}.",
                ClauseReference = clauseRef,
                Severity = ViolationSeverity.NonCompliant
            });
        }
    }

    private record ZoningRules(
        double MaxCoverage,
        double MaxFar,
        double MaxHeightM,
        double FrontSetbackM,
        double RearSetbackM,
        double SideSetbackM);
}
