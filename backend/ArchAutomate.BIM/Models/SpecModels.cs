namespace ArchAutomate.BIM.Models;

/// <summary>
/// A single detectable material/finish extracted from IFC element names and types.
/// </summary>
public class DetectedMaterial
{
    /// <summary>Raw string detected in the IFC (e.g. element Name, ObjectType, Description).</summary>
    public string RawValue { get; set; } = string.Empty;

    /// <summary>Normalised keyword that triggered a spec match (e.g. "brick", "concrete").</summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>IFC entity category the source element belongs to (e.g. "Wall", "Slab").</summary>
    public string ElementCategory { get; set; } = string.Empty;
}

/// <summary>
/// A single specification clause generated for a detected material.
/// </summary>
public class SpecClause
{
    /// <summary>Short heading shown in the spec document (e.g. "Face Brick Masonry").</summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>Full prescriptive specification text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Primary standard or code reference (e.g. "SANS 227", "SANS 10400-K").</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Spec section this clause belongs to.</summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>Normalised keyword that triggered this clause.</summary>
    public string TriggerKeyword { get; set; } = string.Empty;
}

/// <summary>
/// A top-level section of the generated specification (e.g. "Substructure", "Roofing").
/// </summary>
public class SpecSection
{
    public string Name { get; set; } = string.Empty;
    public List<SpecClause> Clauses { get; set; } = [];
}

/// <summary>
/// The full compiled specification returned by SpecEngine.Compile().
/// </summary>
public class CompiledSpec
{
    public List<SpecSection> Sections { get; set; } = [];
    public List<DetectedMaterial> DetectedMaterials { get; set; } = [];
    public List<string> UnmatchedKeywords { get; set; } = [];
    public int ClauseCount { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Input sent by the frontend to trigger spec compilation.</summary>
public class SpecCompileRequest
{
    /// <summary>
    /// Raw material strings collected from IFC element Name/ObjectType/Description attributes.
    /// Each string may contain multiple material keywords (e.g. "Brick-110mm-Cavity-Plaster").
    /// </summary>
    public List<string> MaterialStrings { get; set; } = [];

    /// <summary>Optional: element category for each material string, used to improve section placement.</summary>
    public List<string>? ElementCategories { get; set; }
}
