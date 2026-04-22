using ArchAutomate.CAD.Generators;
using ArchAutomate.CAD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CouncilPackController : ControllerBase
{
    private readonly CouncilTableGenerator _generator;

    public CouncilPackController(CouncilTableGenerator generator)
    {
        _generator = generator;
    }

    [HttpPost("generate-tables")]
    public IActionResult GenerateTables([FromBody] CouncilTableData data)
    {
        byte[] dxfBytes = _generator.Generate(data);

        string fileName = $"CouncilTables_{data.DrawingNumber}_{data.Revision}.dxf";
        return File(dxfBytes, "application/dxf", fileName);
    }
}
