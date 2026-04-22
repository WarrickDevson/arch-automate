using ArchAutomate.CAD.Models;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ArchAutomate.CAD.Generators;

public class SansFormGenerator
{
    public SansFormGenerator() { }

    /// <summary>
    /// For filling interactive form fields (AcroForms) mapped straight from the DB.
    /// </summary>
    public byte[] GenerateInteractiveForms(SansFormData data, string templateFolderPath)
    {
        string form1Path = Path.Combine(templateFolderPath, "SANS10400 A - FORM 1.pdf");

        if (!File.Exists(form1Path))
            throw new FileNotFoundException($"Template not found at {form1Path}");

        using FileStream docStream = new FileStream(form1Path, FileMode.Open, FileAccess.Read);
        using PdfLoadedDocument loadedDocument = new PdfLoadedDocument(docStream);

        PdfLoadedForm form = loadedDocument.Form;

        // If the PDF actually has interactive form fields configured
        if (form != null && form.Fields.Count > 0)
        {
            // Fill fields based on expected mapped names. 
            // e.g. "Text1" or "MunicipalityName"
            TryFillField(form, "Municipality", data.Municipality ?? "");
            TryFillField(form, "ErfNo", data.Erf ?? "");
            TryFillField(form, "ProjectName", data.ProjectName ?? "");
            TryFillField(form, "Owner", data.OwnerName ?? "");
            TryFillField(form, "Architect", data.Architect ?? "");
            TryFillField(form, "RegNo", data.ArchitectRegNo ?? "");

            form.Flatten = true;
        }
        else
        {
            // FALLBACK TO TEXT STAMPING IF NO INTERACTIVE FIELDS EXIST
            PdfLoadedPage page = loadedDocument.Pages[0] as PdfLoadedPage;
            PdfGraphics graphics = page.Graphics;
            PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
            PdfBrush brush = PdfBrushes.DarkBlue;

            graphics.DrawString(data.Municipality ?? "", font, brush, new PointF(150, 160));
            graphics.DrawString(data.Erf ?? "", font, brush, new PointF(150, 180));
            graphics.DrawString(data.ProjectName ?? "", font, brush, new PointF(220, 200));
            graphics.DrawString(data.OwnerName ?? "", font, brush, new PointF(110, 280));
            graphics.DrawString(data.Architect ?? "", font, brush, new PointF(110, 350));
            graphics.DrawString(data.ArchitectRegNo ?? "", font, brush, new PointF(120, 410));
        }

        using MemoryStream ms = new MemoryStream();
        loadedDocument.Save(ms);
        return ms.ToArray();
    }

    private void TryFillField(PdfLoadedForm form, string fieldNameSubstring, string value)
    {
        var field = form.Fields.Cast<PdfLoadedField>().FirstOrDefault(f => f.Name.Contains(fieldNameSubstring));
        if (field is PdfLoadedTextBoxField textBox)
        {
            textBox.Text = value;
        }
    }

    /// <summary>
    /// Utility tool to extract all form fields from a PDF so you can map them in your database.
    /// Includes the bounds and nearby text logic to help identify what each field is.
    /// </summary>
    public List<object> ExtractFormFields(string filePath)
    {
        var fields = new List<object>();
        if (!File.Exists(filePath)) return fields;

        using FileStream docStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using PdfLoadedDocument loadedDocument = new PdfLoadedDocument(docStream);

        var extractedPageLines = new Dictionary<int, dynamic>();

        if (loadedDocument.Form != null)
        {
            foreach (PdfLoadedField field in loadedDocument.Form.Fields)
            {
                var fieldData = new Dictionary<string, object>
                {
                    { "Name", field.Name },
                    { "Type", field.GetType().Name.Replace("PdfLoaded", "").Replace("Field", "") }
                };

                // Most visible input fields inherit from PdfLoadedStyledField 
                if (field is PdfLoadedStyledField styledField)
                {
                    fieldData["Tooltip"] = styledField.ToolTip ?? "";

                    var bounds = styledField.Bounds;
                    fieldData["Bounds"] = new { X = Math.Round(bounds.X, 1), Y = Math.Round(bounds.Y, 1), W = Math.Round(bounds.Width, 1), H = Math.Round(bounds.Height, 1) };

                    if (styledField.Page is PdfLoadedPage page)
                    {
                        var contextText = "No nearby text found";
                        try
                        {
                            // we'll just key by the page object reference
                            int pageId = page.GetHashCode();
                            if (!extractedPageLines.ContainsKey(pageId))
                            {
                                page.ExtractText(out var lineCollection);
                                extractedPageLines[pageId] = lineCollection != null ? lineCollection.TextLine : null;
                            }

                            var textLines = extractedPageLines[pageId];
                            var nearby = new List<string>();

                            if (textLines != null)
                            {
                                foreach (var line in textLines)
                                {
                                    var lB = line.Bounds;
                                    bool sameRowLeft = Math.Abs(lB.Y - bounds.Y) < 15 && lB.X < bounds.X - 2 && (bounds.X - lB.Right) < 200;
                                    bool directlyAbove = Math.Abs(lB.Bottom - bounds.Y) < 25 && Math.Abs(lB.X - bounds.X) < 150 && lB.Bottom < bounds.Y + 2;

                                    if (sameRowLeft || directlyAbove)
                                    {
                                        nearby.Add(line.Text.Trim());
                                    }
                                }
                            }

                            if (nearby.Count > 0)
                            {
                                contextText = string.Join(" | ", nearby.Distinct());
                            }
                        }
                        catch { }

                        fieldData["NearbyTextContext"] = contextText;
                    }
                }

                fields.Add(fieldData);
            }
        }
        return fields;
    }
}
