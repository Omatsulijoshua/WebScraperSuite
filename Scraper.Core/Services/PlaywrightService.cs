using Microsoft.Playwright;

namespace Scraper.Core.Services;

public sealed class PlaywrightService : IAsyncDisposable
{
    private IPlaywright? _interactivePlaywright;
    private IBrowser? _interactiveBrowser;
    private IBrowserContext? _interactiveContext;
    private IPage? _interactivePage;

    public bool HasInteractiveSession => _interactivePage is not null;
    public string LastLoadSummary { get; private set; } = string.Empty;
    public bool AutoScroll { get; set; } = true;

    public async Task<string> GetHtmlAsync(string url, CancellationToken cancellationToken = default)
    {
        using var playwright = await Playwright.CreateAsync();
        var settings = SettingsService.Load();
        var preferred = settings.PreferredBrowser ?? "Chrome";
        var isCdp = preferred.Contains("CDP", StringComparison.OrdinalIgnoreCase) || preferred.Contains("Running", StringComparison.OrdinalIgnoreCase);

        IBrowser? browser = null;
        IBrowserContext? context = null;
        IPage? page = null;

        try
        {
            if (isCdp)
            {
                try
                {
                    browser = await playwright.Chromium.ConnectOverCDPAsync("http://localhost:9222");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Could not connect to running Chrome on port 9222. Make sure Chrome was launched with '--remote-debugging-port=9222'. Error: " + ex.Message, ex);
                }
                context = browser.Contexts.FirstOrDefault() ?? await browser.NewContextAsync();
                page = await context.NewPageAsync();
            }
            else
            {
                browser = await LaunchBrowserAsync(playwright, headless: true);
                context = await browser.NewContextAsync();
                page = await context.NewPageAsync();
            }

            var capturedJsonUrls = new List<string>();
            EventHandler<IResponse> responseHandler = (_, response) => TrackJsonResponse(response, capturedJsonUrls);
            page.Response += responseHandler;

            try
            {
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });

                await WaitForUsefulContentAsync(page);

                if (AutoScroll && settings.EnableAutoScroll && settings.MaxScrollLoops > 0)
                {
                    await ScrollToInfiniteBottomAsync(page, settings.MaxScrollLoops, cancellationToken);
                }

                LastLoadSummary = await BuildLoadSummaryAsync(page, capturedJsonUrls);
                await CaptureAndInjectMediaAsync(page);
                var html = await page.ContentAsync();
                return html;
            }
            finally
            {
                page.Response -= responseHandler;
                if (page is not null)
                {
                    await page.CloseAsync();
                }
            }
        }
        finally
        {
            if (!isCdp)
            {
                if (context is not null) await context.DisposeAsync();
                if (browser is not null) await browser.DisposeAsync();
            }
        }
    }

    public async Task OpenInteractiveSessionAsync(string url, CancellationToken cancellationToken = default)
    {
        await CloseInteractiveSessionAsync();

        var settings = SettingsService.Load();
        var preferred = settings.PreferredBrowser ?? "Chrome";
        var isCdp = preferred.Contains("CDP", StringComparison.OrdinalIgnoreCase) || preferred.Contains("Running", StringComparison.OrdinalIgnoreCase);

        _interactivePlaywright = await Playwright.CreateAsync();

        if (isCdp)
        {
            try
            {
                _interactiveBrowser = await _interactivePlaywright.Chromium.ConnectOverCDPAsync("http://localhost:9222");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not connect to running Chrome on port 9222. Make sure Chrome was launched with '--remote-debugging-port=9222'. Error: " + ex.Message, ex);
            }
            _interactiveContext = _interactiveBrowser.Contexts.FirstOrDefault() ?? await _interactiveBrowser.NewContextAsync();
            _interactivePage = await _interactiveContext.NewPageAsync();
        }
        else
        {
            _interactiveBrowser = await LaunchBrowserAsync(_interactivePlaywright, headless: false);
            _interactiveContext = await _interactiveBrowser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1440, Height = 960 }
            });
            _interactivePage = await _interactiveContext.NewPageAsync();
        }

        await _interactivePage.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task<string> GetInteractiveHtmlAsync(CancellationToken cancellationToken = default)
    {
        if (_interactivePage is null)
        {
            throw new InvalidOperationException("No approval session is open. Start a manual approval session first.");
        }

        await WaitForUsefulContentAsync(_interactivePage);
        
        var settings = SettingsService.Load();
        if (AutoScroll && settings.EnableAutoScroll && settings.MaxScrollLoops > 0)
        {
            await ScrollToInfiniteBottomAsync(_interactivePage, settings.MaxScrollLoops, cancellationToken);
        }

        LastLoadSummary = await BuildLoadSummaryAsync(_interactivePage, []);
        // Do not throw on cancellation so that we can capture and return the content retrieved so far.
        await CaptureAndInjectMediaAsync(_interactivePage);
        return await _interactivePage.ContentAsync();
    }

    public async Task<string> NavigateInteractiveAndGetHtmlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (_interactivePage is null)
        {
            throw new InvalidOperationException("No approval session is open. Start a manual approval session first.");
        }

        await _interactivePage.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForUsefulContentAsync(_interactivePage);

        var settings = SettingsService.Load();
        if (AutoScroll && settings.EnableAutoScroll && settings.MaxScrollLoops > 0)
        {
            await ScrollToInfiniteBottomAsync(_interactivePage, settings.MaxScrollLoops, cancellationToken);
        }

        LastLoadSummary = await BuildLoadSummaryAsync(_interactivePage, []);
        // Do not throw on cancellation so that we can capture and return the content retrieved so far.
        await CaptureAndInjectMediaAsync(_interactivePage);
        return await _interactivePage.ContentAsync();
    }

    private static async Task ScrollToInfiniteBottomAsync(IPage page, int maxScrolls, CancellationToken cancellationToken)
    {
        try
        {
            for (int i = 0; i < maxScrolls; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var currentHeight = await page.EvaluateAsync<int>("document.body.scrollHeight");
                var viewportHeight = await page.EvaluateAsync<int>("window.innerHeight");
                
                // Gradually scroll down to trigger lazy loading of lists and images
                for (int scrollY = 0; scrollY < currentHeight; scrollY += (viewportHeight > 0 ? viewportHeight : 800))
                {
                    await page.EvaluateAsync($"window.scrollTo(0, {scrollY})");
                    await page.WaitForTimeoutAsync(150);
                }

                // Scroll to absolute bottom to trigger infinite scroll loading
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                await page.WaitForTimeoutAsync(1500);

                // Run dynamic media capture step at each scroll iteration
                await RunCaptureStepAsync(page);

                var newHeight = await page.EvaluateAsync<int>("document.body.scrollHeight");
                if (newHeight == currentHeight)
                {
                    // Secondary check/wait
                    await page.WaitForTimeoutAsync(1000);
                    newHeight = await page.EvaluateAsync<int>("document.body.scrollHeight");
                    if (newHeight == currentHeight)
                    {
                        break; // Scrolling did not trigger height change, assume end of page
                    }
                }
            }
        }
        catch
        {
            // Fail-safe scrolling
        }
    }

    private static async Task RunCaptureStepAsync(IPage page)
    {
        try
        {
            var captureScript = 
                """
                (() => {
                    if (!window.__capturedMedia) {
                        window.__capturedMedia = [];
                    }
                    
                    const resolveUrl = (url) => {
                        if (!url) return '';
                        try {
                            return new URL(url, document.baseURI).href;
                        } catch(e) {
                            return url;
                        }
                    };

                    const addMedia = (url, type) => {
                        const absolute = resolveUrl(url);
                        if (absolute && !window.__capturedMedia.some(item => item.url === absolute)) {
                            window.__capturedMedia.push({ url: absolute, type: type });
                        }
                    };
                    
                    const attrs = ['data-src', 'data-lazy-src', 'data-original', 'data-srcset', 'srcset', 'src'];
                    document.querySelectorAll('img').forEach(img => {
                        for (const attr of attrs) {
                            const val = img.getAttribute(attr);
                            if (val && !val.startsWith('data:image/')) {
                                addMedia(val, 'Image');
                            }
                        }
                    });

                    document.querySelectorAll('video, video source').forEach(el => {
                        const val = el.getAttribute('src');
                        if (val) {
                            addMedia(val, 'Video');
                        }
                    });

                    document.querySelectorAll('audio, audio source').forEach(el => {
                        const val = el.getAttribute('src');
                        if (val) {
                            addMedia(val, 'Audio');
                        }
                    });
                })()
                """;

            await page.EvaluateAsync(captureScript);
        }
        catch
        {
            // Fail-safe capture
        }
    }

    private static async Task CaptureAndInjectMediaAsync(IPage page)
    {
        try
        {
            // Run one final capture to catch whatever is currently on the screen
            await RunCaptureStepAsync(page);

            var injectScript =
                """
                (() => {
                    if (!window.__capturedMedia || window.__capturedMedia.length === 0) return;
                    
                    let container = document.getElementById('captured-media-archive');
                    if (!container) {
                        container = document.createElement('div');
                        container.id = 'captured-media-archive';
                        container.style.display = 'none';
                        document.body.appendChild(container);
                    }
                    
                    container.innerHTML = '';
                    
                    window.__capturedMedia.forEach(item => {
                        if (item.type === 'Image') {
                            const img = document.createElement('img');
                            img.src = item.url;
                            container.appendChild(img);
                        } else if (item.type === 'Video') {
                            const video = document.createElement('video');
                            video.src = item.url;
                            container.appendChild(video);
                        } else if (item.type === 'Audio') {
                            const audio = document.createElement('audio');
                            audio.src = item.url;
                            container.appendChild(audio);
                        }
                    });
                })()
                """;

            await page.EvaluateAsync(injectScript);
        }
        catch
        {
            // Fail-safe injection
        }
    }

    public async Task<string?> GetInteractiveUrlAsync()
    {
        if (_interactivePage is null)
        {
            return null;
        }

        return await Task.FromResult(_interactivePage.Url);
    }

    public async Task CloseInteractiveSessionAsync()
    {
        var settings = SettingsService.Load();
        var preferred = settings.PreferredBrowser ?? "Chrome";
        var isCdp = preferred.Contains("CDP", StringComparison.OrdinalIgnoreCase) || preferred.Contains("Running", StringComparison.OrdinalIgnoreCase);

        if (_interactivePage is not null)
        {
            await _interactivePage.CloseAsync();
            _interactivePage = null;
        }

        if (_interactiveContext is not null)
        {
            if (!isCdp)
            {
                await _interactiveContext.CloseAsync();
            }
            _interactiveContext = null;
        }

        if (_interactiveBrowser is not null)
        {
            if (!isCdp)
            {
                await _interactiveBrowser.CloseAsync();
            }
            _interactiveBrowser = null;
        }

        if (_interactivePlaywright is not null)
        {
            _interactivePlaywright.Dispose();
            _interactivePlaywright = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseInteractiveSessionAsync();
        GC.SuppressFinalize(this);
    }

    private static async Task WaitForUsefulContentAsync(IPage page)
    {
        try
        {
            await page.WaitForSelectorAsync("#app, body", new PageWaitForSelectorOptions
            {
                Timeout = 5000
            });
        }
        catch (TimeoutException)
        {
            // Continue and let later heuristics decide whether the page is usable.
        }

        try
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
            {
                Timeout = 10000
            });
        }
        catch (TimeoutException)
        {
            // Some SPA pages keep background requests open; continue with the best rendered state we have.
        }

        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                    const app = document.querySelector('#app');
                    const bodyTextLength = (document.body?.innerText || '').trim().length;
                    const appTextLength = (app?.innerText || '').trim().length;
                    const appChildren = app?.children?.length || 0;
                    const hasTabs = document.querySelectorAll('.nav-tabs .nav-link, [role="tab"]').length > 0;
                    const hasTableHeaders = document.querySelectorAll('.datatable-header, thead th').length > 0;
                    const hasTableRows = document.querySelectorAll('tbody tr, .datatable-row').length > 0;
                    return bodyTextLength > 200 || appTextLength > 80 || appChildren > 3 || hasTabs || hasTableHeaders || hasTableRows;
                }
                """,
                null,
                new PageWaitForFunctionOptions
                {
                    Timeout = 10000
                });
        }
        catch (TimeoutException)
        {
            // Rendered content never materialized enough to satisfy the heuristic.
        }

        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                    const bodyText = (document.body?.innerText || '').toLowerCase();
                    const spinnerVisible = [...document.querySelectorAll('.spinner, .spinner-border, [class*="spinner"]')]
                        .some(element => getComputedStyle(element).display !== 'none' && getComputedStyle(element).visibility !== 'hidden');
                    const hasRows = document.querySelectorAll('tbody tr, .datatable-row').length > 0;
                    const hasHeaders = document.querySelectorAll('.datatable-header, thead th').length > 0;
                    const hasEmptyState = [...document.querySelectorAll('.no-entries, .error-message-container')]
                        .some(element => getComputedStyle(element).display !== 'none' && getComputedStyle(element).visibility !== 'hidden');
                    const loadingVisible = bodyText.includes('loading. please wait');
                    return hasRows || hasEmptyState || (hasHeaders && !spinnerVisible && !loadingVisible);
                }
                """,
                null,
                new PageWaitForFunctionOptions
                {
                    Timeout = 12000
                });
        }
        catch (TimeoutException)
        {
            // Some pages never settle cleanly; take the best DOM snapshot we can.
        }

        await page.WaitForTimeoutAsync(2000);
    }

    private static void TrackJsonResponse(IResponse response, List<string> capturedJsonUrls)
    {
        try
        {
            var contentType = response.Headers.TryGetValue("content-type", out var value) ? value : string.Empty;
            if (!contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (capturedJsonUrls.Contains(response.Url, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            capturedJsonUrls.Add(response.Url);
            if (capturedJsonUrls.Count > 10)
            {
                capturedJsonUrls.RemoveAt(0);
            }
        }
        catch
        {
            // Diagnostics capture should never break page loading.
        }
    }

    private static async Task<string> BuildLoadSummaryAsync(IPage page, IReadOnlyCollection<string> capturedJsonUrls)
    {
        var diagnostics = await page.EvaluateAsync<PageDiagnostics>(
            """
            () => {
                const visible = selector => [...document.querySelectorAll(selector)]
                    .some(element => getComputedStyle(element).display !== 'none' && getComputedStyle(element).visibility !== 'hidden');

                return {
                    hasRows: document.querySelectorAll('tbody tr, .datatable-row').length > 0,
                    rowCount: document.querySelectorAll('tbody tr, .datatable-row').length,
                    hasHeaders: document.querySelectorAll('.datatable-header, thead th').length > 0,
                    loadingVisible: (document.body?.innerText || '').toLowerCase().includes('loading. please wait'),
                    noEntriesVisible: visible('.no-entries'),
                    hasUndefinedValues: (document.body?.innerText || '').includes('undefined')
                };
            }
            """);

        var parts = new List<string>();
        if (diagnostics.HasRows)
        {
            parts.Add($"Detected {diagnostics.RowCount} rendered row(s) in the page.");
        }
        else if (diagnostics.NoEntriesVisible)
        {
            parts.Add("The rendered page reached an empty-state message instead of result rows.");
        }
        else if (diagnostics.LoadingVisible)
        {
            parts.Add("The page still showed a loading message when the HTML snapshot was taken.");
        }
        else if (diagnostics.HasHeaders)
        {
            parts.Add("The table headers rendered, but no result rows were present yet.");
        }

        if (diagnostics.HasUndefinedValues)
        {
            parts.Add("Some visible values were still unresolved and appeared as 'undefined'.");
        }

        if (capturedJsonUrls.Count > 0)
        {
            parts.Add($"Observed {capturedJsonUrls.Count} JSON/API response(s) while the page loaded.");
        }

        return string.Join(" ", parts);
    }

    private static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright, bool headless)
    {
        var settings = SettingsService.Load();
        var preferred = settings.PreferredBrowser ?? "Chrome";
        if (preferred.Contains("CDP", StringComparison.OrdinalIgnoreCase) || preferred.Contains("Running", StringComparison.OrdinalIgnoreCase))
        {
            preferred = "Chrome";
        }

        try
        {
            return await LaunchSpecificBrowserAsync(playwright, preferred, headless);
        }
        catch
        {
            // Fallback chain
            var fallbackOrder = new[] { "Chrome", "Edge", "Chromium", "Firefox", "WebKit" };
            foreach (var browser in fallbackOrder)
            {
                if (string.Equals(browser, preferred, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Already tried and failed
                }
                try
                {
                    return await LaunchSpecificBrowserAsync(playwright, browser, headless);
                }
                catch
                {
                    // Keep trying other fallbacks
                }
            }
        }

        throw new InvalidOperationException("No compatible browser could be launched. Please ensure at least one supported browser is installed (Chrome, Edge, Firefox, or Playwright Chromium).");
    }

    private static async Task<IBrowser> LaunchSpecificBrowserAsync(IPlaywright playwright, string browser, bool headless)
    {
        switch (browser.ToUpperInvariant())
        {
            case "CHROME":
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, Channel = "chrome" });
            case "EDGE":
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, Channel = "msedge" });
            case "FIREFOX":
                return await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
            case "WEBKIT":
                return await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
            case "CHROMIUM":
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
            case "OPERA":
                var operaPath = FindBrowserExecutable("Opera");
                if (string.IsNullOrEmpty(operaPath)) throw new FileNotFoundException("Opera browser executable not found.");
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, ExecutablePath = operaPath });
            case "BRAVE":
                var bravePath = FindBrowserExecutable("Brave");
                if (string.IsNullOrEmpty(bravePath)) throw new FileNotFoundException("Brave browser executable not found.");
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, ExecutablePath = bravePath });
            case "VIVALDI":
                var vivaldiPath = FindBrowserExecutable("Vivaldi");
                if (string.IsNullOrEmpty(vivaldiPath)) throw new FileNotFoundException("Vivaldi browser executable not found.");
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, ExecutablePath = vivaldiPath });
            default:
                throw new NotSupportedException($"Browser '{browser}' is not supported.");
        }
    }

    private static string? FindBrowserExecutable(string name)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var pathsToTry = new List<string>();

        if (name.Equals("Opera", StringComparison.OrdinalIgnoreCase))
        {
            pathsToTry.Add(Path.Combine(localAppData, "Programs", "Opera", "launcher.exe"));
            pathsToTry.Add(Path.Combine(programFiles, "Opera", "launcher.exe"));
            pathsToTry.Add(Path.Combine(programFilesX86, "Opera", "launcher.exe"));
            pathsToTry.Add(Path.Combine(localAppData, "Programs", "Opera", "opera.exe"));
            pathsToTry.Add(Path.Combine(programFiles, "Opera", "opera.exe"));
            pathsToTry.Add(Path.Combine(programFilesX86, "Opera", "opera.exe"));
        }
        else if (name.Equals("Brave", StringComparison.OrdinalIgnoreCase))
        {
            pathsToTry.Add(Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));
            pathsToTry.Add(Path.Combine(programFiles, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));
            pathsToTry.Add(Path.Combine(programFilesX86, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));
        }
        else if (name.Equals("Vivaldi", StringComparison.OrdinalIgnoreCase))
        {
            pathsToTry.Add(Path.Combine(localAppData, "Vivaldi", "Application", "vivaldi.exe"));
            pathsToTry.Add(Path.Combine(programFiles, "Vivaldi", "Application", "vivaldi.exe"));
            pathsToTry.Add(Path.Combine(programFilesX86, "Vivaldi", "Application", "vivaldi.exe"));
        }

        foreach (var path in pathsToTry)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private sealed class PageDiagnostics
    {
        public bool HasRows { get; set; }
        public int RowCount { get; set; }
        public bool HasHeaders { get; set; }
        public bool LoadingVisible { get; set; }
        public bool NoEntriesVisible { get; set; }
        public bool HasUndefinedValues { get; set; }
    }
}
