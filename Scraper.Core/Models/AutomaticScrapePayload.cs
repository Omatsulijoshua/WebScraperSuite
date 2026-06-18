namespace Scraper.Core.Models;

public class AutomaticScrapePayload
{
    public string SourceUrl { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string SelectorType { get; set; } = "CSS";
    public string FieldName { get; set; } = "Value";
    public string ProfileName { get; set; } = "Default";
    public DateTime CapturedAtUtc { get; set; }
    public List<string> Values { get; set; } = [];
    public string CombinedValue { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public List<AutomaticScrapeFieldResult> Fields { get; set; } = [];
    public Dictionary<string, object?> TransmittedData { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class AutomaticScrapeFieldResult
{
    public string OutputKey { get; set; } = "value";
    public string Selector { get; set; } = string.Empty;
    public string SelectorType { get; set; } = "CSS";
    public string ValueMode { get; set; } = "Combined";
    public List<string> Values { get; set; } = [];
    public string FirstValue { get; set; } = string.Empty;
    public string CombinedValue { get; set; } = string.Empty;
    public string ValuesText => string.Join(" | ", Values);
}

public class AutomaticTransmissionFieldDefinition
{
    public string OutputKey { get; set; } = "value";
    public string Selector { get; set; } = string.Empty;
    public string SelectorType { get; set; } = "CSS";
    public string ValueMode { get; set; } = "Combined";
}

public class AutomaticTransmissionProfile
{
    public string ProfileName { get; set; } = "Default";
    public bool IncludeProfileName { get; set; } = true;
    public bool IncludeSourceUrl { get; set; } = true;
    public bool IncludeCapturedAtUtc { get; set; } = true;
    public bool IncludeContentHash { get; set; } = true;
    public bool IncludeFieldMetadata { get; set; }
    public bool NestFieldsUnderDataRoot { get; set; } = true;
    public string DataRootKey { get; set; } = "data";
    public List<AutomaticTransmissionFieldDefinition> Fields { get; set; } = [];
}
