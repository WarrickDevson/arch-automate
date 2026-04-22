using ArchAutomate.BIM.Engines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ZoningSchemesController : ControllerBase
{
    /// <summary>
    /// Returns all known South African zoning designations with their permitted limits.
    /// Use this to populate dropdowns and to display permitted values in the UI before
    /// a full compliance evaluation is run.
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        var schemes = ZoningEngine.GetKnownSchemes()
            .Select(kvp => new
            {
                name = kvp.Key,
                description = kvp.Value.Description,
                maxCoveragePercent = kvp.Value.MaxCoverage * 100,
                maxFar = kvp.Value.MaxFar,
                maxHeightM = kvp.Value.MaxHeightM,
                frontSetbackM = kvp.Value.FrontSetbackM,
                rearSetbackM = kvp.Value.RearSetbackM,
                sideSetbackM = kvp.Value.SideSetbackM,
            })
            .OrderBy(s => s.name)
            .ToList();

        return Ok(schemes);
    }

    /// <summary>Returns limits for a single zoning scheme by name.</summary>
    [HttpGet("{name}")]
    public IActionResult GetByName(string name)
    {
        var schemes = ZoningEngine.GetKnownSchemes();
        if (!schemes.TryGetValue(name, out var r))
            return NotFound(new { message = $"Zoning scheme '{name}' is not recognised." });

        return Ok(new
        {
            name,
            description = r.Description,
            maxCoveragePercent = r.MaxCoverage * 100,
            maxFar = r.MaxFar,
            maxHeightM = r.MaxHeightM,
            frontSetbackM = r.FrontSetbackM,
            rearSetbackM = r.RearSetbackM,
            sideSetbackM = r.SideSetbackM,
        });
    }
}
