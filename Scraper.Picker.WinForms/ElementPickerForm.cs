using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Scraper.Picker.WinForms;

public class ElementPickerForm : Form
{
    private static readonly string SelectorTransferPath = Path.Combine(Path.GetTempPath(), "WebScraperPro", "selector-transfer.json");
    private static readonly JsonSerializerOptions MessageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TextBox _urlBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _selectorBox = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly ComboBox _modeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly WebView2 _browser = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, ForeColor = Color.Gainsboro };
    private readonly Label _inspectionLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 54,
        ForeColor = Color.Silver,
        Text = "Open the page first, interact with it normally if it asks for login or a verification step, then enable the picker only when you are ready to capture selectors."
    };
    private readonly Label _approvalLabel = new()
    {
        Dock = DockStyle.Top,
        Height = 42,
        ForeColor = Color.FromArgb(187, 247, 208),
        Text = "This window is for browsing and selector picking only. No scraping or exporting happens here."
    };
    private readonly ListBox _historyList = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(15, 23, 42),
        ForeColor = Color.Gainsboro,
        BorderStyle = BorderStyle.None
    };

    private SelectorPayload? _lastPayload;
    private bool _pickerEnabled;

    public ElementPickerForm()
    {
        Text = "WebScraper Pro - Element Picker";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1240, 820);
        Size = new Size(1380, 900);
        BackColor = Color.FromArgb(15, 23, 42);
        Font = new Font("Segoe UI", 10F);

        _modeBox.Items.AddRange(["CSS", "XPath"]);
        _modeBox.SelectedIndex = 0;
        _modeBox.SelectedIndexChanged += (_, _) => ApplySelectedMode();
        _historyList.DoubleClick += (_, _) => LoadHistorySelection();

        Controls.Add(BuildLayout());
        Shown += async (_, _) => await InitializeBrowserAsync();
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
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildTopCard(), 0, 0);
        root.Controls.Add(BuildBrowserCard(), 0, 1);
        root.Controls.Add(BuildBottomCard(), 0, 2);
        return root;
    }

    private Control BuildTopCard()
    {
        var card = CreateCard();
        card.Height = 190;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 460));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;

        var title = CreateSectionTitle("Element Picker");
        layout.SetColumnSpan(title, 3);
        layout.Controls.Add(title, 0, 0);

        var description = CreateSectionDescription("Use this like a lightweight DevTools picker: browse normally, pass any manual verification in the page itself, then enable the picker to capture CSS or XPath selectors.");
        layout.SetColumnSpan(description, 3);
        layout.Controls.Add(description, 0, 1);

        layout.Controls.Add(CreateRowLabel("URL"), 0, 2);
        layout.Controls.Add(_urlBox, 1, 2);
        layout.Controls.Add(CreateButtonRow(
            ("Open Browser", async (_, _) => await OpenPageAsync(), 130, true),
            ("Enable Picker", async (_, _) => await SetPickerStateAsync(true), 130, false),
            ("Pause Picker", async (_, _) => await SetPickerStateAsync(false), 130, false)
        ), 2, 2);

        var help = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.Silver,
            Text = "When a site shows login, Cloudflare, or CAPTCHA, leave the picker paused, complete the page step yourself, then click Enable Picker."
        };

        layout.Controls.Add(help, 1, 3);
        layout.Controls.Add(CreateButtonRow(
            ("Refresh Page", async (_, _) => await RefreshPageAsync(), 120, false),
            ("Clear History", (_, _) => ClearHistory(), 120, false),
            ("Copy Current", (_, _) => CopySelectorToClipboard(showConfirmation: false), 120, false)
        ), 2, 3);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildBrowserCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;
        card.Controls.Add(new Label
        {
            Text = "Live Browser Window",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
        });
        card.Controls.Add(_browser);
        return card;
    }

    private Control BuildBottomCard()
    {
        var card = CreateCard();
        card.Height = 240;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var selectorPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        selectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154));
        selectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        selectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        selectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));

        selectorPanel.Controls.Add(CreateRowLabel("Selected Selector"), 0, 0);
        selectorPanel.Controls.Add(_selectorBox, 1, 0);
        selectorPanel.Controls.Add(CreateRowLabel("Mode"), 2, 0);
        selectorPanel.Controls.Add(_modeBox, 3, 0);

        layout.Controls.Add(selectorPanel, 0, 0);
        layout.Controls.Add(CreateSideTitle("Capture History"), 1, 0);

        layout.Controls.Add(_approvalLabel, 0, 1);
        layout.Controls.Add(_inspectionLabel, 1, 1);

        var actionRow = CreateButtonRow(
            ("Copy", (_, _) => CopySelectorToClipboard(showConfirmation: false), 110, false),
            ("Use In Scraper", (_, _) => UseInScraper(), 140, true),
            ("Clear Current", (_, _) => ClearCurrentSelection(), 120, false)
        );
        layout.Controls.Add(actionRow, 0, 2);

        layout.SetRowSpan(_historyList, 2);
        layout.Controls.Add(_historyList, 1, 2);

        layout.SetColumnSpan(_status, 2);
        layout.Controls.Add(_status, 0, 4);

        card.Controls.Add(layout);
        return card;
    }

    private async Task InitializeBrowserAsync()
    {
        if (_browser.CoreWebView2 is not null)
        {
            return;
        }

        await _browser.EnsureCoreWebView2Async();
        var coreWebView = _browser.CoreWebView2!;
        coreWebView.WebMessageReceived += BrowserOnWebMessageReceived;
        coreWebView.Settings.AreDefaultContextMenusEnabled = true;
        coreWebView.Settings.AreDevToolsEnabled = true;
        _browser.NavigationCompleted += async (_, _) => await InstallPickerScriptAsync();
        SetStatus("Browser ready. Open a page, then enable the picker when you are ready.");
    }

    private async Task OpenPageAsync()
    {
        if (string.IsNullOrWhiteSpace(_urlBox.Text))
        {
            SetStatus("Enter a URL first.");
            return;
        }

        await InitializeBrowserAsync();
        _pickerEnabled = false;
        _browser.CoreWebView2.Navigate(_urlBox.Text.Trim());
        _inspectionLabel.Text = "Page opened. Interact with it normally first if you need to sign in or pass a site verification check, then enable the picker.";
        SetStatus("Opening browser...");
    }

    private async Task RefreshPageAsync()
    {
        if (_browser.CoreWebView2 is null)
        {
            return;
        }

        _pickerEnabled = false;
        _browser.CoreWebView2.Reload();
        SetStatus("Refreshing page...");
        await Task.CompletedTask;
    }

    private async Task InstallPickerScriptAsync()
    {
        if (_browser.CoreWebView2 is null)
        {
            return;
        }

        await _browser.CoreWebView2.ExecuteScriptAsync("""
            (() => {
              if (!window.__webScraperPickerInstalled) {
                window.__webScraperPickerInstalled = true;
                window.__webScraperPickerEnabled = false;

                const overlay = document.createElement('div');
                overlay.id = '__webScraperPickerOverlay';
                overlay.style.position = 'fixed';
                overlay.style.pointerEvents = 'none';
                overlay.style.zIndex = '2147483647';
                overlay.style.border = '2px solid #22c55e';
                overlay.style.background = 'rgba(34,197,94,0.12)';
                overlay.style.display = 'none';
                document.documentElement.appendChild(overlay);

                const cssPath = (el) => {
                  if (!(el instanceof Element)) return '';
                  const path = [];
                  while (el && el.nodeType === Node.ELEMENT_NODE && el.tagName.toLowerCase() !== 'html') {
                    let selector = el.tagName.toLowerCase();
                    if (el.id) {
                      selector += '#' + el.id;
                      path.unshift(selector);
                      break;
                    }
                    const classes = [...el.classList].slice(0, 2).join('.');
                    if (classes) selector += '.' + classes;
                    const siblings = el.parentNode ? [...el.parentNode.children].filter(x => x.tagName === el.tagName) : [];
                    if (siblings.length > 1) selector += `:nth-of-type(${siblings.indexOf(el) + 1})`;
                    path.unshift(selector);
                    el = el.parentElement;
                  }
                  return path.join(' > ');
                };

                const xpath = (el) => {
                  if (!el || el.nodeType !== Node.ELEMENT_NODE) return '';
                  if (el.id) return `//*[@id="${el.id}"]`;
                  const parts = [];
                  while (el && el.nodeType === Node.ELEMENT_NODE) {
                    let index = 1;
                    let sibling = el.previousElementSibling;
                    while (sibling) {
                      if (sibling.tagName === el.tagName) index++;
                      sibling = sibling.previousElementSibling;
                    }
                    parts.unshift(`${el.tagName.toLowerCase()}[${index}]`);
                    el = el.parentElement;
                  }
                  return '/' + parts.join('/');
                };

                document.addEventListener('mousemove', event => {
                  const pickerEnabled = !!window.__webScraperPickerEnabled;
                  const currentOverlay = document.getElementById('__webScraperPickerOverlay');
                  if (!pickerEnabled || !currentOverlay) {
                    if (currentOverlay) currentOverlay.style.display = 'none';
                    document.documentElement.style.cursor = '';
                    return;
                  }

                  document.documentElement.style.cursor = 'crosshair';
                  const rect = event.target.getBoundingClientRect();
                  currentOverlay.style.display = 'block';
                  currentOverlay.style.left = rect.left + 'px';
                  currentOverlay.style.top = rect.top + 'px';
                  currentOverlay.style.width = rect.width + 'px';
                  currentOverlay.style.height = rect.height + 'px';
                }, true);

                document.addEventListener('click', event => {
                  if (!window.__webScraperPickerEnabled) {
                    return;
                  }

                  event.preventDefault();
                  event.stopPropagation();
                  const payload = {
                    css: cssPath(event.target),
                    xpath: xpath(event.target),
                    text: (event.target?.innerText || '').trim().slice(0, 120),
                    tag: event.target?.tagName?.toLowerCase?.() || ''
                  };
                  chrome.webview.postMessage(payload);
                }, true);
              }
            })();
            """);

        await SetPickerStateAsync(false, suppressStatus: true);
        SetStatus("Page loaded. Enable Picker when you are ready to capture selectors.");
    }

    private async Task SetPickerStateAsync(bool enabled, bool suppressStatus = false)
    {
        if (_browser.CoreWebView2 is null)
        {
            return;
        }

        _pickerEnabled = enabled;
        await _browser.CoreWebView2.ExecuteScriptAsync($"window.__webScraperPickerEnabled = {(enabled ? "true" : "false")};");
        await _browser.CoreWebView2.ExecuteScriptAsync("""
            (() => {
              const overlay = document.getElementById('__webScraperPickerOverlay');
              if (!window.__webScraperPickerEnabled && overlay) {
                overlay.style.display = 'none';
                document.documentElement.style.cursor = '';
              }
            })();
            """);

        _inspectionLabel.Text = enabled
            ? "Picker is active. Click any visible page element to capture a selector."
            : "Picker is paused. You can scroll, click, sign in, or complete any site verification normally.";

        if (!suppressStatus)
        {
            SetStatus(enabled
                ? "Picker enabled. Click an element in the page."
                : "Picker paused. Interact with the page normally.");
        }
    }

    private void BrowserOnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        SelectorPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SelectorPayload>(e.WebMessageAsJson, MessageJsonOptions);
        }
        catch (JsonException)
        {
            payload = JsonSerializer.Deserialize<SelectorPayload>(e.TryGetWebMessageAsString(), MessageJsonOptions);
        }

        if (payload is null)
        {
            SetStatus("Picker click was received, but the selector payload could not be read.");
            return;
        }

        _lastPayload = payload;
        ApplySelectedMode();
        AddToHistory(payload);
        SetStatus(string.IsNullOrWhiteSpace(_selectorBox.Text)
            ? "Element click received, but no selector text was generated for that target."
            : $"Selector captured from <{payload.Tag}>.");
    }

    private void ApplySelectedMode()
    {
        if (_lastPayload is null)
        {
            _selectorBox.Text = string.Empty;
            return;
        }

        _selectorBox.Text = _modeBox.SelectedItem?.ToString() == "XPath"
            ? _lastPayload.XPath ?? string.Empty
            : _lastPayload.Css ?? string.Empty;
    }

    private void AddToHistory(SelectorPayload payload)
    {
        var modeValue = _modeBox.SelectedItem?.ToString() == "XPath" ? payload.XPath : payload.Css;
        if (string.IsNullOrWhiteSpace(modeValue))
        {
            return;
        }

        var displayText = string.IsNullOrWhiteSpace(payload.Text)
            ? $"{payload.Tag}: {modeValue}"
            : $"{payload.Tag}: {payload.Text} -> {modeValue}";

        if (_historyList.Items.Contains(displayText))
        {
            return;
        }

        _historyList.Items.Insert(0, displayText);
        while (_historyList.Items.Count > 20)
        {
            _historyList.Items.RemoveAt(_historyList.Items.Count - 1);
        }
    }

    private void LoadHistorySelection()
    {
        if (_historyList.SelectedItem is null)
        {
            return;
        }

        var itemText = _historyList.SelectedItem.ToString() ?? string.Empty;
        var separatorIndex = itemText.LastIndexOf(" -> ", StringComparison.Ordinal);
        if (separatorIndex >= 0)
        {
            _selectorBox.Text = itemText[(separatorIndex + 4)..];
            SetStatus("Loaded selector from history.");
            return;
        }

        var tagSeparator = itemText.IndexOf(": ", StringComparison.Ordinal);
        if (tagSeparator >= 0)
        {
            _selectorBox.Text = itemText[(tagSeparator + 2)..];
            SetStatus("Loaded selector from history.");
        }
    }

    private void ClearHistory()
    {
        _historyList.Items.Clear();
        SetStatus("Capture history cleared.");
    }

    private void ClearCurrentSelection()
    {
        _lastPayload = null;
        _selectorBox.Clear();
        SetStatus("Current selector cleared.");
    }

    private void UseInScraper()
    {
        if (!CopySelectorToClipboard(showConfirmation: false))
        {
            return;
        }

        try
        {
            var selectorText = _selectorBox.Text.Trim();
            var selectorMode = _modeBox.SelectedItem?.ToString() ?? "CSS";
            Directory.CreateDirectory(Path.GetDirectoryName(SelectorTransferPath)!);
            var transfer = new SelectorTransferMessage
            {
                Selector = selectorText,
                SelectorType = selectorMode,
                SentAtUtc = DateTime.UtcNow
            };

            File.WriteAllText(SelectorTransferPath, JsonSerializer.Serialize(transfer));
            SetStatus("Selector sent to Advanced Scraper.");
            MessageBox.Show(this, "Selector sent to Advanced Scraper.", "Selector Ready");
        }
        catch (Exception ex)
        {
            SetStatus($"Selector transfer failed: {ex.Message}");
            MessageBox.Show(this, $"Selector copied to clipboard, but live transfer failed: {ex.Message}", "Transfer Warning");
        }
    }

    private bool CopySelectorToClipboard(bool showConfirmation)
    {
        var selectorText = _selectorBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(selectorText))
        {
            SetStatus("Capture an element first before copying a selector.");
            MessageBox.Show(this, "No selector has been captured yet. Click an element in the browser first.", "Nothing To Copy");
            return false;
        }

        Clipboard.SetText(selectorText);
        SetStatus("Selector copied to clipboard.");
        if (showConfirmation)
        {
            MessageBox.Show(this, "Selector copied to clipboard.", "Copied");
        }

        return true;
    }

    private void SetStatus(string message) => _status.Text = message;

    private static FlowLayoutPanel CreateButtonRow(params (string Text, EventHandler Handler, int Width, bool Accent)[] buttons)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0)
        };

        foreach (var (text, handler, width, accent) in buttons)
        {
            row.Controls.Add(CreateButton(text, handler, width, accent));
        }

        return row;
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
            AutoSize = true
        };

    private static Label CreateSideTitle(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

    private static Label CreateSectionTitle(string text) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 8)
        };

    private static Label CreateSectionDescription(string text) =>
        new()
        {
            Text = text,
            AutoSize = true,
            ForeColor = Color.Silver,
            MaximumSize = new Size(1180, 0),
            Margin = new Padding(0, 0, 0, 10)
        };

    private static Button CreateButton(string text, EventHandler onClick, int width, bool accent = false)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = accent ? Color.FromArgb(34, 197, 94) : Color.FromArgb(51, 65, 85),
            ForeColor = Color.White
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private sealed class SelectorPayload
    {
        [JsonPropertyName("css")]
        public string Css { get; set; } = string.Empty;

        [JsonPropertyName("xpath")]
        public string XPath { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("tag")]
        public string Tag { get; set; } = string.Empty;
    }

    private sealed class SelectorTransferMessage
    {
        public string Selector { get; set; } = string.Empty;
        public string SelectorType { get; set; } = "CSS";
        public DateTime SentAtUtc { get; set; }
    }
}
