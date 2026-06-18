using System.Diagnostics;

namespace Scraper.Advanced.WinForms;

public class WelcomeForm : Form
{
    public WelcomeForm()
    {
        Text = "Welcome to Web Scraper Pro v1.0";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1080, 720);
        Size = new Size(1180, 760);
        BackColor = Color.FromArgb(8, 15, 32);
        Font = new Font("Segoe UI", 10F);

        Controls.Add(BuildLayout());
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28),
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHero(), 0, 0);
        root.Controls.Add(BuildComplianceNote(), 0, 1);
        root.Controls.Add(BuildModes(), 0, 2);
        return root;
    }

    private Control BuildHero()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 190,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(24),
            Margin = new Padding(0, 0, 0, 18),
            ColumnCount = 2,
            RowCount = 1
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

        var textPanel = new Panel { Dock = DockStyle.Fill };

        var version = new Label
        {
            Text = "WELCOME TO WEB SCRAPER PRO V1.0",
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(125, 211, 252)
        };

        var title = new Label
        {
            Text = "Modern desktop scraping workspace",
            Dock = DockStyle.Top,
            Height = 56,
            Font = new Font("Segoe UI Semibold", 28F, FontStyle.Bold),
            ForeColor = Color.White
        };

        var body = new Label
        {
            Text = "Choose a focused mode for structured extraction, media harvesting, or selector picking. The suite supports normal page loading and user-driven sign-in flows, but it does not include CAPTCHA, Cloudflare, or anti-bot bypass features.",
            Dock = DockStyle.Fill,
            ForeColor = Color.Silver
        };

        textPanel.Controls.Add(body);
        textPanel.Controls.Add(title);
        textPanel.Controls.Add(version);

        panel.Controls.Add(textPanel, 0, 0);

        var configContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var configButton = new Button
        {
            Text = "⚙ Configuration",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(139, 92, 246), // Violet for AI
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        configButton.FlatAppearance.BorderSize = 0;
        configButton.Click += (_, _) =>
        {
            using var form = new ConfigurationForm();
            form.ShowDialog(this);
        };

        configContainer.Controls.Add(configButton);
        panel.Controls.Add(configContainer, 1, 0);

        return panel;
    }

    private Control BuildComplianceNote()
    {
        return new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = "6 modes available below. Use them for authorized scraping and interactive login only.",
            ForeColor = Color.FromArgb(253, 224, 71),
            Padding = new Padding(4, 0, 0, 0)
        };
    }

    private Control BuildModes()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true
        };

        flow.Controls.Add(CreateModeCard(
            "Advanced Deep Scraper",
            "Structured data scraping for titles, links, prices, categories, and custom selector-driven output.",
            "Open Workspace",
            (_, _) =>
            {
                using var form = new AdvancedWorkspaceForm();
                form.ShowDialog(this);
            }));

        flow.Controls.Add(CreateModeCard(
            "Media Scraper",
            "Scan pages for images, videos, and audio, preview them in a grid, and download selected media.",
            "Launch App",
            (_, _) => LaunchSiblingExecutable("Scraper.Media.WinForms.exe")));

        flow.Controls.Add(CreateModeCard(
            "Element Picker",
            "DevTools-style selector helper with click-to-select behavior and CSS/XPath capture.",
            "Launch App",
            (_, _) => LaunchSiblingExecutable("Scraper.Picker.WinForms.exe")));

        flow.Controls.Add(CreateModeCard(
            "Website Cloner",
            "Mirror a website locally by crawling same-origin pages, downloading assets, and rewriting links for offline browsing.",
            "Launch App",
            (_, _) => LaunchSiblingExecutable("Scraper.SiteCloner.WinForms.exe")));

        flow.Controls.Add(CreateModeCard(
            "Automatic Scraper",
            "Monitor a live page on an interval, extract changing values like scores or prices, and transmit updates to a webhook.",
            "Launch App",
            (_, _) => LaunchSiblingExecutable("Scraper.Automatic.WinForms.exe")));

        flow.Controls.Add(CreateModeCard(
            "Scraper Receiver",
            "A local ASP.NET Core webhook receiver to collect live updates from the Automatic Scraper. Listens on port 3001 and logs events.",
            "Launch Server",
            (_, _) => LaunchSiblingExecutable("Scraper.WebhookReceiver.exe")));

        return flow;
    }

    private Panel CreateModeCard(string title, string description, string actionText, EventHandler onClick)
    {
        var card = new Panel
        {
            Width = 380,
            Height = 340,
            Margin = new Padding(0, 0, 18, 0),
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(20)
        };

        var badge = new Label
        {
            Text = "Mode",
            ForeColor = Color.FromArgb(14, 165, 233),
            Dock = DockStyle.Top,
            Height = 24
        };

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 84,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.White
        };

        var descLabel = new Label
        {
            Text = description,
            Dock = DockStyle.Top,
            Height = 110,
            ForeColor = Color.Silver
        };

        var button = new Button
        {
            Text = actionText,
            Width = 140,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(14, 165, 233),
            ForeColor = Color.White,
            Dock = DockStyle.Bottom
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;

        card.Controls.Add(button);
        card.Controls.Add(descLabel);
        card.Controls.Add(titleLabel);
        card.Controls.Add(badge);
        return card;
    }

    private void LaunchSiblingExecutable(string executableName)
    {
        var candidate = ResolveLaunchPath(executableName);
        if (candidate is null)
        {
            MessageBox.Show(this, $"Build and run {executableName} from the solution to use that mode.", "Mode Not Available");
            return;
        }

        Process.Start(new ProcessStartInfo(candidate) { UseShellExecute = true });
    }

    private static string? ResolveLaunchPath(string executableName)
    {
        var directCandidate = Path.Combine(AppContext.BaseDirectory, executableName);
        if (File.Exists(directCandidate))
        {
            return directCandidate;
        }

        var searchRoots = new List<string?>
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        foreach (var root in searchRoots.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (root is null || !Directory.Exists(root))
            {
                continue;
            }

            try
            {
                var match = Directory.EnumerateFiles(root, executableName, SearchOption.AllDirectories)
                    .FirstOrDefault(path =>
                        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                        path.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                {
                    return match;
                }
            }
            catch
            {
                // Ignore transient or access issues while searching fallback locations.
            }
        }

        return null;
    }
}
