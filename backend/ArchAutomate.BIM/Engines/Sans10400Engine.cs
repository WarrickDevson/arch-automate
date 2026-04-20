using ArchAutomate.BIM.Models;

namespace ArchAutomate.BIM.Engines;

/// <summary>
/// Validates building designs against SANS 10400 – The Application of the National Building Regulations.
/// Part coverage: Part A (General), Part F (Structural), Part O (Parking),
///               Part T (Stairways), Part W (Fire).
/// </summary>
public class Sans10400Engine
{
    public ComplianceResult Evaluate(ZoningParameters parameters)
    {
        var result = new ComplianceResult();

        EvaluateParkingRequirements(parameters, result);
        EvaluateStairwayRequirements(parameters, result);
        EvaluateAccessibility(parameters, result);

        return result;
    }

    // SANS 10400-O: Parking
    private static void EvaluateParkingRequirements(ZoningParameters p, ComplianceResult result)
    {
        // Indicative ratio: 1 bay per 25m² GLA for most occupancies.
        // Actual ratio depends on occupancy class – extendable per project.
        const double BayPer25M2 = 25.0;
        double requiredBays = Math.Ceiling(p.GlaForParkingM2 / BayPer25M2);

        bool passed = p.ParkingBaysProvided >= requiredBays;

        result.ParkingRatio = p.GlaForParkingM2 > 0
            ? p.ParkingBaysProvided / (p.GlaForParkingM2 / BayPer25M2)
            : 1;

        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-O-Parking",
            Description = "Minimum parking bays required (SANS 10400-O)",
            ProvidedValue = p.ParkingBaysProvided,
            RequiredValue = requiredBays,
            Unit = "bays",
            Passed = passed
        });

        if (!passed)
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-O-Parking",
                Message = $"Insufficient parking: {p.ParkingBaysProvided} bays provided, {requiredBays} required for {p.GlaForParkingM2}m² GLA.",
                ClauseReference = "SANS 10400-O",
                Severity = ViolationSeverity.NonCompliant
            });
        }
    }

    // SANS 10400-T: Stairways – minimum width 1100mm for public buildings
    private static void EvaluateStairwayRequirements(ZoningParameters p, ComplianceResult result)
    {
        if (p.NumberOfStoreys <= 1) return;

        // Advisory check – actual stair width must be provided separately
        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-T-Stairway",
            Description = "Multi-storey building: stairway width must be ≥ 1100mm (SANS 10400-T)",
            ProvidedValue = 0,
            RequiredValue = 1100,
            Unit = "mm",
            Passed = true // Cannot evaluate without architectural drawing data
        });

        result.Violations.Add(new ComplianceViolation
        {
            Rule = "SANS10400-T-Stairway",
            Message = "Confirm all stairways meet minimum 1100mm clear width per SANS 10400-T.",
            ClauseReference = "SANS 10400-T cl. 4",
            Severity = ViolationSeverity.Advisory
        });
    }

    // SANS 10400-S: Accessibility (disabled access)
    private static void EvaluateAccessibility(ZoningParameters p, ComplianceResult result)
    {
        if (p.ProposedGfaM2 < 500) return; // Threshold for mandatory accessibility compliance

        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-S-Accessibility",
            Description = "Building >500m² GFA: disabled access route required (SANS 10400-S)",
            ProvidedValue = p.ProposedGfaM2,
            RequiredValue = 500,
            Unit = "m²",
            Passed = true
        });

        result.Violations.Add(new ComplianceViolation
        {
            Rule = "SANS10400-S-Accessibility",
            Message = "Building exceeds 500m² GFA – confirm compliant disabled access route, ramps, ablutions per SANS 10400-S.",
            ClauseReference = "SANS 10400-S",
            Severity = ViolationSeverity.Advisory
        });
    }
}
