using ArchAutomate.CAD.Generators;
using ArchAutomate.CAD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CouncilPackController(CouncilTableGenerator generator) : ControllerBase
{
    [HttpPost("generate-tables")]
    public IActionResult GenerateTables([FromBody] CouncilTableData data)
    {
        byte[] dxfBytes = generator.Generate(data);

        string fileName = $"CouncilTables_{data.DrawingNumber}_{data.Revision}.dxf";
        return File(dxfBytes, "application/dxf", fileName);
    }
}
