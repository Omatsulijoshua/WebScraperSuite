namespace Scraper.SiteCloner.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SiteClonerForm());
    }
}
