using ArchAutomate.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OnboardingController : ControllerBase
{
    private readonly AppDbContext _db;

    public OnboardingController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Called once per new user immediately after email confirmation.
    /// Creates the tenant and links the user's profile to it.
    /// Idempotent: if a profile already exists the existing tenant_id is
    /// returned so the client can safely retry on network failure.
    /// </summary>
    [HttpPost("setup")]
    public async Task<IActionResult> Setup([FromBody] SetupRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.PracticeName))
            return BadRequest(new { error = "practiceName is required" });

        var userId = GetUserId();

        // ── Idempotency guard ────────────────────────────────────────────
        // EF Core SqlQuery<scalar> requires the column to be aliased as "Value"
        var existingTenantId = await _db.Database
            .SqlQuery<Guid>($"SELECT tenant_id AS \"Value\" FROM public.profiles WHERE id = {userId} LIMIT 1")
            .FirstOrDefaultAsync(ct);

        if (existingTenantId != Guid.Empty)
            return Ok(new { tenantId = existingTenantId, alreadyExisted = true });

        // ── Create tenant ────────────────────────────────────────────────
        var tenantId = Guid.NewGuid();
        var slug = GenerateSlug(request.PracticeName.Trim(), tenantId);
        var practiceName = request.PracticeName.Trim();

        await _db.Database.ExecuteSqlAsync(
            $"INSERT INTO public.tenants (id, name, slug, plan) VALUES ({tenantId}, {practiceName}, {slug}, 'free')",
            ct);

        // ── Create profile ───────────────────────────────────────────────
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? GetClaimValue("email") ?? "User"
            : request.DisplayName.Trim();
        var email = GetClaimValue("email") ?? string.Empty;

        await _db.Database.ExecuteSqlAsync(
            $"INSERT INTO public.profiles (id, tenant_id, display_name, email, role) VALUES ({userId}, {tenantId}, {displayName}, {email}, 'owner')",
            ct);

        return Ok(new { tenantId, alreadyExisted = false });
    }

    /// <summary>
    /// Returns whether the authenticated user still needs to complete onboarding.
    /// Called by the frontend router guard on every app load.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var userId = GetUserId();

        var existingTenantId = await _db.Database
            .SqlQuery<Guid>($"SELECT tenant_id AS \"Value\" FROM public.profiles WHERE id = {userId} LIMIT 1")
            .FirstOrDefaultAsync(ct);

        return Ok(new
        {
            needsOnboarding = existingTenantId == Guid.Empty,
            tenantId = existingTenantId == Guid.Empty ? (Guid?)null : existingTenantId,
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private Guid GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value
               ?? throw new UnauthorizedAccessException("sub claim missing");
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException("sub is not a valid GUID");
    }

    private string? GetClaimValue(string type) => User.FindFirst(type)?.Value;

    private static string GenerateSlug(string name, Guid id)
    {
        var slug = System.Text.RegularExpressions.Regex
            .Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-")
            .Trim('-');
        if (slug.Length > 48) slug = slug[..48];
        return $"{slug}-{id.ToString("N")[..8]}";
    }
}

public record SetupRequest(string PracticeName, string? DisplayName);

