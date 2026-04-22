using ArchAutomate.BIM.Models;

namespace ArchAutomate.BIM.Engines;

/// <summary>
/// Evaluates buildings against SANS 10400-XA:2011 "Energy Usage in Buildings".
/// Covers:
///   – Climate zone resolution (cl. 4.2)
///   – Window-to-Wall Ratio / fenestration limits (cl. 4.4.1)
///   – Daylighting adequacy (supporting SANS 10400-O natural light requirement)
///   – Roof &amp; wall thermal resistance (R-value) targets (Table 1/2)
///   – Glazing U-value limits (Table 5)
///   – Lighting Power Density (LPD) limits (Table 7)
///   – Building envelope compactness (surface-to-volume advisory)
/// </summary>
public sealed class XaEnergyEngine
{
    // ── Typical single-window area used when GrossWindowAreaM2 is absent ─────
    private const double TypicalWindowAreaM2 = 1.44; // 1.2 m × 1.2 m

    // ── Climate zone data (indexed 1-6) ──────────────────────────────────────
    // Sources: SANS 10400-XA:2011 Tables 1, 2, 5, 6; SANS 204:2011
    private static readonly IReadOnlyDictionary<int, ClimateZoneSpec> ClimateZones =
        new Dictionary<int, ClimateZoneSpec>
        {
            [1] = new("Cape Coastal (Zone 1)", MaxWwr: 20, MinRoofR: 3.7, MinWallR: 2.2, MaxGlazingU: 3.5, MaxShgc: 0.40),
            [2] = new("Highveld / Interior Plateau (Zone 2)", MaxWwr: 15, MinRoofR: 4.5, MinWallR: 2.2, MaxGlazingU: 3.0, MaxShgc: 0.40),
            [3] = new("Northern Interior (Zone 3)", MaxWwr: 10, MinRoofR: 5.0, MinWallR: 3.0, MaxGlazingU: 2.5, MaxShgc: 0.30),
            [4] = new("Eastern Coastal Belt (Zone 4)", MaxWwr: 20, MinRoofR: 3.5, MinWallR: 2.0, MaxGlazingU: 3.5, MaxShgc: 0.40),
            [5] = new("Semi-Arid Interior (Zone 5)", MaxWwr: 10, MinRoofR: 5.0, MinWallR: 3.5, MaxGlazingU: 2.5, MaxShgc: 0.30),
            [6] = new("Arid Interior / Karoo (Zone 6)", MaxWwr: 10, MinRoofR: 5.5, MinWallR: 4.0, MaxGlazingU: 2.0, MaxShgc: 0.30),
        };

    // ── Province → default climate zone ──────────────────────────────────────
    private static readonly IReadOnlyDictionary<string, int> ProvinceZone =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Western Cape"] = 1,
            ["Eastern Cape"] = 4,
            ["KwaZulu-Natal"] = 4,
            ["Gauteng"] = 2,
            ["Mpumalanga"] = 2,
            ["Limpopo"] = 3,
            ["North West"] = 5,
            ["Northern Cape"] = 6,
            ["Free State"] = 2,
        };

    // ── Lighting Power Density targets (SANS 10400-XA Table 7) ───────────────
    private static readonly IReadOnlyDictionary<OccupancyType, double> MaxLpd =
        new Dictionary<OccupancyType, double>
        {
            [OccupancyType.Residential] = 8.0,
            [OccupancyType.Office] = 12.0,
            [OccupancyType.Retail] = 16.0,
            [OccupancyType.Industrial] = 10.0,
            [OccupancyType.Educational] = 12.0,
            [OccupancyType.Healthcare] = 14.0,
        };

    // ── Minimum daylighting window ratio (supporting SANS 10400-O) ───────────
    // Window area must be ≥ 10 % of GFA for habitable spaces.
    private const double MinDaylightingRatioPercent = 10.0;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the full SANS 10400-XA evaluation against the provided parameters.
    /// </summary>
    public XaEnergyResult Evaluate(XaEnergyParameters p)
    {
        var result = new XaEnergyResult();

        // 1. Resolve climate zone ─────────────────────────────────────────────
        int zone = ResolveClimateZone(p);
        var spec = ClimateZones[zone];
        result.ClimateZone = zone;
        result.ClimateZoneName = spec.Name;
        result.RequiredRoofRValue = spec.MinRoofR;
        result.RequiredWallRValue = spec.MinWallR;
        result.MaxGlazingUValue = spec.MaxGlazingU;
        result.MaxWindowToWallRatioPercent = spec.MaxWwr;
        result.MaxSolarHeatGainCoefficient = spec.MaxShgc;
        result.MaxLightingPowerDensityWPerM2 = MaxLpd.GetValueOrDefault(p.OccupancyType);

        // 2. Derive envelope areas ─────────────────────────────────────────────
        double wallArea = DeriveWallArea(p);
        double windowArea = DeriveWindowArea(p);
        double roofArea = p.RoofAreaM2 > 0 ? p.RoofAreaM2 : p.FootprintM2;

        // 3. Key ratios ────────────────────────────────────────────────────────
        if (wallArea > 0)
        {
            result.WindowToWallRatioPercent = Math.Round(windowArea / wallArea * 100.0, 1);
        }

        if (p.ProposedGfaM2 > 0 && windowArea >= 0)
        {
            result.DaylightingRatioPercent = Math.Round(windowArea / p.ProposedGfaM2 * 100.0, 1);
        }

        result.SurfaceToVolumeRatio = DeriveSurfaceToVolumeRatio(p, wallArea, roofArea);

        // 4. Run individual checks ─────────────────────────────────────────────
        EvaluateWindowToWallRatio(result, spec, windowArea, wallArea, p);
        EvaluateDaylighting(result, windowArea, p);
        EvaluateRoofInsulation(result, spec, p);
        EvaluateWallInsulation(result, spec, p);
        EvaluateGlazingUValue(result, spec, p);
        EvaluateSolarHeatGain(result, spec, p);
        EvaluateLightingPowerDensity(result, p);
        EvaluateBuildingCompactness(result, p, wallArea, roofArea);

        // 5. Derive energy rating ──────────────────────────────────────────────
        result.EnergyRating = DeriveEnergyRating(result);

        return result;
    }

    // ── Climate zone resolution ───────────────────────────────────────────────

    private static int ResolveClimateZone(XaEnergyParameters p)
    {
        if (p.ClimateZone is >= 1 and <= 6)
            return p.ClimateZone;

        if (!string.IsNullOrWhiteSpace(p.Province)
            && ProvinceZone.TryGetValue(p.Province.Trim(), out int z))
            return z;

        return 2; // Default: Highveld (most common urban zone for SA)
    }

    // ── Area derivation helpers ───────────────────────────────────────────────

    /// <summary>
    /// Derives total external wall area.
    /// If not supplied: assumes square plan → perimeter = 4√footprint, wall area = perimeter × height.
    /// </summary>
    private static double DeriveWallArea(XaEnergyParameters p)
    {
        if (p.GrossWallAreaM2 > 0) return p.GrossWallAreaM2;
        if (p.FootprintM2 <= 0 || p.BuildingHeightM <= 0) return 0;
        double side = Math.Sqrt(p.FootprintM2);
        double perimeter = side * 4;
        return Math.Round(perimeter * p.BuildingHeightM, 2);
    }

    /// <summary>
    /// Derives total glazed area.
    /// Priority: GrossWindowAreaM2 → WindowCount × typical size → 0.
    /// </summary>
    private static double DeriveWindowArea(XaEnergyParameters p)
    {
        if (p.GrossWindowAreaM2 > 0) return p.GrossWindowAreaM2;
        if (p.WindowCount > 0) return p.WindowCount * TypicalWindowAreaM2;
        return 0;
    }

    private static double? DeriveSurfaceToVolumeRatio(XaEnergyParameters p, double wallArea, double roofArea)
    {
        double volume = p.ProposedGfaM2 > 0
            ? p.ProposedGfaM2                           // GFA ≈ usable volume proxy
            : p.FootprintM2 * p.BuildingHeightM;
        if (volume <= 0) return null;
        double floorArea = p.FootprintM2 > 0 ? p.FootprintM2 : 0;
        double totalSurface = wallArea + roofArea + floorArea;
        if (totalSurface <= 0) return null;
        return Math.Round(totalSurface / volume, 3);
    }

    // ── Checks ────────────────────────────────────────────────────────────────

    private static void EvaluateWindowToWallRatio(
        XaEnergyResult result, ClimateZoneSpec spec,
        double windowArea, double wallArea, XaEnergyParameters p)
    {
        if (wallArea <= 0)
        {
            // Cannot evaluate — flag advisory
            result.Checks.Add(new ComplianceCheck
            {
                Rule = "SANS10400-XA-WWR",
                Description = "Window-to-Wall Ratio (fenestration limit) — SANS 10400-XA cl. 4.4.1",
                ProvidedValue = 0,
                RequiredValue = spec.MaxWwr,
                Unit = "%",
                Passed = true,
            });
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-WWR",
                Message = $"Window-to-Wall Ratio could not be calculated (no wall area data). " +
                          $"Zone {result.ClimateZone} limit is {spec.MaxWwr}%. " +
                          (p.WindowCount > 0
                              ? $"Estimated from {p.WindowCount} windows × {TypicalWindowAreaM2}m² = {windowArea:F1}m² glazing."
                              : "Provide wall area or window count in parameters."),
                ClauseReference = "SANS 10400-XA cl. 4.4.1",
                Severity = ViolationSeverity.Advisory,
            });
            return;
        }

        double wwr = windowArea / wallArea * 100.0;
        bool passed = wwr <= spec.MaxWwr;
        string source = p.GrossWindowAreaM2 > 0 ? "measured" : $"estimated from {p.WindowCount} windows";

        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-XA-WWR",
            Description = "Window-to-Wall Ratio (fenestration limit) — SANS 10400-XA cl. 4.4.1",
            ProvidedValue = Math.Round(wwr, 1),
            RequiredValue = spec.MaxWwr,
            Unit = "%",
            Passed = passed,
        });

        if (!passed)
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-WWR",
                Message = $"Window-to-Wall Ratio of {wwr:F1}% ({source}) exceeds the Zone {result.ClimateZone} " +
                          $"limit of {spec.MaxWwr}%. Reduce glazing area by " +
                          $"{(windowArea - wallArea * spec.MaxWwr / 100.0):F1}m² or increase opaque wall area.",
                ClauseReference = "SANS 10400-XA cl. 4.4.1",
                Severity = ViolationSeverity.NonCompliant,
            });
        }
    }

    private static void EvaluateDaylighting(
        XaEnergyResult result, double windowArea, XaEnergyParameters p)
    {
        if (p.ProposedGfaM2 <= 0 || windowArea <= 0)
        {
            result.Checks.Add(new ComplianceCheck
            {
                Rule = "SANS10400-XA-Daylighting",
                Description = "Daylighting adequacy — window area ≥ 10 % of GFA (SANS 10400-O / XA)",
                ProvidedValue = 0,
                RequiredValue = MinDaylightingRatioPercent,
                Unit = "%",
                Passed = true,
            });
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-Daylighting",
                Message = "Daylighting adequacy could not be fully evaluated. " +
                          "Ensure window area ≥ 10 % of GFA for all habitable rooms.",
                ClauseReference = "SANS 10400-O cl. 3; XA",
                Severity = ViolationSeverity.Advisory,
            });
            return;
        }

        double ratio = windowArea / p.ProposedGfaM2 * 100.0;
        bool passed = ratio >= MinDaylightingRatioPercent;

        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-XA-Daylighting",
            Description = "Daylighting adequacy — window area ≥ 10 % of GFA (SANS 10400-O / XA)",
            ProvidedValue = Math.Round(ratio, 1),
            RequiredValue = MinDaylightingRatioPercent,
            Unit = "%",
            Passed = passed,
        });

        if (!passed)
        {
            double extraM2 = p.ProposedGfaM2 * MinDaylightingRatioPercent / 100.0 - windowArea;
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-Daylighting",
                Message = $"Window area ({windowArea:F1}m²) provides only {ratio:F1}% of GFA " +
                          $"({p.ProposedGfaM2:F1}m²). Minimum is 10 %. " +
                          $"Add {extraM2:F1}m² of additional glazing.",
                ClauseReference = "SANS 10400-O cl. 3; SANS 10400-XA",
                Severity = ViolationSeverity.Warning,
            });
        }
    }

    private static void EvaluateRoofInsulation(
        XaEnergyResult result, ClimateZoneSpec spec, XaEnergyParameters p)
    {
        if (p.RoofRValue <= 0)
        {
            // R-value not provided — advisory only
            result.Checks.Add(new ComplianceCheck
            {
                Rule = "SANS10400-XA-RoofR",
                Description = $"Roof thermal resistance ≥ R{spec.MinRoofR} m²·K/W — SANS 10400-XA Table 1",
                ProvidedValue = 0,
                RequiredValue = spec.MinRoofR,
                Unit = "m²·K/W",
                Passed = true,
            });
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-RoofR",
                Message = $"Roof R-value not supplied. Zone {result.ClimateZone} requires a minimum roof " +
                          $"R-value of R{spec.MinRoofR} m²·K/W (SANS 10400-XA Table 1). " +
                          "Confirm with thermal analysis or product specification.",
                ClauseReference = "SANS 10400-XA Table 1",
                Severity = ViolationSeverity.Advisory,
            });
            return;
        }

        bool passed = p.RoofRValue >= spec.MinRoofR;
        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-XA-RoofR",
            Description = $"Roof thermal resistance ≥ R{spec.MinRoofR} m²·K/W — SANS 10400-XA Table 1",
            ProvidedValue = p.RoofRValue,
            RequiredValue = spec.MinRoofR,
            Unit = "m²·K/W",
            Passed = passed,
        });

        if (!passed)
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-RoofR",
                Message = $"Roof R-value of R{p.RoofRValue:F1} is below the Zone {result.ClimateZone} " +
                          $"minimum of R{spec.MinRoofR} m²·K/W. Upgrade roof insulation.",
                ClauseReference = "SANS 10400-XA Table 1",
                Severity = ViolationSeverity.NonCompliant,
            });
        }
    }

    private static void EvaluateWallInsulation(
        XaEnergyResult result, ClimateZoneSpec spec, XaEnergyParameters p)
    {
        if (p.WallRValue <= 0)
        {
            result.Checks.Add(new ComplianceCheck
            {
                Rule = "SANS10400-XA-WallR",
                Description = $"External wall thermal resistance ≥ R{spec.MinWallR} m²·K/W — SANS 10400-XA Table 2",
                ProvidedValue = 0,
                RequiredValue = spec.MinWallR,
                Unit = "m²·K/W",
                Passed = true,
            });
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-WallR",
                Message = $"Wall R-value not supplied. Zone {result.ClimateZone} requires ≥ R{spec.MinWallR} m²·K/W " +
                          "for external walls (SANS 10400-XA Table 2). Confirm with product data sheets.",
                ClauseReference = "SANS 10400-XA Table 2",
                Severity = ViolationSeverity.Advisory,
            });
            return;
        }

        bool passed = p.WallRValue >= spec.MinWallR;
        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-XA-WallR",
            Description = $"External wall thermal resistance ≥ R{spec.MinWallR} m²·K/W — SANS 10400-XA Table 2",
            ProvidedValue = p.WallRValue,
            RequiredValue = spec.MinWallR,
            Unit = "m²·K/W",
            Passed = passed,
        });

        if (!passed)
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-WallR",
                Message = $"Wall R-value of R{p.WallRValue:F1} is below the Zone {result.ClimateZone} " +
                          $"minimum of R{spec.MinWallR} m²·K/W. Add insulation to external walls.",
                ClauseReference = "SANS 10400-XA Table 2",
                Severity = ViolationSeverity.NonCompliant,
            });
        }
    }

    private static void EvaluateGlazingUValue(
        XaEnergyResult result, ClimateZoneSpec spec, XaEnergyParameters p)
    {
        if (p.GlazingUValue <= 0)
        {
            result.Checks.Add(new ComplianceCheck
            {
                Rule = "SANS10400-XA-GlazingU",
                Description = $"Glazing U-value ≤ {spec.MaxGlazingU} W/(m²·K) — SANS 10400-XA Table 5",
                ProvidedValue = 0,
                RequiredValue = spec.MaxGlazingU,
                Unit = "W/(m²·K)",
                Passed = true,
            });
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-GlazingU",
                Message = $"Glazing U-value not supplied. Zone {result.ClimateZone} allows a maximum of " +
                          $"{spec.MaxGlazingU} W/(m²·K) (SANS 10400-XA Table 5). " +
                          "Single clear glass (≈ 5.8 W/(m²·K)) will not comply in most zones. " +
                          "Specify double glazing or low-E coating.",
                ClauseReference = "SANS 10400-XA Table 5",
                Severity = ViolationSeverity.Advisory,
            });
            return;
        }

        bool passed = p.GlazingUValue <= spec.MaxGlazingU;
        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-XA-GlazingU",
            Description = $"Glazing U-value ≤ {spec.MaxGlazingU} W/(m²·K) — SANS 10400-XA Table 5",
            ProvidedValue = p.GlazingUValue,
            RequiredValue = spec.MaxGlazingU,
            Unit = "W/(m²·K)",
            Passed = passed,
        });

        if (!passed)
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-GlazingU",
                Message = $"Glazing U-value of {p.GlazingUValue:F2} W/(m²·K) exceeds the Zone {result.ClimateZone} " +
                          $"limit of {spec.MaxGlazingU} W/(m²·K). Upgrade to double-glazing or specify low-E glass.",
                ClauseReference = "SANS 10400-XA Table 5",
                Severity = ViolationSeverity.NonCompliant,
            });
        }
    }

    private static void EvaluateSolarHeatGain(
        XaEnergyResult result, ClimateZoneSpec spec, XaEnergyParameters p)
    {
        if (p.SolarHeatGainCoefficient <= 0)
        {
            result.Checks.Add(new ComplianceCheck
            {
                Rule = "SANS10400-XA-SHGC",
                Description = $"Solar Heat Gain Coefficient (SHGC) \u2264 {spec.MaxShgc:F2} \u2014 SANS 10400-XA Table 6",
                ProvidedValue = 0,
                RequiredValue = spec.MaxShgc,
                Unit = "SHGC",
                Passed = true,
            });
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-SHGC",
                Message = $"Solar Heat Gain Coefficient not supplied. Zone {result.ClimateZone} allows a maximum SHGC of "
                          + $"{spec.MaxShgc:F2} (SANS 10400-XA Table 6). "
                          + "Clear single glass (SHGC \u2248 0.86) will not comply in any zone. "
                          + "Specify a low-e or tinted product with declared SHGC \u2264 " + $"{spec.MaxShgc:F2}.",
                ClauseReference = "SANS 10400-XA Table 6",
                Severity = ViolationSeverity.Advisory,
            });
            return;
        }

        bool passed = p.SolarHeatGainCoefficient <= spec.MaxShgc;
        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-XA-SHGC",
            Description = $"Solar Heat Gain Coefficient (SHGC) \u2264 {spec.MaxShgc:F2} \u2014 SANS 10400-XA Table 6",
            ProvidedValue = p.SolarHeatGainCoefficient,
            RequiredValue = spec.MaxShgc,
            Unit = "SHGC",
            Passed = passed,
        });

        if (!passed)
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-SHGC",
                Message = $"Glazing SHGC of {p.SolarHeatGainCoefficient:F2} exceeds the Zone {result.ClimateZone} "
                          + $"limit of {spec.MaxShgc:F2}. Specify low-e, tinted, or coated glass "
                          + "with a declared SHGC at or below the limit.",
                ClauseReference = "SANS 10400-XA Table 6",
                Severity = ViolationSeverity.NonCompliant,
            });
        }
    }

    private static void EvaluateLightingPowerDensity(
        XaEnergyResult result, XaEnergyParameters p)
    {
        if (!MaxLpd.TryGetValue(p.OccupancyType, out double maxLpd)) return;

        if (p.LightingPowerDensityWPerM2 <= 0)
        {
            result.Checks.Add(new ComplianceCheck
            {
                Rule = "SANS10400-XA-LPD",
                Description = $"Lighting Power Density ≤ {maxLpd} W/m² for {p.OccupancyType} — SANS 10400-XA Table 7",
                ProvidedValue = 0,
                RequiredValue = maxLpd,
                Unit = "W/m²",
                Passed = true,
            });
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-LPD",
                Message = $"Lighting Power Density not supplied. Target for {p.OccupancyType} is ≤ {maxLpd} W/m² " +
                          "(SANS 10400-XA Table 7). Confirm with electrical engineer.",
                ClauseReference = "SANS 10400-XA Table 7",
                Severity = ViolationSeverity.Advisory,
            });
            return;
        }

        bool passed = p.LightingPowerDensityWPerM2 <= maxLpd;
        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-XA-LPD",
            Description = $"Lighting Power Density ≤ {maxLpd} W/m² for {p.OccupancyType} — SANS 10400-XA Table 7",
            ProvidedValue = p.LightingPowerDensityWPerM2,
            RequiredValue = maxLpd,
            Unit = "W/m²",
            Passed = passed,
        });

        if (!passed)
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-LPD",
                Message = $"Lighting Power Density of {p.LightingPowerDensityWPerM2} W/m² exceeds the " +
                          $"{p.OccupancyType} limit of {maxLpd} W/m². " +
                          "Specify energy-efficient luminaires or reduce fixture density.",
                ClauseReference = "SANS 10400-XA Table 7",
                Severity = ViolationSeverity.NonCompliant,
            });
        }
    }

    private static void EvaluateBuildingCompactness(
        XaEnergyResult result, XaEnergyParameters p, double wallArea, double roofArea)
    {
        if (result.SurfaceToVolumeRatio is not { } svr || svr <= 0) return;

        // Indicative target: compact buildings have S/V < 0.5 m⁻¹.
        // Very fragmented plans (S/V > 1.0) lose significant heat/cool.
        const double GoodThreshold = 0.5;
        const double PoorThreshold = 1.0;

        bool passed = svr <= PoorThreshold;

        result.Checks.Add(new ComplianceCheck
        {
            Rule = "SANS10400-XA-Compactness",
            Description = "Building envelope compactness (Surface-to-Volume ratio) — thermal performance advisory",
            ProvidedValue = Math.Round(svr, 3),
            RequiredValue = PoorThreshold,
            Unit = "m⁻¹",
            Passed = passed,
        });

        if (!passed)
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-Compactness",
                Message = $"Surface-to-Volume ratio of {svr:F3} m⁻¹ indicates a highly fragmented envelope, " +
                          "which significantly increases energy demand. Consider a more compact building form.",
                ClauseReference = "SANS 10400-XA (energy efficiency principle)",
                Severity = ViolationSeverity.Warning,
            });
        }
        else if (svr > GoodThreshold)
        {
            result.Violations.Add(new ComplianceViolation
            {
                Rule = "SANS10400-XA-Compactness",
                Message = $"Surface-to-Volume ratio of {svr:F3} m⁻¹ is acceptable but could be improved. " +
                          "A ratio ≤ 0.5 m⁻¹ is considered thermally compact.",
                ClauseReference = "SANS 10400-XA (energy efficiency principle)",
                Severity = ViolationSeverity.Advisory,
            });
        }
    }

    // ── Energy rating ─────────────────────────────────────────────────────────

    /// <summary>
    /// Derives an A–F energy rating from the ratio of passed checks that are
    /// not merely advisory (i.e. have measured input values).
    /// </summary>
    private static string DeriveEnergyRating(XaEnergyResult result)
    {
        var nonAdvisoryViolations = result.Violations
            .Count(v => v.Severity == ViolationSeverity.NonCompliant);

        int total = result.Checks.Count;
        int passed = result.Checks.Count(c => c.Passed);
        if (total == 0) return "–";

        double score = (double)passed / total;

        return (score, nonAdvisoryViolations) switch
        {
            (_, > 2) => "F",
            (_, > 0) when score < 0.6 => "E",
            (_, > 0) => "D",
            ( >= 0.95, 0) => "A",
            ( >= 0.85, 0) => "B",
            ( >= 0.70, 0) => "C",
            _ => "D",
        };
    }

    // ── Internal value object ─────────────────────────────────────────────────

    private record ClimateZoneSpec(
        string Name,
        double MaxWwr,
        double MinRoofR,
        double MinWallR,
        double MaxGlazingU,
        double MaxShgc);
}
