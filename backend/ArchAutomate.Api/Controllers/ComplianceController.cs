using ArchAutomate.BIM.Engines;
using ArchAutomate.BIM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ComplianceController : ControllerBase
{
    private readonly ZoningEngine _zoningEngine;
    private readonly Sans10400Engine _sans10400Engine;
    private readonly XaEnergyEngine _xaEnergyEngine;

    public ComplianceController(
        ZoningEngine zoningEngine,
        Sans10400Engine sans10400Engine,
        XaEnergyEngine xaEnergyEngine)
    {
        _zoningEngine = zoningEngine;
        _sans10400Engine = sans10400Engine;
        _xaEnergyEngine = xaEnergyEngine;
    }

    [HttpPost("evaluate")]
    public IActionResult Evaluate([FromBody] ZoningParameters parameters)
    {
        var zoningResult = _zoningEngine.Evaluate(parameters);
        var sansResult = _sans10400Engine.Evaluate(parameters);

        // Merge checks and violations
        var combined = new ComplianceResult
        {
            SiteCoveragePercent = zoningResult.SiteCoveragePercent,
            FloorAreaRatio = zoningResult.FloorAreaRatio,
            ParkingRatio = sansResult.ParkingRatio
        };

        combined.Checks.AddRange(zoningResult.Checks);
        combined.Checks.AddRange(sansResult.Checks);
        combined.Violations.AddRange(zoningResult.Violations);
        combined.Violations.AddRange(sansResult.Violations);

        return Ok(combined);
    }

    [HttpPost("zoning")]
    public IActionResult EvaluateZoning([FromBody] ZoningParameters parameters)
    {
        var result = _zoningEngine.Evaluate(parameters);
        return Ok(result);
    }

    [HttpPost("sans10400")]
    public IActionResult EvaluateSans10400([FromBody] ZoningParameters parameters)
    {
        var result = _sans10400Engine.Evaluate(parameters);
        return Ok(result);
    }

    /// <summary>
    /// Evaluates SANS 10400-XA (Energy Usage in Buildings) compliance.
    /// Accepts envelope geometry, window counts, and optional thermal property values.
    /// Returns an <see cref="XaEnergyResult"/> with per-check detail and an A–F energy rating.
    /// </summary>
    [HttpPost("xa")]
    public IActionResult EvaluateEnergy([FromBody] XaEnergyParameters parameters)
    {
        var result = _xaEnergyEngine.Evaluate(parameters);
        return Ok(result);
    }
}
