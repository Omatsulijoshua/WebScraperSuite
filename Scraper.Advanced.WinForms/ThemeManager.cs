using System;
using System.Drawing;
using System.Windows.Forms;
using Scraper.Core.Models;
using Scraper.Core.Services;

namespace Scraper.Core.Services;

public static class ThemeManager
{
    public static event Action<AppTheme>? ThemeChanged;

    private static AppTheme? _currentTheme;

    public static AppTheme CurrentTheme
    {
        get
        {
            if (_currentTheme == null)
            {
                var settings = SettingsService.Load();
                _currentTheme = AppTheme.FromName(settings.Theme);
            }
            return _currentTheme;
        }
    }

    public static void SetTheme(string themeName)
    {
        _currentTheme = AppTheme.FromName(themeName);
        ThemeChanged?.Invoke(_currentTheme);
    }

    public static void ApplyTheme(Form form)
    {
        var theme = CurrentTheme;
        form.BackColor = theme.Background;
        form.ForeColor = theme.TextPrimary;

        ApplyToControls(form.Controls, theme);
    }

    public static void ApplyToControls(Control.ControlCollection controls, AppTheme theme)
    {
        foreach (Control control in controls)
        {
            ApplyToControl(control, theme);
        }
    }

    private static void ApplyToControl(Control control, AppTheme theme)
    {
        switch (control)
        {
            case DataGridView grid:
                ApplyToGrid(grid, theme);
                break;

            case TextBox textBox:
                textBox.BackColor = theme.InputBackground;
                textBox.ForeColor = theme.InputForeground;
                break;

            case ComboBox comboBox:
                comboBox.BackColor = theme.InputBackground;
                comboBox.ForeColor = theme.InputForeground;
                break;

            case NumericUpDown numericUpDown:
                numericUpDown.BackColor = theme.InputBackground;
                numericUpDown.ForeColor = theme.InputForeground;
                break;

            case ListBox listBox:
                listBox.BackColor = theme.CardBackground;
                listBox.ForeColor = theme.TextPrimary;
                break;

            case StatusStrip statusStrip:
                statusStrip.BackColor = theme.CardBackground;
                statusStrip.ForeColor = theme.TextMuted;
                foreach (ToolStripItem item in statusStrip.Items)
                {
                    item.ForeColor = theme.TextMuted;
                }
                break;

            case Button button:
                ApplyToButton(button, theme);
                break;

            case Label label:
                ApplyToLabel(label, theme);
                break;

            case CheckBox checkBox:
                checkBox.ForeColor = theme.TextPrimary;
                break;

            case RadioButton radioButton:
                radioButton.ForeColor = theme.TextPrimary;
                break;

            case TabControl tabControl:
                tabControl.BackColor = theme.CardBackground;
                tabControl.ForeColor = theme.TextPrimary;
                foreach (TabPage page in tabControl.TabPages)
                {
                    page.BackColor = theme.CardBackground;
                    page.ForeColor = theme.TextPrimary;
                    ApplyToControls(page.Controls, theme);
                }
                break;

            case TabPage tabPage:
                tabPage.BackColor = theme.CardBackground;
                tabPage.ForeColor = theme.TextPrimary;
                break;

            case TableLayoutPanel tableLayout:
                if (tableLayout.Dock == DockStyle.Fill && tableLayout.Parent is Form)
                {
                    tableLayout.BackColor = theme.Background;
                }
                break;

            case FlowLayoutPanel flowLayout:
                if (flowLayout.Dock == DockStyle.Fill && flowLayout.Parent is Form)
                {
                    flowLayout.BackColor = theme.Background;
                }
                break;

            case Panel panel:
                // If it's a card container
                if (panel.Padding.All >= 10 || panel.Margin.All >= 10 || panel.Height > 100)
                {
                    panel.BackColor = theme.CardBackground;
                }
                break;
        }

        if (control.HasChildren && !(control is TabControl))
        {
            ApplyToControls(control.Controls, theme);
        }
    }

    private static void ApplyToLabel(Label label, AppTheme theme)
    {
        // Don't modify special colored badge/status labels if they are explicitly colored warning/success
        if (label.ForeColor == Color.LightGreen || label.ForeColor == Color.Red || label.ForeColor == Color.Yellow)
        {
            return;
        }

        if (label.Font.Size >= 18 || label.Font.Bold)
        {
            label.ForeColor = theme.TextPrimary;
        }
        else if (label.Text.StartsWith("WELCOME", StringComparison.OrdinalIgnoreCase) ||
                 label.Text.StartsWith("Mode", StringComparison.OrdinalIgnoreCase) ||
                 label.ForeColor == Color.FromArgb(125, 211, 252) ||
                 label.ForeColor == Color.FromArgb(14, 165, 233))
        {
            label.ForeColor = theme.TextSecondary;
        }
        else if (label.ForeColor == Color.Silver || label.ForeColor == Color.Gainsboro)
        {
            label.ForeColor = theme.TextMuted;
        }
        else
        {
            label.ForeColor = theme.TextPrimary;
        }
    }

    private static void ApplyToButton(Button button, AppTheme theme)
    {
        // Check if button is an accent button
        var isAccent = button.BackColor == Color.FromArgb(14, 165, 233) ||
                       button.BackColor == Color.FromArgb(2, 132, 199) ||
                       button.BackColor == Color.FromArgb(16, 185, 129) ||
                       button.BackColor == Color.FromArgb(139, 92, 246);

        if (isAccent)
        {
            button.ForeColor = Color.White;
        }
        else
        {
            button.BackColor = theme.ButtonBackground;
            button.ForeColor = theme.ButtonForeground;
        }
    }

    public static void ApplyToGrid(DataGridView grid, AppTheme theme)
    {
        grid.BackgroundColor = theme.GridBackground;
        grid.GridColor = theme.GridLineColor;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;

        grid.ColumnHeadersDefaultCellStyle.BackColor = theme.GridHeaderBackground;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = theme.GridHeaderForeground;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.GridHeaderBackground;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = theme.GridHeaderForeground;

        grid.DefaultCellStyle.BackColor = theme.GridRowBackground;
        grid.DefaultCellStyle.ForeColor = theme.GridRowForeground;
        grid.DefaultCellStyle.SelectionBackColor = theme.GridSelectionBackground;
        grid.DefaultCellStyle.SelectionForeColor = theme.GridSelectionForeground;

        grid.AlternatingRowsDefaultCellStyle.BackColor = theme.GridAltRowBackground;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = theme.GridRowForeground;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = theme.GridSelectionBackground;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = theme.GridSelectionForeground;
    }
}
