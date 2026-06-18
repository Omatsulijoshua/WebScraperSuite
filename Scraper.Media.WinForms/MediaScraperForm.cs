using System.Diagnostics;
using Scraper.Core.Models;
using Scraper.Core.Services;
using System.Text.RegularExpressions;

namespace Scraper.Media.WinForms;

public class MediaScraperForm : Form
{
    private readonly HttpScraperService _httpScraper = new();
    private readonly PlaywrightService _playwrightScraper = new();
    private readonly MediaDiscoveryService _mediaDiscovery = new();
    private readonly MediaDownloader _downloader = new();

    private readonly TextBox _urlBox = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _siteTypeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly CheckBox _imagesBox = new() { Text = "Images", Checked = true, ForeColor = Color.White, AutoSize = true };
    private readonly CheckBox _videosBox = new() { Text = "Videos", Checked = true, ForeColor = Color.White, AutoSize = true };
    private readonly CheckBox _audioBox = new() { Text = "Audio", Checked = true, ForeColor = Color.White, AutoSize = true };
    private readonly CheckBox _autoScrollBox = new() { Text = "Auto-Scroll", Checked = true, ForeColor = Color.White, AutoSize = true };
    private Button? _stopScrollButton;
    private CancellationTokenSource? _scrollCts;
    private readonly ComboBox _downloadToggle = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox _folderBox = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _pageModeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly NumericUpDown _pageNumberBox = new() { Minimum = 1, Maximum = 500, Value = 1, Width = 80 };
    private readonly NumericUpDown _pageRangeStartBox = new() { Minimum = 1, Maximum = 500, Value = 1, Width = 80 };
    private readonly NumericUpDown _pageRangeEndBox = new() { Minimum = 1, Maximum = 500, Value = 5, Width = 80 };
    private readonly Label _pageInfoLabel = new() { Dock = DockStyle.Fill, ForeColor = Color.Gainsboro, TextAlign = ContentAlignment.MiddleLeft, Text = "Page 1" };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, AllowUserToAddRows = false };
    private readonly ListBox _activityList = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.None };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Bottom, Height = 20 };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, ForeColor = Color.Gainsboro };
    private readonly Label _insightLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 54,
        ForeColor = Color.Silver,
        Text = "Scan pages for media with either a normal fetch or JavaScript rendering. If a site asks for login or a verification challenge, use manual approval first and then resume the scan."
    };
    private readonly Label _approvalLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 44,
        ForeColor = Color.FromArgb(167, 243, 208),
        Text = "Manual approval opens a live browser so you can complete login or page verification yourself before scanning media.",
        Visible = false
    };

    private List<MediaItem> _items = [];
    private string _loadedHtml = string.Empty;
    private bool _isBusy;
    private int? _estimatedTotalPages;

    public MediaScraperForm()
    {
        Text = "WebScraper Pro - Media Scraper";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1080, 720);
        Size = new Size(1180, 760);
        BackColor = Color.FromArgb(15, 23, 42);
        Font = new Font("Segoe UI", 10F);

        _siteTypeBox.Items.AddRange(["Static", "JavaScript"]);
        _siteTypeBox.SelectedIndex = 0;
        _downloadToggle.Items.AddRange(["Yes", "No"]);
        _downloadToggle.SelectedIndex = 1;
        _pageModeBox.Items.AddRange(["This page", "All pages", "Page range"]);
        _pageModeBox.SelectedIndex = 0;
        _pageModeBox.SelectedIndexChanged += (_, _) => UpdatePageModeUi();

        Controls.Add(BuildLayout());
        ApplyGridTheme(_grid);
        FormClosed += async (_, _) => await _playwrightScraper.DisposeAsync();

        var settings = SettingsService.Load();
        _autoScrollBox.Checked = settings.EnableAutoScroll;

        UpdatePageModeUi();
        SetStatus("Ready to scan for page media.");
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 3
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildControlCard(), 0, 1);
        root.Controls.Add(BuildGridCard(), 0, 2);
        return root;
    }

    private Control BuildHeader()
    {
        var card = CreateCard();
        card.Height = 132;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = "Media Scraper",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text = "Scan pages for images, videos, and audio, including JavaScript-rendered pages. You can manually approve protected pages in a live browser and then resume scanning from that approved session.",
            AutoSize = true,
            ForeColor = Color.Silver,
            MaximumSize = new Size(1080, 0)
        }, 0, 1);

        card.Controls.Add(layout);

        return card;
    }

    private Control BuildControlCard()
    {
        var card = CreateCard();
        card.Height = 286;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        layout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;

        layout.Controls.Add(CreateRowLabel("URL"), 0, 0);
        layout.SetColumnSpan(_urlBox, 5);
        layout.Controls.Add(_urlBox, 1, 0);

        layout.Controls.Add(CreateRowLabel("Site Type"), 0, 1);
        layout.Controls.Add(_siteTypeBox, 1, 1);
        layout.Controls.Add(CreateRowLabel("Media Type"), 2, 1);
        var mediaOptions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0), AutoScroll = true };
        mediaOptions.Controls.Add(_imagesBox);
        mediaOptions.Controls.Add(_videosBox);
        mediaOptions.Controls.Add(_audioBox);
        mediaOptions.Controls.Add(_autoScrollBox);
        layout.SetColumnSpan(mediaOptions, 3);
        layout.Controls.Add(mediaOptions, 3, 1);

        layout.Controls.Add(CreateRowLabel("Download"), 0, 2);
        layout.Controls.Add(_downloadToggle, 1, 2);

        layout.Controls.Add(CreateRowLabel("Folder"), 2, 2);
        layout.SetColumnSpan(_folderBox, 3);
        layout.Controls.Add(_folderBox, 3, 2);

        layout.Controls.Add(CreateRowLabel("Page"), 0, 3);
        var pageRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 36,
            WrapContents = false,
            Margin = new Padding(0, 2, 0, 0),
            AutoScroll = false
        };
        pageRow.Controls.Add(_pageModeBox);
        pageRow.Controls.Add(_pageNumberBox);
        pageRow.Controls.Add(new Label { Text = "From", ForeColor = Color.Gainsboro, Width = 40, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 9, 0, 0) });
        pageRow.Controls.Add(_pageRangeStartBox);
        pageRow.Controls.Add(new Label { Text = "To", ForeColor = Color.Gainsboro, Width = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 9, 0, 0) });
        pageRow.Controls.Add(_pageRangeEndBox);
        pageRow.Controls.Add(CreateButton("Load Page", async (_, _) => await LoadHtmlForScanAsync(), 110, accent: true));
        pageRow.Controls.Add(CreateButton("Prev", async (_, _) => await ChangePageAsync(-1), 90));
        pageRow.Controls.Add(CreateButton("Next", async (_, _) => await ChangePageAsync(1), 90));
        pageRow.Controls.Add(_pageInfoLabel);
        layout.SetColumnSpan(pageRow, 5);
        layout.Controls.Add(pageRow, 1, 3);

        _stopScrollButton = CreateButton("Stop Scroll", (_, _) => StopAutoScroll(), 120);

        var actionRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 42,
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0, 6, 0, 0),
            Padding = new Padding(0)
        };
        actionRow.Controls.Add(CreateButton("Open Chrome", (_, _) => OpenChromeWithCdp(), 130));
        actionRow.Controls.Add(CreateButton("Browse", (_, _) => BrowseFolder(), 110));
        actionRow.Controls.Add(CreateButton("Load", async (_, _) => await LoadHtmlForScanAsync(), 120, accent: true));
        actionRow.Controls.Add(CreateButton("Approve / Login", async (_, _) => await StartApprovalSessionAsync(), 150));
        actionRow.Controls.Add(CreateButton("Resume Approved", async (_, _) => await ResumeApprovedSessionAsync(), 150));
        actionRow.Controls.Add(_stopScrollButton);
        actionRow.Controls.Add(CreateButton("Scan", async (_, _) => await ScanLoadedHtmlAsync(), 120));
        actionRow.Controls.Add(CreateButton("AI Smart Scan", async (_, _) => await RunAiMediaScanAsync(), 130, accent: true));
        actionRow.Controls.Add(CreateButton("Download", async (_, _) => await DownloadAsync(), 120));
        layout.SetColumnSpan(actionRow, 5);
        layout.Controls.Add(actionRow, 1, 4);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildGridCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;

        var tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };

        var previewTab = new TabPage("Preview Grid") { BackColor = Color.FromArgb(15, 23, 42) };
        _grid.Dock = DockStyle.Fill;
        previewTab.Controls.Add(_grid);

        var logTab = new TabPage("Activity Log") { BackColor = Color.FromArgb(15, 23, 42) };
        _activityList.Dock = DockStyle.Fill;
        logTab.Controls.Add(_activityList);

        tabControl.TabPages.Add(previewTab);
        tabControl.TabPages.Add(logTab);

        card.Controls.Add(tabControl);
        card.Controls.Add(_insightLabel);
        card.Controls.Add(_approvalLabel);
        card.Controls.Add(_progress);
        card.Controls.Add(_status);
        return card;
    }

    private async Task LoadHtmlForScanAsync()
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progress.Value);
            return;
        }

        if (string.IsNullOrWhiteSpace(_urlBox.Text))
        {
            SetStatus("Enter a URL to load.");
            return;
        }

        try
        {
            _isBusy = true;
            _approvalLabel.Visible = false;
            LogActivity("Starting page load for media scan.");
            SetProgress(15);
            var loadPage = GetPrimaryPageForLoad();
            var input = BuildEffectiveUrlForPage(_urlBox.Text.Trim(), loadPage);
            SetStatus($"Fetching page {loadPage} HTML...");
            LogActivity($"Loading page {loadPage}: {input}");

            _scrollCts = new CancellationTokenSource();
            _playwrightScraper.AutoScroll = _autoScrollBox.Checked;

            _loadedHtml = (_siteTypeBox.SelectedItem?.ToString() ?? "Static") == "JavaScript"
                ? await _playwrightScraper.GetHtmlAsync(input, _scrollCts.Token)
                : await _httpScraper.GetHtmlAsync(input, _scrollCts.Token);
            _urlBox.Text = input;
            _pageNumberBox.Value = Math.Max(_pageNumberBox.Minimum, Math.Min(_pageNumberBox.Maximum, loadPage));
            _estimatedTotalPages = TryEstimateTotalPages(_loadedHtml);
            UpdatePageInfoLabel();

            SetProgress(100);
            _insightLabel.Text = string.IsNullOrWhiteSpace(_playwrightScraper.LastLoadSummary)
                ? "Page HTML loaded for media scanning. Click Scan to discover media files from the rendered page."
                : $"Page HTML loaded for media scanning. {_playwrightScraper.LastLoadSummary} Click Scan to discover media files from the rendered page.";
            LogActivity($"Page {loadPage} loaded successfully.");
            SetStatus($"Page {loadPage} HTML loaded. Click Scan to discover media.", 100);

            if (_downloadToggle.SelectedItem?.ToString() == "Yes")
            {
                await ScanLoadedHtmlAsync();
                await DownloadAsync();
            }
        }
        catch (Exception ex)
        {
            SetProgress(0);
            SetStatus($"Load failed: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }

        if (!string.IsNullOrWhiteSpace(_loadedHtml))
        {
            var settings = SettingsService.Load();
            if (settings.EnableAutopilot && !string.IsNullOrEmpty(settings.ApiKey))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    this.BeginInvoke(new Action(async () => await RunAiMediaScanAsync()));
                });
            }
        }
    }

    private async Task ScanLoadedHtmlAsync()
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progress.Value);
            return;
        }

        if (string.IsNullOrWhiteSpace(_loadedHtml))
        {
            SetStatus("Load a page first, or resume an approved session before scanning.", 0);
            return;
        }

        try
        {
            _isBusy = true;
            _approvalLabel.Visible = false;
            _items = [];

            _scrollCts = new CancellationTokenSource();
            _playwrightScraper.AutoScroll = _autoScrollBox.Checked;

            var mode = _pageModeBox.SelectedItem?.ToString() ?? "This page";
            if (mode == "All pages")
            {
                await ScanAllPagesAsync();
            }
            else
            {
                var pages = ResolvePagesToScan();
                var totalPages = pages.Count;
                LogActivity($"Starting media scan across {totalPages} page(s).");
                for (var index = 0; index < totalPages; index++)
                {
                    var page = pages[index];
                    var pageUrl = BuildEffectiveUrlForPage(_urlBox.Text.Trim(), page);
                    var html = index == 0 && page == (int)_pageNumberBox.Value
                        ? _loadedHtml
                        : await FetchPageHtmlAsync(page);
                    SetStatus($"Scanning page {page} ({index + 1}/{totalPages})...", CalculateProgress(index, totalPages, 15, 80));
                    LogActivity($"Scanning page {page} ({index + 1}/{totalPages}).");
                    var pageItems = _mediaDiscovery.Discover(html, pageUrl, _imagesBox.Checked, _videosBox.Checked, _audioBox.Checked);
                    foreach (var item in pageItems)
                    {
                        item.Status = $"Page {page}";
                    }

                    _items.AddRange(pageItems);
                    LogActivity($"Found {pageItems.Count} media file(s) on page {page}.");
                }
            }

            _items = _items
                .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            _grid.DataSource = null;
            _grid.DataSource = _items;
            SetProgress(100);
            _insightLabel.Text = _items.Count == 0
                ? "No media URLs were discovered in the current HTML snapshot. Some sites use protected or adaptive streaming, so a manual approval session may load the page but still not expose direct downloadable media URLs."
                : $"Scan complete. {_items.Count} media file(s) were discovered across {GetScannedPageCount()} page(s).";
            LogActivity($"Scan complete across {GetScannedPageCount()} page(s): {_items.Count} media file(s) discovered.");
            SetStatus($"Scan complete. {_items.Count} media files discovered across {GetScannedPageCount()} page(s).");
        }
        catch (Exception ex)
        {
            SetProgress(0);
            SetStatus($"Scan failed: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task StartApprovalSessionAsync()
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progress.Value);
            return;
        }

        if (string.IsNullOrWhiteSpace(_urlBox.Text) || _urlBox.Text.Contains('<'))
        {
            SetStatus("Enter a real URL before starting manual approval mode.", 0);
            return;
        }

        try
        {
            _isBusy = true;
            _siteTypeBox.SelectedItem = "JavaScript";
            SetProgress(25);
            SetStatus("Opening live browser for manual approval...");
            var loadPage = GetPrimaryPageForLoad();
            var input = BuildEffectiveUrlForPage(_urlBox.Text.Trim(), loadPage);
            LogActivity($"Opening approval browser for page {loadPage}: {input}");
            await _playwrightScraper.OpenInteractiveSessionAsync(input);
            _approvalLabel.Text = "Approval browser opened. Complete login or the verification check yourself there, then come back here and click Resume Approved.";
            _approvalLabel.Visible = true;
            _insightLabel.Text = "Manual approval browser is ready. Once the real page is visible, click Resume Approved and then Scan.";
            SetProgress(60);
            SetStatus("Approval browser ready. Finish the page challenge, then click Resume Approved.");
        }
        catch (Exception ex)
        {
            SetProgress(0);
            SetStatus($"Approval browser failed: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task ResumeApprovedSessionAsync()
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progress.Value);
            return;
        }

        if (!_playwrightScraper.HasInteractiveSession)
        {
            SetStatus("No approval browser is open yet. Click Approve / Login first.", 0);
            return;
        }

        try
        {
            _isBusy = true;
            SetProgress(70);
            SetStatus("Resuming from approved browser session...");
            LogActivity("Capturing HTML from approved browser session.");

            _scrollCts = new CancellationTokenSource();
            _playwrightScraper.AutoScroll = _autoScrollBox.Checked;

            _loadedHtml = await _playwrightScraper.GetInteractiveHtmlAsync(_scrollCts.Token);
            var currentUrl = await _playwrightScraper.GetInteractiveUrlAsync();
            if (!string.IsNullOrWhiteSpace(currentUrl))
            {
                _urlBox.Text = currentUrl;
                var detectedPage = GetPageNumber(currentUrl);
                if (detectedPage.HasValue)
                {
                    _pageNumberBox.Value = Math.Max(_pageNumberBox.Minimum, Math.Min(_pageNumberBox.Maximum, detectedPage.Value));
                }
            }
            _estimatedTotalPages = TryEstimateTotalPages(_loadedHtml);
            UpdatePageInfoLabel();

            _approvalLabel.Text = "Approved session captured. You can now scan the same page content that was opened after your manual verification.";
            _approvalLabel.Visible = true;
            _insightLabel.Text = string.IsNullOrWhiteSpace(_playwrightScraper.LastLoadSummary)
                ? "Approved session HTML loaded. Click Scan to discover media on the verified page."
                : $"Approved session HTML loaded. {_playwrightScraper.LastLoadSummary} Click Scan to discover media on the verified page.";
            SetProgress(100);
            LogActivity("Approved session HTML captured successfully.");
            SetStatus("Approved session HTML loaded. Click Scan.");
        }
        catch (Exception ex)
        {
            SetProgress(0);
            SetStatus($"Resume failed: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }

        if (!string.IsNullOrWhiteSpace(_loadedHtml))
        {
            var settings = SettingsService.Load();
            if (settings.EnableAutopilot && !string.IsNullOrEmpty(settings.ApiKey))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    this.BeginInvoke(new Action(async () => await RunAiMediaScanAsync()));
                });
            }
        }
    }

    private async Task DownloadAsync()
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progress.Value);
            return;
        }

        if (_items.Count == 0)
        {
            SetStatus("Scan for media before downloading.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_folderBox.Text))
        {
            SetStatus("Choose a destination folder first.");
            return;
        }

        try
        {
            _isBusy = true;
            _approvalLabel.Visible = false;
            Directory.CreateDirectory(_folderBox.Text);
            var total = _items.Count;
            var completed = 0;
            LogActivity($"Starting media download for {total} item(s).");

            foreach (var item in _items)
            {
                try
                {
                    var fileName = string.IsNullOrWhiteSpace(item.FileName) ? $"{Guid.NewGuid():N}.bin" : item.FileName;
                    var destination = Path.Combine(_folderBox.Text, fileName);
                    SetStatus($"Downloading {completed + 1}/{total}: {fileName}", CalculateProgress(completed, total, 10, 95));
                    await _downloader.DownloadAsync(item.Url, destination);
                    item.Status = "Downloaded";
                }
                catch
                {
                    item.Status = "Failed";
                }

                completed++;
                SetProgress(CalculateProgress(completed, total, 10, 100));
                LogActivity($"Downloaded {completed}/{total}: {item.FileName} ({item.Status})");
            }

            _grid.Refresh();
            SetStatus("Media download run completed.");
            LogActivity("Media download run completed.");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task ChangePageAsync(int delta)
    {
        if ((_pageModeBox.SelectedItem?.ToString() ?? "This page") != "This page")
        {
            _pageModeBox.SelectedItem = "This page";
            UpdatePageModeUi();
        }

        var nextValue = (int)_pageNumberBox.Value + delta;
        if (nextValue < _pageNumberBox.Minimum || nextValue > _pageNumberBox.Maximum)
        {
            return;
        }

        _pageNumberBox.Value = nextValue;
        await LoadHtmlForScanAsync();
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folderBox.Text = dialog.SelectedPath;
        }
    }

    private static void OpenChromeWithCdp()
    {
        Process.Start(new ProcessStartInfo("cmd.exe", "/c start chrome --remote-debugging-port=9222")
        {
            UseShellExecute = true,
            CreateNoWindow = true
        });
    }

    private void StopAutoScroll()
    {
        if (_scrollCts is not null && !_scrollCts.IsCancellationRequested)
        {
            _scrollCts.Cancel();
            LogActivity("Stopping auto-scroll...");
            SetStatus("Stopping auto-scroll. Fetching current page state...");
        }
    }

    private void SetStatus(string message) => _status.Text = message;

    private void SetStatus(string message, int progress)
    {
        _status.Text = message;
        SetProgress(progress);
    }

    private void SetProgress(int value) => _progress.Value = Math.Max(_progress.Minimum, Math.Min(_progress.Maximum, value));

    private void LogActivity(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _activityList.Items.Insert(0, $"{timestamp}  {message}");
        while (_activityList.Items.Count > 100)
        {
            _activityList.Items.RemoveAt(_activityList.Items.Count - 1);
        }
    }

    private void UpdatePageInfoLabel()
    {
        var mode = _pageModeBox.SelectedItem?.ToString() ?? "This page";
        _pageInfoLabel.Text = mode switch
        {
            "All pages" when _estimatedTotalPages.HasValue => $"All pages (1-{_estimatedTotalPages.Value})",
            "All pages" => "All pages",
            "Page range" => $"Pages {(int)_pageRangeStartBox.Value}-{(int)_pageRangeEndBox.Value}",
            _ when _estimatedTotalPages.HasValue => $"Page {(int)_pageNumberBox.Value} of {_estimatedTotalPages.Value}",
            _ => $"Page {(int)_pageNumberBox.Value}"
        };
    }

    private void UpdatePageModeUi()
    {
        var mode = _pageModeBox.SelectedItem?.ToString() ?? "This page";
        var isSinglePage = mode == "This page";
        var isRange = mode == "Page range";
        _pageNumberBox.Enabled = isSinglePage;
        _pageRangeStartBox.Enabled = isRange;
        _pageRangeEndBox.Enabled = isRange;
        UpdatePageInfoLabel();
    }

    private int GetPrimaryPageForLoad()
    {
        var mode = _pageModeBox.SelectedItem?.ToString() ?? "This page";
        return mode switch
        {
            "Page range" => (int)_pageRangeStartBox.Value,
            _ => (int)_pageNumberBox.Value
        };
    }

    private List<int> ResolvePagesToScan()
    {
        var mode = _pageModeBox.SelectedItem?.ToString() ?? "This page";
        return mode switch
            {
            "All pages" => Enumerable.Range(1, Math.Max(1, _estimatedTotalPages ?? (int)_pageNumberBox.Value)).ToList(),
            "Page range" => BuildPageRange(),
            _ => [(int)_pageNumberBox.Value]
        };
    }

    private async Task ScanAllPagesAsync()
    {
        var baseUrl = BuildEffectiveUrlForPage(_urlBox.Text.Trim(), 1);
        var currentUrl = baseUrl;
        var currentHtml = _loadedHtml;
        var pageNumber = 1;
        var nextUrl = DiscoverNextPageUrl(currentHtml, currentUrl);
        var discoveredPageCount = _estimatedTotalPages ?? TryEstimateTotalPages(currentHtml);
        var maxPagesToScan = Math.Max(1, (int)_pageNumberBox.Maximum);
        var scannedPages = 0;

        LogActivity(discoveredPageCount.HasValue
            ? $"Starting media scan across all detected pages (up to {discoveredPageCount.Value})."
            : $"Starting media scan across all pages until no next page is found (safety cap {maxPagesToScan}).");

        while (pageNumber <= maxPagesToScan)
        {
            var pageLabel = discoveredPageCount.HasValue ? $"{pageNumber}/{discoveredPageCount.Value}" : pageNumber.ToString();
            SetStatus($"Scanning page {pageLabel}...", CalculateProgress(pageNumber - 1, discoveredPageCount ?? maxPagesToScan, 15, 80));
            LogActivity($"Scanning page {pageLabel}.");

            var pageItems = _mediaDiscovery.Discover(currentHtml, currentUrl, _imagesBox.Checked, _videosBox.Checked, _audioBox.Checked);
            foreach (var item in pageItems)
            {
                item.Status = $"Page {pageNumber}";
            }

            _items.AddRange(pageItems);
            scannedPages++;
            LogActivity($"Found {pageItems.Count} media file(s) on page {pageNumber}.");

            if (discoveredPageCount.HasValue && pageNumber >= discoveredPageCount.Value)
            {
                break;
            }

            var upcomingUrl = !string.IsNullOrWhiteSpace(nextUrl)
                ? nextUrl
                : BuildEffectiveUrlForPage(baseUrl, pageNumber + 1);

            if (string.IsNullOrWhiteSpace(upcomingUrl) || string.Equals(upcomingUrl, currentUrl, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            pageNumber++;
            currentUrl = upcomingUrl;
            currentHtml = await FetchHtmlForUrlAsync(currentUrl, pageNumber);
            discoveredPageCount ??= TryEstimateTotalPages(currentHtml);
            nextUrl = DiscoverNextPageUrl(currentHtml, currentUrl);

            if (!discoveredPageCount.HasValue && string.IsNullOrWhiteSpace(nextUrl))
            {
                var fallbackCandidate = BuildEffectiveUrlForPage(baseUrl, pageNumber + 1);
                if (string.Equals(fallbackCandidate, currentUrl, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
        }

        _estimatedTotalPages ??= scannedPages;
    }

    private List<int> BuildPageRange()
    {
        var start = (int)Math.Min(_pageRangeStartBox.Value, _pageRangeEndBox.Value);
        var end = (int)Math.Max(_pageRangeStartBox.Value, _pageRangeEndBox.Value);
        if (_estimatedTotalPages.HasValue)
        {
            start = Math.Min(start, _estimatedTotalPages.Value);
            end = Math.Min(end, _estimatedTotalPages.Value);
        }

        _pageRangeStartBox.Value = start;
        _pageRangeEndBox.Value = end;
        return Enumerable.Range(start, end - start + 1).ToList();
    }

    private async Task<string> FetchPageHtmlAsync(int page)
    {
        var input = BuildEffectiveUrlForPage(_urlBox.Text.Trim(), page);
        LogActivity($"Loading page {page} for scan.");
        return await FetchHtmlForUrlAsync(input, page);
    }

    private async Task<string> FetchHtmlForUrlAsync(string input, int pageNumber)
    {
        LogActivity($"Loading page {pageNumber}: {input}");
        var siteType = _siteTypeBox.SelectedItem?.ToString() ?? "Static";

        _scrollCts ??= new CancellationTokenSource();
        _playwrightScraper.AutoScroll = _autoScrollBox.Checked;

        var html = siteType == "JavaScript"
            ? _playwrightScraper.HasInteractiveSession
                ? await _playwrightScraper.NavigateInteractiveAndGetHtmlAsync(input, _scrollCts.Token)
                : await _playwrightScraper.GetHtmlAsync(input, _scrollCts.Token)
            : await _httpScraper.GetHtmlAsync(input, _scrollCts.Token);
        _estimatedTotalPages ??= TryEstimateTotalPages(html);
        return html;
    }

    private static string BuildEffectiveUrlForPage(string url, int pageNumber)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Contains('<'))
        {
            return url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        var basePath = uri.GetLeftPart(UriPartial.Path);
        var parameters = ParseQueryString(uri.Query);
        if (pageNumber <= 1)
        {
            parameters.Remove("page");
        }
        else
        {
            parameters["page"] = pageNumber.ToString();
        }

        var query = string.Join("&", parameters.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return string.IsNullOrWhiteSpace(query) ? basePath : $"{basePath}?{query}";
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return values;
        }

        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            var key = Uri.UnescapeDataString(pieces[0]);
            var value = pieces.Length > 1 ? Uri.UnescapeDataString(pieces[1]) : string.Empty;
            values[key] = value;
        }

        return values;
    }

    private static int? TryEstimateTotalPages(string html)
    {
        var textMatch = Regex.Match(html, @"Showing\s+\d+\s+to\s+\d+\s+of\s+(?<total>\d+)\s+results", RegexOptions.IgnoreCase);
        if (textMatch.Success && int.TryParse(textMatch.Groups["total"].Value, out var totalCount))
        {
            var pageSizeMatch = Regex.Match(html, @"Showing\s+\d+\s+to\s+(?<pageSize>\d+)\s+of", RegexOptions.IgnoreCase);
            if (pageSizeMatch.Success && int.TryParse(pageSizeMatch.Groups["pageSize"].Value, out var pageSize) && pageSize > 0)
            {
                return (int)Math.Ceiling(totalCount / (double)pageSize);
            }
        }

        var pageNumbers = Regex.Matches(html, @"[?&]page=(\d+)", RegexOptions.IgnoreCase)
            .Select(match => int.TryParse(match.Groups[1].Value, out var pageNumber) ? pageNumber : 0)
            .Where(pageNumber => pageNumber > 0)
            .ToList();

        return pageNumbers.Count == 0 ? null : pageNumbers.Max();
    }

    private static int? GetPageNumber(string url)
    {
        var match = Regex.Match(url, @"[?&]page=(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var pageNumber) ? pageNumber : null;
    }

    private int GetScannedPageCount()
    {
        return _items
            .Select(item => Regex.Match(item.Status ?? string.Empty, @"Page\s+(?<page>\d+)", RegexOptions.IgnoreCase))
            .Where(match => match.Success)
            .Select(match => int.TryParse(match.Groups["page"].Value, out var pageNumber) ? pageNumber : 0)
            .Where(pageNumber => pageNumber > 0)
            .DefaultIfEmpty(1)
            .Max();
    }

    private static string? DiscoverNextPageUrl(string html, string currentUrl)
    {
        var document = new HtmlAgilityPack.HtmlDocument();
        document.LoadHtml(html);

        var nextNode = document.DocumentNode.SelectSingleNode("//a[@rel='next']")
            ?? document.DocumentNode.SelectSingleNode("//a[contains(@aria-label,'Next') or contains(normalize-space(.),'Next')]");

        if (nextNode is not null)
        {
            var href = nextNode.GetAttributeValue("href", string.Empty);
            return BuildAbsoluteUrl(currentUrl, href);
        }

        var currentPage = GetPageNumber(currentUrl) ?? 1;
        var pageLinks = document.DocumentNode.SelectNodes("//a[contains(@href,'page=')]")
            ?.Select(node => node.GetAttributeValue("href", string.Empty))
            .Select(href => BuildAbsoluteUrl(currentUrl, href))
            .Where(href => !string.IsNullOrWhiteSpace(href))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        return pageLinks
            .Select(href => new { Href = href, Page = GetPageNumber(href!) })
            .Where(item => item.Page.HasValue && item.Page.Value > currentPage)
            .OrderBy(item => item.Page!.Value)
            .Select(item => item.Href)
            .FirstOrDefault();
    }

    private static string? BuildAbsoluteUrl(string currentUrl, string href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var current))
        {
            return href;
        }

        return Uri.TryCreate(current, href, out var combined) ? combined.ToString() : href;
    }

    private static int CalculateProgress(int current, int total, int start, int end)
    {
        if (total <= 0)
        {
            return end;
        }

        var ratio = current / (double)Math.Max(1, total);
        return start + (int)Math.Round((end - start) * ratio);
    }

    private static Panel CreateCard() =>
        new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 16)
        };

    private static Label CreateRowLabel(string text) =>
        new()
        {
            Text = text,
            ForeColor = Color.Gainsboro,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0)
        };

    private static Button CreateButton(string text, EventHandler onClick, int width, bool accent = false)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = accent ? Color.FromArgb(16, 185, 129) : Color.FromArgb(51, 65, 85),
            ForeColor = Color.White
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private static void ApplyGridTheme(DataGridView grid)
    {
        grid.BackgroundColor = Color.FromArgb(15, 23, 42);
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.DefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
        grid.DefaultCellStyle.ForeColor = Color.Gainsboro;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(16, 185, 129);
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    private async Task RunAiMediaScanAsync()
    {
        if (_isBusy) return;
        var settings = SettingsService.Load();
        if (string.IsNullOrEmpty(settings.ApiKey))
        {
            MessageBox.Show(this, "Please configure your API Key in Settings first.", "API Key Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_loadedHtml))
        {
            SetStatus("Load a page first before running AI Smart Scan.", 0);
            return;
        }

        try
        {
            _isBusy = true;
            SetProgress(20);
            SetStatus("AI discovering media files on page...", 30);
            LogActivity("AI Smart Media Scan job started.");

            var service = new AiService();
            var results = await service.ExtractMediaAsync(_loadedHtml, _urlBox.Text.Trim(), settings);

            _items = results.Where(item => 
            {
                if (item.Type.Equals("Image", StringComparison.OrdinalIgnoreCase) && !_imagesBox.Checked) return false;
                if (item.Type.Equals("Video", StringComparison.OrdinalIgnoreCase) && !_videosBox.Checked) return false;
                if (item.Type.Equals("Audio", StringComparison.OrdinalIgnoreCase) && !_audioBox.Checked) return false;
                return true;
            }).ToList();

            _grid.DataSource = null;
            _grid.DataSource = _items;

            SetProgress(100);
            SetStatus($"AI Scan complete. Discovered {_items.Count} media file(s).");
            LogActivity($"AI found {_items.Count} media file(s).");

            if (_items.Count > 0 && (settings.AutoDownloadImages || settings.AutoDownloadAudio || settings.AutoDownloadVideos))
            {
                await AutoDownloadMediaAsync(settings);
            }
        }
        catch (Exception ex)
        {
            SetProgress(0);
            SetStatus($"AI Media Scan failed: {ex.Message}");
            MessageBox.Show(this, $"AI error: {ex.Message}", "AI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task AutoDownloadMediaAsync(ScraperSettings settings)
    {
        try
        {
            var targetFolder = string.IsNullOrEmpty(settings.OutputFolder) ? _folderBox.Text : settings.OutputFolder;
            if (string.IsNullOrEmpty(targetFolder))
            {
                targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WebScraperPro", "Downloads");
            }
            Directory.CreateDirectory(targetFolder);
            
            var total = _items.Count;
            var completed = 0;
            LogActivity($"Starting autopilot media download for {total} item(s) to: {targetFolder}");

            foreach (var item in _items)
            {
                bool allow = false;
                if (item.Type.Equals("Image", StringComparison.OrdinalIgnoreCase) && settings.AutoDownloadImages) allow = true;
                if (item.Type.Equals("Audio", StringComparison.OrdinalIgnoreCase) && settings.AutoDownloadAudio) allow = true;
                if (item.Type.Equals("Video", StringComparison.OrdinalIgnoreCase) && settings.AutoDownloadVideos) allow = true;
                
                if (!allow)
                {
                    item.Status = "Skipped (Config)";
                    completed++;
                    continue;
                }

                try
                {
                    var fileName = string.IsNullOrWhiteSpace(item.FileName) ? $"{Guid.NewGuid():N}.bin" : item.FileName;
                    var destination = Path.Combine(targetFolder, fileName);
                    SetStatus($"Downloading {completed + 1}/{total}: {fileName}");
                    await _downloader.DownloadAsync(item.Url, destination);
                    item.Status = "Downloaded";
                }
                catch
                {
                    item.Status = "Failed";
                }

                completed++;
                LogActivity($"Downloaded {completed}/{total}: {item.FileName} ({item.Status})");
            }

            _grid.Refresh();
            SetStatus("Autopilot media download completed.");
            LogActivity("Autopilot media download completed.");
        }
        catch (Exception ex)
        {
            LogActivity($"Autopilot media download failed: {ex.Message}");
        }
    }
}
