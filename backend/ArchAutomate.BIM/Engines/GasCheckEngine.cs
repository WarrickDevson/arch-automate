using ArchAutomate.BIM.Models;

namespace ArchAutomate.BIM.Engines;

/// <summary>
/// Validates LPG gas installation callouts against SANS 10087-7:2013
/// (The handling, storage, distribution and maintenance of liquefied petroleum gas in domestic,
/// commercial and industrial installations — Part 7: Cylinders connected to a manifold).
/// </summary>
public class GasCheckEngine
{
    public GasCheckResult Evaluate(GasCheckRequest request)
    {
        if (!request.HasGasInstallation)
        {
            return new GasCheckResult
            {
                HasGasInstallation = false,
                OverallPass = true,
                Items = [],
            };
        }

        var items = new List<GasCheckItem>();

        // ── STORAGE ──────────────────────────────────────────────────────────

        items.Add(new GasCheckItem
        {
            Category = "Storage",
            Item = "Gas Cylinder Storage Cage Specified",
            ClauseReference = "SANS 10087-7 cl.5.2",
            Requirement = "An approved cylinder storage cage must be specified and detailed on the drawings",
            Passed = request.CageSpecified,
            ProvidedValue = request.CageSpecified ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new GasCheckItem
        {
            Category = "Storage",
            Item = "Cage Material Compliant",
            ClauseReference = "SANS 10087-7 cl.5.2.1",
            Requirement = "Cage to be fabricated from 25×25mm square tubing with expanded steel mesh to front, back and sides",
            Passed = request.CageMaterialCompliant,
            ProvidedValue = request.CageMaterialCompliant ? "25×25mm sq. tubing + expanded mesh" : null,
            IsMandatory = true,
        });

        items.Add(new GasCheckItem
        {
            Category = "Storage",
            Item = "Cage Doors are Lockable",
            ClauseReference = "SANS 10087-7 cl.5.2.2",
            Requirement = "Cage must have lockable steel front opening doors",
            Passed = request.CageDoorsLockable,
            ProvidedValue = request.CageDoorsLockable ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new GasCheckItem
        {
            Category = "Storage",
            Item = "SANS 10087 Reference Noted on Drawing",
            ClauseReference = "SANS 10087-7",
            Requirement = "Drawing must explicitly reference that all work must comply with SANS 10087 Part 7",
            Passed = request.Sans10087CalloutOnDrawing,
            ProvidedValue = request.Sans10087CalloutOnDrawing ? "Yes" : null,
            IsMandatory = true,
        });

        // ── DISTANCES ────────────────────────────────────────────────────────

        items.Add(new GasCheckItem
        {
            Category = "Distances",
            Item = "3m Radius from Combustible Materials",
            ClauseReference = "SANS 10087-7 cl.5.4.1",
            Requirement = "Minimum 3m radius from all combustible materials; vegetation around cage to be kept short",
            Passed = request.CombustibleDistanceM >= 3.0,
            ProvidedValue = request.CombustibleDistanceM > 0 ? $"{request.CombustibleDistanceM}m" : null,
            IsMandatory = true,
            Note = request.CombustibleDistanceM < 3.0 && request.CombustibleDistanceM > 0
                ? $"Specified {request.CombustibleDistanceM}m is below the required 3m minimum"
                : null,
        });

        items.Add(new GasCheckItem
        {
            Category = "Distances",
            Item = "1m from Building Openings",
            ClauseReference = "SANS 10087-7 cl.5.4.2",
            Requirement = "Minimum 1m from any window, door or other building opening",
            Passed = request.OpeningDistanceM >= 1.0,
            ProvidedValue = request.OpeningDistanceM > 0 ? $"{request.OpeningDistanceM}m" : null,
            IsMandatory = true,
        });

        // ── SAFETY SIGNAGE ───────────────────────────────────────────────────

        items.Add(new GasCheckItem
        {
            Category = "Safety Signage",
            Item = "No Open Flames Sign",
            ClauseReference = "SANS 10087-7 cl.6.1 / SABS 1186",
            Requirement = "ISO-compliant 'No Open Flames' safety sign specified at storage area",
            Passed = request.NoFlamesSign,
            ProvidedValue = request.NoFlamesSign ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new GasCheckItem
        {
            Category = "Safety Signage",
            Item = "No Smoking Sign",
            ClauseReference = "SANS 10087-7 cl.6.1 / SABS 1186",
            Requirement = "ISO-compliant 'No Smoking' sign specified at storage area",
            Passed = request.NoSmokingSign,
            ProvidedValue = request.NoSmokingSign ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new GasCheckItem
        {
            Category = "Safety Signage",
            Item = "No Cellphones Sign",
            ClauseReference = "SANS 10087-7 cl.6.1",
            Requirement = "'No Cellphones' sign specified at storage area to prevent ignition sources",
            Passed = request.NoCellphonesSign,
            ProvidedValue = request.NoCellphonesSign ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new GasCheckItem
        {
            Category = "Safety Signage",
            Item = "No Unauthorized Entry Sign",
            ClauseReference = "SANS 10087-7 cl.6.1 / SABS 1186",
            Requirement = "'No Unauthorized Entry — Danger' sign specified at storage area",
            Passed = request.NoUnauthorizedEntrySign,
            ProvidedValue = request.NoUnauthorizedEntrySign ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new GasCheckItem
        {
            Category = "Safety Signage",
            Item = "Fire Extinguisher Specified (min 9kg dry powder)",
            ClauseReference = "SANS 10087-7 cl.6.2",
            Requirement = "Minimum 1× 9kg dry powder fire extinguisher must be available and its location shown on the drawing with a directional arrow sign",
            Passed = request.FireExtinguisherSpecified && request.ExtinguisherRatingKg >= 9,
            ProvidedValue = request.FireExtinguisherSpecified
                ? $"{request.ExtinguisherRatingKg}kg dry powder"
                : null,
            IsMandatory = true,
            Note = request.FireExtinguisherSpecified && request.ExtinguisherRatingKg < 9
                ? $"Specified {request.ExtinguisherRatingKg}kg is below the 9kg minimum"
                : null,
        });

        // ── PIPING ───────────────────────────────────────────────────────────

        items.Add(new GasCheckItem
        {
            Category = "Piping",
            Item = "Pipe Material Specified",
            ClauseReference = "SANS 10087-7 cl.7.1",
            Requirement = "Approved 15mmØ composite pipe (or SABS-approved alternative) must be specified for gas supply lines",
            Passed = request.PipeMaterialSpecified && !string.IsNullOrWhiteSpace(request.PipeMaterial),
            ProvidedValue = string.IsNullOrWhiteSpace(request.PipeMaterial) ? null : request.PipeMaterial,
            IsMandatory = true,
        });

        if (request.HasUndergroundPiping)
        {
            items.Add(new GasCheckItem
            {
                Category = "Piping",
                Item = "Underground Pipe Depth (min 500mm)",
                ClauseReference = "SANS 10087-7 cl.7.3.2",
                Requirement = "Any underground gas piping must be installed at a minimum depth of 500mm",
                Passed = request.UndergroundDepthMm >= 500,
                ProvidedValue = $"{request.UndergroundDepthMm}mm",
                IsMandatory = true,
                Note = request.UndergroundDepthMm < 500
                    ? $"Specified depth of {request.UndergroundDepthMm}mm is below the 500mm minimum"
                    : null,
            });
        }

        items.Add(new GasCheckItem
        {
            Category = "Piping",
            Item = "Gas Line Route Shown on Drawing",
            ClauseReference = "SANS 10087-7 cl.4.1",
            Requirement = "Gas line route from storage to appliances must be clearly shown on the layout plan",
            Passed = request.GasLineShownOnDrawing,
            ProvidedValue = request.GasLineShownOnDrawing ? "Yes" : null,
            IsMandatory = true,
        });

        // ── CERTIFICATION ────────────────────────────────────────────────────

        items.Add(new GasCheckItem
        {
            Category = "Certification",
            Item = "Approved Installer Required (noted on drawing)",
            ClauseReference = "SANS 10087-7 cl.8.1 / Gas Act 85 of 2001",
            Requirement = "Drawings must note that gas installation must be performed by an approved and registered LPG installer",
            Passed = request.InstallerCertRequired,
            ProvidedValue = request.InstallerCertRequired ? "Yes" : null,
            IsMandatory = true,
        });

        items.Add(new GasCheckItem
        {
            Category = "Certification",
            Item = "Certificate of Workmanship Required (noted on drawing)",
            ClauseReference = "SANS 10087-7 cl.8.2",
            Requirement = "Drawings must note that a certificate of workmanship and installation must be provided by the installer upon completion",
            Passed = request.WorkmanshipCertRequired,
            ProvidedValue = request.WorkmanshipCertRequired ? "Yes" : null,
            IsMandatory = true,
        });

        bool overallPass = items.Where(i => i.IsMandatory).All(i => i.Passed);

        return new GasCheckResult
        {
            HasGasInstallation = true,
            OverallPass = overallPass,
            Items = items,
        };
    }
}

public class GasCheckRequest
{
    public bool HasGasInstallation { get; set; }
    public bool CageSpecified { get; set; }
    public bool CageMaterialCompliant { get; set; }
    public bool CageDoorsLockable { get; set; }
    public bool Sans10087CalloutOnDrawing { get; set; }
    public double CombustibleDistanceM { get; set; }
    public double OpeningDistanceM { get; set; }
    public bool NoFlamesSign { get; set; }
    public bool NoSmokingSign { get; set; }
    public bool NoCellphonesSign { get; set; }
    public bool NoUnauthorizedEntrySign { get; set; }
    public bool FireExtinguisherSpecified { get; set; }
    public double ExtinguisherRatingKg { get; set; }
    public bool PipeMaterialSpecified { get; set; }
    public string PipeMaterial { get; set; } = string.Empty;
    public bool HasUndergroundPiping { get; set; }
    public int UndergroundDepthMm { get; set; }
    public bool GasLineShownOnDrawing { get; set; }
    public bool InstallerCertRequired { get; set; }
    public bool WorkmanshipCertRequired { get; set; }
}
