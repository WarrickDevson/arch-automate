namespace ArchAutomate.AI.Models;

public class ParsedRejection
{
    public string SourceDocument { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public List<RejectionItem> Items { get; set; } = [];
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
}

public class RejectionItem
{
    public int ItemNumber { get; set; }
    public string ClauseReference { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}
