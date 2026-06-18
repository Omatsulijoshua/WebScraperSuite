namespace Scraper.Core.Models;

public class DetectedField
{
    public string FieldName { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public SelectorKind SelectorKind { get; set; } = SelectorKind.Css;
}
