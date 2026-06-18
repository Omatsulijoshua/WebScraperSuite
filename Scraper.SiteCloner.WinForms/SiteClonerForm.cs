using System.ComponentModel;
using System.Diagnostics;
using Scraper.Core.Models;
using Scraper.Core.Services;

namespace Scraper.SiteCloner.WinForms;

public class SiteClonerForm : Form
{
    private readonly HttpScraperService _httpScraper = new();
    private readonly PlaywrightService _playwrightScraper = new();
    private readonly SiteCloneService _cloneService = new();

    private readonly TextBox _urlBox = new() { Dock = DockStyle.Fill, PlaceholderText = "https://example.com" };
    private readonly ComboBox _siteTypeBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _scopeBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _maxPagesBox = new() { Minimum = 1, Maximum = 500, Value = 50, Width = 90 };
    private readonly TextBox _folderBox = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _includeAssetsBox = new() { Text = "Download assets", Checked = true, ForeColor = Color.White, AutoSize = true };
    private readonly CheckBox _sameOriginOnlyBox = new() { Text = "Same origin only", Checked = true, ForeColor = Color.White, AutoSize = true };
    private readonly CheckBox _autoScrollBox = new() { Text = "Auto-Scroll", Checked = true, ForeColor = Color.White, AutoSize = true };
    private Button? _stopScrollButton;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, AllowUserToAddRows = false };
    private readonly ListBox _activityList = new() { Dock = DockStyle.Top, Height = 140, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.None };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Bottom, Height = 20 };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, ForeColor = Color.Gainsboro };
    private readonly Label _insightLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 60,
        ForeColor = Color.Silver,
        Text = "Clone public or manually approved pages into a local folder. The cloner crawls same-origin pages, saves HTML locally, and rewrites page and asset links for offline browsing."
    };
    private readonly Label _approvalLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 44,
        ForeColor = Color.FromArgb(167, 243, 208),
        Text = "Manual approval opens a live browser so you can sign in or complete page verification yourself before cloning."
    };

    private readonly BindingList<ClonePageResult> _results = [];
    private bool _isBusy;
    private CancellationTokenSource? _scrollCts;

    public SiteClonerForm()
    {
        Text = "WebScraper Pro - Website Cloner";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 760);
        Size = new Size(1240, 820);
        BackColor = Color.FromArgb(15, 23, 42);
        Font = new Font("Segoe UI", 10F);

        _siteTypeBox.Items.AddRange(["Static", "JavaScript"]);
        _siteTypeBox.SelectedIndex = 0;
        _scopeBox.Items.AddRange(["This page", "All pages"]);
        _scopeBox.SelectedIndex = 1;
        _grid.DataSource = _results;

        Controls.Add(BuildLayout());
        ApplyGridTheme(_grid);
        FormClosed += async (_, _) => await _playwrightScraper.DisposeAsync();

        var settings = SettingsService.Load();
        _autoScrollBox.Checked = settings.EnableAutoScroll;

        SetStatus("Ready to clone a website.");
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
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildControlCard(), 0, 1);
        root.Controls.Add(BuildResultCard(), 0, 2);
        return root;
    }

    private Control BuildHeader()
    {
        var card = CreateCard();
        card.Height = 138;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.Controls.Add(new Label
        {
            Text = "Website Cloner",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = "Mirror a website locally by crawling same-origin pages, downloading assets, and rewriting links for offline browsing. Login-assisted cloning is supported only when you complete authentication yourself.",
            AutoSize = true,
            ForeColor = Color.Silver,
            MaximumSize = new Size(1120, 0)
        }, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildControlCard()
    {
        var card = CreateCard();
        card.Height = 250;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        layout.Controls.Add(CreateRowLabel("URL"), 0, 0);
        layout.SetColumnSpan(_urlBox, 5);
        layout.Controls.Add(_urlBox, 1, 0);

        layout.Controls.Add(CreateRowLabel("Site Type"), 0, 1);
        layout.Controls.Add(_siteTypeBox, 1, 1);
        layout.Controls.Add(CreateRowLabel("Scope"), 2, 1);
        layout.Controls.Add(_scopeBox, 3, 1);
        layout.Controls.Add(CreateRowLabel("Max Pages"), 4, 1);
        layout.Controls.Add(_maxPagesBox, 5, 1);

        layout.Controls.Add(CreateRowLabel("Folder"), 0, 2);
        layout.SetColumnSpan(_folderBox, 3);
        layout.Controls.Add(_folderBox, 1, 2);
        var optionsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        optionsPanel.Controls.Add(_includeAssetsBox);
        optionsPanel.Controls.Add(_sameOriginOnlyBox);
        optionsPanel.Controls.Add(_autoScrollBox);
        layout.SetColumnSpan(optionsPanel, 2);
        layout.Controls.Add(optionsPanel, 4, 2);

        _stopScrollButton = CreateButton("Stop Scroll", (_, _) => StopAutoScroll(), 130);
        var actionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        actionRow.Controls.Add(CreateButton("Open Chrome", (_, _) => OpenChromeWithCdp(), 130));
        actionRow.Controls.Add(CreateButton("Browse", (_, _) => BrowseFolder(), 110));
        actionRow.Controls.Add(CreateButton("Approve / Login", async (_, _) => await StartApprovalSessionAsync(), 150));
        actionRow.Controls.Add(CreateButton("Resume Approved", async (_, _) => await ResumeApprovedSessionAsync(), 150));
        actionRow.Controls.Add(_stopScrollButton);
        actionRow.Controls.Add(CreateButton("Clone Site", async (_, _) => await CloneSiteAsync(), 130, accent: true));
        actionRow.Controls.Add(CreateButton("Open Folder", (_, _) => OpenOutputFolder(), 120));
        layout.SetColumnSpan(actionRow, 5);
        layout.Controls.Add(actionRow, 1, 3);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildResultCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;
        card.Controls.Add(new Label
        {
            Text = "Clone Results",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
        });
        card.Controls.Add(_grid);
        card.Controls.Add(_activityList);
        card.Controls.Add(new Label
        {
            Text = "Live activity",
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.Gainsboro
        });
        card.Controls.Add(_progress);
        card.Controls.Add(_status);
        card.Controls.Add(_insightLabel);
        card.Controls.Add(_approvalLabel);
        return card;
    }

    private async Task StartApprovalSessionAsync()
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progress.Value);
            return;
        }

        if (string.IsNullOrWhiteSpace(_urlBox.Text))
        {
            SetStatus("Enter a real URL before starting manual approval.");
            return;
        }

        try
        {
            _isBusy = true;
            _siteTypeBox.SelectedItem = "JavaScript";
            SetProgress(20);
            await _playwrightScraper.OpenInteractiveSessionAsync(_urlBox.Text.Trim());
            _approvalLabel.Text = "Approval browser opened. Complete login or verification there, then return here and click Resume Approved.";
            _insightLabel.Text = "Once the real page is visible in the live browser, click Resume Approved and then Clone Site.";
            SetStatus("Approval browser ready. Finish the page challenge, then click Resume Approved.", 60);
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
            var currentUrl = await _playwrightScraper.GetInteractiveUrlAsync();
            if (!string.IsNullOrWhiteSpace(currentUrl))
            {
                _urlBox.Text = currentUrl;
            }

            _approvalLabel.Text = "Approved session captured. Clone Site will now reuse that browser session for JavaScript page navigation.";
            _insightLabel.Text = string.IsNullOrWhiteSpace(_playwrightScraper.LastLoadSummary)
                ? "Approved session is ready. You can clone the visible site now."
                : $"Approved session is ready. {_playwrightScraper.LastLoadSummary} You can clone the visible site now.";
            SetProgress(100);
            SetStatus("Approved session ready for cloning.");
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
    }

    private async Task CloneSiteAsync()
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progress.Value);
            return;
        }

        if (string.IsNullOrWhiteSpace(_urlBox.Text))
        {
            SetStatus("Enter a URL first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_folderBox.Text))
        {
            SetStatus("Choose an output folder first.");
            return;
        }

        if (!Uri.TryCreate(_urlBox.Text.Trim(), UriKind.Absolute, out var startUri))
        {
            SetStatus("Enter a valid absolute URL first.");
            return;
        }

        try
        {
            _isBusy = true;
            _results.Clear();
            _activityList.Items.Clear();
            Directory.CreateDirectory(_folderBox.Text);

            var snapshots = new Dictionary<string, PageSnapshot>(StringComparer.OrdinalIgnoreCase);
            var assetMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var pageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(startUri.ToString());

            var cloneAllPages = (_scopeBox.SelectedItem?.ToString() ?? "All pages") == "All pages";
            var maxPages = cloneAllPages ? (int)_maxPagesBox.Value : 1;
            var crawled = 0;

            LogActivity($"Starting clone crawl from {startUri}");
            while (queue.Count > 0 && crawled < maxPages)
            {
                var currentUrl = queue.Dequeue();
                if (!visited.Add(currentUrl))
                {
                    continue;
                }

                SetStatus($"Cloning page {crawled + 1}/{maxPages}: {currentUrl}", CalculateProgress(crawled, maxPages, 10, 55));
                LogActivity($"Loading page {crawled + 1}: {currentUrl}");
                var html = await FetchHtmlAsync(currentUrl);
                var pageLinks = _cloneService.DiscoverPageLinks(html, currentUrl, _sameOriginOnlyBox.Checked);
                var assetLinks = _includeAssetsBox.Checked
                    ? _cloneService.DiscoverAssetLinks(html, currentUrl)
                    : [];

                pageMap[currentUrl] = _cloneService.BuildLocalPagePath(currentUrl, _folderBox.Text.Trim());
                snapshots[currentUrl] = new PageSnapshot
                {
                    Url = currentUrl,
                    Html = html,
                    PageLinks = pageLinks,
                    AssetLinks = assetLinks
                };

                foreach (var assetUrl in assetLinks)
                {
                    if (!assetMap.ContainsKey(assetUrl))
                    {
                        assetMap[assetUrl] = _cloneService.BuildLocalAssetPath(assetUrl, _folderBox.Text.Trim());
                    }
                }

                if (cloneAllPages)
                {
                    foreach (var link in pageLinks)
                    {
                        if (_sameOriginOnlyBox.Checked && !IsSameOrigin(startUri, new Uri(link)))
                        {
                            continue;
                        }

                        if (!visited.Contains(link))
                        {
                            queue.Enqueue(link);
                        }
                    }
                }

                crawled++;
                _results.Add(new ClonePageResult
                {
                    Url = currentUrl,
                    LocalPath = pageMap[currentUrl],
                    AssetCount = assetLinks.Count,
                    LinkCount = pageLinks.Count,
                    Status = "Fetched"
                });
                LogActivity($"Fetched page {crawled}: {currentUrl} ({assetLinks.Count} asset(s), {pageLinks.Count} link(s))");
            }

            var assets = assetMap.ToList();
            for (var index = 0; index < assets.Count; index++)
            {
                var asset = assets[index];
                try
                {
                    SetStatus($"Downloading asset {index + 1}/{assets.Count}", CalculateProgress(index, Math.Max(1, assets.Count), 56, 80));
                    Directory.CreateDirectory(Path.GetDirectoryName(asset.Value)!);
                    var bytes = await _httpScraper.GetBytesAsync(asset.Key);
                    await File.WriteAllBytesAsync(asset.Value, bytes);
                }
                catch (Exception ex)
                {
                    LogActivity($"Asset download failed: {asset.Key} ({ex.Message})");
                }
            }

            var snapshotList = snapshots.Values.ToList();
            for (var index = 0; index < snapshotList.Count; index++)
            {
                var snapshot = snapshotList[index];
                var rewrittenHtml = _cloneService.RewriteDocumentLinks(snapshot.Html, snapshot.Url, pageMap, assetMap, _folderBox.Text.Trim());
                var localPagePath = pageMap[snapshot.Url];
                Directory.CreateDirectory(Path.GetDirectoryName(localPagePath)!);
                await File.WriteAllTextAsync(localPagePath, rewrittenHtml);
                SetStatus($"Saving cloned page {index + 1}/{snapshotList.Count}", CalculateProgress(index, Math.Max(1, snapshotList.Count), 81, 100));
                var row = _results.FirstOrDefault(result => string.Equals(result.Url, snapshot.Url, StringComparison.OrdinalIgnoreCase));
                if (row is not null)
                {
                    row.Status = "Saved";
                }
                _grid.Refresh();
            }

            LogActivity($"Clone complete. Saved {snapshotList.Count} page(s) and attempted {assets.Count} asset download(s).");
            _insightLabel.Text = $"Clone complete. Saved {snapshotList.Count} page(s) into '{_folderBox.Text}'. Open the folder and start with the first page's local index.html file.";
            SetStatus($"Clone complete. Saved {snapshotList.Count} page(s).", 100);
            SetProgress(100);
        }
        catch (Exception ex)
        {
            SetProgress(0);
            SetStatus($"Clone failed: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task<string> FetchHtmlAsync(string url)
    {
        _playwrightScraper.AutoScroll = _autoScrollBox.Checked;
        var siteType = _siteTypeBox.SelectedItem?.ToString() ?? "Static";
        if (siteType == "JavaScript")
        {
            if (_playwrightScraper.HasInteractiveSession)
            {
                return await _playwrightScraper.NavigateInteractiveAndGetHtmlAsync(url);
            }

            _scrollCts = new CancellationTokenSource();
            return await _playwrightScraper.GetHtmlAsync(url, _scrollCts.Token);
        }

        return await _httpScraper.GetHtmlAsync(url);
    }

    private static void OpenChromeWithCdp()
    {
        var debugProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data Debug");
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c start chrome --remote-debugging-port=9222 --user-data-dir=\"{debugProfilePath}\"")
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
            SetStatus("Stopping auto-scroll. Fetching current page state...", _progress.Value);
        }
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folderBox.Text = dialog.SelectedPath;
        }
    }

    private void OpenOutputFolder()
    {
        if (string.IsNullOrWhiteSpace(_folderBox.Text) || !Directory.Exists(_folderBox.Text))
        {
            MessageBox.Show(this, "Choose or create an output folder first.", "Folder Not Ready");
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_folderBox.Text) { UseShellExecute = true });
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
        while (_activityList.Items.Count > 120)
        {
            _activityList.Items.RemoveAt(_activityList.Items.Count - 1);
        }
    }

    private static bool IsSameOrigin(Uri left, Uri right)
        => string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
           && left.Port == right.Port;

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

    private sealed class PageSnapshot
    {
        public string Url { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public List<string> PageLinks { get; set; } = [];
        public List<string> AssetLinks { get; set; } = [];
    }
}
