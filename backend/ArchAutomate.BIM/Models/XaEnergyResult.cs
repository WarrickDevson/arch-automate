namespace ArchAutomate.BIM.Models;

/// <summary>
/// Detailed energy compliance result for SANS 10400-XA.
/// Extends the standard <see cref="ComplianceResult"/> with XA-specific metrics.
/// </summary>
public class XaEnergyResult : ComplianceResult
{
    /// <summary>Resolved SANS 10400-XA climate zone (1–6).</summary>
    public int ClimateZone { get; set; }

    /// <summary>Human-readable climate zone name.</summary>
    public string ClimateZoneName { get; set; } = string.Empty;

    /// <summary>
    /// Calculated window-to-wall ratio as a percentage [0–100].
    /// Null when insufficient data was supplied.
    /// </summary>
    public double? WindowToWallRatioPercent { get; set; }

    /// <summary>
    /// Calculated daylighting ratio: total window area / GFA as a percentage.
    /// </summary>
    public double? DaylightingRatioPercent { get; set; }

    /// <summary>
    /// Estimated surface-to-volume ratio in m⁻¹.
    /// Lower values indicate a more thermally compact building envelope.
    /// </summary>
    public double? SurfaceToVolumeRatio { get; set; }

    /// <summary>Required minimum roof R-value for the climate zone in m²·K/W.</summary>
    public double RequiredRoofRValue { get; set; }

    /// <summary>Required minimum wall R-value for the climate zone in m²·K/W.</summary>
    public double RequiredWallRValue { get; set; }

    /// <summary>Maximum permitted glazing U-value for the climate zone in W/(m²·K).</summary>
    public double MaxGlazingUValue { get; set; }

    /// <summary>Maximum permitted window-to-wall ratio for the climate zone (%).</summary>
    public double MaxWindowToWallRatioPercent { get; set; }

    /// <summary>Maximum permitted Solar Heat Gain Coefficient (SHGC) for the climate zone.</summary>
    public double MaxSolarHeatGainCoefficient { get; set; }

    /// <summary>
    /// Overall SANS 10400-XA energy rating letter derived from the compliance score.
    /// A = excellent … F = non-compliant.
    /// </summary>
    public string EnergyRating { get; set; } = "–";

    /// <summary>
    /// Maximum permitted lighting power density for the occupancy type (W/m²).
    /// Null when occupancy has no defined target.
    /// </summary>
    public double? MaxLightingPowerDensityWPerM2 { get; set; }
}
