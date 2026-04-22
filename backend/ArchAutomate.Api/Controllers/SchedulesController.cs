using System.Text.Json;
using ArchAutomate.Data;
using ArchAutomate.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Api.Controllers;

/// <summary>
/// Manages IFC-extracted door and window schedules for a project.
/// Schedules are extracted client-side by the 3D viewer and pushed here for persistence.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SchedulesController(AppDbContext db)
    {
        _db = db;
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Returns the saved schedule for a project, or 404 if none has been extracted yet.
    /// </summary>
    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);

        var schedule = await _db.ProjectSchedules
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.TenantId == tenantId, ct);

        if (schedule is null) return NotFound();

        return Ok(ToDto(schedule));
    }

    /// <summary>
    /// Upserts (insert or replace) the schedule for a project.
    /// Called automatically by the frontend whenever a new IFC file is loaded.
    /// </summary>
    [HttpPost("{projectId:guid}")]
    public async Task<IActionResult> Save(Guid projectId, [FromBody] SaveScheduleRequest request, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);

        // Validate the project belongs to this tenant
        var projectExists = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.TenantId == tenantId, ct);

        if (!projectExists) return NotFound();

        var doorJson = JsonSerializer.Serialize(request.Doors, _jsonOptions);
        var windowJson = JsonSerializer.Serialize(request.Windows, _jsonOptions);

        var existing = await _db.ProjectSchedules
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.TenantId == tenantId, ct);

        if (existing is null)
        {
            existing = new ProjectSchedule
            {
                ProjectId = projectId,
                TenantId = tenantId,
            };
            _db.ProjectSchedules.Add(existing);
        }

        existing.DoorScheduleJson = doorJson;
        existing.WindowScheduleJson = windowJson;
        existing.DoorCount = request.Doors.Count();
        existing.WindowCount = request.Windows.Count();
        existing.ExtractedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(existing));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ScheduleDto ToDto(ProjectSchedule s)
    {
        var doors = JsonSerializer.Deserialize<List<ScheduleItemDto>>(
            s.DoorScheduleJson, _jsonOptions) ?? [];
        var windows = JsonSerializer.Deserialize<List<ScheduleItemDto>>(
            s.WindowScheduleJson, _jsonOptions) ?? [];

        return new ScheduleDto(
            s.ProjectId,
            s.ExtractedAt,
            doors,
            windows,
            s.DoorCount,
            s.WindowCount);
    }

    private async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        var raw = User.FindFirst("tenant_id")?.Value;
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
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("sub claim missing.");

        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException("sub claim is not a valid GUID.");
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>A single row in a door or window schedule.</summary>
public record ScheduleItemDto(
    int LocalId,
    string Mark,
    string Name,
    string Type,
    int WidthMm,
    int HeightMm,
    double AreaM2,
    string Level);

/// <summary>Full schedule response for a project.</summary>
public record ScheduleDto(
    Guid ProjectId,
    DateTime ExtractedAt,
    List<ScheduleItemDto> Doors,
    List<ScheduleItemDto> Windows,
    int DoorCount,
    int WindowCount);

/// <summary>Request body for saving a schedule.</summary>
public record SaveScheduleRequest(
    IEnumerable<ScheduleItemDto> Doors,
    IEnumerable<ScheduleItemDto> Windows);
