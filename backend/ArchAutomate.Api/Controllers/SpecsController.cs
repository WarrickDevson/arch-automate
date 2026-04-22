using System.Security.Claims;
using System.Text.Json;
using ArchAutomate.BIM.Engines;
using ArchAutomate.BIM.Models;
using ArchAutomate.Data;
using ArchAutomate.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SpecsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SpecEngine _specEngine;

    public SpecsController(AppDbContext db, SpecEngine specEngine)
    {
        _db = db;
        _specEngine = specEngine;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ── POST /api/specs/{projectId}/compile ───────────────────────────────────

    [HttpPost("{projectId:guid}/compile")]
    public async Task<IActionResult> Compile(Guid projectId, [FromBody] SpecCompileRequest request, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);

        // Ensure the project belongs to this tenant
        var projectExists = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.TenantId == tenantId, ct);
        if (!projectExists) return NotFound(new { message = "Project not found." });

        var compiled = _specEngine.Compile(request);

        var specJson = JsonSerializer.Serialize(compiled, JsonOpts);
        var materialsJson = JsonSerializer.Serialize(request.MaterialStrings, JsonOpts);

        var existing = await _db.ProjectSpecs
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.TenantId == tenantId, ct);

        if (existing is null)
        {
            _db.ProjectSpecs.Add(new ProjectSpec
            {
                ProjectId = projectId,
                TenantId = tenantId,
                ExtractedAt = DateTime.UtcNow,
                CompiledAt = DateTime.UtcNow,
                MaterialsJson = materialsJson,
                SpecJson = specJson,
                ClauseCount = compiled.ClauseCount,
            });
        }
        else
        {
            existing.ExtractedAt = DateTime.UtcNow;
            existing.CompiledAt = DateTime.UtcNow;
            existing.MaterialsJson = materialsJson;
            existing.SpecJson = specJson;
            existing.ClauseCount = compiled.ClauseCount;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(compiled);
    }

    // ── GET /api/specs/{projectId} ────────────────────────────────────────────

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);

        var row = await _db.ProjectSpecs
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.TenantId == tenantId, ct);

        if (row is null) return NotFound(new { message = "No specification found for this project." });

        CompiledSpec? spec;
        try
        {
            spec = JsonSerializer.Deserialize<CompiledSpec>(row.SpecJson, JsonOpts);
        }
        catch
        {
            spec = null;
        }

        return Ok(new
        {
            projectId,
            compiledAt = row.CompiledAt,
            extractedAt = row.ExtractedAt,
            clauseCount = row.ClauseCount,
            spec,
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        var raw = User.FindFirstValue("tenant_id")
               ?? User.FindFirstValue("tenantId")
               ?? User.FindFirstValue("https://arch-automate.app/tenant_id");

        if (!string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var fromClaim))
            return fromClaim;

        var userId = GetUserId();
        var tenantId = await _db.Database
            .SqlQuery<Guid>($"SELECT tenant_id AS \"Value\" FROM public.profiles WHERE id = {userId} LIMIT 1")
            .FirstOrDefaultAsync(ct);

        if (tenantId == Guid.Empty)
            throw new UnauthorizedAccessException("No tenant found for this user.");

        return tenantId;
    }

    private Guid GetUserId()
    {
        var raw =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("sub claim missing.");

        return Guid.TryParse(raw, out var id)
            ? id
            : throw new UnauthorizedAccessException("sub claim is not a valid GUID.");
    }
}
