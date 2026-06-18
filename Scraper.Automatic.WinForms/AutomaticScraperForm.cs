using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Scraper.Core.Helpers;
using Scraper.Core.Models;
using Scraper.Core.Services;

namespace Scraper.Automatic.WinForms;

public class AutomaticScraperForm : Form
{
    private static readonly string SelectorTransferPath = Path.Combine(Path.GetTempPath(), "WebScraperPro", "selector-transfer.json");

    private readonly HttpScraperService _httpScraper = new();
    private readonly PlaywrightService _playwrightScraper = new();
    private readonly SelectorHelper _selectorHelper = new();
    private readonly WebhookDispatchService _webhookDispatch = new();

    private readonly TextBox _urlBox = new() { Dock = DockStyle.Fill, PlaceholderText = "https://example.com/live" };
    private readonly ComboBox _siteTypeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _selectorTypeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox _selectorBox = new() { Dock = DockStyle.Fill, PlaceholderText = ".scoreboard .score or //span[@class='price']" };
    private readonly TextBox _fieldNameBox = new() { Dock = DockStyle.Fill, Text = "Value" };
    private readonly TextBox _webhookBox = new() { Dock = DockStyle.Fill, PlaceholderText = "https://your-endpoint.example/webhook" };
    private readonly NumericUpDown _intervalSecondsBox = new() { Minimum = 2, Maximum = 3600, Value = 15, Width = 90 };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, AllowUserToAddRows = false };
    private readonly ListBox _activityList = new() { Dock = DockStyle.Top, Height = 140, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.None };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Bottom, Height = 20 };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, ForeColor = Color.Gainsboro };
    private readonly Label _profileSummaryLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 48,
        ForeColor = Color.FromArgb(125, 211, 252),
        Text = "Transmission settings: default profile with one quick field."
    };
    private readonly Label _insightLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 60,
        ForeColor = Color.Silver,
        Text = "Poll a page on an interval, extract live values like scores or prices, and send updates to a webhook only when the scraped content changes."
    };
    private readonly Label _approvalLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 44,
        ForeColor = Color.FromArgb(167, 243, 208),
        Text = "Manual approval opens a live browser so you can sign in or complete page verification yourself before starting the live monitor.",
        Visible = false
    };
    private readonly System.Windows.Forms.Timer _selectorImportTimer = new() { Interval = 1000 };
    private readonly CheckBox _autoScrollBox = new() { Text = "Auto-Scroll", Checked = true, ForeColor = Color.White, AutoSize = true };
    private Button? _stopScrollButton;

    private readonly BindingSource _bindingSource = new();
    private AutomaticTransmissionProfile _transmissionProfile = CreateDefaultTransmissionProfile();
    private CancellationTokenSource? _monitorCts;
    private bool _isBusy;
    private string _lastContentHash = string.Empty;
    private DateTime _lastSelectorTransferSeenUtc = DateTime.MinValue;

    public AutomaticScraperForm()
    {
        Text = "WebScraper Pro - Automatic Scraper";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 760);
        Size = new Size(1240, 820);
        BackColor = Color.FromArgb(15, 23, 42);
        Font = new Font("Segoe UI", 10F);

        _siteTypeBox.Items.AddRange(["Static", "JavaScript"]);
        _siteTypeBox.SelectedIndex = 0;
        _selectorTypeBox.Items.AddRange(["CSS", "XPath"]);
        _selectorTypeBox.SelectedIndex = 0;

        _bindingSource.DataSource = new List<AutomaticScrapeFieldResult>();
        _grid.DataSource = _bindingSource;

        ConfigureOutputGrid();
        Controls.Add(BuildLayout());
        _selectorImportTimer.Tick += (_, _) => TryImportTransferredSelector();
        _selectorImportTimer.Start();
        FormClosed += async (_, _) =>
        {
            StopMonitoring();
            await _playwrightScraper.DisposeAsync();
        };
        RefreshProfileSummary();

        var settings = SettingsService.Load();
        _autoScrollBox.Checked = settings.EnableAutoScroll;

        SetStatus("Ready to monitor a live page.");
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
            Text = "Automatic Scraper",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = "Monitor a live page like a football match or crypto price board, extract changes from selectors, and transmit those changes externally in a payload shape you control.",
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
        card.Height = 330;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        layout.Controls.Add(CreateRowLabel("URL"), 0, 0);
        layout.SetColumnSpan(_urlBox, 5);
        layout.Controls.Add(_urlBox, 1, 0);

        layout.Controls.Add(CreateRowLabel("Site Type"), 0, 1);
        layout.Controls.Add(_siteTypeBox, 1, 1);
        layout.Controls.Add(CreateRowLabel("Selector Type"), 2, 1);
        layout.Controls.Add(_selectorTypeBox, 3, 1);
        layout.Controls.Add(CreateRowLabel("Field Name"), 4, 1);
        layout.Controls.Add(_fieldNameBox, 5, 1);

        layout.Controls.Add(CreateRowLabel("Selector"), 0, 2);
        layout.SetColumnSpan(_selectorBox, 5);
        layout.Controls.Add(_selectorBox, 1, 2);

        layout.Controls.Add(CreateRowLabel("Webhook"), 0, 3);
        layout.SetColumnSpan(_webhookBox, 3);
        layout.Controls.Add(_webhookBox, 1, 3);
        layout.Controls.Add(CreateRowLabel("Interval (s)"), 4, 3);
        layout.Controls.Add(_intervalSecondsBox, 5, 3);

        layout.Controls.Add(CreateRowLabel("Payload"), 0, 4);
        layout.SetColumnSpan(_profileSummaryLabel, 5);
        layout.Controls.Add(_profileSummaryLabel, 1, 4);

        // Row 5: action buttons
        _stopScrollButton = CreateButton("Stop Scroll", (_, _) => StopAutoScroll(), 130);
        var actionRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0)
        };
        actionRow.Controls.Add(CreateButton("Open Chrome", (_, _) => OpenChromeWithCdp(), 130));
        actionRow.Controls.Add(CreateButton("Preview", async (_, _) => await PreviewCurrentAsync(), 110, accent: true));
        actionRow.Controls.Add(CreateButton("Transmission Settings", (_, _) => OpenTransmissionSettings(), 170));
        actionRow.Controls.Add(CreateButton("Approve / Login", async (_, _) => await StartApprovalSessionAsync(), 150));
        actionRow.Controls.Add(CreateButton("Resume Approved", async (_, _) => await ResumeApprovedSessionAsync(), 150));
        actionRow.Controls.Add(_stopScrollButton);
        actionRow.Controls.Add(_autoScrollBox);
        actionRow.Controls.Add(CreateButton("Open Picker App", (_, _) => OpenSiblingTool("Scraper.Picker.WinForms.exe"), 150));
        actionRow.Controls.Add(CreateButton("Start Monitor", async (_, _) => await StartMonitoringAsync(), 130));
        actionRow.Controls.Add(CreateButton("Stop Monitor", (_, _) => StopMonitoring(), 130));
        layout.SetColumnSpan(actionRow, 5);
        layout.Controls.Add(actionRow, 1, 5);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildResultCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;
        card.Controls.Add(new Label
        {
            Text = "Latest Extracted Fields",
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

    private void ConfigureOutputGrid()
    {
        _grid.BackgroundColor = Color.FromArgb(15, 23, 42);
        _grid.BorderStyle = BorderStyle.None;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.DefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
        _grid.DefaultCellStyle.ForeColor = Color.Gainsboro;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(16, 185, 129);
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AutomaticScrapeFieldResult.OutputKey),
            HeaderText = "Output Key",
            FillWeight = 18
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AutomaticScrapeFieldResult.SelectorType),
            HeaderText = "Type",
            FillWeight = 9
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AutomaticScrapeFieldResult.ValueMode),
            HeaderText = "Send As",
            FillWeight = 11
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AutomaticScrapeFieldResult.FirstValue),
            HeaderText = "First Value",
            FillWeight = 16
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AutomaticScrapeFieldResult.CombinedValue),
            HeaderText = "Combined Value",
            FillWeight = 24
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AutomaticScrapeFieldResult.ValuesText),
            HeaderText = "All Values",
            FillWeight = 22
        });
    }

    private async Task PreviewCurrentAsync()
    {
        if (_isBusy)
        {
            SetStatus("Please wait for the current task to finish.", _progress.Value);
            return;
        }

        if (!ValidateInputs(requireWebhook: false))
        {
            return;
        }

        try
        {
            _isBusy = true;
            _approvalLabel.Visible = false;
            SetStatus("Loading preview...", 20);
            var payload = await CapturePayloadAsync(CancellationToken.None);
            _bindingSource.DataSource = payload.Fields;
            _bindingSource.ResetBindings(false);
            _insightLabel.Text = payload.Fields.Count == 0
                ? "Preview completed, but no configured fields matched. Adjust the selectors or load the page in JavaScript mode."
                : $"Preview captured {payload.Fields.Count} configured field(s). Start Monitor to transmit updates with the current payload profile.";
            LogActivity($"Preview captured {payload.Fields.Count} configured field(s) for profile '{payload.ProfileName}'.");
            SetStatus("Preview complete.", 100);
        }
        catch (Exception ex)
        {
            SetStatus($"Preview failed: {ex.Message}", 0);
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

        if (string.IsNullOrWhiteSpace(_urlBox.Text))
        {
            SetStatus("Enter a real URL before starting manual approval.", 0);
            return;
        }

        try
        {
            _isBusy = true;
            _siteTypeBox.SelectedItem = "JavaScript";
            SetStatus("Opening live browser for manual approval...", 20);
            await _playwrightScraper.OpenInteractiveSessionAsync(_urlBox.Text.Trim());
            _approvalLabel.Text = "Approval browser opened. Complete login or verification there, then return here and click Resume Approved.";
            _approvalLabel.Visible = true;
            _insightLabel.Text = "Once the real live page is visible in the browser, click Resume Approved and then Start Monitor.";
            SetStatus("Approval browser ready. Finish the page challenge, then click Resume Approved.", 60);
        }
        catch (Exception ex)
        {
            SetStatus($"Approval browser failed: {ex.Message}", 0);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task ResumeApprovedSessionAsync()
    {
        if (!_playwrightScraper.HasInteractiveSession)
        {
            SetStatus("No approval browser is open yet. Click Approve / Login first.", 0);
            return;
        }

        try
        {
            var currentUrl = await _playwrightScraper.GetInteractiveUrlAsync();
            if (!string.IsNullOrWhiteSpace(currentUrl))
            {
                _urlBox.Text = currentUrl;
            }

            _approvalLabel.Text = "Approved session captured. The monitor will now reuse that browser session for live polling in JavaScript mode.";
            _approvalLabel.Visible = true;
            _insightLabel.Text = string.IsNullOrWhiteSpace(_playwrightScraper.LastLoadSummary)
                ? "Approved session is ready. You can preview or start monitoring now."
                : $"Approved session is ready. {_playwrightScraper.LastLoadSummary} You can preview or start monitoring now.";
            SetStatus("Approved session ready for live monitoring.", 100);
        }
        catch (Exception ex)
        {
            SetStatus($"Resume failed: {ex.Message}", 0);
        }
    }

    private async Task StartMonitoringAsync()
    {
        if (_monitorCts is not null)
        {
            SetStatus("Monitoring is already running.", _progress.Value);
            return;
        }

        if (!ValidateInputs(requireWebhook: true))
        {
            return;
        }

        _monitorCts = new CancellationTokenSource();
        _lastContentHash = string.Empty;
        _approvalLabel.Visible = false;
        LogActivity($"Starting live monitor with profile '{_transmissionProfile.ProfileName}'.");
        _insightLabel.Text = "Live monitoring is running. Webhook transmissions follow your transmission settings and only fire when the extracted content changes.";

        try
        {
            await MonitorLoopAsync(_monitorCts.Token);
        }
        finally
        {
            _monitorCts?.Dispose();
            _monitorCts = null;
        }
    }

    private void StopMonitoring()
    {
        if (_monitorCts is null)
        {
            SetStatus("No live monitor is currently running.", _progress.Value);
            return;
        }

        _monitorCts.Cancel();
        LogActivity("Stopping live monitor.");
        SetStatus("Stopping live monitor...", _progress.Value);
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds((double)_intervalSecondsBox.Value);
        var cycle = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                cycle++;
                SetStatus($"Polling cycle {cycle}...", 25);
                AutomaticScrapePayload payload;
                try
                {
                    payload = await CapturePayloadAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    LogActivity($"Polling cycle {cycle} failed: {ex.Message}");
                    SetStatus($"Polling cycle {cycle} failed: {ex.Message}", 0);
                    await Task.Delay(interval, cancellationToken);
                    continue;
                }

                _bindingSource.DataSource = payload.Fields;
                _bindingSource.ResetBindings(false);

                if (!string.Equals(payload.ContentHash, _lastContentHash, StringComparison.Ordinal))
                {
                    var outboundPayload = BuildOutboundPayload(payload);
                    await _webhookDispatch.DispatchJsonAsync(_webhookBox.Text.Trim(), outboundPayload, cancellationToken);
                    _lastContentHash = payload.ContentHash;
                    LogActivity($"Cycle {cycle}: change detected and transmitted using profile '{payload.ProfileName}' ({payload.Fields.Count} field(s)).");
                    SetStatus($"Cycle {cycle}: change detected and transmitted.", 100);
                }
                else
                {
                    LogActivity($"Cycle {cycle}: no change detected.");
                    SetStatus($"Cycle {cycle}: no change detected.", 100);
                }

                await Task.Delay(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("Live monitor stopped.", _progress.Value);
        }
    }

    private async Task<AutomaticScrapePayload> CapturePayloadAsync(CancellationToken cancellationToken)
    {
        var html = await FetchHtmlAsync(cancellationToken);
        var fieldDefinitions = GetEffectiveFieldDefinitions();
        var fieldResults = new List<AutomaticScrapeFieldResult>();
        var transmittedData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fieldDefinitions)
        {
            var selectorKind = string.Equals(field.SelectorType, "XPath", StringComparison.OrdinalIgnoreCase)
                ? SelectorKind.XPath
                : SelectorKind.Css;
            var values = _selectorHelper.ExtractValues(html, field.Selector.Trim(), selectorKind);
            var combinedValue = string.Join(" | ", values);
            var firstValue = values.FirstOrDefault() ?? string.Empty;
            fieldResults.Add(new AutomaticScrapeFieldResult
            {
                OutputKey = field.OutputKey,
                Selector = field.Selector,
                SelectorType = field.SelectorType,
                ValueMode = field.ValueMode,
                Values = values,
                FirstValue = firstValue,
                CombinedValue = combinedValue
            });
            transmittedData[field.OutputKey] = ResolveOutputValue(field.ValueMode, values, firstValue, combinedValue);
        }

        var dataJson = JsonSerializer.Serialize(transmittedData.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataJson))).ToLowerInvariant();
        var firstField = fieldResults.FirstOrDefault();

        return new AutomaticScrapePayload
        {
            SourceUrl = _urlBox.Text.Trim(),
            Selector = firstField?.Selector ?? _selectorBox.Text.Trim(),
            SelectorType = firstField?.SelectorType ?? (_selectorTypeBox.SelectedItem?.ToString() ?? "CSS"),
            FieldName = firstField?.OutputKey ?? (string.IsNullOrWhiteSpace(_fieldNameBox.Text) ? "Value" : _fieldNameBox.Text.Trim()),
            ProfileName = string.IsNullOrWhiteSpace(_transmissionProfile.ProfileName) ? "Default" : _transmissionProfile.ProfileName.Trim(),
            CapturedAtUtc = DateTime.UtcNow,
            Values = firstField?.Values ?? [],
            CombinedValue = string.Join(" || ", fieldResults.Select(field => $"{field.OutputKey}={field.CombinedValue}")),
            ContentHash = hash,
            Fields = fieldResults,
            TransmittedData = transmittedData
        };
    }

    private async Task<string> FetchHtmlAsync(CancellationToken cancellationToken)
    {
        var url = _urlBox.Text.Trim();
        var siteType = _siteTypeBox.SelectedItem?.ToString() ?? "Static";
        _playwrightScraper.AutoScroll = _autoScrollBox.Checked;
        if (siteType == "JavaScript")
        {
            if (_playwrightScraper.HasInteractiveSession)
            {
                return await _playwrightScraper.NavigateInteractiveAndGetHtmlAsync(url, cancellationToken);
            }

            return await _playwrightScraper.GetHtmlAsync(url, cancellationToken);
        }

        return await _httpScraper.GetHtmlAsync(url, cancellationToken);
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
        // In the Automatic Scraper, stopping the scroll stops the whole monitor cycle
        StopMonitoring();
    }

    private bool ValidateInputs(bool requireWebhook)
    {
        if (string.IsNullOrWhiteSpace(_urlBox.Text))
        {
            SetStatus("Enter a URL first.", 0);
            return false;
        }

        var fieldDefinitions = GetEffectiveFieldDefinitions();
        if (fieldDefinitions.Count == 0)
        {
            SetStatus("Add at least one selector in Transmission Settings or the quick selector boxes.", 0);
            return false;
        }

        if (requireWebhook && string.IsNullOrWhiteSpace(_webhookBox.Text))
        {
            SetStatus("Enter a webhook URL first.", 0);
            return false;
        }

        return true;
    }

    private List<AutomaticTransmissionFieldDefinition> GetEffectiveFieldDefinitions()
    {
        var configuredFields = _transmissionProfile.Fields
            .Where(field => !string.IsNullOrWhiteSpace(field.OutputKey) && !string.IsNullOrWhiteSpace(field.Selector))
            .Select(CloneFieldDefinition)
            .ToList();
        if (configuredFields.Count > 0)
        {
            return configuredFields;
        }

        if (string.IsNullOrWhiteSpace(_selectorBox.Text))
        {
            return [];
        }

        return
        [
            new AutomaticTransmissionFieldDefinition
            {
                OutputKey = string.IsNullOrWhiteSpace(_fieldNameBox.Text) ? "value" : _fieldNameBox.Text.Trim(),
                Selector = _selectorBox.Text.Trim(),
                SelectorType = _selectorTypeBox.SelectedItem?.ToString() ?? "CSS",
                ValueMode = "Combined"
            }
        ];
    }

    private Dictionary<string, object?> BuildOutboundPayload(AutomaticScrapePayload payload)
    {
        var outbound = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (_transmissionProfile.IncludeProfileName)
        {
            outbound["profileName"] = payload.ProfileName;
        }

        if (_transmissionProfile.IncludeSourceUrl)
        {
            outbound["sourceUrl"] = payload.SourceUrl;
        }

        if (_transmissionProfile.IncludeCapturedAtUtc)
        {
            outbound["capturedAtUtc"] = payload.CapturedAtUtc;
        }

        if (_transmissionProfile.IncludeContentHash)
        {
            outbound["contentHash"] = payload.ContentHash;
        }

        if (_transmissionProfile.NestFieldsUnderDataRoot)
        {
            outbound[SanitizeDataRootKey(_transmissionProfile.DataRootKey)] = payload.TransmittedData;
        }
        else
        {
            foreach (var pair in payload.TransmittedData)
            {
                outbound[pair.Key] = pair.Value;
            }
        }

        if (_transmissionProfile.IncludeFieldMetadata)
        {
            outbound["fields"] = payload.Fields.Select(field => new
            {
                outputKey = field.OutputKey,
                selector = field.Selector,
                selectorType = field.SelectorType,
                valueMode = field.ValueMode,
                firstValue = field.FirstValue,
                combinedValue = field.CombinedValue,
                values = field.Values
            }).ToList();
        }

        return outbound;
    }

    private void OpenTransmissionSettings()
    {
        using var dialog = new TransmissionSettingsForm(_transmissionProfile, BuildCurrentSelectorFieldDefinition());
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _transmissionProfile = dialog.Profile;
        RefreshProfileSummary();
        LogActivity($"Transmission settings updated to profile '{_transmissionProfile.ProfileName}'.");
        SetStatus("Transmission settings updated.", 100);
    }

    private void RefreshProfileSummary()
    {
        var effectiveFields = GetEffectiveFieldDefinitions();
        var rootMode = _transmissionProfile.NestFieldsUnderDataRoot
            ? $"nested under '{SanitizeDataRootKey(_transmissionProfile.DataRootKey)}'"
            : "sent as flat root keys";
        _profileSummaryLabel.Text = $"Transmission settings: profile '{_transmissionProfile.ProfileName}' with {effectiveFields.Count} field(s), {rootMode}.";
    }

    private AutomaticTransmissionFieldDefinition BuildCurrentSelectorFieldDefinition() =>
        new()
        {
            OutputKey = string.IsNullOrWhiteSpace(_fieldNameBox.Text) ? "value" : _fieldNameBox.Text.Trim(),
            Selector = _selectorBox.Text.Trim(),
            SelectorType = _selectorTypeBox.SelectedItem?.ToString() ?? "CSS",
            ValueMode = "Combined"
        };

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
            RefreshProfileSummary();
            LogActivity($"Imported selector from Element Picker: {_selectorBox.Text}");
            SetStatus($"Imported selector from Element Picker ({_selectorTypeBox.SelectedItem}).", 100);
        }
        catch
        {
            // Ignore transient file contention and retry later.
        }
    }

    private void OpenSiblingTool(string executableName)
    {
        var directCandidate = Path.Combine(AppContext.BaseDirectory, executableName);
        if (File.Exists(directCandidate))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(directCandidate) { UseShellExecute = true });
            return;
        }

        MessageBox.Show(this, $"Build and place {executableName} beside this app to launch it directly.", "Tool Not Available");
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

    private static object? ResolveOutputValue(string valueMode, List<string> values, string firstValue, string combinedValue) =>
        valueMode switch
        {
            "First" => firstValue,
            "All" => values,
            _ => combinedValue
        };

    private static AutomaticTransmissionProfile CreateDefaultTransmissionProfile() =>
        new()
        {
            ProfileName = "Default",
            IncludeProfileName = true,
            IncludeSourceUrl = true,
            IncludeCapturedAtUtc = true,
            IncludeContentHash = true,
            IncludeFieldMetadata = false,
            NestFieldsUnderDataRoot = true,
            DataRootKey = "data"
        };

    private static string SanitizeDataRootKey(string value) =>
        string.IsNullOrWhiteSpace(value) ? "data" : value.Trim();

    private static AutomaticTransmissionFieldDefinition CloneFieldDefinition(AutomaticTransmissionFieldDefinition field) =>
        new()
        {
            OutputKey = field.OutputKey,
            Selector = field.Selector,
            SelectorType = field.SelectorType,
            ValueMode = field.ValueMode
        };

    private sealed class SelectorTransferMessage
    {
        public string Selector { get; set; } = string.Empty;
        public string SelectorType { get; set; } = "CSS";
        public DateTime SentAtUtc { get; set; }
    }
}
