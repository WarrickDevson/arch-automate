using ArchAutomate.BIM.Models;

namespace ArchAutomate.BIM.Engines;

/// <summary>
/// Validates foundation specifications against SANS 10400-H:2010 (Foundations).
/// Minimum dimensions are for normal soil (Class II, 50–75 kPa bearing capacity).
/// Engineer-designed foundations take precedence over these rule-based limits.
/// </summary>
public class FoundationEngine
{
    // SANS 10400-H:2010 Table H1 — Strip foundation minimums for normal soil
    private record FoundationMinimums(
        double ExtWidthMm,
        double ExtDepthMm,
        double IntWidthMm,
        double IntDepthMm);

    private static readonly Dictionary<int, FoundationMinimums> Minimums = new()
    {
        [1] = new(600, 200, 450, 200),   // Single storey
        [2] = new(700, 250, 600, 250),   // Double storey
    };

    public FoundationCheckResult Evaluate(FoundationRequest request)
    {
        var result = new FoundationCheckResult
        {
            NumberOfStoreys = request.NumberOfStoreys,
            FoundationType = request.FoundationType,
        };

        // Engineer required for 3+ storeys or non-strip foundations
        bool engineerRequired = request.NumberOfStoreys >= 3
            || request.FoundationType is "Pad" or "Raft" or "Pile";
        result.EngineerRequired = engineerRequired;

        if (engineerRequired)
        {
            result.Checks.Add(new FoundationCheckItem
            {
                Rule = "Structural Engineer Appointment",
                Description = "A structural engineer must design and approve foundations for 3+ storey buildings or non-strip foundation types",
                ClauseReference = "SANS 10400-A cl.10 / NHBRC",
                CheckType = "Boolean",
                Passed = request.EngineerAppointed,
                ProvidedValue = request.EngineerAppointed ? 1 : 0,
                RequiredValue = 1,
                Unit = "required",
                IsMandatory = true,
                Note = "Engineer's design and specifications supersede these rule-based minimums",
            });

            // Still check concrete and DPC regardless
            AddConcreteAndDpcChecks(result, request);
            result.OverallPass = result.Checks.All(c => !c.IsMandatory || c.Passed);
            return result;
        }

        var min = Minimums[Math.Min(request.NumberOfStoreys, 2)];

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "External Foundation Width",
            Description = $"Minimum strip foundation width for external walls ({request.NumberOfStoreys}-storey)",
            ClauseReference = "SANS 10400-H Table H1",
            CheckType = "Numeric",
            ProvidedValue = request.ExternalWidthMm,
            RequiredValue = min.ExtWidthMm,
            Unit = "mm",
            Passed = request.ExternalWidthMm >= min.ExtWidthMm,
        });

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "External Foundation Depth",
            Description = $"Minimum strip foundation depth for external walls ({request.NumberOfStoreys}-storey)",
            ClauseReference = "SANS 10400-H Table H1",
            CheckType = "Numeric",
            ProvidedValue = request.ExternalDepthMm,
            RequiredValue = min.ExtDepthMm,
            Unit = "mm",
            Passed = request.ExternalDepthMm >= min.ExtDepthMm,
        });

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "Internal Foundation Width",
            Description = $"Minimum strip foundation width for internal walls ({request.NumberOfStoreys}-storey)",
            ClauseReference = "SANS 10400-H Table H1",
            CheckType = "Numeric",
            ProvidedValue = request.InternalWidthMm,
            RequiredValue = min.IntWidthMm,
            Unit = "mm",
            Passed = request.InternalWidthMm >= min.IntWidthMm,
        });

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "Internal Foundation Depth",
            Description = $"Minimum strip foundation depth for internal walls ({request.NumberOfStoreys}-storey)",
            ClauseReference = "SANS 10400-H Table H1",
            CheckType = "Numeric",
            ProvidedValue = request.InternalDepthMm,
            RequiredValue = min.IntDepthMm,
            Unit = "mm",
            Passed = request.InternalDepthMm >= min.IntDepthMm,
        });

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "Depth Below Natural Ground Level",
            Description = "Minimum depth of foundation below NGL for external walls",
            ClauseReference = "SANS 10400-H cl.4.3.1",
            CheckType = "Numeric",
            ProvidedValue = request.BelowNglMm,
            RequiredValue = 300,
            Unit = "mm",
            Passed = request.BelowNglMm >= 300,
        });

        AddConcreteAndDpcChecks(result, request);

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "Brick Force in Walls",
            Description = "Correct-width brick force in all brick walls — every 2nd course minimum",
            ClauseReference = "SANS 10400-K cl.5.2.4",
            CheckType = "Boolean",
            ProvidedValue = request.BrickForceSpecified ? 1 : 0,
            RequiredValue = 1,
            Unit = "required",
            Passed = request.BrickForceSpecified,
        });

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "Backfill Compaction Specification",
            Description = "Filling to be compacted to 90% MOD A.A.H.S.T.O in layers not exceeding 150mm; compaction tests required",
            ClauseReference = "SANS 10400-H cl.4.6 / SANS 3001-GR52",
            CheckType = "Boolean",
            ProvidedValue = request.CompactionSpec ? 1 : 0,
            RequiredValue = 1,
            Unit = "required",
            Passed = request.CompactionSpec,
        });

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "Termite / Pest Treatment",
            Description = "All foundation excavations and subfloor surfaces must be treated with an SABS-approved termiticide with a minimum 10-year guarantee",
            ClauseReference = "SANS 10400-H cl.4.7 / SANS 1153",
            CheckType = "Boolean",
            ProvidedValue = request.PestTreatmentSpecified ? 1 : 0,
            RequiredValue = 1,
            Unit = "required",
            Passed = request.PestTreatmentSpecified,
        });

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "Finished Floor Level Above NGL",
            Description = "Finished ground floor level must be at least 170mm above natural ground level",
            ClauseReference = "SANS 10400-H cl.4.5",
            CheckType = "Numeric",
            ProvidedValue = request.FinishedFloorAboveNglMm,
            RequiredValue = 170,
            Unit = "mm",
            Passed = request.FinishedFloorAboveNglMm >= 170,
        });

        result.OverallPass = result.Checks.All(c => !c.IsMandatory || c.Passed);
        return result;
    }

    private static void AddConcreteAndDpcChecks(FoundationCheckResult result, FoundationRequest request)
    {
        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "Concrete Compressive Strength",
            Description = "Minimum concrete compressive strength of foundation after 28 days",
            ClauseReference = "SANS 10400-H cl.4.4.1 / SANS 5863",
            CheckType = "Numeric",
            ProvidedValue = request.ConcreteMpa,
            RequiredValue = 15,
            Unit = "MPa",
            Passed = request.ConcreteMpa >= 15,
            Note = request.ConcreteMpa >= 25
                ? "Specified ≥25MPa — exceeds minimum, aligns with industry best practice"
                : request.ConcreteMpa >= 15
                    ? "Meets SANS minimum; 25MPa is the industry standard for SA residential"
                    : null,
        });

        result.Checks.Add(new FoundationCheckItem
        {
            Rule = "Damp-Proof Course (DPC)",
            Description = "DPC membrane required in all masonry walls at foundation level",
            ClauseReference = "SANS 10400-B cl.3.2.1 / SANS 952",
            CheckType = "Boolean",
            ProvidedValue = request.DpcSpecified ? 1 : 0,
            RequiredValue = 1,
            Unit = "required",
            Passed = request.DpcSpecified,
        });

        if (request.DpcSpecified)
        {
            result.Checks.Add(new FoundationCheckItem
            {
                Rule = "DPC Thickness",
                Description = "Minimum DPC membrane thickness — 375 micron polythene or SABS-approved equivalent",
                ClauseReference = "SANS 10400-B Table B1 / SANS 952",
                CheckType = "Numeric",
                ProvidedValue = request.DpcMicron,
                RequiredValue = 375,
                Unit = "µm",
                Passed = request.DpcMicron >= 375,
            });
        }
    }
}

public class FoundationRequest
{
    public int NumberOfStoreys { get; set; } = 1;
    public string FoundationType { get; set; } = "Strip"; // Strip | Pad | Raft | Pile
    public double ExternalWidthMm { get; set; }
    public double ExternalDepthMm { get; set; }
    public double InternalWidthMm { get; set; }
    public double InternalDepthMm { get; set; }
    public double ConcreteMpa { get; set; }
    public double BelowNglMm { get; set; }
    public double FinishedFloorAboveNglMm { get; set; }
    public bool DpcSpecified { get; set; }
    public int DpcMicron { get; set; } = 375;
    public bool BrickForceSpecified { get; set; }
    public bool CompactionSpec { get; set; }
    public bool EngineerAppointed { get; set; }
    public bool PestTreatmentSpecified { get; set; }
}
