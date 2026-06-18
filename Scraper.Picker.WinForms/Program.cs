namespace Scraper.Picker.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ElementPickerForm());
    }
}
