using ArchAutomate.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MunicipalitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MunicipalitiesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns all municipalities ordered by name for use in dropdowns.
    /// Includes the province name from the provinces reference table.
    /// The list is small (~250 rows) and read-only reference data, so no pagination is needed.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var municipalities = await _db.Database
            .SqlQuery<MunicipalityDto>($"""
                SELECT m.id       AS "Id",
                       m.name     AS "Name",
                       m.short_name AS "ShortName",
                       m.category AS "Category",
                       m.zoning_scheme AS "ZoningScheme",
                       m.province_id   AS "ProvinceId",
                       COALESCE(p.name, '') AS "ProvinceName"
                FROM   municipalities m
                LEFT JOIN provinces p ON p.id = m.province_id
                ORDER  BY m.name
                """)
            .ToListAsync(ct);

        return Ok(municipalities);
    }
}

/// <summary>Flat DTO for the municipalities list endpoint.</summary>
public record MunicipalityDto(
    short Id,
    string Name,
    string ShortName,
    string Category,
    string ZoningScheme,
    short ProvinceId,
    string ProvinceName);
