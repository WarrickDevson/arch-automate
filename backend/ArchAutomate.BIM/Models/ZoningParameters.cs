namespace ArchAutomate.BIM.Models;

/// <summary>
/// Input parameters for SANS 10400 and municipal zoning compliance calculations.
/// </summary>
public class ZoningParameters
{
    /// <summary>Site area in square metres.</summary>
    public double SiteAreaM2 { get; set; }

    /// <summary>Total gross floor area of proposed building(s) in m².</summary>
    public double ProposedGfaM2 { get; set; }

    /// <summary>Building footprint (ground coverage) in m².</summary>
    public double FootprintM2 { get; set; }

    /// <summary>Number of storeys above natural ground level.</summary>
    public int NumberOfStoreys { get; set; }

    /// <summary>Height to top of roof structure in metres.</summary>
    public double BuildingHeightM { get; set; }

    /// <summary>Proposed front building line setback in metres.</summary>
    public double FrontSetbackM { get; set; }

    /// <summary>Proposed rear building line setback in metres.</summary>
    public double RearSetbackM { get; set; }

    /// <summary>Proposed side building line setback in metres (smallest).</summary>
    public double SideSetbackM { get; set; }

    /// <summary>Municipal zoning designation, e.g. "Residential 1", "General Residential 2".</summary>
    public string ZoningScheme { get; set; } = string.Empty;

    /// <summary>Number of parking bays provided.</summary>
    public int ParkingBaysProvided { get; set; }

    /// <summary>Gross lettable area for parking ratio calculation (m²).</summary>
    public double GlaForParkingM2 { get; set; }
}
