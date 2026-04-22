using ArchAutomate.BIM.Models;

namespace ArchAutomate.BIM.Engines;

/// <summary>
/// Validates roof drawing callouts against SANS 10400-L:2011 (Roofs),
/// SANS 10400-K:2011 (Walling) and SANS 10400-XA:2011 (Energy).
/// A passing checklist confirms all mandatory items are specified on the drawing
/// set — it does not replace structural engineering certification.
/// </summary>
public class RoofCheckEngine
{
    // Minimum pitch per roof covering type (SANS 10400-L Table L1)
    private static readonly Dictionary<string, double> MinPitch = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chromadek"] = 5.0,
        ["IBR Sheeting"] = 5.0,
        ["Corrugated"] = 8.0,
        ["Clay Tiles"] = 17.5,
        ["Concrete Tiles"] = 17.5,
        ["Slate"] = 25.0,
        ["Thatch"] = 45.0,
        ["Flat (waterproofed)"] = 1.0,
    };

    public RoofCheckResult Evaluate(RoofCheckRequest request)
    {
        var items = new List<RoofCheckItem>();

        double reqPitch = MinPitch.TryGetValue(request.RoofType, out var mp) ? mp : 5.0;

        // ── GENERAL ──────────────────────────────────────────────────────────

        items.Add(new RoofCheckItem
        {
            Category = "General",
            Item = "Roof Covering Type Specified",
            ClauseReference = "SANS 10400-L cl.3",
            Requirement = "The roof covering type must be explicitly stated on the drawings",
            Passed = !string.IsNullOrWhiteSpace(request.RoofType),
            ProvidedValue = string.IsNullOrWhiteSpace(request.RoofType) ? null : request.RoofType,
            IsMandatory = true,
        });

        items.Add(new RoofCheckItem
        {
            Category = "General",
            Item = "Roof Pitch Stated and Compliant",
            ClauseReference = "SANS 10400-L Table L1",
            Requirement = $"Minimum pitch for {(string.IsNullOrWhiteSpace(request.RoofType) ? "selected covering" : request.RoofType)}: {reqPitch}°",
            Passed = request.PitchDegrees >= reqPitch,
            ProvidedValue = request.PitchDegrees > 0 ? $"{request.PitchDegrees}°" : null,
            IsMandatory = true,
            Note = request.PitchDegrees >= reqPitch ? null : $"Specified {request.PitchDegrees}° is below the {reqPitch}° minimum for {request.RoofType}",
        });

        items.Add(new RoofCheckItem
        {
            Category = "General",
            Item = "Roof Colour Specified",
            ClauseReference = "NBR / Municipal / HOA requirement",
            Requirement = "Roof colour must be stated on drawings for HOA and council approval",
            Passed = !string.IsNullOrWhiteSpace(request.RoofColour),
            ProvidedValue = string.IsNullOrWhiteSpace(request.RoofColour) ? null : request.RoofColour,
            IsMandatory = true,
        });

        items.Add(new RoofCheckItem
        {
            Category = "General",
            Item = "Overhang Dimension Stated",
            ClauseReference = "SANS 10400-K cl.5.3",
            Requirement = "Roof overhang dimension must be noted on drawings",
            Passed = request.OverhangMm > 0,
            ProvidedValue = request.OverhangMm > 0 ? $"{request.OverhangMm}mm" : null,
            IsMandatory = true,
        });

        // ── STRUCTURE ────────────────────────────────────────────────────────

        items.Add(new RoofCheckItem
        {
            Category = "Structure",
            Item = "Truss Type Specified",
            ClauseReference = "SANS 10400-K cl.6.2",
            Requirement = "The roof truss type must be specified (e.g. SA Pine pre-fabricated, exposed)",
            Passed = !string.IsNullOrWhiteSpace(request.TrussType),
            ProvidedValue = string.IsNullOrWhiteSpace(request.TrussType) ? null : request.TrussType,
            IsMandatory = true,
        });

        items.Add(new RoofCheckItem
        {
            Category = "Structure",
            Item = "Truss Designer / Structural Engineer Named",
            ClauseReference = "SANS 10400-K cl.6.2.2",
            Requirement = "All timber roof trusses must be designed and approved by a structural engineer; the designer must be referenced on the drawings",
            Passed = !string.IsNullOrWhiteSpace(request.TrussDesigner),
            ProvidedValue = string.IsNullOrWhiteSpace(request.TrussDesigner) ? null : request.TrussDesigner,
            IsMandatory = true,
        });

        items.Add(new RoofCheckItem
        {
            Category = "Structure",
            Item = "Purlin Spacing Stated",
            ClauseReference = "SANS 10400-K cl.5.2 / roof specialist spec",
            Requirement = "Purlin spacing must be stated on drawings — typically ±1000mm CTC for sheeting roofs",
            Passed = request.PurlinSpacingMm > 0 && request.PurlinSpacingMm <= 1500,
            ProvidedValue = request.PurlinSpacingMm > 0 ? $"{request.PurlinSpacingMm}mm CTC" : null,
            IsMandatory = true,
            Note = request.PurlinSpacingMm > 1500 ? "Spacing exceeds 1500mm — verify with structural engineer" : null,
        });

        items.Add(new RoofCheckItem
        {
            Category = "Structure",
            Item = "Ridge Cap / Plate Specified",
            ClauseReference = "SANS 10400-L cl.5.1",
            Requirement = "Ridge cap or ridge plate must be specified on drawings",
            Passed = request.RidgePlateSpecified,
            ProvidedValue = request.RidgePlateSpecified ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new RoofCheckItem
        {
            Category = "Structure",
            Item = "Fascia Board Specified and Painted",
            ClauseReference = "SANS 10400-L cl.4.3 / SANS 10400-T",
            Requirement = "Fascia board must be specified on drawings and noted as painted/protected",
            Passed = request.FasciaSpecified,
            ProvidedValue = request.FasciaSpecified ? "Yes" : null,
            IsMandatory = true,
        });

        // ── MATERIALS ────────────────────────────────────────────────────────

        items.Add(new RoofCheckItem
        {
            Category = "Materials",
            Item = "Timber Treatment Specified",
            ClauseReference = "SANS 10400-K cl.6.2.4 / SANS 10005",
            Requirement = "All timber roof trusses must be treated with SABS-approved termite-resistant and preservative substances",
            Passed = request.TimberTreatmentSpecified,
            ProvidedValue = request.TimberTreatmentSpecified ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new RoofCheckItem
        {
            Category = "Materials",
            Item = "Insulation Specified with R-value",
            ClauseReference = "SANS 10400-XA cl.4.4.1",
            Requirement = "Ceiling / roof insulation must be specified including minimum R-value (typically R3.5–R5.5 for roofs depending on climate zone)",
            Passed = request.InsulationSpecified && request.InsulationRValue >= 2.5,
            ProvidedValue = request.InsulationSpecified && request.InsulationRValue > 0
                ? $"R{request.InsulationRValue}"
                : null,
            IsMandatory = true,
            Note = request.InsulationRValue is > 0 and < 3.5
                ? "R-value may be insufficient depending on the project's climate zone — verify against SANS 10400-XA Table XA2"
                : null,
        });

        items.Add(new RoofCheckItem
        {
            Category = "Materials",
            Item = "Rainwater Gutters and Downpipes",
            ClauseReference = "SANS 10400-P cl.5.1.1",
            Requirement = "Rainwater disposal gutters and downpipes must be shown on drawings",
            Passed = request.GuttersSpecified,
            ProvidedValue = request.GuttersSpecified ? "Yes" : null,
            IsMandatory = false,
            Note = "Required by most municipalities at plan submission stage",
        });

        // ── CERTIFICATION ────────────────────────────────────────────────────

        items.Add(new RoofCheckItem
        {
            Category = "Certification",
            Item = "Structural Engineer Certificate Required (noted)",
            ClauseReference = "SANS 10400-K cl.6.2.2",
            Requirement = "Drawings must note that a structural engineer's truss design certificate is required before installation commences",
            Passed = request.EngineerCertRequired,
            ProvidedValue = request.EngineerCertRequired ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new RoofCheckItem
        {
            Category = "Certification",
            Item = "Roof Specialist / Manufacturer Specification Referenced",
            ClauseReference = "SANS 10400-L cl.3.2",
            Requirement = "Drawings must note that the roof covering must be installed to the manufacturer's specifications",
            Passed = request.ManufacturerSpecReferenced,
            ProvidedValue = request.ManufacturerSpecReferenced ? "Yes" : null,
            IsMandatory = true,
        });

        bool overallPass = items.Where(i => i.IsMandatory).All(i => i.Passed);

        return new RoofCheckResult
        {
            OverallPass = overallPass,
            RoofType = request.RoofType,
            Items = items,
        };
    }
}

public class RoofCheckRequest
{
    public string RoofType { get; set; } = string.Empty; // Chromadek | IBR Sheeting | Clay Tiles | etc.
    public double PitchDegrees { get; set; }
    public string TrussType { get; set; } = string.Empty;
    public string TrussDesigner { get; set; } = string.Empty;
    public int PurlinSpacingMm { get; set; }
    public int OverhangMm { get; set; }
    public string RoofColour { get; set; } = string.Empty;
    public bool InsulationSpecified { get; set; }
    public double InsulationRValue { get; set; }
    public bool RidgePlateSpecified { get; set; }
    public bool FasciaSpecified { get; set; }
    public bool TimberTreatmentSpecified { get; set; }
    public bool GuttersSpecified { get; set; }
    public bool EngineerCertRequired { get; set; }
    public bool ManufacturerSpecReferenced { get; set; }
}
