using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Scraper.Core.Helpers;
using Scraper.Core.Models;
using Scraper.Core.Services;

namespace Scraper.Advanced.WinForms;

public class AdvancedWorkspaceForm : Form
{
    private static readonly string SelectorTransferPath = Path.Combine(Path.GetTempPath(), "WebScraperPro", "selector-transfer.json");
    private readonly HttpScraperService _httpScraper = new();
    private readonly PlaywrightService _playwrightScraper = new();
    private readonly SelectorHelper _selectorHelper = new();
    private readonly ExportService _exportService = new();
    private readonly System.Windows.Forms.Timer _selectorImportTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _pasteDebounceTimer = new();
    private string _lastLoadedUrl = string.Empty;

    private readonly TextBox _inputBox = new() { PlaceholderText = "https://example.com or paste raw HTML", Dock = DockStyle.Fill };
    private readonly ComboBox _siteTypeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _selectorTypeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox _selectorBox = new() { PlaceholderText = ".product-card a or //div[@class='item']", Dock = DockStyle.Fill };
    private readonly ComboBox _rowSelectorTypeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly TextBox _rowSelectorBox = new() { PlaceholderText = ".exhibitor-card or table tbody tr", Dock = DockStyle.Fill };
    private readonly ComboBox _pageModeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly NumericUpDown _pageNumberBox = new() { Minimum = 1, Maximum = 500, Value = 1, Width = 80 };
    private readonly NumericUpDown _pageRangeStartBox = new() { Minimum = 1, Maximum = 500, Value = 1, Width = 80 };
    private readonly NumericUpDown _pageRangeEndBox = new() { Minimum = 1, Maximum = 500, Value = 5, Width = 80 };
    private readonly Label _pageInfoLabel = new() { ForeColor = Color.Gainsboro, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true, Margin = new Padding(10, 8, 10, 0) };
    private readonly CheckBox _showManualSelectorsBox = new() { Text = "Custom selectors", ForeColor = Color.White, AutoSize = true, Margin = new Padding(10, 8, 10, 0) };
    private readonly CheckBox _autoScrollBox = new() { Text = "Auto-Scroll", Checked = true, ForeColor = Color.White, AutoSize = true, Margin = new Padding(10, 8, 10, 0) };
    private Button? _stopScrollButton;
    private CancellationTokenSource? _scrollCts;
    private int? _estimatedTotalPages;
    private TableLayoutPanel _manualPanel = null!;
    private readonly TextBox _htmlPreviewBox = new() { Multiline = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new Font("Cascadia Code", 9F) };
    private readonly TabControl _workspaceTabs = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
    private readonly Label _htmlPreviewStatusLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 40,
        ForeColor = Color.Silver,
        Text = "Current HTML snapshot: waiting to load. The Input tab shows one page snapshot at a time; combined multi-page results appear in the Output tab."
    };
    private readonly TextBox _outputHtmlPreviewBox = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Cascadia Code", 9F) };
    private readonly Label _outputHtmlStatusLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 40,
        ForeColor = Color.Silver,
        Text = "Scraped HTML preview: waiting to start. Each scraped page will be appended here one by one during extraction."
    };
    private readonly DataGridView _fieldGrid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, BackgroundColor = Color.FromArgb(15, 23, 42), BorderStyle = BorderStyle.None };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, AllowUserToAddRows = false };
    private readonly ListBox _activityList = new() { Dock = DockStyle.Top, Height = 120, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.None };
    private readonly Label _runSummaryLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 26,
        ForeColor = Color.White,
        Text = "Run summary: waiting to start."
    };
    private readonly Label _pageProgressLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 24,
        ForeColor = Color.FromArgb(125, 211, 252),
        Text = "Page progress: no extraction running."
    };
    private readonly ComboBox _exportFormatBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly ListBox _previewList = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.None };
    private readonly Label _insightLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 60,
        ForeColor = Color.Silver,
        Text = "Load a page, then preview a selector to confirm the extracted values match the data you want before running full extraction."
    };
    private readonly Label _approvalLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 44,
        ForeColor = Color.FromArgb(125, 211, 252),
        Text = "Manual approval mode lets you open a real browser, sign in or complete page challenges yourself, then resume scraping from that approved session.",
        Visible = false
    };
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripProgressBar _progressBar = new() { Minimum = 0, Maximum = 100, Size = new Size(180, 16) };

    private string _loadedHtml = string.Empty;
    private readonly BindingList<FieldMapping> _fieldMappings = [];
    private DataTable _resultTable = new();
    private bool _isBusy;
    private DateTime _lastSelectorTransferSeenUtc = DateTime.MinValue;

    public AdvancedWorkspaceForm()
    {
        Text = "WebScraper Pro - Advanced Scraper";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 760);
        Size = new Size(1280, 820);
        BackColor = Color.FromArgb(15, 23, 42);
        Font = new Font("Segoe UI", 10F);

        _siteTypeBox.Items.AddRange(["Static", "JavaScript"]);
        _siteTypeBox.SelectedIndex = 0;

        _selectorTypeBox.Items.AddRange(["CSS", "XPath"]);
        _selectorTypeBox.SelectedIndex = 0;
        _rowSelectorTypeBox.Items.AddRange(["CSS", "XPath"]);
        _rowSelectorTypeBox.SelectedIndex = 0;

        _exportFormatBox.Items.AddRange(["CSV", "JSON", "Excel"]);
        _exportFormatBox.SelectedIndex = 0;

        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_progressBar);
        _statusStrip.BackColor = Color.FromArgb(15, 23, 42);
        _statusStrip.ForeColor = Color.Gainsboro;

        _pageModeBox.Items.AddRange(["All pages", "Custom page", "Page range"]);
        _pageModeBox.SelectedIndex = 0;
        _pageModeBox.SelectedIndexChanged += (_, _) => UpdatePageModeUi();
        _showManualSelectorsBox.CheckedChanged += (_, _) => ToggleManualSelectors();
        
        _pasteDebounceTimer.Interval = 800;
        _pasteDebounceTimer.Tick += PasteDebounceTimer_Tick;
        _inputBox.TextChanged += InputBox_TextChanged;

        Controls.Add(BuildLayout());
        Controls.Add(_statusStrip);
        ConfigureFieldGrid();
        ApplyGridTheme(_grid);
        _selectorImportTimer.Tick += (_, _) => TryImportTransferredSelector();
        _selectorImportTimer.Start();
        FormClosed += async (_, _) => await _playwrightScraper.DisposeAsync();

        var settings = SettingsService.Load();
        _autoScrollBox.Checked = settings.EnableAutoScroll;

        UpdatePageModeUi();
        UpdatePageInfoLabel();
        SetStatus("Ready for structured extraction.", 0);
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
        root.Controls.Add(BuildCommandPanel(), 0, 1);
        root.Controls.Add(BuildWorkspaceTabs(), 0, 2);
        return root;
    }

    private Control BuildHeader()
    {
        var panel = CreateCard();
        panel.Height = 132;

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
            Text = "Advanced Deep Scraper",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text = "Structured data scraping for static and JavaScript-heavy pages. Login-assisted sessions are fine when the user authenticates directly; challenge bypass is intentionally not included.",
            AutoSize = true,
            MaximumSize = new Size(1120, 0),
            ForeColor = Color.Silver
        }, 0, 1);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildCommandPanel()
    {
        var card = CreateCard();
        card.Height = 180; // Shorter height by default since manual controls are collapsed

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); // Row 0: URL
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); // Row 1: Page range mode
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));  // Row 2: Manual Selectors Panel (collapsed by default)
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); // Row 3: Action Buttons

        // Row 0: URL Row
        var urlRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        urlRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        urlRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        urlRow.Controls.Add(CreateRowLabel("URL / HTML"), 0, 0);
        urlRow.Controls.Add(_inputBox, 1, 0);
        mainLayout.Controls.Add(urlRow, 0, 0);

        // Row 1: Page Mode Row
        var pageRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 2, 0, 0),
            WrapContents = false
        };
        pageRow.Controls.Add(CreateRowLabel("Pages"));
        pageRow.Controls.Add(_pageModeBox);
        pageRow.Controls.Add(_pageNumberBox);
        
        var fromLabel = new Label { Text = "From", ForeColor = Color.Gainsboro, Width = 40, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 9, 0, 0) };
        var toLabel = new Label { Text = "To", ForeColor = Color.Gainsboro, Width = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 9, 0, 0) };
        pageRow.Controls.Add(fromLabel);
        pageRow.Controls.Add(_pageRangeStartBox);
        pageRow.Controls.Add(toLabel);
        pageRow.Controls.Add(_pageRangeEndBox);
        pageRow.Controls.Add(_pageInfoLabel);
        pageRow.Controls.Add(_showManualSelectorsBox);
        pageRow.Controls.Add(_autoScrollBox);
        mainLayout.Controls.Add(pageRow, 0, 1);

        // Row 2: Manual Selectors Panel (collapsed by default)
        _manualPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            Margin = new Padding(0),
            Visible = false
        };
        _manualPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        _manualPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        _manualPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        _manualPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        _manualPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        _manualPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        
        _manualPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        _manualPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        // Line 1: Site Type & Selector
        _manualPanel.Controls.Add(CreateRowLabel("Site Type"), 0, 0);
        _manualPanel.Controls.Add(_siteTypeBox, 1, 0);
        _manualPanel.Controls.Add(CreateRowLabel("Selector"), 2, 0);
        _manualPanel.Controls.Add(_selectorTypeBox, 3, 0);
        _manualPanel.Controls.Add(CreateButton("Auto Detect", (_, _) => AutoDetectSelector(), 110, accent: true), 4, 0);
        _manualPanel.Controls.Add(_selectorBox, 5, 0);

        // Line 2: Row Scope
        _manualPanel.Controls.Add(CreateRowLabel("Row Scope"), 0, 1);
        _manualPanel.Controls.Add(_rowSelectorTypeBox, 1, 1);
        _manualPanel.SetColumnSpan(_rowSelectorBox, 4);
        _manualPanel.Controls.Add(_rowSelectorBox, 2, 1);

        mainLayout.Controls.Add(_manualPanel, 0, 2);

        // Row 3: Action Buttons
        _stopScrollButton = CreateButton("Stop Scroll", (_, _) => StopAutoScroll(), 120);
        var actionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 46, AutoSize = false, WrapContents = false, AutoScroll = true };
        actionRow.Controls.Add(CreateButton("Open Chrome", (_, _) => OpenChromeWithCdp(), 130));
        actionRow.Controls.Add(CreateButton("Load", async (_, _) => await LoadHtmlAsync(force: true), 110, accent: true));
        actionRow.Controls.Add(CreateButton("Approve / Login", async (_, _) => await StartApprovalSessionAsync(), 140));
        actionRow.Controls.Add(CreateButton("Resume Approved", async (_, _) => await ResumeApprovedSessionAsync(), 140));
        actionRow.Controls.Add(_stopScrollButton);
        actionRow.Controls.Add(CreateButton("Preview", (_, _) => PreviewSelection(), 110));
        actionRow.Controls.Add(CreateButton("Extract", async (_, _) => await ExtractDataAsync(), 110));
        actionRow.Controls.Add(CreateButton("AI Smart Extract", async (_, _) => await RunAiExtractionAsync(), 150, accent: true));
        actionRow.Controls.Add(CreateButton("Export", (_, _) => ExportRows(), 110));
        actionRow.Controls.Add(CreateButton("Open Media App", (_, _) => OpenSiblingTool("Scraper.Media.WinForms.exe"), 150));
        actionRow.Controls.Add(CreateButton("Open Picker App", (_, _) => OpenSiblingTool("Scraper.Picker.WinForms.exe"), 150));

        mainLayout.Controls.Add(actionRow, 0, 3);

        card.Controls.Add(mainLayout);
        return card;
    }

    private Control BuildWorkspaceTabs()
    {
        var inputTab = new TabPage("Input") { BackColor = Color.FromArgb(15, 23, 42) };
        inputTab.Controls.Add(BuildInputCard());

        var previewTab = new TabPage("Preview") { BackColor = Color.FromArgb(15, 23, 42) };
        previewTab.Controls.Add(BuildPreviewCard());

        var fieldsTab = new TabPage("Fields") { BackColor = Color.FromArgb(15, 23, 42) };
        fieldsTab.Controls.Add(BuildFieldMappingCard());

        var outputTab = new TabPage("Output") { BackColor = Color.FromArgb(15, 23, 42) };
        outputTab.Controls.Add(BuildOutputCard());

        var exportTab = new TabPage("Export") { BackColor = Color.FromArgb(15, 23, 42) };
        exportTab.Controls.Add(BuildExportCard());

        _workspaceTabs.TabPages.Clear();
        _workspaceTabs.TabPages.Add(inputTab);
        _workspaceTabs.TabPages.Add(previewTab);
        _workspaceTabs.TabPages.Add(fieldsTab);
        _workspaceTabs.TabPages.Add(outputTab);
        _workspaceTabs.TabPages.Add(exportTab);
        return _workspaceTabs;
    }

    private Control BuildInputCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;

        var titleLabel = new Label
        {
            Text = "Loaded HTML Preview",
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
        };

        card.Controls.Add(_htmlPreviewBox);
        card.Controls.Add(_htmlPreviewStatusLabel);
        card.Controls.Add(titleLabel);
        return card;
    }

    private Control BuildPreviewCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;

        var title = new Label
        {
            Text = "Pre-Extraction Preview",
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
        };

        card.Controls.Add(_previewList);
        card.Controls.Add(_insightLabel);
        card.Controls.Add(_approvalLabel);
        card.Controls.Add(title);
        return card;
    }

    private Control BuildFieldMappingCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;

        var info = new Label
        {
            Dock = DockStyle.Top,
            Height = 48,
            ForeColor = Color.Gainsboro,
            Text = "Inspect the page first, then define the exact output fields you want. Each field becomes a real output column during extraction."
        };

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, WrapContents = false };
        actions.Controls.Add(CreateButton("Add Field", (_, _) => AddFieldMapping(), 120, accent: true));
        actions.Controls.Add(CreateButton("Use Current Selector", (_, _) => UseCurrentSelectorForSelectedField(), 150));
        actions.Controls.Add(CreateButton("Remove Field", (_, _) => RemoveSelectedField(), 130));
        actions.Controls.Add(CreateButton("Build Output Layout", async (_, _) => await ExtractDataAsync(), 150));

        card.Controls.Add(_fieldGrid);
        card.Controls.Add(actions);
        card.Controls.Add(info);
        return card;
    }

    private Control BuildExportCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;

        var info = new Label
        {
            Dock = DockStyle.Top,
            Height = 48,
            ForeColor = Color.Gainsboro,
            Text = "Export the current result set to CSV, JSON, or Excel."
        };

        var row = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
        row.Controls.Add(new Label { Text = "Format", ForeColor = Color.Gainsboro, Width = 80, Padding = new Padding(0, 10, 0, 0) });
        row.Controls.Add(_exportFormatBox);
        row.Controls.Add(CreateButton("Export Current Results", (_, _) => ExportRows(), 190, accent: true));

        card.Controls.Add(row);
        card.Controls.Add(info);
        return card;
    }

    private Control BuildOutputCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;

        var titleLabel = new Label
        {
            Text = "Results Preview",
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
        };

        var activityLabel = new Label
        {
            Text = "Live activity",
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.Gainsboro
        };

        var outputHintLabel = new Label
        {
            Text = "This tab shows the combined extraction result set. During multi-page runs, watch Run summary and Page progress here instead of the Input tab.",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = Color.Silver
        };

        var topPanel = new Panel { Dock = DockStyle.Fill };
        topPanel.Controls.Add(_grid);
        topPanel.Controls.Add(_activityList);
        topPanel.Controls.Add(activityLabel);
        topPanel.Controls.Add(outputHintLabel);
        topPanel.Controls.Add(_pageProgressLabel);
        topPanel.Controls.Add(_runSummaryLabel);

        var htmlHeaderRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight
        };
        htmlHeaderRow.Controls.Add(new Label
        {
            Text = "Scraped HTML Output",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            Width = 220,
            Height = 30,
            Padding = new Padding(0, 5, 0, 0)
        });
        htmlHeaderRow.Controls.Add(CreateButton("Copy All HTML", (_, _) => CopyOutputHtmlPreview(), 140, accent: true));

        var bottomPanel = new Panel { Dock = DockStyle.Fill };
        bottomPanel.Controls.Add(_outputHtmlPreviewBox);
        bottomPanel.Controls.Add(_outputHtmlStatusLabel);
        bottomPanel.Controls.Add(htmlHeaderRow);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 260F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(topPanel, 0, 0);
        layout.Controls.Add(bottomPanel, 0, 1);

        card.Controls.Add(layout);
        card.Controls.Add(titleLabel);
        return card;
    }

    private Panel WrapInCard(Control content, string title)
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;
        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
        };

        content.Dock = DockStyle.Fill;
        card.Controls.Add(content);
        card.Controls.Add(titleLabel);
        return card;
    }

    private async Task LoadHtmlAsync(bool force = false)
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progressBar.Value);
            return;
        }

        var input = _inputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            SetStatus("Enter a URL or raw HTML first.", 0);
            return;
        }

        if (!force && IsValidUrl(input) && input == _lastLoadedUrl)
        {
            return;
        }

        try
        {
            _isBusy = true;
            _approvalLabel.Visible = false;
            LogActivity("Starting page load.");
            SetStatus("Loading page source...", 25);
            var siteType = _siteTypeBox.SelectedItem?.ToString();

            _scrollCts = new CancellationTokenSource();
            _playwrightScraper.AutoScroll = _autoScrollBox.Checked;

            _loadedHtml = input.Contains('<') && input.Contains('>')
                ? input
                : siteType == "JavaScript"
                    ? await _playwrightScraper.GetHtmlAsync(input, _scrollCts.Token)
                    : await _httpScraper.GetHtmlAsync(input, _scrollCts.Token);

            _lastLoadedUrl = input;

            UpdateHtmlPreviewSnapshot(_loadedHtml, "loaded source page", input.Contains('<') && input.Contains('>') ? null : input);
            _runSummaryLabel.Text = "Run summary: page loaded and ready.";
            _pageProgressLabel.Text = "Page progress: no extraction running.";
            // Run AI layout analysis if settings are configured
            await RunAiLayoutAnalysisAsync(input, showResultDialog: force);

            if (string.IsNullOrWhiteSpace(_rowSelectorBox.Text))
            {
                _rowSelectorBox.Text = _selectorHelper.AutoDetectRowSelector(_loadedHtml);
            }
            EnsureStarterField();
            var signals = ProtectionDetector.GetSignals(_loadedHtml);
            _insightLabel.Text = BuildLoadInsight(_loadedHtml, siteType == "JavaScript", _playwrightScraper.LastLoadSummary);
            PreviewSelection(autoPreview: true);
            LogActivity("Page load complete. HTML preview updated.");
            SetStatus(
                signals.Count > 0
                    ? $"Loaded with warning: potential protection detected ({string.Join(", ", signals)})."
                    : "HTML loaded successfully.",
                100);
        }
        catch (Exception ex)
        {
            SetStatus($"Load failed: {ex.Message}", 0);
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
                    this.BeginInvoke(new Action(async () => await RunAiExtractionAsync()));
                });
            }
        }
    }

    private void TryImportTransferredSelector()
    {
        try
        {
            if (!File.Exists(SelectorTransferPath))
            {
                return;
            }

            var json = File.ReadAllText(SelectorTransferPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var transfer = JsonSerializer.Deserialize<SelectorTransferMessage>(json);
            if (transfer is null || string.IsNullOrWhiteSpace(transfer.Selector))
            {
                return;
            }

            if (transfer.SentAtUtc <= _lastSelectorTransferSeenUtc)
            {
                return;
            }

            _lastSelectorTransferSeenUtc = transfer.SentAtUtc;
            _selectorBox.Text = transfer.Selector.Trim();
            _selectorTypeBox.SelectedItem = string.Equals(transfer.SelectorType, "XPath", StringComparison.OrdinalIgnoreCase) ? "XPath" : "CSS";
            SwitchToWorkspaceTab("Input");
            _insightLabel.Text = $"Selector imported from Element Picker. Preview it now to confirm the values match the data you want before extraction.";
            SetStatus($"Imported selector from Element Picker ({_selectorTypeBox.SelectedItem}).", 100);
            LogActivity($"Imported selector from Element Picker: {_selectorBox.Text}");
        }
        catch
        {
            // Ignore transient file/JSON contention and try again on the next timer tick.
        }
    }

    private async Task StartApprovalSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(_inputBox.Text) || _inputBox.Text.Contains('<'))
        {
            SetStatus("Enter a real URL before starting manual approval mode.", 0);
            return;
        }

        try
        {
            _siteTypeBox.SelectedItem = "JavaScript";
            SetStatus("Opening live browser for manual approval...", 20);
            await _playwrightScraper.OpenInteractiveSessionAsync(_inputBox.Text.Trim());
            _approvalLabel.Text = "Approval browser opened. Complete login, CAPTCHA, or Cloudflare checks yourself in that browser, then come back here and click Resume Approved.";
            _approvalLabel.Visible = true;
            _insightLabel.Text = "The live approval browser is open. After you finish the page challenge yourself, click Resume Approved to pull the rendered HTML back into the scraper.";
            SetStatus("Approval browser ready. Finish the page challenge, then click Resume Approved.", 60);
        }
        catch (Exception ex)
        {
            SetStatus($"Approval browser failed: {ex.Message}", 0);
        }
    }

    private async Task ResumeApprovedSessionAsync()
    {
        if (!_playwrightScraper.HasInteractiveSession)
        {
            SetStatus("No live approval session is open yet. Click Approve / Login first.", 0);
            return;
        }

        try
        {
            SetStatus("Resuming from approved browser session...", 70);
            _scrollCts = new CancellationTokenSource();
            _playwrightScraper.AutoScroll = _autoScrollBox.Checked;
            _loadedHtml = await _playwrightScraper.GetInteractiveHtmlAsync(_scrollCts.Token);
            UpdateHtmlPreviewSnapshot(_loadedHtml, "approved session snapshot", await _playwrightScraper.GetInteractiveUrlAsync());
            _runSummaryLabel.Text = "Run summary: approved session HTML loaded.";
            _pageProgressLabel.Text = "Page progress: ready to extract.";
            if (string.IsNullOrWhiteSpace(_rowSelectorBox.Text))
            {
                _rowSelectorBox.Text = _selectorHelper.AutoDetectRowSelector(_loadedHtml);
            }
            EnsureStarterField();
            var currentUrl = await _playwrightScraper.GetInteractiveUrlAsync();
            if (!string.IsNullOrWhiteSpace(currentUrl))
            {
                _inputBox.Text = currentUrl;
            }

            // Run AI layout analysis on resume
            if (!string.IsNullOrWhiteSpace(currentUrl))
            {
                await RunAiLayoutAnalysisAsync(currentUrl);
            }

            _approvalLabel.Text = "Approved session captured successfully. You can now preview selectors and extract against the authenticated or challenge-cleared page.";
            _approvalLabel.Visible = true;
            _insightLabel.Text = $"Approved session content loaded. {_playwrightScraper.LastLoadSummary} Preview a selector now to verify you are targeting the real post-login or post-challenge data.".Trim();
            PreviewSelection(autoPreview: true);
            SetStatus("Approved session HTML loaded.", 100);
        }
        catch (Exception ex)
        {
            SetStatus($"Resume failed: {ex.Message}", 0);
        }

        if (!string.IsNullOrWhiteSpace(_loadedHtml))
        {
            var settings = SettingsService.Load();
            if (settings.EnableAutopilot && !string.IsNullOrEmpty(settings.ApiKey))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    this.BeginInvoke(new Action(async () => await RunAiExtractionAsync()));
                });
            }
        }
    }

    private void AutoDetectSelector()
    {
        if (string.IsNullOrWhiteSpace(_loadedHtml))
        {
            SetStatus("Load HTML before using auto detect.", 0);
            return;
        }

        var detectedFields = _selectorHelper.AutoDetectFields(_loadedHtml);
        var detectedRowSelector = _selectorHelper.AutoDetectRowSelector(_loadedHtml);
        if (!string.IsNullOrWhiteSpace(detectedRowSelector))
        {
            _rowSelectorBox.Text = detectedRowSelector;
            _rowSelectorTypeBox.SelectedItem = "CSS";
        }

        if (detectedFields.Count == 0)
        {
            _selectorBox.Clear();
            _insightLabel.Text = $"Auto Detect could not find real extractable nodes yet. {_playwrightScraper.LastLoadSummary} This usually means the page is still a JavaScript shell or the data is loaded from an API after render. Try JavaScript mode first, then use Element Picker on the live page.".Trim();
            SetStatus("Auto Detect found only a page shell. Load with JavaScript mode or inspect the live page.", 0);
            return;
        }

        _fieldMappings.Clear();
        foreach (var detectedField in detectedFields)
        {
            _fieldMappings.Add(new FieldMapping
            {
                FieldName = detectedField.FieldName,
                Selector = detectedField.Selector,
                SelectorType = detectedField.SelectorKind == SelectorKind.XPath ? "XPath" : "CSS"
            });
        }

        var primaryField = detectedFields.First();
        _selectorBox.Text = primaryField.Selector;
        _selectorTypeBox.SelectedItem = primaryField.SelectorKind == SelectorKind.XPath ? "XPath" : "CSS";
        _fieldGrid.Refresh();
        _insightLabel.Text = $"Auto Detect prefilled {_fieldMappings.Count} field(s){(string.IsNullOrWhiteSpace(detectedRowSelector) ? string.Empty : $" and detected row scope '{detectedRowSelector}'")}. You can extract immediately, or rename and refine any field in the Fields tab before exporting.";
        LogActivity($"Auto Detect prepared {_fieldMappings.Count} field(s).");
        SetStatus($"Auto Detect found {_fieldMappings.Count} fields and prefilled the output layout.", 100);
    }

    private async Task ExtractDataAsync()
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progressBar.Value);
            return;
        }

        if (string.IsNullOrWhiteSpace(_loadedHtml))
        {
            SetStatus("Nothing to extract yet. Load a page first.", 0);
            return;
        }

        try
        {
            var mappings = GetActiveFieldMappings();
            if (mappings.Count == 0)
            {
                SetStatus("Add at least one output field or use the current selector on a field first.", 0);
                return;
            }

            _isBusy = true;
            _approvalLabel.Visible = false;
            _activityList.Items.Clear();
            ClearOutputHtmlPreview();
            SwitchToWorkspaceTab("Output");
            _runSummaryLabel.Text = "Run summary: extraction started.";
            _pageProgressLabel.Text = "Page progress: preparing extraction plan.";
            LogActivity("Preparing extraction job.");

            var pageMode = _pageModeBox.SelectedItem?.ToString() ?? "All pages";
            var isMultiPageRun = pageMode == "All pages" || pageMode == "Page range";
            if (isMultiPageRun && string.IsNullOrWhiteSpace(_rowSelectorBox.Text))
            {
                var detectedRowSelector = _selectorHelper.AutoDetectRowSelector(_loadedHtml);
                if (!string.IsNullOrWhiteSpace(detectedRowSelector))
                {
                    _rowSelectorBox.Text = detectedRowSelector;
                    _rowSelectorTypeBox.SelectedItem = "CSS";
                    LogActivity($"Row scope auto-detected for multi-page extraction: {detectedRowSelector}");
                }
                else
                {
                    SetStatus("Multi-page extraction needs a row scope. Set Row Scope or use Auto Detect first.", 0);
                    LogActivity("Stopped before extraction because no row scope was available for multi-page mode.");
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(_rowSelectorBox.Text))
            {
                await ExtractRowBasedDataAsync(mappings);
                return;
            }

            ExtractColumnBasedData(mappings);
        }
        catch (Exception ex)
        {
            SetStatus($"Extraction failed: {ex.Message}", 0);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void ExtractColumnBasedData(List<FieldMapping> mappings)
    {
        _runSummaryLabel.Text = $"Run summary: single-page extraction with {mappings.Count} field(s).";
        _pageProgressLabel.Text = "Page progress: processing current page only.";
        AppendOutputHtmlPreviewSnapshot(_loadedHtml, "single-page extraction source", _inputBox.Text.Contains('<') ? null : _inputBox.Text.Trim(), clearExisting: true);
        SetStatus("Building dynamic output layout...", 40);
        LogActivity("Extracting independent selector columns from the current page.");
        var extractedColumns = new List<(FieldMapping Mapping, List<string> Values)>();
        for (var columnIndex = 0; columnIndex < mappings.Count; columnIndex++)
        {
            var mapping = mappings[columnIndex];
            SetStatus($"Extracting field {columnIndex + 1} of {mappings.Count}: {mapping.FieldName}", 45 + (columnIndex * 35 / Math.Max(1, mappings.Count)));
            LogActivity($"Field {columnIndex + 1}/{mappings.Count}: {mapping.FieldName}");
            var selectorKind = ParseSelectorKind(mapping.SelectorType);
            var values = _selectorHelper.ExtractValues(_loadedHtml, mapping.Selector.Trim(), selectorKind);
            extractedColumns.Add((mapping, values));
        }

        var rowCount = extractedColumns.Max(column => column.Values.Count);
        if (rowCount == 0)
        {
            _grid.DataSource = null;
            _resultTable = new DataTable();
            SetStatus("No rows matched the configured field selectors.", 0);
            return;
        }

        _resultTable = BuildResultTable(
            extractedColumns.Select(column => column.Mapping.FieldName).ToList(),
            Enumerable.Range(0, rowCount).Select(rowIndex =>
            {
                var row = new Dictionary<string, string>();
                foreach (var column in extractedColumns)
                {
                    row[column.Mapping.FieldName] = rowIndex < column.Values.Count ? column.Values[rowIndex] : string.Empty;
                }

                return row;
            }).ToList());

        _grid.DataSource = null;
        _grid.DataSource = _resultTable;
        _insightLabel.Text = $"Output layout built from {mappings.Count} field definition(s). Review the dynamic grid and refine any selectors before exporting.";
        _runSummaryLabel.Text = $"Run summary: {_resultTable.Rows.Count} row(s) built from the current page.";
        _pageProgressLabel.Text = "Page progress: single-page extraction complete.";
        LogActivity($"Extraction complete. Built {_resultTable.Rows.Count} row(s).");
        SetStatus($"Extraction complete. {_resultTable.Rows.Count} rows built across {_resultTable.Columns.Count} columns.", 100);
    }

    private async Task ExtractRowBasedDataAsync(List<FieldMapping> mappings)
    {
        var pages = ResolvePagesToScan();
        var pagesToProcess = pages.Count;
        var isMultiPageRun = pagesToProcess > 1;
        
        if (_inputBox.Text.Contains('<') && _inputBox.Text.Contains('>') && isMultiPageRun)
        {
            isMultiPageRun = false;
            pagesToProcess = 1;
            pages = [1];
            LogActivity("Raw HTML input detected. Multi-page crawl disabled for this run.");
        }
 
        var pageRows = new List<Dictionary<string, string>>();
        var pageNumberIndex = 0;
        var currentUrl = _inputBox.Text.Trim();
        var baseUrl = currentUrl;
        var currentHtml = _loadedHtml;
        string? nextUrl = null;
        
        int? discoveredPageCount = _estimatedTotalPages ?? TryEstimateTotalPages(currentHtml);
        
        _runSummaryLabel.Text = isMultiPageRun
            ? $"Run summary: multi-page extraction queued for up to {pagesToProcess} page(s)."
            : "Run summary: current page only.";
        _pageProgressLabel.Text = $"Page progress: waiting for page {pages.FirstOrDefault()}.";
        
        if (isMultiPageRun)
        {
            _loadedHtml = currentHtml;
            _htmlPreviewBox.Clear();
            AppendHtmlPreviewSnapshot(currentHtml, $"page {pages.FirstOrDefault()} snapshot for multi-page run", currentUrl, clearExisting: true);
            AppendOutputHtmlPreviewSnapshot(currentHtml, $"page {pages.FirstOrDefault()} scraped during multi-page extraction", currentUrl, clearExisting: true);
        }
        else
        {
            UpdateHtmlPreviewSnapshot(currentHtml, "current page snapshot", currentUrl);
            AppendOutputHtmlPreviewSnapshot(currentHtml, "single-page row extraction source", currentUrl, clearExisting: true);
        }
 
        var fieldPlans = mappings.Select(mapping => new FieldExtractionPlan
        {
            FieldName = mapping.FieldName,
            Selector = mapping.Selector,
            SelectorKind = ParseSelectorKind(mapping.SelectorType)
        }).ToList();
 
        while (pageNumberIndex < pagesToProcess)
        {
            var pageNumber = pages[pageNumberIndex];
            string? pageTarget;
            
            if (pageNumber == 1)
            {
                pageTarget = currentUrl;
            }
            else if (pageNumberIndex > 0 && !string.IsNullOrWhiteSpace(nextUrl))
            {
                pageTarget = nextUrl;
            }
            else
            {
                pageTarget = BuildEffectiveUrlForPage(baseUrl, pageNumber);
            }
 
            if (pageNumberIndex > 0 && string.IsNullOrWhiteSpace(pageTarget))
            {
                LogActivity("No next page target could be determined. Pagination crawl is complete.");
                break;
            }
 
            var pageLabel = discoveredPageCount is > 0 ? $"{pageNumber} of {discoveredPageCount}" : pageNumber.ToString();
            _pageProgressLabel.Text = $"Page progress: loading page {pageLabel}.";
            _runSummaryLabel.Text = $"Run summary: {pageRows.Count} row(s) collected so far across {pageNumberIndex} completed page(s).";
            SetStatus($"Processing page {pageLabel}...", CalculateProgress(pageNumberIndex + 1, pagesToProcess, 5, 70));
            LogActivity($"Loading page {pageLabel}: {pageTarget}");
 
            if (pageNumber != 1)
            {
                currentHtml = await FetchHtmlForUrlAsync(pageTarget!);
                _loadedHtml = currentHtml;
                if (isMultiPageRun)
                {
                    AppendHtmlPreviewSnapshot(currentHtml, $"page {pageLabel} snapshot during extraction", pageTarget);
                    AppendOutputHtmlPreviewSnapshot(currentHtml, $"page {pageLabel} scraped during extraction", pageTarget);
                }
                else
                {
                    UpdateHtmlPreviewSnapshot(currentHtml, $"page {pageLabel} snapshot during extraction", pageTarget);
                    AppendOutputHtmlPreviewSnapshot(currentHtml, $"page {pageLabel} scraped during extraction", pageTarget, clearExisting: true);
                }
                discoveredPageCount ??= TryEstimateTotalPages(currentHtml);
            }
 
            var rowSelectorKind = ParseSelectorKind(_rowSelectorTypeBox.SelectedItem?.ToString() ?? "CSS");
            var rows = _selectorHelper.ExtractRows(currentHtml, _rowSelectorBox.Text.Trim(), rowSelectorKind, fieldPlans);
            LogActivity($"Extracted {rows.Count} row(s) from page {pageLabel}.");
            pageRows.AddRange(rows);
            _pageProgressLabel.Text = $"Page progress: page {pageLabel} complete, {rows.Count} row(s) extracted from this page.";
            _runSummaryLabel.Text = $"Run summary: {pageRows.Count} row(s) collected after page {pageNumber}.";
 
            if (!isMultiPageRun)
            {
                break;
            }
 
            nextUrl = DiscoverNextPageUrl(currentHtml, pageTarget!);
            if (!string.IsNullOrWhiteSpace(nextUrl))
            {
                LogActivity($"Next page discovered: {nextUrl}");
            }
            else if (pageNumberIndex + 1 < pagesToProcess)
            {
                var fallbackTarget = BuildEffectiveUrlForPage(baseUrl, pages[pageNumberIndex + 1]);
                LogActivity($"Next page link not found in markup. The next step will use generated URL: {fallbackTarget}");
            }
 
            pageNumberIndex++;
        }
 
        _resultTable = BuildResultTable(mappings.Select(mapping => mapping.FieldName).ToList(), pageRows);
        _grid.DataSource = null;
        _grid.DataSource = _resultTable;
 
        var pageSummary = pageNumberIndex <= 0 ? 1 : pageNumberIndex;
        _insightLabel.Text = $"Row-based extraction completed across {pageSummary} page(s). Review the live activity trail and output grid before exporting.";
        _runSummaryLabel.Text = $"Run summary: {_resultTable.Rows.Count} total row(s) collected across {pageSummary} page(s).";
        _pageProgressLabel.Text = $"Page progress: finished {pageSummary} page(s).";
        LogActivity($"Run complete. Total rows: {_resultTable.Rows.Count}.");
        SetStatus($"Extraction complete. {_resultTable.Rows.Count} rows collected from {pageSummary} page(s).", 100);
    }

    private void UpdateHtmlPreviewSnapshot(string html, string snapshotDescription, string? sourceUrl = null)
    {
        _loadedHtml = html;
        _htmlPreviewBox.Text = html;
        var sourceText = string.IsNullOrWhiteSpace(sourceUrl) ? string.Empty : $" Source: {sourceUrl}";
        _htmlPreviewStatusLabel.Text = $"Current HTML snapshot: {snapshotDescription}.{sourceText} The Input tab shows one page snapshot at a time; combined multi-page results appear in the Output tab.";
    }

    private void AppendHtmlPreviewSnapshot(string html, string snapshotDescription, string? sourceUrl = null, bool clearExisting = false)
    {
        if (clearExisting)
        {
            _htmlPreviewBox.Clear();
        }

        var header = new StringBuilder()
            .AppendLine()
            .AppendLine("============================================================")
            .AppendLine($"HTML SNAPSHOT: {snapshotDescription.ToUpperInvariant()}")
            .AppendLine(string.IsNullOrWhiteSpace(sourceUrl) ? "SOURCE: (not available)" : $"SOURCE: {sourceUrl}")
            .AppendLine("============================================================")
            .AppendLine()
            .ToString();

        _htmlPreviewBox.AppendText(header);
        _htmlPreviewBox.AppendText(html);
        _htmlPreviewBox.AppendText(Environment.NewLine);
        _htmlPreviewStatusLabel.Text = $"Current HTML preview: combined multi-page snapshot in progress. Latest section: {snapshotDescription}.{(string.IsNullOrWhiteSpace(sourceUrl) ? string.Empty : $" Source: {sourceUrl}")}";
    }

    private void ClearOutputHtmlPreview()
    {
        _outputHtmlPreviewBox.Clear();
        _outputHtmlStatusLabel.Text = "Scraped HTML preview: waiting to start. Each scraped page will be appended here one by one during extraction.";
    }

    private void AppendOutputHtmlPreviewSnapshot(string html, string snapshotDescription, string? sourceUrl = null, bool clearExisting = false)
    {
        if (clearExisting)
        {
            _outputHtmlPreviewBox.Clear();
        }

        var header = new StringBuilder()
            .AppendLine()
            .AppendLine("============================================================")
            .AppendLine($"SCRAPED HTML: {snapshotDescription.ToUpperInvariant()}")
            .AppendLine(string.IsNullOrWhiteSpace(sourceUrl) ? "SOURCE: (not available)" : $"SOURCE: {sourceUrl}")
            .AppendLine("============================================================")
            .AppendLine()
            .ToString();

        _outputHtmlPreviewBox.AppendText(header);
        _outputHtmlPreviewBox.AppendText(html);
        _outputHtmlPreviewBox.AppendText(Environment.NewLine);
        _outputHtmlPreviewBox.SelectionStart = _outputHtmlPreviewBox.TextLength;
        _outputHtmlPreviewBox.ScrollToCaret();
        _outputHtmlStatusLabel.Text = $"Scraped HTML preview: appended page HTML successfully. Latest section: {snapshotDescription}.{(string.IsNullOrWhiteSpace(sourceUrl) ? string.Empty : $" Source: {sourceUrl}")}";
    }

    private void CopyOutputHtmlPreview()
    {
        if (string.IsNullOrWhiteSpace(_outputHtmlPreviewBox.Text))
        {
            SetStatus("There is no scraped HTML to copy yet.", 0);
            return;
        }

        try
        {
            Clipboard.SetText(_outputHtmlPreviewBox.Text);
            SetStatus("Copied scraped HTML preview to clipboard.", 100);
        }
        catch (Exception ex)
        {
            SetStatus($"Copy failed: {ex.Message}", 0);
        }
    }

    private void SwitchToWorkspaceTab(string tabText)
    {
        foreach (TabPage tabPage in _workspaceTabs.TabPages)
        {
            if (string.Equals(tabPage.Text, tabText, StringComparison.OrdinalIgnoreCase))
            {
                _workspaceTabs.SelectedTab = tabPage;
                return;
            }
        }
    }

    private void PreviewSelection(bool autoPreview = false)
    {
        _previewList.Items.Clear();

        if (string.IsNullOrWhiteSpace(_loadedHtml))
        {
            if (!autoPreview)
            {
                SetStatus("Load a page before previewing a selector.", 0);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectorBox.Text))
        {
            _insightLabel.Text = $"{BuildLoadInsight(_loadedHtml, (_siteTypeBox.SelectedItem?.ToString() ?? "Static") == "JavaScript")} Enter a selector to preview sample values.";
            if (!autoPreview)
            {
                SetStatus("Enter a selector to preview sample values.", 0);
            }
            return;
        }

        try
        {
            var selectorKind = (_selectorTypeBox.SelectedItem?.ToString() ?? "CSS") == "XPath"
                ? SelectorKind.XPath
                : SelectorKind.Css;

            var values = _selectorHelper.ExtractValues(_loadedHtml, _selectorBox.Text.Trim(), selectorKind);
            var sample = values.Take(20).ToList();

            if (sample.Count == 0)
            {
                _previewList.Items.Add("No matching values found.");
                _insightLabel.Text = $"{BuildLoadInsight(_loadedHtml, (_siteTypeBox.SelectedItem?.ToString() ?? "Static") == "JavaScript")} No values matched this selector yet.";
                if (!autoPreview)
                {
                    SetStatus("Preview found no matching values.", 0);
                }
                return;
            }

            foreach (var value in sample)
            {
                _previewList.Items.Add(value);
            }

            var suffix = values.Count > sample.Count ? $" Showing first {sample.Count} of {values.Count} matches." : $" {values.Count} matches found.";
            _insightLabel.Text = $"Previewing selector results before extraction.{suffix}";
            if (!autoPreview)
            {
                SetStatus($"Preview ready: {values.Count} matching values.", 100);
            }
        }
        catch (Exception ex)
        {
            _previewList.Items.Add($"Preview failed: {ex.Message}");
            _insightLabel.Text = "The selector could not be previewed. Check whether the selector type and site type are correct.";
            if (!autoPreview)
            {
                SetStatus($"Preview failed: {ex.Message}", 0);
            }
        }
    }

    private void ExportRows()
    {
        if (_resultTable.Rows.Count == 0 || _resultTable.Columns.Count == 0)
        {
            SetStatus("There are no rows to export yet.", 0);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|JSON (*.json)|*.json|Excel Workbook (*.xls)|*.xls",
            FileName = "scrape-results"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            LogActivity($"Exporting {_resultTable.Rows.Count} row(s) as {_exportFormatBox.SelectedItem}.");
            var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            switch (extension)
            {
                case ".json":
                    _exportService.ToJson(_resultTable, dialog.FileName);
                    break;
                case ".xls":
                    _exportService.ToExcel(_resultTable, dialog.FileName);
                    break;
                default:
                    _exportService.ToCsv(_resultTable, dialog.FileName);
                    break;
            }

            SetStatus($"Exported {_resultTable.Rows.Count} rows to {Path.GetFileName(dialog.FileName)}.", 100);
            LogActivity($"Export complete: {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex)
        {
            SetStatus($"Export failed: {ex.Message}", 0);
        }
    }

    private void OpenSiblingTool(string executableName)
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, executableName);
        if (!File.Exists(candidate))
        {
            MessageBox.Show(this, $"Build and run {executableName} from the solution to use that module.", "Module Not Found");
            return;
        }

        Process.Start(new ProcessStartInfo(candidate) { UseShellExecute = true });
    }

    private void SetStatus(string message, int progress)
    {
        _statusLabel.Text = message;
        _progressBar.Value = Math.Max(_progressBar.Minimum, Math.Min(_progressBar.Maximum, progress));
    }

    private void LogActivity(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _activityList.Items.Insert(0, $"{timestamp}  {message}");
        while (_activityList.Items.Count > 100)
        {
            _activityList.Items.RemoveAt(_activityList.Items.Count - 1);
        }
    }

    private void ConfigureFieldGrid()
    {
        ApplyGridTheme(_fieldGrid);
        _fieldGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _fieldGrid.Columns.Clear();
        _fieldGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Field Name",
            DataPropertyName = nameof(FieldMapping.FieldName)
        });
        _fieldGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Selector",
            DataPropertyName = nameof(FieldMapping.Selector),
            FillWeight = 180
        });
        _fieldGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            HeaderText = "Selector Type",
            DataPropertyName = nameof(FieldMapping.SelectorType),
            DataSource = new[] { "CSS", "XPath" }
        });
        _fieldGrid.DataSource = _fieldMappings;
    }

    private void AddFieldMapping()
    {
        var fieldNumber = _fieldMappings.Count + 1;
        _fieldMappings.Add(new FieldMapping
        {
            FieldName = $"Field {fieldNumber}",
            Selector = string.Empty,
            SelectorType = "CSS"
        });
        _fieldGrid.ClearSelection();
        if (_fieldGrid.Rows.Count > 0)
        {
            var index = _fieldGrid.Rows.Count - 1;
            _fieldGrid.Rows[index].Selected = true;
            _fieldGrid.CurrentCell = _fieldGrid.Rows[index].Cells[0];
        }

        SetStatus("Field added. Name it and assign a selector.", 100);
    }

    private void UseCurrentSelectorForSelectedField()
    {
        if (string.IsNullOrWhiteSpace(_selectorBox.Text))
        {
            SetStatus("Preview or detect a selector first, then assign it to a field.", 0);
            return;
        }

        var mapping = GetSelectedFieldMapping();
        if (mapping is null)
        {
            AddFieldMapping();
            mapping = GetSelectedFieldMapping();
        }

        if (mapping is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(mapping.FieldName) || mapping.FieldName.StartsWith("Field ", StringComparison.Ordinal))
        {
            mapping.FieldName = SuggestFieldName();
        }

        mapping.Selector = _selectorBox.Text.Trim();
        mapping.SelectorType = _selectorTypeBox.SelectedItem?.ToString() ?? "CSS";
        _fieldGrid.Refresh();
        SetStatus($"Assigned current selector to {mapping.FieldName}.", 100);
    }

    private void RemoveSelectedField()
    {
        var mapping = GetSelectedFieldMapping();
        if (mapping is null)
        {
            SetStatus("Select a field row to remove first.", 0);
            return;
        }

        _fieldMappings.Remove(mapping);
        SetStatus("Field removed from the output layout.", 100);
    }

    private FieldMapping? GetSelectedFieldMapping()
    {
        if (_fieldGrid.CurrentRow?.DataBoundItem is FieldMapping mapping)
        {
            return mapping;
        }

        return _fieldGrid.SelectedRows.Count > 0
            ? _fieldGrid.SelectedRows[0].DataBoundItem as FieldMapping
            : null;
    }

    private List<FieldMapping> GetActiveFieldMappings()
    {
        return _fieldMappings
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.FieldName) && !string.IsNullOrWhiteSpace(mapping.Selector))
            .ToList();
    }

    private void EnsureStarterField()
    {
        if (_fieldMappings.Count > 0 || string.IsNullOrWhiteSpace(_selectorBox.Text))
        {
            return;
        }

        _fieldMappings.Add(new FieldMapping
        {
            FieldName = SuggestFieldName(),
            Selector = _selectorBox.Text.Trim(),
            SelectorType = _selectorTypeBox.SelectedItem?.ToString() ?? "CSS"
        });
    }

    private string SuggestFieldName()
    {
        var seed = _selectorBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(seed))
        {
            return $"Field {_fieldMappings.Count + 1}";
        }

        var token = seed
            .Split([' ', '>', '.', '#', '/', '[', ']', ':'], StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        if (string.IsNullOrWhiteSpace(token))
        {
            return $"Field {_fieldMappings.Count + 1}";
        }

        var normalized = string.Concat(token
            .Select(character => character == '-' || character == '_' ? ' ' : character))
            .Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? $"Field {_fieldMappings.Count + 1}"
            : char.ToUpperInvariant(normalized[0]) + normalized[1..];
    }

    private static SelectorKind ParseSelectorKind(string selectorType)
    {
        return string.Equals(selectorType, "XPath", StringComparison.OrdinalIgnoreCase)
            ? SelectorKind.XPath
            : SelectorKind.Css;
    }

    private static Panel CreateCard()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 16)
        };
    }

    private static Label CreateRowLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.Gainsboro,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };
    }

    private static Button CreateButton(string text, EventHandler onClick, int width, bool accent = false)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = accent ? Color.FromArgb(14, 165, 233) : Color.FromArgb(51, 65, 85),
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
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(14, 165, 233);
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    private static string BuildLoadInsight(string html, bool usedJavaScriptMode)
    {
        return BuildLoadInsight(html, usedJavaScriptMode, string.Empty);
    }

    private static string BuildLoadInsight(string html, bool usedJavaScriptMode, string loadSummary)
    {
        var bodyTextLength = System.Text.RegularExpressions.Regex
            .Replace(html, "<.*?>", " ", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Trim()
            .Length;

        var looksLikeSpaShell =
            html.Contains("id=\"app\"", StringComparison.OrdinalIgnoreCase) &&
            html.Contains("type=\"module\"", StringComparison.OrdinalIgnoreCase) &&
            bodyTextLength < 150;

        if (looksLikeSpaShell && !usedJavaScriptMode)
        {
            return AppendLoadSummary("This page looks like a JavaScript app shell only. Switch Site Type to JavaScript, reload, then preview the selector again so you can see the real session data before extracting.", loadSummary);
        }

        if (looksLikeSpaShell && usedJavaScriptMode)
        {
            return AppendLoadSummary("This page still looks sparse after rendering. Use Preview with a precise selector or inspect the live page in Element Picker to target the real session rows.", loadSummary);
        }

        return AppendLoadSummary("Loaded content looks usable. Preview a selector now to confirm the values match the exact data you want before running extraction.", loadSummary);
    }

    private static string AppendLoadSummary(string baseMessage, string loadSummary)
    {
        return string.IsNullOrWhiteSpace(loadSummary)
            ? baseMessage
            : $"{baseMessage} {loadSummary}".Trim();
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
            SetStatus("Stopping auto-scroll. Fetching current page state...", _progressBar.Value);
        }
    }

    private async Task<string> FetchHtmlForUrlAsync(string url)
    {
        var siteType = _siteTypeBox.SelectedItem?.ToString() ?? "Static";
        if (siteType == "JavaScript")
        {
            if (_playwrightScraper.HasInteractiveSession)
            {
                LogActivity($"Using approved browser session for: {url}");
                return await _playwrightScraper.NavigateInteractiveAndGetHtmlAsync(url);
            }

            LogActivity($"Using fresh JavaScript browser load for: {url}");
            return await _playwrightScraper.GetHtmlAsync(url);
        }

        LogActivity($"Using static HTTP load for: {url}");
        return await _httpScraper.GetHtmlAsync(url);
    }

    private static DataTable BuildResultTable(IReadOnlyList<string> fieldNames, IReadOnlyList<Dictionary<string, string>> rows)
    {
        var table = new DataTable("ScrapeResults");
        foreach (var fieldName in fieldNames)
        {
            table.Columns.Add(fieldName);
        }

        foreach (var sourceRow in rows)
        {
            var row = table.NewRow();
            foreach (var fieldName in fieldNames)
            {
                row[fieldName] = sourceRow.TryGetValue(fieldName, out var value) ? value : string.Empty;
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private static int CalculateProgress(int currentPage, int totalPages, int start, int end)
    {
        if (totalPages <= 1)
        {
            return end;
        }

        var ratio = (double)Math.Max(0, currentPage - 1) / Math.Max(1, totalPages);
        return start + (int)Math.Round((end - start) * ratio);
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
            .Select(link => new { Link = link!, Page = GetPageNumber(link!) ?? 0 })
            .Where(entry => entry.Page > currentPage)
            .OrderBy(entry => entry.Page)
            .FirstOrDefault()
            ?.Link;
    }

    private static int? GetPageNumber(string url)
    {
        var match = Regex.Match(url, @"[?&]page=(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var pageNumber) ? pageNumber : null;
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

        return Uri.TryCreate(new Uri(currentUrl), href, out var combined) ? combined.ToString() : null;
    }

    private async Task RunAiExtractionAsync()
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
            SetStatus("Load a page first before running AI Smart Extract.", 0);
            return;
        }

        try
        {
            _isBusy = true;
            _approvalLabel.Visible = false;
            SetStatus("AI analyzing page and extracting structured data...", 40);
            LogActivity("AI Smart Extraction job started.");
            
            var service = new AiService();
            var results = await service.ExtractStructuredDataAsync(_loadedHtml, _inputBox.Text.Trim(), settings);
            
            if (results.Count == 0)
            {
                SetStatus("AI did not find structured data on the page.", 0);
                return;
            }

            var headers = results.First().Keys.ToList();
            _resultTable = BuildResultTable(headers, results);
            _grid.DataSource = null;
            _grid.DataSource = _resultTable;
            
            SwitchToWorkspaceTab("Output");
            _runSummaryLabel.Text = $"Run summary: AI extracted {results.Count} items.";
            SetStatus($"AI Extraction complete. Extracted {results.Count} items.", 100);
            LogActivity($"AI Extracted {results.Count} items. Columns: {string.Join(", ", headers)}");

            if (settings.AutoSaveCsv || settings.AutoSaveJson)
            {
                AutoSaveTextResults(results, headers, settings);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"AI Extraction failed: {ex.Message}", 0);
            MessageBox.Show(this, $"AI error: {ex.Message}", "AI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void AutoSaveTextResults(List<Dictionary<string, string>> results, List<string> headers, ScraperSettings settings)
    {
        try
        {
            Directory.CreateDirectory(settings.OutputFolder);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sanitizedUrl = Regex.Replace(_inputBox.Text, @"[^a-zA-Z0-9]", "_").Trim('_');
            if (sanitizedUrl.Length > 30) sanitizedUrl = sanitizedUrl.Substring(0, 30);
            
            if (settings.AutoSaveJson)
            {
                var jsonPath = Path.Combine(settings.OutputFolder, $"extract_{sanitizedUrl}_{timestamp}.json");
                var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, json);
                LogActivity($"Auto-saved JSON to: {jsonPath}");
            }
            
            if (settings.AutoSaveCsv)
            {
                var csvPath = Path.Combine(settings.OutputFolder, $"extract_{sanitizedUrl}_{timestamp}.csv");
                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine(string.Join(",", headers.Select(h => $"\"{h.Replace("\"", "\"\"")}\"")));
                foreach (var row in results)
                {
                    var line = string.Join(",", headers.Select(h => 
                    {
                        var val = row.ContainsKey(h) ? row[h] : "";
                        return $"\"{val.Replace("\"", "\"\"")}\"";
                    }));
                    csvBuilder.AppendLine(line);
                }
                File.WriteAllText(csvPath, csvBuilder.ToString(), Encoding.UTF8);
                LogActivity($"Auto-saved CSV to: {csvPath}");
            }
        }
        catch (Exception ex)
        {
            LogActivity($"Auto-save failed: {ex.Message}");
        }
    }

    private void ToggleManualSelectors()
    {
        var show = _showManualSelectorsBox.Checked;
        _manualPanel.Visible = show;
        
        var mainLayout = _manualPanel.Parent as TableLayoutPanel;
        if (mainLayout != null)
        {
            mainLayout.RowStyles[2].Height = show ? 80 : 0;
            var card = mainLayout.Parent as Panel;
            if (card != null)
            {
                card.Height = show ? 270 : 180;
            }
        }
    }


    private void UpdatePageModeUi()
    {
        var mode = _pageModeBox.SelectedItem?.ToString() ?? "All pages";
        var isCustomPage = mode == "Custom page";
        var isRange = mode == "Page range";
        _pageNumberBox.Enabled = isCustomPage;
        _pageRangeStartBox.Enabled = isRange;
        _pageRangeEndBox.Enabled = isRange;
        UpdatePageInfoLabel();
    }

    private void UpdatePageInfoLabel()
    {
        var mode = _pageModeBox.SelectedItem?.ToString() ?? "All pages";
        _pageInfoLabel.Text = mode switch
        {
            "All pages" when _estimatedTotalPages.HasValue => $"All pages (1-{_estimatedTotalPages.Value})",
            "All pages" => "All pages",
            "Page range" => $"Pages {(int)_pageRangeStartBox.Value}-{(int)_pageRangeEndBox.Value}",
            _ when _estimatedTotalPages.HasValue => $"Page {(int)_pageNumberBox.Value} of {_estimatedTotalPages.Value}",
            _ => $"Page {(int)_pageNumberBox.Value}"
        };
    }

    private List<int> ResolvePagesToScan()
    {
        var mode = _pageModeBox.SelectedItem?.ToString() ?? "All pages";
        return mode switch
        {
            "All pages" => Enumerable.Range(1, Math.Max(1, _estimatedTotalPages ?? (int)_pageNumberBox.Value)).ToList(),
            "Page range" => BuildPageRange(),
            _ => [(int)_pageNumberBox.Value]
        };
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

    private async Task RunAiLayoutAnalysisAsync(string url, bool showResultDialog = false)
    {
        var settings = SettingsService.Load();
        if (string.IsNullOrEmpty(settings.ApiKey))
        {
            if (showResultDialog)
            {
                MessageBox.Show(this,
                    "AI Auto-Detection is disabled because no API Key is configured.\n\n" +
                    "To automatically detect the number of pages, site type, and selectors, click the ⚙ button on the dashboard to configure your API key.",
                    "API Key Required for Auto-Detection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            return;
        }

        try
        {
            SetStatus("AI analyzing page structure & pagination...", 50);
            LogActivity("AI analyzing page layout.");
            
            var service = new AiService();
            var analysis = await service.AnalyzePageLayoutAsync(_loadedHtml, url, settings);
            
            _siteTypeBox.SelectedItem = analysis.SiteType;
            _rowSelectorBox.Text = analysis.RowSelector;
            _rowSelectorTypeBox.SelectedItem = analysis.RowSelectorType;
            _estimatedTotalPages = analysis.EstimatedPages;
            
            if (analysis.EstimatedPages > 0)
            {
                _pageNumberBox.Maximum = Math.Max(analysis.EstimatedPages, 500);
                _pageRangeStartBox.Maximum = Math.Max(analysis.EstimatedPages, 500);
                _pageRangeEndBox.Maximum = Math.Max(analysis.EstimatedPages, 500);
            }
            
            _fieldMappings.Clear();
            foreach (var f in analysis.Fields)
            {
                _fieldMappings.Add(new FieldMapping
                {
                    FieldName = f.FieldName,
                    Selector = f.Selector,
                    SelectorType = f.SelectorType
                });
            }
            _fieldGrid.Refresh();
            
            UpdatePageInfoLabel();
            
            LogActivity($"AI Layout Analysis: Site Type = {analysis.SiteType}, Rows = {analysis.RowSelector}, Pages = {analysis.EstimatedPages}, Fields = {analysis.Fields.Count}.");
            SetStatus($"AI analyzed page. Detected {analysis.EstimatedPages} pages.", 100);
            
            if (analysis.SiteType == "JavaScript" && _siteTypeBox.SelectedItem?.ToString() == "Static")
            {
                LogActivity("AI detected dynamic JavaScript page. Automatically reloading in JavaScript mode...");
                _siteTypeBox.SelectedItem = "JavaScript";
                _loadedHtml = await _playwrightScraper.GetHtmlAsync(url);
                UpdateHtmlPreviewSnapshot(_loadedHtml, "loaded source page (JavaScript)", url);
                
                LogActivity("AI analyzing dynamic page layout.");
                analysis = await service.AnalyzePageLayoutAsync(_loadedHtml, url, settings);
                
                _rowSelectorBox.Text = analysis.RowSelector;
                _rowSelectorTypeBox.SelectedItem = analysis.RowSelectorType;
                _estimatedTotalPages = analysis.EstimatedPages;
                
                if (analysis.EstimatedPages > 0)
                {
                    _pageNumberBox.Maximum = Math.Max(analysis.EstimatedPages, 500);
                    _pageRangeStartBox.Maximum = Math.Max(analysis.EstimatedPages, 500);
                    _pageRangeEndBox.Maximum = Math.Max(analysis.EstimatedPages, 500);
                }
                
                _fieldMappings.Clear();
                foreach (var f in analysis.Fields)
                {
                    _fieldMappings.Add(new FieldMapping
                    {
                        FieldName = f.FieldName,
                        Selector = f.Selector,
                        SelectorType = f.SelectorType
                    });
                }
                _fieldGrid.Refresh();
                
                UpdatePageInfoLabel();
                LogActivity($"AI dynamic analysis complete: {analysis.Fields.Count} fields found.");
            }

            if (showResultDialog)
            {
                MessageBox.Show(this, 
                    $"AI analysis completed successfully!\n\n" +
                    $"• Site Type: {analysis.SiteType}\n" +
                    $"• Detected Pages: {analysis.EstimatedPages}\n" +
                    $"• Extracted Fields: {analysis.Fields.Count}\n\n" +
                    $"Now, select whether you want to extract 'All pages', a 'Custom page', or a 'Page range' under the Pages options, then click Extract.",
                    "Page Analysis Complete", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            LogActivity($"AI Layout analysis failed: {ex.Message}");
        }
    }

    private static bool IsValidUrl(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) && 
               Uri.TryCreate(text, UriKind.Absolute, out _);
    }

    private void InputBox_TextChanged(object? sender, EventArgs e)
    {
        _pasteDebounceTimer.Stop();
        var text = _inputBox.Text.Trim();
        if (IsValidUrl(text))
        {
            _pasteDebounceTimer.Start();
        }
    }

    private async void PasteDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _pasteDebounceTimer.Stop();
        await LoadHtmlAsync(force: false);
    }

    private sealed class FieldMapping
    {
        public string FieldName { get; set; } = string.Empty;
        public string Selector { get; set; } = string.Empty;
        public string SelectorType { get; set; } = "CSS";
    }

    private sealed class SelectorTransferMessage
    {
        public string Selector { get; set; } = string.Empty;
        public string SelectorType { get; set; } = "CSS";
        public DateTime SentAtUtc { get; set; }
    }
}
