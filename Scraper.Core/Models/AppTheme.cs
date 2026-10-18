using System.Drawing;

namespace Scraper.Core.Models;

public class AppTheme
{
    public string Name { get; init; } = "Dark";
    public Color Background { get; init; }
    public Color CardBackground { get; init; }
    public Color CardBorder { get; init; }
    public Color TextPrimary { get; init; }
    public Color TextSecondary { get; init; }
    public Color TextMuted { get; init; }
    public Color Accent { get; init; }
    public Color AccentHover { get; init; }
    public Color InputBackground { get; init; }
    public Color InputForeground { get; init; }
    public Color InputBorder { get; init; }
    public Color ButtonBackground { get; init; }
    public Color ButtonForeground { get; init; }
    public Color GridBackground { get; init; }
    public Color GridHeaderBackground { get; init; }
    public Color GridHeaderForeground { get; init; }
    public Color GridRowBackground { get; init; }
    public Color GridAltRowBackground { get; init; }
    public Color GridRowForeground { get; init; }
    public Color GridSelectionBackground { get; init; }
    public Color GridSelectionForeground { get; init; }
    public Color GridLineColor { get; init; }
    public bool IsDark { get; init; } = true;

    public static readonly AppTheme Dark = new()
    {
        Name = "Dark",
        Background = Color.FromArgb(8, 15, 32),
        CardBackground = Color.FromArgb(15, 23, 42),
        CardBorder = Color.FromArgb(30, 41, 59),
        TextPrimary = Color.White,
        TextSecondary = Color.FromArgb(125, 211, 252),
        TextMuted = Color.Silver,
        Accent = Color.FromArgb(14, 165, 233),
        AccentHover = Color.FromArgb(56, 189, 248),
        InputBackground = Color.FromArgb(15, 23, 42),
        InputForeground = Color.White,
        InputBorder = Color.FromArgb(51, 65, 85),
        ButtonBackground = Color.FromArgb(30, 41, 59),
        ButtonForeground = Color.White,
        GridBackground = Color.FromArgb(15, 23, 42),
        GridHeaderBackground = Color.FromArgb(30, 41, 59),
        GridHeaderForeground = Color.White,
        GridRowBackground = Color.FromArgb(15, 23, 42),
        GridAltRowBackground = Color.FromArgb(20, 30, 55),
        GridRowForeground = Color.Gainsboro,
        GridSelectionBackground = Color.FromArgb(14, 165, 233),
        GridSelectionForeground = Color.White,
        GridLineColor = Color.FromArgb(30, 41, 59),
        IsDark = true
    };

    public static readonly AppTheme Light = new()
    {
        Name = "Light",
        Background = Color.FromArgb(241, 245, 249),
        CardBackground = Color.White,
        CardBorder = Color.FromArgb(226, 232, 240),
        TextPrimary = Color.FromArgb(15, 23, 42),
        TextSecondary = Color.FromArgb(2, 132, 199),
        TextMuted = Color.FromArgb(100, 116, 139),
        Accent = Color.FromArgb(2, 132, 199),
        AccentHover = Color.FromArgb(14, 165, 233),
        InputBackground = Color.FromArgb(248, 250, 252),
        InputForeground = Color.FromArgb(15, 23, 42),
        InputBorder = Color.FromArgb(203, 213, 225),
        ButtonBackground = Color.FromArgb(226, 232, 240),
        ButtonForeground = Color.FromArgb(15, 23, 42),
        GridBackground = Color.White,
        GridHeaderBackground = Color.FromArgb(241, 245, 249),
        GridHeaderForeground = Color.FromArgb(15, 23, 42),
        GridRowBackground = Color.White,
        GridAltRowBackground = Color.FromArgb(248, 250, 252),
        GridRowForeground = Color.FromArgb(15, 23, 42),
        GridSelectionBackground = Color.FromArgb(2, 132, 199),
        GridSelectionForeground = Color.White,
        GridLineColor = Color.FromArgb(226, 232, 240),
        IsDark = false
    };

    public static readonly AppTheme Midnight = new()
    {
        Name = "Midnight",
        Background = Color.FromArgb(3, 7, 18),
        CardBackground = Color.FromArgb(11, 15, 26),
        CardBorder = Color.FromArgb(30, 41, 59),
        TextPrimary = Color.FromArgb(248, 250, 252),
        TextSecondary = Color.FromArgb(56, 189, 248),
        TextMuted = Color.FromArgb(148, 163, 184),
        Accent = Color.FromArgb(56, 189, 248),
        AccentHover = Color.FromArgb(125, 211, 252),
        InputBackground = Color.FromArgb(17, 24, 39),
        InputForeground = Color.White,
        InputBorder = Color.FromArgb(55, 65, 81),
        ButtonBackground = Color.FromArgb(31, 41, 55),
        ButtonForeground = Color.White,
        GridBackground = Color.FromArgb(11, 15, 26),
        GridHeaderBackground = Color.FromArgb(31, 41, 55),
        GridHeaderForeground = Color.White,
        GridRowBackground = Color.FromArgb(11, 15, 26),
        GridAltRowBackground = Color.FromArgb(17, 24, 39),
        GridRowForeground = Color.FromArgb(226, 232, 240),
        GridSelectionBackground = Color.FromArgb(56, 189, 248),
        GridSelectionForeground = Color.Black,
        GridLineColor = Color.FromArgb(31, 41, 55),
        IsDark = true
    };

    public static readonly AppTheme Emerald = new()
    {
        Name = "Emerald",
        Background = Color.FromArgb(6, 26, 20),
        CardBackground = Color.FromArgb(11, 41, 32),
        CardBorder = Color.FromArgb(16, 68, 52),
        TextPrimary = Color.FromArgb(236, 253, 245),
        TextSecondary = Color.FromArgb(52, 211, 153),
        TextMuted = Color.FromArgb(167, 243, 208),
        Accent = Color.FromArgb(16, 185, 129),
        AccentHover = Color.FromArgb(52, 211, 153),
        InputBackground = Color.FromArgb(8, 34, 26),
        InputForeground = Color.White,
        InputBorder = Color.FromArgb(16, 78, 60),
        ButtonBackground = Color.FromArgb(16, 68, 52),
        ButtonForeground = Color.White,
        GridBackground = Color.FromArgb(11, 41, 32),
        GridHeaderBackground = Color.FromArgb(16, 68, 52),
        GridHeaderForeground = Color.White,
        GridRowBackground = Color.FromArgb(11, 41, 32),
        GridAltRowBackground = Color.FromArgb(15, 54, 42),
        GridRowForeground = Color.FromArgb(236, 253, 245),
        GridSelectionBackground = Color.FromArgb(16, 185, 129),
        GridSelectionForeground = Color.White,
        GridLineColor = Color.FromArgb(16, 68, 52),
        IsDark = true
    };

    public static AppTheme FromName(string? name)
    {
        return (name?.Trim().ToLowerInvariant()) switch
        {
            "light" => Light,
            "midnight" or "amoled" => Midnight,
            "emerald" or "emerald dark" => Emerald,
            _ => Dark
        };
    }
}
