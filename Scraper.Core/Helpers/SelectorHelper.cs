using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Scraper.Core.Models;

namespace Scraper.Core.Helpers;

public class SelectorHelper
{
    private readonly HtmlParserHelper _parser = new();

    public List<string> ExtractValues(string html, string selector, SelectorKind selectorKind)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return [];
        }

        var document = _parser.Load(html);
        var nodes = selectorKind switch
        {
            SelectorKind.Css => document.DocumentNode.QuerySelectorAll(selector).ToList(),
            SelectorKind.XPath => document.DocumentNode.SelectNodes(selector)?.ToList() ?? [],
            _ => []
        };

        return nodes
            .Select(GetNodeValue)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();
    }



    public string AutoDetectSelector(string html)
    {
        return AutoDetectFields(html).FirstOrDefault()?.Selector ?? string.Empty;
    }

    public List<DetectedField> AutoDetectFields(string html)
    {
        var document = _parser.Load(html);
        if (LooksLikeJavaScriptShell(html))
        {
            return [];
        }

        var tableFields = DetectTableFields(document);
        if (tableFields.Count > 0)
        {
            return tableFields;
        }

        var fields = new List<DetectedField>();
        foreach (var candidate in GetCandidateSelectors())
        {
            var count = document.DocumentNode.QuerySelectorAll(candidate).Count();
            if (count < 2 || count > 250)
            {
                continue;
            }

            fields.Add(new DetectedField
            {
                FieldName = BuildFieldName(candidate, fields.Count + 1),
                Selector = candidate,
                SelectorKind = SelectorKind.Css
            });
        }

        if (fields.Count == 0 && document.DocumentNode.QuerySelectorAll("#app *").Any())
        {
            fields.Add(new DetectedField
            {
                FieldName = "App Content",
                Selector = "#app *",
                SelectorKind = SelectorKind.Css
            });
        }

        return fields
            .GroupBy(field => field.Selector, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(8)
            .ToList();
    }

    public string AutoDetectRowSelector(string html)
    {
        var document = _parser.Load(html);
        var repeatedCandidates = new[]
        {
            ".exhibitor-card",
            ".product-card",
            ".product-item",
            ".listing-card",
            ".item-card",
            ".card",
            "table tbody tr",
            "table tr",
            "[role='row']",
            "li"
        };

        foreach (var candidate in repeatedCandidates)
        {
            var count = document.DocumentNode.QuerySelectorAll(candidate).Count();
            if (count >= 2 && count <= 500)
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    public List<Dictionary<string, string>> ExtractRows(
        string html,
        string rowSelector,
        SelectorKind rowSelectorKind,
        IEnumerable<FieldExtractionPlan> fieldPlans)
    {
        if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(rowSelector))
        {
            return [];
        }

        var plans = fieldPlans
            .Where(plan => !string.IsNullOrWhiteSpace(plan.FieldName) && !string.IsNullOrWhiteSpace(plan.Selector))
            .ToList();

        if (plans.Count == 0)
        {
            return [];
        }

        var document = _parser.Load(html);
        var rowNodes = rowSelectorKind switch
        {
            SelectorKind.Css => document.DocumentNode.QuerySelectorAll(rowSelector).ToList(),
            SelectorKind.XPath => document.DocumentNode.SelectNodes(rowSelector)?.ToList() ?? [],
            _ => []
        };

        var rows = new List<Dictionary<string, string>>();
        foreach (var rowNode in rowNodes)
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var plan in plans)
            {
                var values = ExtractValuesFromNode(rowNode, plan.Selector, plan.SelectorKind);
                row[plan.FieldName] = values.Count == 0
                    ? string.Empty
                    : string.Join("; ", values);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static bool LooksLikeJavaScriptShell(string html)
    {
        var safeHtml = html ?? string.Empty;
        var textOnlyLength = HtmlEntity.DeEntitize(HtmlNode.CreateNode("<div>" + safeHtml + "</div>").InnerText)
            .Trim()
            .Length;

        return safeHtml.Contains("id=\"app\"", StringComparison.OrdinalIgnoreCase) &&
               safeHtml.Contains("type=\"module\"", StringComparison.OrdinalIgnoreCase) &&
               textOnlyLength < 150;
    }

    private static List<DetectedField> DetectTableFields(HtmlDocument document)
    {
        var table = document.DocumentNode.QuerySelectorAll("table")
            .FirstOrDefault(candidate =>
            {
                var rows = candidate.QuerySelectorAll("tbody tr").ToList();
                if (rows.Count == 0)
                {
                    rows = candidate.QuerySelectorAll("tr").Skip(1).ToList();
                }

                return rows.Count > 0;
            });

        if (table is null)
        {
            return [];
        }

        var headers = table.QuerySelectorAll("thead th")
            .Select(header => HtmlEntity.DeEntitize(header.InnerText).Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        var rows = table.QuerySelectorAll("tbody tr").ToList();
        var rowSelector = "table tbody tr";
        if (rows.Count == 0)
        {
            rows = table.QuerySelectorAll("tr").Skip(1).ToList();
            rowSelector = "table tr";
        }

        var sampleRow = rows.FirstOrDefault();
        if (sampleRow is null)
        {
            return [];
        }

        var cells = sampleRow.QuerySelectorAll("td, th").ToList();
        if (cells.Count == 0)
        {
            return [];
        }

        var fields = new List<DetectedField>();
        for (var index = 0; index < cells.Count; index++)
        {
            var header = index < headers.Count ? headers[index] : $"Column {index + 1}";
            fields.Add(new DetectedField
            {
                FieldName = NormalizeFieldName(header, index + 1),
                Selector = $"{rowSelector} td:nth-of-type({index + 1}), {rowSelector} th:nth-of-type({index + 1})",
                SelectorKind = SelectorKind.Css
            });
        }

        return fields;
    }

    private static IEnumerable<string> GetCandidateSelectors()
    {
        return
        [
            "table tbody tr",
            "table tr",
            "[role='row']",
            ".results-table tbody tr",
            ".result",
            ".result-row",
            ".session-row",
            ".lap-row",
            ".driver",
            ".driver-name",
            ".position",
            ".lap-time",
            ".name",
            ".title",
            ".product-card",
            ".product-item",
            ".item",
            ".card",
            "article",
            "main a",
            "h1, h2, h3",
            "li"
        ];
    }

    private static string BuildFieldName(string selector, int number)
    {
        var token = selector
            .Split([' ', '>', '.', '#', '/', '[', ']', ':', ','], StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return NormalizeFieldName(token, number);
    }

    private static string NormalizeFieldName(string? value, int number)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"Field {number}";
        }

        var words = value
            .Replace("-", " ", StringComparison.Ordinal)
            .Replace("_", " ", StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant());

        var combined = string.Join(' ', words).Trim();
        return string.IsNullOrWhiteSpace(combined) ? $"Field {number}" : combined;
    }

    private static List<string> ExtractValuesFromNode(HtmlNode node, string selector, SelectorKind selectorKind)
    {
        var effectiveSelector = selectorKind == SelectorKind.XPath && selector.StartsWith("//", StringComparison.Ordinal)
            ? "." + selector
            : selector;

        var nodes = selectorKind switch
        {
            SelectorKind.Css => node.QuerySelectorAll(selector).ToList(),
            SelectorKind.XPath => node.SelectNodes(effectiveSelector)?.ToList() ?? [],
            _ => []
        };

        return nodes
            .Select(GetNodeValue)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();
    }

    private static string GetNodeValue(HtmlNode node)
    {
        var href = node.GetAttributeValue("href", string.Empty);
        if (!string.IsNullOrWhiteSpace(href))
        {
            return href.Trim();
        }

        var src = node.GetAttributeValue("src", string.Empty);
        if (!string.IsNullOrWhiteSpace(src))
        {
            return src.Trim();
        }

        return HtmlEntity.DeEntitize(node.InnerText).Trim();
    }
}
