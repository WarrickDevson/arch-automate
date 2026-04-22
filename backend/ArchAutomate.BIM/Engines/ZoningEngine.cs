using ArchAutomate.BIM.Models;

namespace ArchAutomate.BIM.Engines;

/// <summary>
/// Calculates zoning compliance based on South African municipal zoning schemes.
/// Rules are indicative for common zones used across South African municipalities.
/// Architects must verify limits against the applicable municipal zoning scheme by-law.
/// </summary>
public class ZoningEngine
{
    /// <summary>
    /// Indicative limits for common South African zoning designations.
    /// Sourced from City of Cape Town, eThekwini, City of Johannesburg, and Tshwane zoning schemes.
    /// </summary>
    private static readonly Dictionary<string, ZoningRules> _knownZones = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Residential ──────────────────────────────────────────────────────
        ["Residential 1"] = new(MaxCoverage: 0.50, MaxFar: 1.0, MaxHeightM: 8.5, FrontSetbackM: 4.5, RearSetbackM: 3.0, SideSetbackM: 1.5, Description: "Low-density single residential"),
        ["Residential 2"] = new(MaxCoverage: 0.60, MaxFar: 1.5, MaxHeightM: 11.0, FrontSetbackM: 3.0, RearSetbackM: 2.0, SideSetbackM: 1.5, Description: "Medium-density single residential"),
        ["Residential 3"] = new(MaxCoverage: 0.65, MaxFar: 2.0, MaxHeightM: 14.0, FrontSetbackM: 3.0, RearSetbackM: 2.0, SideSetbackM: 1.0, Description: "High-density single residential"),
        ["General Residential 1"] = new(MaxCoverage: 0.60, MaxFar: 1.5, MaxHeightM: 11.0, FrontSetbackM: 3.0, RearSetbackM: 2.0, SideSetbackM: 1.5, Description: "Low-density group housing / flats"),
        ["General Residential 2"] = new(MaxCoverage: 0.70, MaxFar: 2.5, MaxHeightM: 18.0, FrontSetbackM: 3.0, RearSetbackM: 2.0, SideSetbackM: 1.5, Description: "Medium-density group housing / flats"),
        ["General Residential 3"] = new(MaxCoverage: 0.75, MaxFar: 3.5, MaxHeightM: 30.0, FrontSetbackM: 3.0, RearSetbackM: 2.0, SideSetbackM: 1.0, Description: "High-density flats"),
        ["General Residential 4"] = new(MaxCoverage: 0.80, MaxFar: 5.0, MaxHeightM: 50.0, FrontSetbackM: 2.0, RearSetbackM: 2.0, SideSetbackM: 1.0, Description: "Very high-density flats"),
        ["Special Residential"] = new(MaxCoverage: 0.40, MaxFar: 0.8, MaxHeightM: 8.5, FrontSetbackM: 5.0, RearSetbackM: 4.0, SideSetbackM: 2.0, Description: "Heritage / restricted residential"),

        // ── Mixed Use ────────────────────────────────────────────────────────
        ["Mixed Use 1"] = new(MaxCoverage: 0.80, MaxFar: 2.0, MaxHeightM: 15.0, FrontSetbackM: 0.0, RearSetbackM: 0.0, SideSetbackM: 0.0, Description: "Low-intensity mixed-use (ground commercial / upper residential)"),
        ["Mixed Use 2"] = new(MaxCoverage: 0.90, MaxFar: 4.0, MaxHeightM: 30.0, FrontSetbackM: 0.0, RearSetbackM: 0.0, SideSetbackM: 0.0, Description: "Medium-intensity mixed-use"),
        ["Mixed Use 3"] = new(MaxCoverage: 1.00, MaxFar: 8.0, MaxHeightM: 60.0, FrontSetbackM: 0.0, RearSetbackM: 0.0, SideSetbackM: 0.0, Description: "High-intensity mixed-use / urban core"),

        // ── Business ─────────────────────────────────────────────────────────
        ["Business 1"] = new(MaxCoverage: 0.80, MaxFar: 3.0, MaxHeightM: 30.0, FrontSetbackM: 0.0, RearSetbackM: 0.0, SideSetbackM: 0.0, Description: "Neighbourhood commercial"),
        ["Business 2"] = new(MaxCoverage: 0.90, MaxFar: 5.0, MaxHeightM: 45.0, FrontSetbackM: 0.0, RearSetbackM: 0.0, SideSetbackM: 0.0, Description: "General commercial / retail"),
        ["Business 3"] = new(MaxCoverage: 1.00, MaxFar: 10.0, MaxHeightM: 80.0, FrontSetbackM: 0.0, RearSetbackM: 0.0, SideSetbackM: 0.0, Description: "Regional commercial / CBD"),
        ["Business 4"] = new(MaxCoverage: 1.00, MaxFar: 15.0, MaxHeightM: 120.0, FrontSetbackM: 0.0, RearSetbackM: 0.0, SideSetbackM: 0.0, Description: "CBD / high-density commercial"),

        // ── Industrial ───────────────────────────────────────────────────────
        ["Industrial 1"] = new(MaxCoverage: 0.70, MaxFar: 2.0, MaxHeightM: 20.0, FrontSetbackM: 6.0, RearSetbackM: 3.0, SideSetbackM: 3.0, Description: "Light industrial / workshops"),
        ["Industrial 2"] = new(MaxCoverage: 0.75, MaxFar: 2.5, MaxHeightM: 30.0, FrontSetbackM: 6.0, RearSetbackM: 3.0, SideSetbackM: 3.0, Description: "General industrial"),
        ["Industrial 3"] = new(MaxCoverage: 0.80, MaxFar: 3.5, MaxHeightM: 40.0, FrontSetbackM: 8.0, RearSetbackM: 4.0, SideSetbackM: 4.0, Description: "Heavy industrial"),

        // ── Agricultural & Special ────────────────────────────────────────────
        ["Agricultural"] = new(MaxCoverage: 0.05, MaxFar: 0.1, MaxHeightM: 8.0, FrontSetbackM: 10.0, RearSetbackM: 10.0, SideSetbackM: 5.0, Description: "Agricultural / rural"),
        ["Agricultural Holdings"] = new(MaxCoverage: 0.10, MaxFar: 0.2, MaxHeightM: 8.5, FrontSetbackM: 6.0, RearSetbackM: 6.0, SideSetbackM: 3.0, Description: "Small-scale agricultural holdings"),
        ["Community Facilities"] = new(MaxCoverage: 0.50, MaxFar: 1.5, MaxHeightM: 15.0, FrontSetbackM: 5.0, RearSetbackM: 3.0, SideSetbackM: 2.0, Description: "Schools, clinics, places of worship"),
        ["Public Open Space"] = new(MaxCoverage: 0.10, MaxFar: 0.2, MaxHeightM: 6.0, FrontSetbackM: 3.0, RearSetbackM: 3.0, SideSetbackM: 3.0, Description: "Parks, conservation"),
        ["Transport"] = new(MaxCoverage: 0.60, MaxFar: 1.0, MaxHeightM: 10.0, FrontSetbackM: 0.0, RearSetbackM: 0.0, SideSetbackM: 0.0, Description: "Transport infrastructure / depots"),
    };

    /// <summary>Returns all known zoning schemes with their permitted limits.</summary>
    public static IReadOnlyDictionary<string, ZoningRules> GetKnownSchemes() => _knownZones;

    public ComplianceResult Evaluate(ZoningParameters parameters)
    {
        var result = new ComplianceResult();

        result.SiteCoveragePercent = parameters.SiteAreaM2 > 0
            ? (parameters.FootprintM2 / parameters.SiteAreaM2) * 100.0
            : 0;

        result.FloorAreaRatio = parameters.SiteAreaM2 > 0
            ? parameters.ProposedGfaM2 / parameters.SiteAreaM2
            : 0;

        if (!_knownZones.TryGetValue(parameters.ZoningScheme, out var rules))
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "ZoningScheme",
                Message = $"Zoning scheme '{parameters.ZoningScheme}' is not recognised. Verify against the applicable municipal zoning scheme by-law.",
                ClauseReference = "Municipal Zoning Scheme",
                Severity = ViolationSeverity.Warning
            });
            return result;
        }

        Check(result, "SiteCoverage", "Maximum site coverage",
            result.SiteCoveragePercent, rules.MaxCoverage * 100,
            "%", isMaximum: true, clauseRef: "Municipal Zoning Scheme – Coverage");

        Check(result, "FAR", "Floor Area Ratio (FAR)",
            result.FloorAreaRatio, rules.MaxFar,
            "FAR", isMaximum: true, clauseRef: "Municipal Zoning Scheme – FAR");

        Check(result, "BuildingHeight", "Maximum building height",
            parameters.BuildingHeightM, rules.MaxHeightM,
            "m", isMaximum: true, clauseRef: "Municipal Zoning Scheme – Height");

        Check(result, "FrontSetback", "Front building line setback",
            parameters.FrontSetbackM, rules.FrontSetbackM,
            "m", isMaximum: false, clauseRef: "Municipal Zoning Scheme – Building Lines");

        Check(result, "RearSetback", "Rear building line setback",
            parameters.RearSetbackM, rules.RearSetbackM,
            "m", isMaximum: false, clauseRef: "Municipal Zoning Scheme – Building Lines");

        Check(result, "SideSetback", "Side building line setback",
            parameters.SideSetbackM, rules.SideSetbackM,
            "m", isMaximum: false, clauseRef: "Municipal Zoning Scheme – Building Lines");

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
                Message = $"{description} {direction}: provided {provided:F2} {unit}, required {required:F2} {unit}.",
                ClauseReference = clauseRef,
                Severity = ViolationSeverity.NonCompliant
            });
        }
    }

    /// <summary>Permitted limits for a zoning scheme.</summary>
    public record ZoningRules(
        double MaxCoverage,
        double MaxFar,
        double MaxHeightM,
        double FrontSetbackM,
        double RearSetbackM,
        double SideSetbackM,
        string Description = "");
}

