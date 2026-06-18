namespace Scraper.Automatic.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new AutomaticScraperForm());
    }
}
