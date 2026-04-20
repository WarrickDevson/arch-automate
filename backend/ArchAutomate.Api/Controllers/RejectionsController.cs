using ArchAutomate.AI.Services;
using ArchAutomate.Data;
using ArchAutomate.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RejectionsController(
    PdfOcrService ocrService,
    RejectionParserService parserService,
    AppDbContext db) : ControllerBase
{
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

        string rawText = ocrService.ExtractText(pdfBytes);
        var parsed = await parserService.ParseAsync(rawText, file.FileName, ct);

        return Ok(parsed);
    }

    /// <summary>Save parsed rejection items to a project.</summary>
    [HttpPost("projects/{projectId:guid}/rejections")]
    public async Task<IActionResult> SaveRejections(
        Guid projectId,
        [FromBody] List<SaveRejectionRequest> requests,
        CancellationToken ct)
    {
        var tenantId = GetTenantId();
        bool projectExists = await db.Projects
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

        db.RejectionComments.AddRange(entities);
        await db.SaveChangesAsync(ct);

        return Ok(new { saved = entities.Count });
    }

    [HttpGet("projects/{projectId:guid}/rejections")]
    public async Task<IActionResult> GetRejections(Guid projectId, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        bool projectExists = await db.Projects
            .AnyAsync(p => p.Id == projectId && p.TenantId == tenantId, ct);

        if (!projectExists) return NotFound("Project not found.");

        var rejections = await db.RejectionComments
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
        var tenantId = GetTenantId();
        var rejection = await db.RejectionComments
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == rejectionId
                && r.ProjectId == projectId
                && r.Project.TenantId == tenantId, ct);

        if (rejection is null) return NotFound();

        rejection.Status = request.Status;
        if (request.Status == RejectionStatus.Resolved)
            rejection.ResolvedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(rejection);
    }

    private Guid GetTenantId()
    {
        var raw = User.FindFirst("tenant_id")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("sub claim missing.");
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException("Cannot resolve tenant_id as GUID.");
    }
}

public record SaveRejectionRequest(
    string SourceDocument,
    string ClauseReference,
    string CommentText,
    string SuggestedAction,
    string Category);

public record UpdateRejectionStatusRequest(RejectionStatus Status);
