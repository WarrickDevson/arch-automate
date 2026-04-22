namespace ArchAutomate.BIM.Models;

/// <summary>
/// Input parameters for SANS 10400-XA (Energy Usage in Buildings) compliance evaluation.
/// </summary>
public class XaEnergyParameters
{
    /// <summary>
    /// SANS 10400-XA climate zone (1–6). Derived from province/municipality when not supplied.
    /// 1 = Cape coastal  2 = Highveld  3 = Northern interior  4 = Eastern coastal
    /// 5 = Semi-arid interior  6 = Arid interior (Karoo/Kalahari).
    /// </summary>
    public int ClimateZone { get; set; } = 1;

    /// <summary>Broad occupancy classification that drives energy targets.</summary>
    public OccupancyType OccupancyType { get; set; } = OccupancyType.Residential;

    /// <summary>Gross floor area of the building in m².</summary>
    public double ProposedGfaM2 { get; set; }

    /// <summary>Building footprint (ground coverage) in m².</summary>
    public double FootprintM2 { get; set; }

    /// <summary>Overall building height in metres.</summary>
    public double BuildingHeightM { get; set; }

    /// <summary>Number of storeys above ground level.</summary>
    public int NumberOfStoreys { get; set; } = 1;

    // ── Envelope areas ──────────────────────────────────────────────────────

    /// <summary>
    /// Total opaque external wall area in m².
    /// When zero the engine estimates it from footprint, height and a square-plan assumption.
    /// </summary>
    public double GrossWallAreaM2 { get; set; }

    /// <summary>
    /// Total glazed (window + door-light) area in m².
    /// When zero the engine falls back to <see cref="WindowCount"/> × typical single-window area.
    /// </summary>
    public double GrossWindowAreaM2 { get; set; }

    /// <summary>
    /// Number of windows extracted from the IFC model.
    /// Used as a fallback when <see cref="GrossWindowAreaM2"/> is not available.
    /// </summary>
    public int WindowCount { get; set; }

    /// <summary>Roof/ceiling area in m². When zero, the footprint is used.</summary>
    public double RoofAreaM2 { get; set; }

    // ── Thermal properties (optional – drives advisory checks) ───────────────

    /// <summary>
    /// Declared roof assembly R-value in m²·K/W.
    /// 0 means "not provided" – the check becomes advisory only.
    /// </summary>
    public double RoofRValue { get; set; }

    /// <summary>
    /// Declared external wall assembly R-value in m²·K/W.
    /// 0 means "not provided".
    /// </summary>
    public double WallRValue { get; set; }

    /// <summary>
    /// Declared glazing U-value in W/(m²·K).
    /// 0 means "not provided".
    /// </summary>
    public double GlazingUValue { get; set; }

    // ── Mechanical systems ───────────────────────────────────────────────────

    /// <summary>Installed artificial lighting power density in W/m². 0 = not provided.</summary>
    public double LightingPowerDensityWPerM2 { get; set; }

    /// <summary>
    /// Declared overall glazing Solar Heat Gain Coefficient (SHGC / g-value).
    /// 0 means "not provided". Zone limit per SANS 10400-XA Table 6.
    /// Typical values: clear single glass ≈ 0.86, low-e double ≈ 0.30–0.45.
    /// </summary>
    public double SolarHeatGainCoefficient { get; set; }

    /// <summary>Province name used for automatic climate-zone resolution when ClimateZone == 0.</summary>
    public string Province { get; set; } = string.Empty;
}

/// <summary>Broad occupancy classification for SANS 10400-XA energy targets.</summary>
public enum OccupancyType
{
    Residential,
    Office,
    Retail,
    Industrial,
    Educational,
    Healthcare,
}
