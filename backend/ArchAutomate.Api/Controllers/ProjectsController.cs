using ArchAutomate.Data;
using ArchAutomate.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var projects = await db.Projects
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Status,
                p.Municipality,
                p.Erf,
                p.SiteAreaM2,
                p.ZoningScheme,
                p.IfcPath,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var project = await db.Projects
            .Include(p => p.Stakeholders)
            .Include(p => p.RejectionComments)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var project = new Project
        {
            TenantId = await GetTenantIdAsync(ct),
            OwnerUserId = GetUserId(),
            Name = request.Name,
            Description = request.Description,
            Municipality = request.Municipality,
            Erf = request.Erf,
            SiteAreaM2 = request.SiteAreaM2,
            ZoningScheme = request.ZoningScheme
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();

        project.Name = request.Name;
        project.Description = request.Description;
        project.Municipality = request.Municipality;
        project.Erf = request.Erf;
        project.SiteAreaM2 = request.SiteAreaM2;
        project.ZoningScheme = request.ZoningScheme;
        project.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(project);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();

        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Called by the frontend after a successful Supabase Storage upload.
    /// Stores the storage path so other devices / sessions can restore the model.
    /// </summary>
    [HttpPatch("{id:guid}/ifc-path")]
    public async Task<IActionResult> SetIfcPath(Guid id, [FromBody] SetIfcPathRequest request, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();

        project.IfcPath = request.IfcPath;
        project.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { project.IfcPath });
    }

    // Prefers the `tenant_id` custom claim added by the Supabase JWT hook.
    // Falls back to a DB lookup on public.profiles when the hook is not yet enabled.
    private async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        var raw = User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var fromClaim))
            return fromClaim;

        // JWT hook not enabled — resolve from profiles table
        var userId = GetUserId();
        var tenantId = await db.Database
            .SqlQuery<Guid>($"SELECT tenant_id AS \"Value\" FROM public.profiles WHERE id = {userId} LIMIT 1")
            .FirstOrDefaultAsync(ct);

        if (tenantId == Guid.Empty)
            throw new UnauthorizedAccessException("No tenant found for this user. Complete onboarding first.");

        return tenantId;
    }

    private Guid GetUserId()
    {
        var raw = GetUserIdRaw();
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException("sub claim is not a valid GUID.");
    }

    private string GetUserIdRaw() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("sub claim missing.");
}

public record CreateProjectRequest(
    string Name,
    string Description,
    string Municipality,
    string Erf,
    double SiteAreaM2,
    string ZoningScheme);

public record SetIfcPathRequest(string? IfcPath);
