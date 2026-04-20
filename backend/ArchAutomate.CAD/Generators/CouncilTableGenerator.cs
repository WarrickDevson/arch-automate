using ArchAutomate.CAD.Models;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

namespace ArchAutomate.CAD.Generators;

/// <summary>
/// Generates a DXF drawing containing council-submission site statistics
/// and area schedule tables using netDxf 2.x API.
/// </summary>
public class CouncilTableGenerator
{
    private const double RowHeight = 8.0;
    private const double ColWidth = 60.0;
    private const double TextHeight = 3.0;
    private const double HeaderTextHeight = 3.5;

    public byte[] Generate(CouncilTableData data)
    {
        var doc = new DxfDocument();

        var layerTitle = new Layer("TITLE-BLOCK") { Color = AciColor.Default };
        var layerTable = new Layer("TABLES") { Color = AciColor.Cyan };
        var layerText = new Layer("TEXT") { Color = AciColor.Default };
        doc.Layers.Add(layerTitle);
        doc.Layers.Add(layerTable);
        doc.Layers.Add(layerText);

        double originX = 0;
        double originY = 0;

        InsertTitleBlock(doc, layerText, data, originX, originY);

        double tableY = originY - 40;
        InsertSiteStatisticsTable(doc, layerTable, layerText, data, originX, tableY);

        double areaTableX = originX + (ColWidth * 3) + 20;
        InsertAreaScheduleTable(doc, layerTable, layerText, data, areaTableX, tableY);

        using var ms = new MemoryStream();
        doc.Save(ms, isBinary: false);
        return ms.ToArray();
    }

    private static void InsertTitleBlock(DxfDocument doc, Layer layerText,
        CouncilTableData data, double x, double y)
    {
        var fields = new[]
        {
            ("PROJECT", data.ProjectName),
            ("ERF", data.Erf),
            ("MUNICIPALITY", data.Municipality),
            ("ZONING", data.ZoningScheme),
            ("ARCHITECT", data.Architect),
            ("DATE", data.Date.ToString("dd MMM yyyy")),
            ("SCALE", data.Scale),
            ("DRAWING NO.", data.DrawingNumber),
            ("REVISION", data.Revision),
        };

        double curY = y;
        foreach (var (label, value) in fields)
        {
            AddText(doc, layerText, $"{label}:  {value}", x, curY, TextHeight);
            curY -= RowHeight;
        }
    }

    private static void InsertSiteStatisticsTable(DxfDocument doc, Layer layerBorder, Layer layerText,
        CouncilTableData data, double x, double y)
    {
        string[] headers = ["DESCRIPTION", "PERMITTED", "PROPOSED"];
        DrawTableHeaders(doc, layerBorder, layerText, headers, x, y, ColWidth, RowHeight, HeaderTextHeight);

        double curY = y - RowHeight;
        foreach (var row in data.SiteStatistics)
        {
            var textColor = row.Compliant ? AciColor.Default : AciColor.Red;
            DrawTableRow(doc, layerBorder, layerText,
                [row.Description, row.Permitted, row.Proposed],
                x, curY, ColWidth, RowHeight, TextHeight, textColor);
            curY -= RowHeight;
        }
    }

    private static void InsertAreaScheduleTable(DxfDocument doc, Layer layerBorder, Layer layerText,
        CouncilTableData data, double x, double y)
    {
        string[] headers = ["SPACE", "LEVEL", "AREA (m\u00b2)"];
        DrawTableHeaders(doc, layerBorder, layerText, headers, x, y, ColWidth, RowHeight, HeaderTextHeight);

        double curY = y - RowHeight;
        double total = 0;
        foreach (var row in data.AreaSchedule)
        {
            DrawTableRow(doc, layerBorder, layerText,
                [row.SpaceName, row.Level, row.AreaM2.ToString("F2")],
                x, curY, ColWidth, RowHeight, TextHeight);
            total += row.AreaM2;
            curY -= RowHeight;
        }

        DrawTableRow(doc, layerBorder, layerText,
            ["TOTAL GFA", "", total.ToString("F2") + " m\u00b2"],
            x, curY, ColWidth, RowHeight, HeaderTextHeight);
    }

    private static void DrawTableHeaders(DxfDocument doc, Layer border, Layer text,
        string[] headers, double x, double y, double colW, double rowH, double textH)
    {
        for (int i = 0; i < headers.Length; i++)
            AddCell(doc, border, text, headers[i], x + i * colW, y, colW, rowH, textH);
    }

    private static void DrawTableRow(DxfDocument doc, Layer border, Layer text,
        string[] cells, double x, double y, double colW, double rowH, double textH,
        AciColor? textColor = null)
    {
        for (int i = 0; i < cells.Length; i++)
            AddCell(doc, border, text, cells[i], x + i * colW, y, colW, rowH, textH, textColor);
    }

    private static void AddCell(DxfDocument doc, Layer border, Layer text,
        string content, double x, double y, double w, double h, double textH,
        AciColor? textColor = null)
    {
        // Draw cell border using LwPolyline
        var poly = new LwPolyline();
        poly.Vertexes.Add(new LwPolylineVertex(x, y));
        poly.Vertexes.Add(new LwPolylineVertex(x + w, y));
        poly.Vertexes.Add(new LwPolylineVertex(x + w, y - h));
        poly.Vertexes.Add(new LwPolylineVertex(x, y - h));
        poly.IsClosed = true;
        poly.Layer = border;
        doc.AddEntity(poly);

        // Add text centred vertically in cell
        var mtext = new MText
        {
            Value = content,
            Position = new Vector3(x + 2, y - h / 2.0, 0),
            Height = textH,
            RectangleWidth = w - 4,
            Layer = text,
            Color = textColor ?? AciColor.ByLayer
        };
        doc.AddEntity(mtext);
    }

    private static void AddText(DxfDocument doc, Layer layer, string content, double x, double y, double textH)
    {
        var t = new Text
        {
            Value = content,
            Position = new Vector3(x, y, 0),
            Height = textH,
            Layer = layer
        };
        doc.AddEntity(t);
    }
}
