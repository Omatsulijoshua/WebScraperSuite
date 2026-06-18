using System.ComponentModel;
using Scraper.Core.Models;

namespace Scraper.Automatic.WinForms;

public sealed class TransmissionSettingsForm : Form
{
    private readonly TextBox _profileNameBox = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _includeProfileNameBox = new() { Text = "Include profile name", ForeColor = Color.Gainsboro, AutoSize = true };
    private readonly CheckBox _includeSourceUrlBox = new() { Text = "Include source URL", ForeColor = Color.Gainsboro, AutoSize = true };
    private readonly CheckBox _includeCapturedAtUtcBox = new() { Text = "Include capture time", ForeColor = Color.Gainsboro, AutoSize = true };
    private readonly CheckBox _includeContentHashBox = new() { Text = "Include content hash", ForeColor = Color.Gainsboro, AutoSize = true };
    private readonly CheckBox _includeFieldMetadataBox = new() { Text = "Include field metadata", ForeColor = Color.Gainsboro, AutoSize = true };
    private readonly CheckBox _nestFieldsUnderDataRootBox = new() { Text = "Nest extracted fields under a data object", ForeColor = Color.Gainsboro, AutoSize = true };
    private readonly TextBox _dataRootKeyBox = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _fieldsGrid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        BackgroundColor = Color.FromArgb(15, 23, 42),
        BorderStyle = BorderStyle.None,
        EnableHeadersVisualStyles = false,
        RowHeadersVisible = false
    };
    private readonly BindingList<AutomaticTransmissionFieldDefinition> _fields = [];

    private readonly AutomaticTransmissionFieldDefinition _currentSelectorField;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public AutomaticTransmissionProfile Profile { get; private set; }

    public TransmissionSettingsForm(AutomaticTransmissionProfile profile, AutomaticTransmissionFieldDefinition currentSelectorField)
    {
        Profile = CloneProfile(profile);
        _currentSelectorField = CloneField(currentSelectorField);

        Text = "Transmission Settings";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1100, 720);
        MinimumSize = new Size(960, 620);
        BackColor = Color.FromArgb(15, 23, 42);
        Font = new Font("Segoe UI", 10F);

        BuildGridColumns();
        ApplyGridTheme();
        LoadProfile(Profile);

        Controls.Add(BuildLayout());
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = "Configure the webhook payload shape",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);

        root.Controls.Add(BuildTopCard(), 0, 1);
        root.Controls.Add(BuildFieldsCard(), 0, 2);
        root.Controls.Add(BuildActionRow(), 0, 3);
        return root;
    }

    private Control BuildTopCard()
    {
        var card = CreateCard();
        card.Height = 210;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(CreateLabel("Profile Name"), 0, 0);
        layout.Controls.Add(_profileNameBox, 1, 0);

        var metadataPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };
        metadataPanel.Controls.Add(_includeProfileNameBox);
        metadataPanel.Controls.Add(_includeSourceUrlBox);
        metadataPanel.Controls.Add(_includeCapturedAtUtcBox);
        metadataPanel.Controls.Add(_includeContentHashBox);
        metadataPanel.Controls.Add(_includeFieldMetadataBox);
        layout.Controls.Add(CreateLabel("Metadata"), 0, 1);
        layout.Controls.Add(metadataPanel, 1, 1);

        layout.Controls.Add(CreateLabel("Data Layout"), 0, 2);
        layout.Controls.Add(_nestFieldsUnderDataRootBox, 1, 2);

        layout.Controls.Add(CreateLabel("Data Root Key"), 0, 3);
        layout.Controls.Add(_dataRootKeyBox, 1, 3);

        layout.Controls.Add(new Label
        {
            Text = "Each row below becomes a named outgoing field. You can send the first match, all matches, or the combined text for each selector.",
            ForeColor = Color.Silver,
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Margin = new Padding(0, 8, 0, 0)
        }, 1, 4);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildFieldsCard()
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;

        var titleRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            ColumnCount = 2
        };
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titleRow.Controls.Add(new Label
        {
            Text = "Transmitted Fields",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var fieldButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false
        };
        fieldButtons.Controls.Add(CreateButton("Add Field", (_, _) => AddEmptyField(), 110, accent: true));
        fieldButtons.Controls.Add(CreateButton("Use Current Selector", (_, _) => AddCurrentSelectorField(), 160));
        fieldButtons.Controls.Add(CreateButton("Remove Selected", (_, _) => RemoveSelectedField(), 130));
        titleRow.Controls.Add(fieldButtons, 1, 0);

        _fieldsGrid.DataSource = _fields;

        card.Controls.Add(_fieldsGrid);
        card.Controls.Add(titleRow);
        return card;
    }

    private Control BuildActionRow()
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        row.Controls.Add(CreateButton("Save Settings", (_, _) => SaveAndClose(), 130, accent: true));
        row.Controls.Add(CreateButton("Cancel", (_, _) => DialogResult = DialogResult.Cancel, 110));
        return row;
    }

    private void BuildGridColumns()
    {
        _fieldsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AutomaticTransmissionFieldDefinition.OutputKey),
            HeaderText = "Output Key",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 24
        });
        _fieldsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(AutomaticTransmissionFieldDefinition.Selector),
            HeaderText = "Selector",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 48
        });
        _fieldsGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(AutomaticTransmissionFieldDefinition.SelectorType),
            HeaderText = "Selector Type",
            DataSource = new[] { "CSS", "XPath" },
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 14
        });
        _fieldsGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(AutomaticTransmissionFieldDefinition.ValueMode),
            HeaderText = "Send As",
            DataSource = new[] { "First", "Combined", "All" },
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 14
        });
    }

    private void ApplyGridTheme()
    {
        _fieldsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
        _fieldsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _fieldsGrid.DefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
        _fieldsGrid.DefaultCellStyle.ForeColor = Color.Gainsboro;
        _fieldsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(16, 185, 129);
        _fieldsGrid.DefaultCellStyle.SelectionForeColor = Color.White;
    }

    private void LoadProfile(AutomaticTransmissionProfile profile)
    {
        _profileNameBox.Text = profile.ProfileName;
        _includeProfileNameBox.Checked = profile.IncludeProfileName;
        _includeSourceUrlBox.Checked = profile.IncludeSourceUrl;
        _includeCapturedAtUtcBox.Checked = profile.IncludeCapturedAtUtc;
        _includeContentHashBox.Checked = profile.IncludeContentHash;
        _includeFieldMetadataBox.Checked = profile.IncludeFieldMetadata;
        _nestFieldsUnderDataRootBox.Checked = profile.NestFieldsUnderDataRoot;
        _dataRootKeyBox.Text = profile.DataRootKey;

        _fields.Clear();
        foreach (var field in profile.Fields.Select(CloneField))
        {
            _fields.Add(field);
        }

        if (_fields.Count == 0)
        {
            _fields.Add(CloneField(_currentSelectorField));
        }
    }

    private void AddEmptyField()
    {
        _fields.Add(new AutomaticTransmissionFieldDefinition
        {
            OutputKey = $"field{_fields.Count + 1}",
            SelectorType = "CSS",
            ValueMode = "Combined"
        });
    }

    private void AddCurrentSelectorField()
    {
        _fields.Add(CloneField(_currentSelectorField));
    }

    private void RemoveSelectedField()
    {
        if (_fieldsGrid.CurrentRow?.DataBoundItem is not AutomaticTransmissionFieldDefinition field)
        {
            return;
        }

        _fields.Remove(field);
    }

    private void SaveAndClose()
    {
        var profileName = string.IsNullOrWhiteSpace(_profileNameBox.Text) ? "Default" : _profileNameBox.Text.Trim();
        var dataRootKey = string.IsNullOrWhiteSpace(_dataRootKeyBox.Text) ? "data" : _dataRootKeyBox.Text.Trim();
        var cleanedFields = _fields
            .Where(field => !string.IsNullOrWhiteSpace(field.OutputKey) && !string.IsNullOrWhiteSpace(field.Selector))
            .Select(CloneField)
            .ToList();

        if (cleanedFields.Count == 0)
        {
            MessageBox.Show(this, "Add at least one field with both an output key and a selector.", "Transmission Settings");
            return;
        }

        Profile = new AutomaticTransmissionProfile
        {
            ProfileName = profileName,
            IncludeProfileName = _includeProfileNameBox.Checked,
            IncludeSourceUrl = _includeSourceUrlBox.Checked,
            IncludeCapturedAtUtc = _includeCapturedAtUtcBox.Checked,
            IncludeContentHash = _includeContentHashBox.Checked,
            IncludeFieldMetadata = _includeFieldMetadataBox.Checked,
            NestFieldsUnderDataRoot = _nestFieldsUnderDataRootBox.Checked,
            DataRootKey = dataRootKey,
            Fields = cleanedFields
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private static Panel CreateCard() =>
        new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 16)
        };

    private static Label CreateLabel(string text) =>
        new()
        {
            Text = text,
            ForeColor = Color.Gainsboro,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
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

    private static AutomaticTransmissionProfile CloneProfile(AutomaticTransmissionProfile profile) =>
        new()
        {
            ProfileName = profile.ProfileName,
            IncludeProfileName = profile.IncludeProfileName,
            IncludeSourceUrl = profile.IncludeSourceUrl,
            IncludeCapturedAtUtc = profile.IncludeCapturedAtUtc,
            IncludeContentHash = profile.IncludeContentHash,
            IncludeFieldMetadata = profile.IncludeFieldMetadata,
            NestFieldsUnderDataRoot = profile.NestFieldsUnderDataRoot,
            DataRootKey = profile.DataRootKey,
            Fields = profile.Fields.Select(CloneField).ToList()
        };

    private static AutomaticTransmissionFieldDefinition CloneField(AutomaticTransmissionFieldDefinition field) =>
        new()
        {
            OutputKey = field.OutputKey,
            Selector = field.Selector,
            SelectorType = field.SelectorType,
            ValueMode = field.ValueMode
        };
}
