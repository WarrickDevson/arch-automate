using System.Text.Json;
using ArchAutomate.AI.Models;
using Google.Cloud.AIPlatform.V1;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;

namespace ArchAutomate.AI.Services;

/// <summary>
/// Uses Google Vertex AI Gemini to parse raw council rejection letter text
/// into structured <see cref="RejectionItem"/> records.
/// </summary>
public class RejectionParserService
{
    private readonly string _projectId;
    private readonly string _location;
    private readonly string _model;

    public RejectionParserService(IConfiguration configuration)
    {
        _projectId = configuration["Vertex:ProjectId"]
            ?? throw new InvalidOperationException("Vertex:ProjectId is not configured.");
        _location = configuration["Vertex:Location"] ?? "us-central1";
        _model = configuration["Vertex:Model"] ?? "gemini-1.5-pro-002";
    }

    private const string SystemPrompt = """
        You are an expert South African building compliance assistant.
        You receive the raw text of a council rejection or comment letter and extract each
        numbered comment or objection into a structured JSON array.

        For each item return:
        {
          "itemNumber": <int>,
          "clauseReference": "<e.g. SANS 10400-A cl. 4.2 or Zoning Scheme cl. 3.1>",
          "comment": "<verbatim or summarised rejection comment>",
          "suggestedAction": "<concrete corrective action the architect should take>",
          "category": "<one of: Zoning, BuildingLines, Parking, Accessibility, StructuralDocumentation, FireCompliance, Drainage, Other>",
          "confidenceScore": <0.0-1.0>
        }

        Return ONLY a valid JSON array, no markdown fences, no extra text.
        """;

    public async Task<ParsedRejection> ParseAsync(string rawText, string sourceDocument,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"{_location}-aiplatform.googleapis.com";
        var client = new PredictionServiceClientBuilder { Endpoint = endpoint }.Build();

        var endpointName = EndpointName.FromProjectLocationPublisherModel(
            _projectId, _location, "google", _model);

        var request = new GenerateContentRequest
        {
            Model = endpointName.ToString(),
            SystemInstruction = new Content
            {
                Role = "system",
                Parts = { new Part { Text = SystemPrompt } }
            },
            Contents =
            {
                new Content
                {
                    Role = "user",
                    Parts = { new Part { Text = $"Source document: {sourceDocument}\n\n{rawText}" } }
                }
            },
            GenerationConfig = new GenerationConfig
            {
                Temperature = 0.1f,
                MaxOutputTokens = 8192
            }
        };

        GenerateContentResponse response = await client.GenerateContentAsync(request, cancellationToken);
        string json = response.Candidates[0].Content.Parts[0].Text.Trim();

        List<RejectionItem> items;
        try
        {
            items = JsonSerializer.Deserialize<List<RejectionItem>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch (JsonException)
        {
            items = [];
        }

        return new ParsedRejection
        {
            SourceDocument = sourceDocument,
            RawText = rawText,
            Items = items
        };
    }
}
