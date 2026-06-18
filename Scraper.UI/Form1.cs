using Microsoft.Web.WebView2.WinForms;
using Scraper.UI.Services;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace Scraper.UI
{
    public partial class Form1 : Form
    {
        private readonly ApiClient _api = new ApiClient();


        private WebView2 browser;

        public Form1()
        {
            InitializeComponent();

            InitEvents();
            InitBrowser();
            ShowMode("advanced");


            // rbhtml.CheckedChanged += rbHtml_CheckedChanged;
            //button1.Click += button1_Click;
            btnExtract.Click += btnExtract_Click;
            btnExport.Click += btnExport1_Click;
            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].Name = "Index";
            dataGridView1.Columns[1].Name = "Value";
            rbhtml.CheckedChanged += rbhtml_CheckedChanged;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }





        private string _loadedHtml = "";
        private List<string> _extractedData = new();






        // ================= INIT EVENTS =================
        private void InitEvents()
        {
            btnScrape.Click += btnScrape_Click;
            btnPaste.Click += (s, e) => txtUrl.Text = Clipboard.GetText();
            btnClear.Click += (s, e) => txtUrl.Clear();

            rbAdvanced.CheckedChanged += (s, e) => { if (rbAdvanced.Checked) ShowMode("advanced"); };
            rbMedia.CheckedChanged += (s, e) => { if (rbMedia.Checked) ShowMode("media"); };
            rbPicker.CheckedChanged += (s, e) => { if (rbPicker.Checked) ShowMode("element"); };

            //btnOpenBrowser.Click += BtnOpenBrowser_Click;
            //btnCopy.Click += (s, e) => Clipboard.SetText(txtSelected.Text);
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {

        }

        private async void btnScrape_Click(object sender, EventArgs e)
        {
            try
            {
                lblStatus.Text = "Scraping...";
                progressBar.Value = 30;

                var request = new
                {
                    url = txtUrl.Text,
                    selector = txtCss.Text,
                    useJs = chkJs.Checked
                };

                var result = await _api.ScrapeAsync(request);

                if (result == null)
                {
                    lblStatus.Text = "API Error";
                    return;
                }

                resultGrid.DataSource = result.Results
                    .Select((x, i) => new
                    {
                        Index = i + 1,
                        Value = x
                    })
                    .ToList();

                lblStatus.Text = $"Done: {result.Count} items";
                progressBar.Value = 100;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error";
                txtLogs.AppendText(ex.Message + Environment.NewLine);
            }

        }

        private void ShowMode(string mode)
        {
            // Hide everything
            urlPanel.Visible = false;
            settingsPanel.Visible = false;
            selectorPanel.Visible = false;
            actionPanel.Visible = false;
            resultGrid.Visible = false;

            mediaPanel.Visible = false;
            elementPanel.Visible = false;

            // Hide inner panels
            panelhtml.Visible = false;

            switch (mode)
            {
                case "advanced":
                    urlPanel.Visible = true;
                    settingsPanel.Visible = true;
                    selectorPanel.Visible = true;
                    actionPanel.Visible = true;
                    resultGrid.Visible = true;
                    break;

                case "media":
                    mediaPanel.Visible = true;
                    break;

                case "element":
                    elementPanel.Visible = true;
                    break;

                case "html": // ✅ THIS WAS MISSING
                    elementPanel.Visible = true;
                    panelhtml.Visible = true;
                    panelhtml.BringToFront();
                    break;
            }
        }

        private async void InitBrowser()
        {
            browser = new WebView2
            {
                Dock = DockStyle.Fill
            };

            elementPanel.Controls.Add(browser);
            await browser.EnsureCoreWebView2Async();
        }

        // ================= OPEN BROWSER =================
        private void BtnOpenBrowser_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtUrl.Text))
                browser.Source = new Uri(txtUrl.Text);
        }

        private void rbPicker_CheckedChanged(object sender, EventArgs e)
        {

        }


        //  await client.GetStringAsync(txtInput.Text);
        private void rbhtml_CheckedChanged(object sender, EventArgs e)
        {
            if (rbhtml.Checked)
            {
                ShowMode("html");
            }
        }
        


        private void btnExtract_Click(object sender, EventArgs e)
        {
            try
            {
                progressBar1.Value = 0;
                lblStatus.Text = "Extracting...";

                dataGridView1.Rows.Clear();

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(_loadedHtml);

                progressBar1.Value = 30;

                var nodes = doc.DocumentNode.SelectNodes("//body//*[not(self::script or self::style)]");

                _extractedData = nodes?
                    .Select(n => n.InnerText.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .ToList() ?? new List<string>();

                progressBar1.Value = 70;

                int index = 1;
                foreach (var item in _extractedData)
                {
                    dataGridView1.Rows.Add(index++, item);
                }

                progressBar1.Value = 100;
                lblStatus.Text = $"Extracted {_extractedData.Count} items";
            }
            catch (Exception ex)
            {
                progressBar1.Value = 0;
                lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private void btnExport1_Click(object sender, EventArgs e)
        {
            try
            {
                if (_extractedData.Count == 0)
                {
                    MessageBox.Show("No data to export");
                    return;
                }

                SaveFileDialog save = new SaveFileDialog();

                if (cmbExportType.Text == "JSON")
                {
                    save.Filter = "JSON File|*.json";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(
                            _extractedData,
                            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                        File.WriteAllText(save.FileName, json);
                    }
                }
                else if (cmbExportType.Text == "CSV")
                {
                    save.Filter = "CSV File|*.csv";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        var csv = string.Join(Environment.NewLine,
                            _extractedData.Select(x => $"\"{x}\""));

                        File.WriteAllText(save.FileName, csv);
                    }
                }

                lblStatus.Text = "Export completed";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private async void btnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                progressBar1.Value = 10;
                lblStatus.Text = "Loading...";

                if (cmbInputType.Text == "URL")
                {
                    using HttpClient client = new HttpClient();
                    _loadedHtml = await client.GetStringAsync(txtInput.Text);
                }
                else if (cmbInputType.Text == "File")
                {
                    _loadedHtml = File.ReadAllText(txtInput.Text);
                }

                progressBar1.Value = 100;
                lblStatus.Text = "Loaded successfully";
            }
            catch (Exception ex)
            {
                progressBar1.Value = 0;
                lblStatus.Text = "Error: " + ex.Message;
            }
        }
    }
}
