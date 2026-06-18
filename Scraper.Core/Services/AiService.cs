using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Scraper.Core.Models;

namespace Scraper.Core.Services;

public class AiService
{
    private static readonly HttpClient _httpClient = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

    public async Task<List<Dictionary<string, string>>> ExtractStructuredDataAsync(string html, string url, ScraperSettings settings)
    {
        var cleanedHtml = CleanHtml(html);
        var prompt = $"You are an expert web scraping assistant. Below is the cleaned HTML of a web page loaded from url: {url}.\n" +
                     "Analyze the HTML and identify the core repeated list of items or details (e.g. search results, products, articles, tables) and extract all useful details.\n" +
                     "Format your response as a JSON object with a single key 'results' which contains a list of objects. Each object should represent one item and contain its key-value pairs (e.g., 'title', 'price', 'link', 'description', etc.).\n" +
                     "Do not return any markdown formatting outside of JSON. The output must be valid parsable JSON only.\n\n" +
                     "Here is the HTML:\n" + cleanedHtml;

        string responseText = await CallProviderAsync(prompt, settings);
        
        try
        {
            responseText = CleanResponseJson(responseText);
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("results", out var resultsElement) && resultsElement.ValueKind == JsonValueKind.Array)
            {
                var list = new List<Dictionary<string, string>>();
                foreach (var item in resultsElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var dict = new Dictionary<string, string>();
                        foreach (var prop in item.EnumerateObject())
                        {
                            dict[prop.Name] = prop.Value.ToString();
                        }
                        list.Add(dict);
                    }
                }
                return list;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse AI response: {ex.Message}. Response was: {responseText}", ex);
        }
        
        return new List<Dictionary<string, string>>();
    }

    public async Task<List<MediaItem>> ExtractMediaAsync(string html, string url, ScraperSettings settings)
    {
        var cleanedHtml = CleanHtml(html);
        var prompt = $"You are an expert media harvesting assistant. Below is the cleaned HTML of a web page loaded from url: {url}.\n" +
                     "Identify all media assets (images, audio/mp3 files, videos) that are linked or embedded in the page. Make sure to resolve relative URLs using the page url if possible.\n" +
                     "Format your response as a JSON object with a single key 'media' which contains a list of objects. Each object should have keys:\n" +
                     "- 'url': The absolute URL of the asset\n" +
                     "- 'type': String representing the category (must be exactly 'Image', 'Video', or 'Audio')\n" +
                     "- 'fileName': An appropriate name for the file (including correct extension like .jpg, .mp3, .mp4)\n" +
                     "Do not return any markdown formatting outside of JSON. The output must be valid parsable JSON only.\n\n" +
                     "Here is the HTML:\n" + cleanedHtml;

        string responseText = await CallProviderAsync(prompt, settings);
        
        try
        {
            responseText = CleanResponseJson(responseText);
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("media", out var mediaElement) && mediaElement.ValueKind == JsonValueKind.Array)
            {
                var list = new List<MediaItem>();
                foreach (var item in mediaElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var mediaItem = new MediaItem
                        {
                            Url = item.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                            Type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                            FileName = item.TryGetProperty("fileName", out var f) ? f.GetString() ?? "" : "",
                            Status = "Ready"
                        };
                        
                        // Resolve relative URLs
                        if (!string.IsNullOrEmpty(mediaItem.Url) && !mediaItem.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(url, UriKind.Absolute, out var baseUri))
                        {
                            if (Uri.TryCreate(baseUri, mediaItem.Url, out var resolvedUri))
                            {
                                mediaItem.Url = resolvedUri.AbsoluteUri;
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(mediaItem.Url))
                        {
                            list.Add(mediaItem);
                        }
                    }
                }
                return list;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse AI media response: {ex.Message}. Response was: {responseText}", ex);
        }
        
        return new List<MediaItem>();
    }

    private async Task<string> CallProviderAsync(string prompt, ScraperSettings settings)
    {
        int timeoutSeconds = settings.ApiTimeoutSeconds > 0 ? settings.ApiTimeoutSeconds : 100;
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            if (settings.ApiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                var modelName = string.IsNullOrEmpty(settings.ApiModel) ? "gemini-1.5-flash" : settings.ApiModel;
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={settings.ApiKey}";
                var payload = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };
                
                var requestContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, requestContent, cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errContent = await response.Content.ReadAsStringAsync(cts.Token);
                    throw new Exception(FormatApiError("Gemini", response.StatusCode, errContent));
                }
                
                var resJson = await response.Content.ReadAsStringAsync(cts.Token);
                try
                {
                    using var doc = JsonDocument.Parse(resJson);
                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                        
                    return text ?? string.Empty;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Gemini API response could not be parsed. The response might have been empty, blocked by safety settings, or structured differently. Details: {ex.Message}. Raw Response: {resJson}", ex);
                }
            }
            else if (settings.ApiProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                var modelName = string.IsNullOrEmpty(settings.ApiModel) ? "gpt-4o-mini" : settings.ApiModel;
                var url = "https://api.openai.com/v1/chat/completions";
                
                var payload = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    response_format = new { type = "json_object" }
                };
                
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {settings.ApiKey}");
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.SendAsync(request, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    var errContent = await response.Content.ReadAsStringAsync(cts.Token);
                    throw new Exception(FormatApiError("OpenAI", response.StatusCode, errContent));
                }
                
                var resJson = await response.Content.ReadAsStringAsync(cts.Token);
                try
                {
                    using var doc = JsonDocument.Parse(resJson);
                    var text = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();
                        
                    return text ?? string.Empty;
                }
                catch (Exception ex)
                {
                    throw new Exception($"OpenAI API response could not be parsed. Details: {ex.Message}. Raw Response: {resJson}", ex);
                }
            }
            else if (settings.ApiProvider.Equals("NVIDIA", StringComparison.OrdinalIgnoreCase))
            {
                var modelName = string.IsNullOrEmpty(settings.ApiModel) ? "nvidia/llama-3.1-nemotron-70b-instruct" : settings.ApiModel;
                var url = "https://integrate.api.nvidia.com/v1/chat/completions";
                
                var payload = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.2
                };
                
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {settings.ApiKey}");
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.SendAsync(request, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    var errContent = await response.Content.ReadAsStringAsync(cts.Token);
                    throw new Exception(FormatApiError("NVIDIA", response.StatusCode, errContent));
                }
                
                var resJson = await response.Content.ReadAsStringAsync(cts.Token);
                try
                {
                    using var doc = JsonDocument.Parse(resJson);
                    var text = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();
                        
                    return text ?? string.Empty;
                }
                catch (Exception ex)
                {
                    throw new Exception($"NVIDIA API response could not be parsed. Details: {ex.Message}. Raw Response: {resJson}", ex);
                }
            }
            else if (settings.ApiProvider.Equals("Custom / Other", StringComparison.OrdinalIgnoreCase) || 
                     (!string.IsNullOrWhiteSpace(settings.CustomEndpoint) && 
                      !settings.ApiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && 
                      !settings.ApiProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) &&
                      !settings.ApiProvider.Equals("NVIDIA", StringComparison.OrdinalIgnoreCase)))
            {
                var url = settings.CustomEndpoint;
                if (string.IsNullOrWhiteSpace(url))
                {
                    throw new Exception("Custom API Endpoint URL is not configured.");
                }

                var modelName = string.IsNullOrEmpty(settings.ApiModel) ? "custom-model" : settings.ApiModel;

                if (url.Contains("anthropic.com", StringComparison.OrdinalIgnoreCase))
                {
                    var payload = new
                    {
                        model = modelName,
                        max_tokens = 4000,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        }
                    };
                    
                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("x-api-key", settings.ApiKey);
                    request.Headers.Add("anthropic-version", "2023-06-01");
                    request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    
                    var response = await _httpClient.SendAsync(request, cts.Token);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errContent = await response.Content.ReadAsStringAsync(cts.Token);
                        throw new Exception(FormatApiError("Anthropic", response.StatusCode, errContent));
                    }
                    
                    var resJson = await response.Content.ReadAsStringAsync(cts.Token);
                    try
                    {
                        using var doc = JsonDocument.Parse(resJson);
                        var text = doc.RootElement
                            .GetProperty("content")[0]
                            .GetProperty("text")
                            .GetString();
                            
                        return text ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Custom API (Anthropic) response could not be parsed. Details: {ex.Message}. Raw Response: {resJson}", ex);
                    }
                }
                else
                {
                    var payload = new
                    {
                        model = modelName,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        }
                    };
                    
                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    if (!string.IsNullOrEmpty(settings.ApiKey))
                    {
                        request.Headers.Add("Authorization", $"Bearer {settings.ApiKey}");
                    }
                    request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    
                    var response = await _httpClient.SendAsync(request, cts.Token);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errContent = await response.Content.ReadAsStringAsync(cts.Token);
                        throw new Exception(FormatApiError("Custom", response.StatusCode, errContent));
                    }
                    
                    var resJson = await response.Content.ReadAsStringAsync(cts.Token);
                    try
                    {
                        using var doc = JsonDocument.Parse(resJson);
                        var text = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();
                            
                        return text ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Custom API response could not be parsed. Details: {ex.Message}. Raw Response: {resJson}", ex);
                    }
                }
            }
            else
            {
                throw new NotSupportedException($"API Provider '{settings.ApiProvider}' is not supported.");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Could not connect to the {settings.ApiProvider} service. Please check your internet connection and verify that the API host/endpoint is reachable. Details: {ex.Message}", ex);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException($"The AI request timed out after the configured limit of {timeoutSeconds} seconds. You can increase this limit in the Configuration settings.");
        }
    }

    private static string FormatApiError(string provider, System.Net.HttpStatusCode statusCode, string errContent)
    {
        var statusStr = $"{(int)statusCode} {statusCode}";
        if (string.IsNullOrWhiteSpace(errContent))
        {
            return $"{provider} API returned an error status: {statusStr}.";
        }

        try
        {
            using var doc = JsonDocument.Parse(errContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var errorEl))
            {
                if (errorEl.ValueKind == JsonValueKind.Object)
                {
                    if (errorEl.TryGetProperty("message", out var msgEl))
                    {
                        var msg = msgEl.GetString();
                        if (!string.IsNullOrEmpty(msg))
                        {
                            return $"{provider} API Error ({statusStr}): {msg}";
                        }
                    }
                    else if (errorEl.ValueKind == JsonValueKind.String)
                    {
                        return $"{provider} API Error ({statusStr}): {errorEl.GetString()}";
                    }
                }
                else if (errorEl.ValueKind == JsonValueKind.String)
                {
                    var msg = errorEl.GetString();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        return $"{provider} API Error ({statusStr}): {msg}";
                    }
                }
            }

            string[] possibleKeys = { "message", "detail", "msg", "error_description" };
            foreach (var key in possibleKeys)
            {
                if (root.TryGetProperty(key, out var valEl) && valEl.ValueKind == JsonValueKind.String)
                {
                    var msg = valEl.GetString();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        return $"{provider} API Error ({statusStr}): {msg}";
                    }
                }
            }
        }
        catch
        {
            // Fallback if not valid JSON
        }

        if (errContent.Trim().StartsWith("<"))
        {
            var stripped = Regex.Replace(errContent, "<.*?>", string.Empty).Trim();
            stripped = Regex.Replace(stripped, @"\s+", " ");
            if (stripped.Length > 200)
            {
                stripped = stripped.Substring(0, 200) + "...";
            }
            return $"{provider} API Error ({statusStr}): {stripped}";
        }

        var displayContent = errContent.Trim();
        if (displayContent.Length > 300)
        {
            displayContent = displayContent.Substring(0, 300) + "...";
        }

        return $"{provider} API Error ({statusStr}): {displayContent}";
    }

    private static string CleanResponseJson(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(7);
        }
        else if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(3);
        }
        if (text.EndsWith("```"))
        {
            text = text.Substring(0, text.Length - 3);
        }
        return text.Trim();
    }

    private static string CleanHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//svg|//head|//comment()|//iframe|//noscript|//header|//footer|//nav|//aside");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                {
                    node.Remove();
                }
            }
            
            var body = doc.DocumentNode.SelectSingleNode("//body");
            var result = body != null ? body.OuterHtml : doc.DocumentNode.OuterHtml;
            
            // Clean up attributes that are not useful for LLM structure detection to save tokens
            var bodyDoc = new HtmlDocument();
            bodyDoc.LoadHtml(result);
            foreach (var node in bodyDoc.DocumentNode.DescendantsAndSelf())
            {
                if (node.HasAttributes)
                {
                    // Keep 'href', 'src', and some class elements to help LLM recognize lists/links, drop the rest
                    var attrsToRemove = node.Attributes
                        .Where(a => !a.Name.Equals("href", StringComparison.OrdinalIgnoreCase) && 
                                    !a.Name.Equals("src", StringComparison.OrdinalIgnoreCase) &&
                                    !a.Name.Equals("class", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (var attr in attrsToRemove)
                    {
                        node.Attributes.Remove(attr);
                    }
                }
            }
            
            result = bodyDoc.DocumentNode.OuterHtml;
            result = Regex.Replace(result, @"\s+", " ");
            result = Regex.Replace(result, @"<!--.*?-->", "");
            
            return result;
        }
        catch
        {
            return html;
        }
    }

    public async Task<PageLayoutAnalysis> AnalyzePageLayoutAsync(string html, string url, ScraperSettings settings)
    {
        var cleanedHtml = CleanHtml(html);
        var prompt = $"You are an expert web scraping layout analyst. Below is the cleaned HTML of a web page loaded from url: {url}.\n" +
                     "Analyze the HTML and extract:\n" +
                     "1. Site Type: Whether the content appears fully rendered in the static HTML ('Static') or if it's a dynamic JavaScript application shell with little static text content ('JavaScript').\n" +
                     "2. Row Selector: The CSS or XPath selector that groups each item in the main list (e.g., search results, products, articles).\n" +
                     "3. Row Selector Type: Either 'CSS' or 'XPath'.\n" +
                     "4. Fields: A list of fields within each item row (e.g., 'title', 'price', 'link', 'description', etc.) with their relative CSS/XPath selectors (relative to the row selector).\n" +
                     "5. Estimated Pages: The total number of pages in the pagination list, if any pagination is found in the HTML.\n\n" +
                     "Format your response as a JSON object with keys:\n" +
                     "- 'siteType': string ('Static' or 'JavaScript')\n" +
                     "- 'rowSelector': string\n" +
                     "- 'rowSelectorType': string ('CSS' or 'XPath')\n" +
                     "- 'estimatedPages': integer\n" +
                     "- 'fields': array of objects, each containing:\n" +
                     "  - 'fieldName': string\n" +
                     "  - 'selector': string (relative to the row selector)\n" +
                     "  - 'selectorType': string ('CSS' or 'XPath')\n\n" +
                     "Do not return any markdown formatting outside of JSON. The output must be valid parsable JSON only.\n\n" +
                     "Here is the HTML:\n" + cleanedHtml;

        string responseText = await CallProviderAsync(prompt, settings);
        
        try
        {
            responseText = CleanResponseJson(responseText);
            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;
            
            var analysis = new PageLayoutAnalysis
            {
                SiteType = root.TryGetProperty("siteType", out var s) ? s.GetString() ?? "Static" : "Static",
                RowSelector = root.TryGetProperty("rowSelector", out var r) ? r.GetString() ?? "" : "",
                RowSelectorType = root.TryGetProperty("rowSelectorType", out var rt) ? rt.GetString() ?? "CSS" : "CSS",
                EstimatedPages = root.TryGetProperty("estimatedPages", out var ep) && ep.ValueKind == JsonValueKind.Number ? ep.GetInt32() : 1
            };
            
            if (root.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in fieldsElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        analysis.Fields.Add(new FieldMappingDto
                        {
                            FieldName = item.TryGetProperty("fieldName", out var fn) ? fn.GetString() ?? "" : "",
                            Selector = item.TryGetProperty("selector", out var sel) ? sel.GetString() ?? "" : "",
                            SelectorType = item.TryGetProperty("selectorType", out var st) ? st.GetString() ?? "CSS" : "CSS"
                        });
                    }
                }
            }
            return analysis;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse AI layout analysis: {ex.Message}. Response was: {responseText}", ex);
        }
    }
}

public class PageLayoutAnalysis
{
    public string SiteType { get; set; } = "Static";
    public string RowSelector { get; set; } = string.Empty;
    public string RowSelectorType { get; set; } = "CSS";
    public int EstimatedPages { get; set; } = 1;
    public List<FieldMappingDto> Fields { get; set; } = new();
}

public class FieldMappingDto
{
    public string FieldName { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string SelectorType { get; set; } = "CSS";
}
