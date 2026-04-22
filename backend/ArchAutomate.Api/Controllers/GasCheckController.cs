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
public class GasCheckController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GasCheckEngine _engine;

    public GasCheckController(AppDbContext db, GasCheckEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // ── GET /api/gascheck/{projectId} ────────────────────────────────────────

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);

        var row = await _db.ProjectGasChecks
            .Where(g => g.ProjectId == projectId && g.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

        if (row is null) return NotFound(new { message = "No gas check found for this project." });

        var input = JsonSerializer.Deserialize<GasCheckRequest>(row.InputJson, JsonOpts);
        var results = JsonSerializer.Deserialize<GasCheckResult>(row.ResultsJson, JsonOpts);

        return Ok(new
        {
            projectId,
            checkedAt = row.CheckedAt,
            overallPass = row.OverallPass,
            hasGasInstallation = row.HasGasInstallation,
            input,
            results,
        });
    }

    // ── POST /api/gascheck/{projectId} ───────────────────────────────────────

    [HttpPost("{projectId:guid}")]
    public async Task<IActionResult> Evaluate(Guid projectId, [FromBody] GasCheckRequest request, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);

        var projectExists = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.TenantId == tenantId, ct);
        if (!projectExists) return NotFound(new { message = "Project not found." });

        var result = _engine.Evaluate(request);

        var inputJson = JsonSerializer.Serialize(request, JsonOpts);
        var resultsJson = JsonSerializer.Serialize(result, JsonOpts);

        var row = await _db.ProjectGasChecks
            .Where(g => g.ProjectId == projectId && g.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

        if (row is null)
        {
            row = new ProjectGasCheck
            {
                ProjectId = projectId,
                TenantId = tenantId,
            };
            _db.ProjectGasChecks.Add(row);
        }

        row.CheckedAt = DateTime.UtcNow;
        row.InputJson = inputJson;
        row.ResultsJson = resultsJson;
        row.OverallPass = result.OverallPass;
        row.HasGasInstallation = request.HasGasInstallation;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            projectId,
            checkedAt = row.CheckedAt,
            overallPass = result.OverallPass,
            hasGasInstallation = request.HasGasInstallation,
            input = request,
            results = result,
        });
    }

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
