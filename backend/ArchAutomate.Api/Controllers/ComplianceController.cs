using ArchAutomate.BIM.Engines;
using ArchAutomate.BIM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ComplianceController(ZoningEngine zoningEngine, Sans10400Engine sans10400Engine) : ControllerBase
{
    [HttpPost("evaluate")]
    public IActionResult Evaluate([FromBody] ZoningParameters parameters)
    {
        var zoningResult = zoningEngine.Evaluate(parameters);
        var sansResult = sans10400Engine.Evaluate(parameters);

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
        var result = zoningEngine.Evaluate(parameters);
        return Ok(result);
    }

    [HttpPost("sans10400")]
    public IActionResult EvaluateSans10400([FromBody] ZoningParameters parameters)
    {
        var result = sans10400Engine.Evaluate(parameters);
        return Ok(result);
    }
}
