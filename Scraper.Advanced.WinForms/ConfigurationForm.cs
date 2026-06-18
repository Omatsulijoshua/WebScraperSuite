using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Scraper.Core.Models;
using Scraper.Core.Services;

namespace Scraper.Advanced.WinForms;

public class ConfigurationForm : Form
{
    private readonly ComboBox _providerBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _modelBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox _apiKeyBox = new() { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
    private readonly TextBox _endpointBox = new() { PlaceholderText = "https://api.openai.com/v1/chat/completions", Dock = DockStyle.Fill };
    private readonly TextBox _folderBox = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _browserBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly CheckBox _autopilotBox = new() { Text = "Enable AI Autopilot Mode (extract on load)", ForeColor = Color.White, AutoSize = true };
    private readonly CheckBox _enableAutoScrollBox = new() { Text = "Enable Auto-Scroll", ForeColor = Color.White, AutoSize = true, Checked = true };
    private readonly CheckBox _saveCsvBox = new() { Text = "Auto-save CSV", ForeColor = Color.Gainsboro, AutoSize = true, Checked = true };
    private readonly CheckBox _saveJsonBox = new() { Text = "Auto-save JSON", ForeColor = Color.Gainsboro, AutoSize = true, Checked = true };
    private readonly CheckBox _dlImagesBox = new() { Text = "Auto-download images", ForeColor = Color.Gainsboro, AutoSize = true, Checked = true };
    private readonly CheckBox _dlAudioBox = new() { Text = "Auto-download audio (MP3)", ForeColor = Color.Gainsboro, AutoSize = true, Checked = true };
    private readonly CheckBox _dlVideosBox = new() { Text = "Auto-download videos", ForeColor = Color.Gainsboro, AutoSize = true, Checked = true };
    private readonly NumericUpDown _scrollLoopsBox = new() { Minimum = 0, Maximum = 100, Value = 5, Dock = DockStyle.Fill };
    private readonly NumericUpDown _apiTimeoutBox = new() { Minimum = 5, Maximum = 1800, Value = 100, Dock = DockStyle.Fill };
    private readonly Label _statusLabel = new() { Text = "Ready to configure scraper settings.", ForeColor = Color.Silver, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _testButton = new() { Text = "Test Key", Width = 110, Height = 36, FlatStyle = FlatStyle.Flat };
    private readonly Button _saveButton = new() { Text = "Save Settings", Width = 130, Height = 36, FlatStyle = FlatStyle.Flat };
    private readonly Button _showHideButton = new() { Text = "Show", Width = 64, Height = 28, FlatStyle = FlatStyle.Flat };

    private Label _endpointLabel = null!;
    private TableLayoutPanel _inputsLayout = null!;

    public ConfigurationForm()
    {
        Text = "WebScraper Pro - Configuration";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        
        // Compact height of 694 to fit extra row without clipping
        ClientSize = new Size(620, 694);
        BackColor = Color.FromArgb(8, 15, 32);
        Font = new Font("Segoe UI", 10F);
        Padding = new Padding(20);

        // Build controls
        var header = BuildHeader();
        header.Dock = DockStyle.Top;
        header.Height = 70;

        var buttons = BuildButtonsRow();
        buttons.Dock = DockStyle.Bottom;
        buttons.Height = 44;

        var card = BuildInputsCard();
        card.Dock = DockStyle.Fill;
        
        var cardContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 10, 0, 10)
        };
        cardContainer.Controls.Add(card);

        // Add controls (Z-order: DockStyle.Fill container must be added first/controls added in Z-order stack)
        Controls.Add(cardContainer);
        Controls.Add(buttons);
        Controls.Add(header);

        _providerBox.Items.AddRange(["Gemini", "OpenAI", "NVIDIA", "Custom / Other"]);
        _providerBox.SelectedIndexChanged += (_, _) => OnProviderChanged();

        _browserBox.Items.AddRange(["Chrome", "Edge", "Firefox", "WebKit", "Chromium", "Opera", "Brave", "Vivaldi", "Running Chrome (CDP 9222)"]);
        
        _showHideButton.FlatAppearance.BorderSize = 0;
        _showHideButton.BackColor = Color.FromArgb(30, 41, 59);
        _showHideButton.ForeColor = Color.White;
        _showHideButton.Click += TogglePasswordMask;

        _testButton.FlatAppearance.BorderSize = 0;
        _testButton.BackColor = Color.FromArgb(139, 92, 246); // violet
        _testButton.ForeColor = Color.White;
        _testButton.Click += async (s, e) => await TestConnectionAsync();

        _saveButton.FlatAppearance.BorderSize = 0;
        _saveButton.BackColor = Color.FromArgb(16, 185, 129); // emerald
        _saveButton.ForeColor = Color.White;
        _saveButton.Click += (s, e) => SaveSettings();

        LoadSettings();
    }

    private Control BuildHeader()
    {
        var panel = new Panel { Height = 70 };
        
        var title = new Label
        {
            Text = "⚙ Scraper Configuration",
            Dock = DockStyle.Top,
            Height = 32,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            ForeColor = Color.White
        };
        
        var desc = new Label
        {
            Text = "Configure preferred browser, LLM keys, and smart autopilot extraction options.",
            Dock = DockStyle.Fill,
            ForeColor = Color.Silver,
            Font = new Font("Segoe UI", 9F)
        };
        
        panel.Controls.Add(desc);
        panel.Controls.Add(title);
        return panel;
    }

    private Control BuildInputsCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(16),
            AutoScroll = true
        };

        _inputsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 12
        };
        _inputsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        _inputsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _inputsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Provider (Row 0)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));  // Custom Endpoint (Row 1) - starts hidden (0 height)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Model (Row 2)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // API Key (Row 3)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Default Folder (Row 4)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Preferred Browser (Row 5)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Autopilot mode (Row 6)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Auto-Scroll checkbox (Row 7)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Max Scroll Loops (Row 8)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // API Timeout (Row 9)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // Options Checkboxes (Row 10)
        _inputsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // Padding (Row 11)

        // Provider Row
        _inputsLayout.Controls.Add(CreateLabel("API Provider"), 0, 0);
        _inputsLayout.SetColumnSpan(_providerBox, 2);
        _inputsLayout.Controls.Add(_providerBox, 1, 0);

        // Custom Endpoint Row
        _endpointLabel = CreateLabel("API Endpoint");
        _endpointLabel.Visible = false;
        _endpointBox.Visible = false;
        _inputsLayout.Controls.Add(_endpointLabel, 0, 1);
        _inputsLayout.SetColumnSpan(_endpointBox, 2);
        _inputsLayout.Controls.Add(_endpointBox, 1, 1);

        // Model Row
        _inputsLayout.Controls.Add(CreateLabel("Model Name"), 0, 2);
        _inputsLayout.SetColumnSpan(_modelBox, 2);
        _inputsLayout.Controls.Add(_modelBox, 1, 2);

        // Key Row
        _inputsLayout.Controls.Add(CreateLabel("API Key"), 0, 3);
        _inputsLayout.Controls.Add(_apiKeyBox, 1, 3);
        
        var showBtnContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        _showHideButton.Dock = DockStyle.Top;
        showBtnContainer.Controls.Add(_showHideButton);
        _inputsLayout.Controls.Add(showBtnContainer, 2, 3);

        // Folder Row
        _inputsLayout.Controls.Add(CreateLabel("Default Folder"), 0, 4);
        _inputsLayout.Controls.Add(_folderBox, 1, 4);
        
        var browseButton = new Button
        {
            Text = "Browse",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Dock = DockStyle.Top
        };
        browseButton.FlatAppearance.BorderSize = 0;
        browseButton.Click += (s, e) => BrowseOutputFolder();
        var browseBtnContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        browseButton.Height = 28;
        browseBtnContainer.Controls.Add(browseButton);
        _inputsLayout.Controls.Add(browseBtnContainer, 2, 4);

        // Preferred Browser Row
        _inputsLayout.Controls.Add(CreateLabel("Browser Choice"), 0, 5);
        _inputsLayout.SetColumnSpan(_browserBox, 2);
        _inputsLayout.Controls.Add(_browserBox, 1, 5);

        // Autopilot Row
        _inputsLayout.Controls.Add(CreateLabel("Smart Autopilot"), 0, 6);
        _inputsLayout.SetColumnSpan(_autopilotBox, 2);
        _inputsLayout.Controls.Add(_autopilotBox, 1, 6);

        // Auto-Scroll Row
        _inputsLayout.Controls.Add(CreateLabel("Auto-Scroll"), 0, 7);
        _inputsLayout.SetColumnSpan(_enableAutoScrollBox, 2);
        _inputsLayout.Controls.Add(_enableAutoScrollBox, 1, 7);

        // Max Scroll Loops Row
        _inputsLayout.Controls.Add(CreateLabel("Max Scroll Loops"), 0, 8);
        _inputsLayout.SetColumnSpan(_scrollLoopsBox, 2);
        _inputsLayout.Controls.Add(_scrollLoopsBox, 1, 8);

        // API Timeout Row
        _inputsLayout.Controls.Add(CreateLabel("API Timeout (sec)"), 0, 9);
        _inputsLayout.SetColumnSpan(_apiTimeoutBox, 2);
        _inputsLayout.Controls.Add(_apiTimeoutBox, 1, 9);

        // Checkbox items Panel
        var checkContainer = new Panel { Dock = DockStyle.Fill };
        
        var titleOpts = new Label
        {
            Text = "Autopilot Automation Targets:",
            ForeColor = Color.FromArgb(125, 211, 252),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 24
        };
        
        var checkLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0)
        };
        checkLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        checkLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        checkLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        checkLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        checkLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        checkLayout.Controls.Add(_saveCsvBox, 0, 0);
        checkLayout.Controls.Add(_saveJsonBox, 0, 1);
        checkLayout.Controls.Add(_dlImagesBox, 1, 0);
        checkLayout.Controls.Add(_dlAudioBox, 1, 1);
        checkLayout.Controls.Add(_dlVideosBox, 1, 2);

        checkContainer.Controls.Add(checkLayout);
        checkContainer.Controls.Add(titleOpts);
        
        _inputsLayout.SetColumnSpan(checkContainer, 2);
        _inputsLayout.Controls.Add(checkContainer, 1, 10);

        card.Controls.Add(_inputsLayout);
        return card;
    }

    private Control BuildButtonsRow()
    {
        var panel = new Panel
        {
            Height = 44
        };

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.ForeColor = Color.Silver;

        _saveButton.Dock = DockStyle.Right;
        _saveButton.Width = 130;
        var saveContainer = new Panel { Dock = DockStyle.Right, Width = 140, Padding = new Padding(10, 4, 0, 4) };
        saveContainer.Controls.Add(_saveButton);

        _testButton.Dock = DockStyle.Right;
        _testButton.Width = 110;
        var testContainer = new Panel { Dock = DockStyle.Right, Width = 120, Padding = new Padding(10, 4, 0, 4) };
        testContainer.Controls.Add(_testButton);

        panel.Controls.Add(saveContainer);
        panel.Controls.Add(testContainer);
        panel.Controls.Add(_statusLabel);

        return panel;
    }

    private Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.Gainsboro,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
        };
    }

    private void PopulateModels()
    {
        _modelBox.Items.Clear();
        var provider = _providerBox.SelectedItem?.ToString() ?? "Gemini";
        if (provider == "Gemini")
        {
            _modelBox.Items.AddRange(["gemini-1.5-flash", "gemini-1.5-pro", "gemini-2.5-flash", "gemini-2.5-pro"]);
            _modelBox.SelectedIndex = 0;
        }
        else if (provider == "OpenAI")
        {
            _modelBox.Items.AddRange(["gpt-4o-mini", "gpt-4o"]);
            _modelBox.SelectedIndex = 0;
        }
        else if (provider == "NVIDIA")
        {
            _modelBox.Items.AddRange([
                "nvidia/llama-3.1-nemotron-70b-instruct",
                "meta/llama-3.1-70b-instruct",
                "meta/llama-3.1-8b-instruct",
                "mistralai/mixtral-8x22b-instruct-v0.1"
            ]);
            _modelBox.SelectedIndex = 0;
        }
    }

    private void OnProviderChanged()
    {
        var provider = _providerBox.SelectedItem?.ToString() ?? "Gemini";
        var isCustom = provider == "Custom / Other";

        _endpointLabel.Visible = isCustom;
        _endpointBox.Visible = isCustom;
        
        if (_inputsLayout != null)
        {
            _inputsLayout.RowStyles[1].Height = isCustom ? 38 : 0;
        }

        if (isCustom)
        {
            _modelBox.DropDownStyle = ComboBoxStyle.DropDown;
            _modelBox.Text = "deepseek-chat";
            if (string.IsNullOrWhiteSpace(_endpointBox.Text))
            {
                _endpointBox.Text = "https://api.deepseek.com/v1/chat/completions";
            }
        }
        else
        {
            _modelBox.DropDownStyle = ComboBoxStyle.DropDownList;
            PopulateModels();
        }
    }

    private void TogglePasswordMask(object? sender, EventArgs e)
    {
        _apiKeyBox.UseSystemPasswordChar = !_apiKeyBox.UseSystemPasswordChar;
        _showHideButton.Text = _apiKeyBox.UseSystemPasswordChar ? "Show" : "Hide";
    }

    private void BrowseOutputFolder()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folderBox.Text = dialog.SelectedPath;
        }
    }

    private void LoadSettings()
    {
        var s = SettingsService.Load();
        
        var provIdx = _providerBox.Items.IndexOf(s.ApiProvider);
        _providerBox.SelectedIndex = provIdx >= 0 ? provIdx : 0;
        
        OnProviderChanged();
        
        if (s.ApiProvider == "Custom / Other")
        {
            _modelBox.Text = s.ApiModel;
            _endpointBox.Text = s.CustomEndpoint;
        }
        else
        {
            var modelIdx = _modelBox.Items.IndexOf(s.ApiModel);
            if (modelIdx >= 0) _modelBox.SelectedIndex = modelIdx;
        }
        
        _apiKeyBox.Text = s.ApiKey;
        _folderBox.Text = s.OutputFolder;
        _autopilotBox.Checked = s.EnableAutopilot;
        
        _saveCsvBox.Checked = s.AutoSaveCsv;
        _saveJsonBox.Checked = s.AutoSaveJson;
        _dlImagesBox.Checked = s.AutoDownloadImages;
        _dlAudioBox.Checked = s.AutoDownloadAudio;
        _dlVideosBox.Checked = s.AutoDownloadVideos;
        _enableAutoScrollBox.Checked = s.EnableAutoScroll;
        _scrollLoopsBox.Value = Math.Max(_scrollLoopsBox.Minimum, Math.Min(_scrollLoopsBox.Maximum, s.MaxScrollLoops));
        _apiTimeoutBox.Value = Math.Max(_apiTimeoutBox.Minimum, Math.Min(_apiTimeoutBox.Maximum, s.ApiTimeoutSeconds));

        var browserIdx = _browserBox.Items.IndexOf(s.PreferredBrowser ?? "Chrome");
        _browserBox.SelectedIndex = browserIdx >= 0 ? browserIdx : 0;
    }

    private void SaveSettings()
    {
        var provider = _providerBox.SelectedItem?.ToString() ?? "Gemini";
        var isCustom = provider == "Custom / Other";

        var s = new ScraperSettings
        {
            ApiProvider = provider,
            ApiModel = isCustom ? _modelBox.Text.Trim() : (_modelBox.SelectedItem?.ToString() ?? ""),
            CustomEndpoint = isCustom ? _endpointBox.Text.Trim() : "",
            ApiKey = _apiKeyBox.Text.Trim(),
            OutputFolder = _folderBox.Text.Trim(),
            EnableAutopilot = _autopilotBox.Checked,
            AutoSaveCsv = _saveCsvBox.Checked,
            AutoSaveJson = _saveJsonBox.Checked,
            AutoDownloadImages = _dlImagesBox.Checked,
            AutoDownloadAudio = _dlAudioBox.Checked,
            AutoDownloadVideos = _dlVideosBox.Checked,
            EnableAutoScroll = _enableAutoScrollBox.Checked,
            MaxScrollLoops = (int)_scrollLoopsBox.Value,
            PreferredBrowser = _browserBox.SelectedItem?.ToString() ?? "Chrome",
            ApiTimeoutSeconds = (int)_apiTimeoutBox.Value
        };

        try
        {
            SettingsService.Save(s);
            _statusLabel.ForeColor = Color.LightGreen;
            _statusLabel.Text = "Settings saved successfully!";
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = $"Save error: {ex.Message}";
        }
    }

    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiKeyBox.Text))
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = "API Key cannot be empty for testing.";
            return;
        }

        _testButton.Enabled = false;
        _statusLabel.ForeColor = Color.Yellow;
        _statusLabel.Text = "Testing API key connection...";

        var provider = _providerBox.SelectedItem?.ToString() ?? "Gemini";
        var isCustom = provider == "Custom / Other";

        var tempSettings = new ScraperSettings
        {
            ApiProvider = provider,
            ApiModel = isCustom ? _modelBox.Text.Trim() : (_modelBox.SelectedItem?.ToString() ?? ""),
            CustomEndpoint = isCustom ? _endpointBox.Text.Trim() : "",
            ApiKey = _apiKeyBox.Text.Trim(),
            ApiTimeoutSeconds = (int)_apiTimeoutBox.Value
        };

        try
        {
            var testService = new AiService();
            var result = await testService.ExtractStructuredDataAsync("<html><body><p>Hello World</p></body></html>", "http://test.com", tempSettings);
            _statusLabel.ForeColor = Color.LightGreen;
            _statusLabel.Text = "API Connection test successful!";
        }
        catch (Exception ex)
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = $"Connection test failed: {ex.Message}";
        }
        finally
        {
            _testButton.Enabled = true;
        }
    }
}
