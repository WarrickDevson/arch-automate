using ArchAutomate.AI.Services;
using ArchAutomate.Data;
using ArchAutomate.Data.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RejectionsController : ControllerBase
{
    private readonly PdfOcrService _ocrService;
    private readonly RejectionParserService _parserService;
    private readonly AppDbContext _db;

    public RejectionsController(
        PdfOcrService ocrService,
        RejectionParserService parserService,
        AppDbContext db)
    {
        _ocrService = ocrService;
        _parserService = parserService;
        _db = db;
    }

    /// <summary>Upload a PDF rejection letter and parse it with AI.</summary>
    [HttpPost("parse")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
    public async Task<IActionResult> Parse(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PDF files are supported.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        byte[] pdfBytes = ms.ToArray();

        string rawText = _ocrService.ExtractText(pdfBytes);
        var parsed = await _parserService.ParseAsync(rawText, file.FileName, ct);

        return Ok(parsed);
    }

    /// <summary>Save parsed rejection items to a project.</summary>
    [HttpPost("projects/{projectId:guid}/rejections")]
    public async Task<IActionResult> SaveRejections(
        Guid projectId,
        [FromBody] List<SaveRejectionRequest> requests,
        CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        bool projectExists = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.TenantId == tenantId, ct);

        if (!projectExists) return NotFound("Project not found.");

        var entities = requests.Select(r => new RejectionComment
        {
            ProjectId = projectId,
            SourceDocument = r.SourceDocument,
            ClauseReference = r.ClauseReference,
            CommentText = r.CommentText,
            ParsedAction = r.SuggestedAction,
            Category = Enum.TryParse<RejectionCategory>(r.Category, ignoreCase: true, out var cat)
                ? cat : RejectionCategory.Other
        }).ToList();

        _db.RejectionComments.AddRange(entities);
        await _db.SaveChangesAsync(ct);

        return Ok(new { saved = entities.Count });
    }

    [HttpGet("projects/{projectId:guid}/rejections")]
    public async Task<IActionResult> GetRejections(Guid projectId, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        bool projectExists = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.TenantId == tenantId, ct);

        if (!projectExists) return NotFound("Project not found.");

        var rejections = await _db.RejectionComments
            .Where(r => r.ProjectId == projectId)
            .OrderBy(r => r.ReceivedAt)
            .ToListAsync(ct);

        return Ok(rejections);
    }

    [HttpPatch("projects/{projectId:guid}/rejections/{rejectionId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid projectId, Guid rejectionId,
        [FromBody] UpdateRejectionStatusRequest request,
        CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        var rejection = await _db.RejectionComments
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == rejectionId
                && r.ProjectId == projectId
                && r.Project.TenantId == tenantId, ct);

        if (rejection is null) return NotFound();

        rejection.Status = request.Status;
        if (request.Status == RejectionStatus.Resolved)
            rejection.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(rejection);
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
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("sub claim missing.");

        return Guid.TryParse(raw, out var id)
            ? id
            : throw new UnauthorizedAccessException("sub claim is not a valid GUID.");
    }
}

public record SaveRejectionRequest(
    string SourceDocument,
    string ClauseReference,
    string CommentText,
    string SuggestedAction,
    string Category);

public record UpdateRejectionStatusRequest(RejectionStatus Status);
