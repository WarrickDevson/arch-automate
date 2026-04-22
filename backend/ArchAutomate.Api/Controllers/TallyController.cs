using System.Security.Claims;
using System.Text.Json;
using ArchAutomate.Data;
using ArchAutomate.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TallyController : ControllerBase
{
    private readonly AppDbContext _db;

    public TallyController(AppDbContext db)
    {
        _db = db;
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    public record TallyItemDto(
        int LocalId,
        string Category,
        string IfcType,
        string Mark,
        string Name,
        string Type,
        string Level);

    public record SaveTallyRequest(List<TallyItemDto> Items);

    // ── GET /api/tally/{projectId} ────────────────────────────────────────────

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);

        var row = await _db.ProjectTallies
            .Where(t => t.ProjectId == projectId && t.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

        if (row is null) return NotFound(new { message = "No tally found for this project." });

        var items = JsonSerializer.Deserialize<List<TallyItemDto>>(row.TallyJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];

        return Ok(new
        {
            projectId,
            extractedAt = row.ExtractedAt,
            lightingCount = row.LightingCount,
            electricalCount = row.ElectricalCount,
            sanitaryCount = row.SanitaryCount,
            hvacCount = row.HvacCount,
            fireCount = row.FireCount,
            otherCount = row.OtherCount,
            totalCount = row.TotalCount,
            items,
        });
    }

    // ── POST /api/tally/{projectId} ───────────────────────────────────────────

    [HttpPost("{projectId:guid}")]
    public async Task<IActionResult> Upsert(Guid projectId, [FromBody] SaveTallyRequest request, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);

        // Verify the project belongs to this tenant
        var projectExists = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.TenantId == tenantId, ct);
        if (!projectExists) return NotFound(new { message = "Project not found." });

        var items = request.Items ?? [];

        int Count(string category) => items.Count(i =>
            string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase));

        int lightingCount = Count("Lighting");
        int electricalCount = Count("Electrical");
        int sanitaryCount = Count("Sanitary");
        int hvacCount = Count("HVAC");
        int fireCount = Count("Fire");
        int otherCount = items.Count - lightingCount - electricalCount - sanitaryCount - hvacCount - fireCount;

        var tallyJson = JsonSerializer.Serialize(items,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var existing = await _db.ProjectTallies
            .FirstOrDefaultAsync(t => t.ProjectId == projectId && t.TenantId == tenantId, ct);

        if (existing is null)
        {
            existing = new ProjectTally
            {
                ProjectId = projectId,
                TenantId = tenantId,
            };
            _db.ProjectTallies.Add(existing);
        }

        existing.ExtractedAt = DateTime.UtcNow;
        existing.TallyJson = tallyJson;
        existing.LightingCount = lightingCount;
        existing.ElectricalCount = electricalCount;
        existing.SanitaryCount = sanitaryCount;
        existing.HvacCount = hvacCount;
        existing.FireCount = fireCount;
        existing.OtherCount = Math.Max(0, otherCount);
        existing.TotalCount = items.Count;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            projectId,
            extractedAt = existing.ExtractedAt,
            lightingCount = existing.LightingCount,
            electricalCount = existing.ElectricalCount,
            sanitaryCount = existing.SanitaryCount,
            hvacCount = existing.HvacCount,
            fireCount = existing.FireCount,
            otherCount = existing.OtherCount,
            totalCount = existing.TotalCount,
            items,
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        var claim = User.FindFirstValue("tenant_id")
                 ?? User.FindFirstValue("tenantId")
                 ?? User.FindFirstValue("https://arch-automate.app/tenant_id");

        if (Guid.TryParse(claim, out var fromClaim))
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
