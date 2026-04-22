using ArchAutomate.CAD.Generators;
using ArchAutomate.CAD.Models;
using ArchAutomate.Data;
using ArchAutomate.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Dynamic;
using System.Text.Json;

namespace ArchAutomate.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CouncilPackController : ControllerBase
{
    private readonly CouncilTableGenerator _generator;
    private readonly SansFormGenerator _sansGenerator;
    private readonly AppDbContext _db;

    public CouncilPackController(CouncilTableGenerator generator, SansFormGenerator sansGenerator, AppDbContext db)
    {
        _generator = generator;
        _sansGenerator = sansGenerator;
        _db = db;
    }

    [HttpPost("generate-sans-forms")]
    public async Task<IActionResult> GenerateSansForms([FromQuery] Guid projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Stakeholders)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            return NotFound("Project not found.");

        string templateFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "SANS_Forms");
        string path = Path.Combine(templateFolder, "SANS10400 A - FORM 1.pdf");

        if (!System.IO.File.Exists(path))
            return NotFound("SANS10400 template not found.");

        var owner = project.Stakeholders.FirstOrDefault(s => s.Role == StakeholderRole.Client);
        var architect = project.Stakeholders.FirstOrDefault(s => s.Role == StakeholderRole.Architect);

        // We use the new dictionary based generic service
        var genericPdfService = new GenericPdfFormService();

        // But we don't know the exact complex PDF field names like "topmostSubform[0].Page1[0].ErfNo[0]"
        // So we can extract the exact names and match by substrings!
        var extractedFields = _sansGenerator.ExtractFormFields(path);

        var formDataDictionary = new Dictionary<string, object>();

        foreach (var fieldObj in extractedFields)
        {
            var dict = fieldObj as Dictionary<string, object>;
            if (dict == null) continue;

            if (dict.TryGetValue("Name", out var nameObj) && nameObj is string fieldName)
            {
                // Property and Location Information
                if (fieldName.EndsWith("TextField1[0]"))
                    formDataDictionary[fieldName] = project.Municipality ?? "";
                else if (fieldName.EndsWith("TextField1[1]"))
                    formDataDictionary[fieldName] = project.Erf ?? "";
                else if (fieldName.EndsWith("TextField1[2]"))
                    formDataDictionary[fieldName] = project.Name ?? "";
                else if (fieldName.EndsWith("TextField1[3]"))
                    formDataDictionary[fieldName] = project.Address ?? "";

                // Owner Declaration
                else if (fieldName.EndsWith("TextField1[5]") || fieldName.EndsWith("TextField1[6]") || fieldName.EndsWith("TextField1[7]"))
                    formDataDictionary[fieldName] = owner?.Name ?? "";
                else if (fieldName.EndsWith("TextField1[8]"))
                    formDataDictionary[fieldName] = owner?.Email ?? project.Address ?? "";
                else if (fieldName.EndsWith("TextField1[9]"))
                    formDataDictionary[fieldName] = owner?.Phone ?? "";
                else if (fieldName.EndsWith("DateTimeField1[0]"))
                    formDataDictionary[fieldName] = DateTime.Now.ToString("yyyy-MM-dd");

                // Professional Person Declaration
                else if (fieldName.EndsWith("TextField1[10]"))
                    formDataDictionary[fieldName] = architect?.Name ?? "";
                else if (fieldName.EndsWith("TextField1[11]"))
                    formDataDictionary[fieldName] = architect?.Organisation ?? "SACAP Registration";
                else if (fieldName.EndsWith("DateTimeField1[1]"))
                    formDataDictionary[fieldName] = DateTime.Now.ToString("yyyy-MM-dd");

                // Fallbacks for named fields (generic approach)
                else if (fieldName.Contains("Municipality"))
                    formDataDictionary[fieldName] = project.Municipality ?? "";
                else if (fieldName.Contains("ErfNo"))
                    formDataDictionary[fieldName] = project.Erf ?? "";
                else if (fieldName.Contains("ProjectName"))
                    formDataDictionary[fieldName] = project.Name ?? "";
                else if (fieldName.Contains("Owner"))
                    formDataDictionary[fieldName] = owner?.Name ?? "";
                else if (fieldName.Contains("Architect") && !fieldName.Contains("RegNo"))
                    formDataDictionary[fieldName] = architect?.Name ?? "";
                else if (fieldName.Contains("RegNo"))
                    formDataDictionary[fieldName] = architect?.Organisation ?? "SACAP Registration";
            }
        }

        byte[] pdfBytes = genericPdfService.FillForm(path, formDataDictionary);

        string fileName = $"SANS_Form_1_{project.Erf ?? project.Id.ToString().Substring(0, 8)}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    [AllowAnonymous]
    [HttpGet("extract-fields")]
    public IActionResult ExtractPdfFields([FromQuery] string formName = "SANS10400 A - FORM 1.pdf")
    {
        string templateFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "SANS_Forms");
        string path = Path.Combine(templateFolder, formName);

        var fields = _sansGenerator.ExtractFormFields(path);

        return Ok(new
        {
            Form = formName,
            FieldCount = fields.Count,
            Fields = fields
        });
    }

    [HttpPost("generate-tables")]
    public IActionResult GenerateTables([FromBody] CouncilTableData data)
    {
        byte[] dxfBytes = _generator.Generate(data);

        string fileName = $"CouncilTables_{data.DrawingNumber}_{data.Revision}.dxf";
        return File(dxfBytes, "application/dxf", fileName);
    }
}
