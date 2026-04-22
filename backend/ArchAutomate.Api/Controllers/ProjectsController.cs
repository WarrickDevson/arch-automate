using ArchAutomate.Data;
using ArchAutomate.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProjectsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var projects = await _db.Projects
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Status,
                p.Municipality,
                p.MunicipalityId,
                p.Erf,
                p.Address,
                p.SiteAreaM2,
                p.ZoningScheme,
                p.ProposedGfaM2,
                p.FootprintM2,
                p.NumberOfStoreys,
                p.BuildingHeightM,
                p.FrontSetbackM,
                p.RearSetbackM,
                p.SideSetbackM,
                p.ParkingBays,
                p.GlaForParkingM2,
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
        var project = await _db.Projects
            .Include(p => p.Stakeholders)
            .Include(p => p.RejectionComments)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var errors = ValidateProject(request.Name, request.Municipality, request.Erf, request.SiteAreaM2, request.ZoningScheme);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var municipalityId = await ResolveMunicipalityIdAsync(request.Municipality, ct);

        var project = new Project
        {
            TenantId = await GetTenantIdAsync(ct),
            OwnerUserId = GetUserId(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Municipality = request.Municipality.Trim(),
            MunicipalityId = municipalityId,
            Address = request.Address?.Trim() ?? string.Empty,
            Erf = request.Erf.Trim(),
            SiteAreaM2 = request.SiteAreaM2,
            ZoningScheme = request.ZoningScheme,
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken ct)
    {
        var errors = ValidateProject(request.Name, request.Municipality, request.Erf, request.SiteAreaM2, request.ZoningScheme);
        if (request.ProposedGfaM2 < 0) errors["proposedGfaM2"] = ["Must be 0 or greater."];
        if (request.FootprintM2 < 0) errors["footprintM2"] = ["Must be 0 or greater."];
        if (request.BuildingHeightM < 0) errors["buildingHeightM"] = ["Must be 0 or greater."];
        if (request.NumberOfStoreys is < 1) errors["numberOfStoreys"] = ["Must be at least 1."];
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var tenantId = await GetTenantIdAsync(ct);
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim() ?? string.Empty;
        project.Municipality = request.Municipality.Trim();
        project.MunicipalityId = await ResolveMunicipalityIdAsync(request.Municipality, ct);
        project.Address = request.Address?.Trim() ?? string.Empty;
        project.Erf = request.Erf.Trim();
        project.SiteAreaM2 = request.SiteAreaM2;
        project.ZoningScheme = request.ZoningScheme;
        project.ProposedGfaM2 = request.ProposedGfaM2 ?? project.ProposedGfaM2;
        project.FootprintM2 = request.FootprintM2 ?? project.FootprintM2;
        project.NumberOfStoreys = request.NumberOfStoreys ?? project.NumberOfStoreys;
        project.BuildingHeightM = request.BuildingHeightM ?? project.BuildingHeightM;
        project.FrontSetbackM = request.FrontSetbackM ?? project.FrontSetbackM;
        project.RearSetbackM = request.RearSetbackM ?? project.RearSetbackM;
        project.SideSetbackM = request.SideSetbackM ?? project.SideSetbackM;
        project.ParkingBays = request.ParkingBays ?? project.ParkingBays;
        project.GlaForParkingM2 = request.GlaForParkingM2 ?? project.GlaForParkingM2;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(project);
    }

    /// <summary>
    /// Called automatically by the frontend after IFC parsing to persist
    /// the extracted dimensional data without user interaction.
    /// </summary>
    [HttpPatch("{id:guid}/ifc-data")]
    public async Task<IActionResult> SetIfcData(Guid id, [FromBody] IfcDataRequest request, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();

        if (request.ProposedGfaM2.HasValue) project.ProposedGfaM2 = request.ProposedGfaM2.Value;
        if (request.FootprintM2.HasValue) project.FootprintM2 = request.FootprintM2.Value;
        if (request.BuildingHeightM.HasValue) project.BuildingHeightM = request.BuildingHeightM.Value;
        if (request.NumberOfStoreys.HasValue) project.NumberOfStoreys = request.NumberOfStoreys.Value;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { project.ProposedGfaM2, project.FootprintM2, project.BuildingHeightM, project.NumberOfStoreys });
    }

    /// <summary>
    /// Saves the analysis parameters entered in the workbench sidebar
    /// (setbacks, parking, GFA override) without requiring the full project update form.
    /// </summary>
    [HttpPatch("{id:guid}/params")]
    public async Task<IActionResult> SetParams(Guid id, [FromBody] ParamsRequest request, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();

        if (request.ProposedGfaM2.HasValue) project.ProposedGfaM2 = request.ProposedGfaM2.Value;
        if (request.FootprintM2.HasValue) project.FootprintM2 = request.FootprintM2.Value;
        if (request.BuildingHeightM.HasValue) project.BuildingHeightM = request.BuildingHeightM.Value;
        if (request.NumberOfStoreys.HasValue) project.NumberOfStoreys = request.NumberOfStoreys.Value;
        if (request.FrontSetbackM.HasValue) project.FrontSetbackM = request.FrontSetbackM.Value;
        if (request.RearSetbackM.HasValue) project.RearSetbackM = request.RearSetbackM.Value;
        if (request.SideSetbackM.HasValue) project.SideSetbackM = request.SideSetbackM.Value;
        if (request.ParkingBays.HasValue) project.ParkingBays = request.ParkingBays.Value;
        if (request.GlaForParkingM2.HasValue) project.GlaForParkingM2 = request.GlaForParkingM2.Value;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new
        {
            project.ProposedGfaM2,
            project.FootprintM2,
            project.BuildingHeightM,
            project.NumberOfStoreys,
            project.FrontSetbackM,
            project.RearSetbackM,
            project.SideSetbackM,
            project.ParkingBays,
            project.GlaForParkingM2,
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);

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
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (project is null) return NotFound();

        project.IfcPath = request.IfcPath;
        project.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

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
        var tenantId = await _db.Database
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

    private static Dictionary<string, string[]> ValidateProject(
        string name, string municipality, string erf, double siteAreaM2, string zoningScheme)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = ["Project name is required."];
        else if (name.Length > 200) errors["name"] = ["Project name must be 200 characters or fewer."];
        if (string.IsNullOrWhiteSpace(municipality)) errors["municipality"] = ["Municipality is required."];
        if (string.IsNullOrWhiteSpace(erf)) errors["erf"] = ["ERF number is required."];
        if (siteAreaM2 <= 0) errors["siteAreaM2"] = ["Site area must be greater than 0."];
        if (siteAreaM2 > 1_000_000) errors["siteAreaM2"] = ["Site area seems unreasonably large (> 1 000 000 m²)."];
        if (string.IsNullOrWhiteSpace(zoningScheme)) errors["zoningScheme"] = ["Zoning scheme is required."];
        return errors;
    }

    /// Resolves the municipality name to its reference-table ID (nullable — unknown names are accepted gracefully).
    private async Task<short?> ResolveMunicipalityIdAsync(string name, CancellationToken ct)
    {
        var trimmed = name.Trim();
        var match = await _db.Municipalities
            .Where(m => m.Name == trimmed || m.ShortName == trimmed)
            .Select(m => (short?)m.Id)
            .FirstOrDefaultAsync(ct);
        return match;
    }
}

public record CreateProjectRequest(
    string Name,
    string? Description,
    string Municipality,
    string? Address,
    string Erf,
    double SiteAreaM2,
    string ZoningScheme);

public record UpdateProjectRequest(
    string Name,
    string Description,
    string Municipality,
    string? Address,
    string Erf,
    double SiteAreaM2,
    string ZoningScheme,
    double? ProposedGfaM2,
    double? FootprintM2,
    int? NumberOfStoreys,
    double? BuildingHeightM,
    double? FrontSetbackM,
    double? RearSetbackM,
    double? SideSetbackM,
    int? ParkingBays,
    double? GlaForParkingM2);

public record IfcDataRequest(
    double? ProposedGfaM2,
    double? FootprintM2,
    double? BuildingHeightM,
    int? NumberOfStoreys);

public record ParamsRequest(
    double? ProposedGfaM2,
    double? FootprintM2,
    double? BuildingHeightM,
    int? NumberOfStoreys,
    double? FrontSetbackM,
    double? RearSetbackM,
    double? SideSetbackM,
    int? ParkingBays,
    double? GlaForParkingM2);

public record SetIfcPathRequest(string? IfcPath);
