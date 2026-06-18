namespace Scraper.UI
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            topBar = new Panel();
            lblTitle = new Label();
            lblStatusConn = new Label();
            btnSettings = new Button();
            panelhtml = new Panel();
            btnLoad = new Button();
            dataGridView1 = new DataGridView();
            btnExport1 = new Button();
            btnExtract = new Button();
            progressBar1 = new ProgressBar();
            txtOutput = new TextBox();
            txtInput = new TextBox();
            cmbExportType = new ComboBox();
            cmbSiteType = new ComboBox();
            cmbInputType = new ComboBox();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            modePanel = new Panel();
            rbAdvanced = new RadioButton();
            rbhtml = new RadioButton();
            rbMedia = new RadioButton();
            rbPicker = new RadioButton();
            urlPanel = new Panel();
            label1 = new Label();
            txtUrl = new TextBox();
            btnPaste = new Button();
            comboBox1 = new ComboBox();
            btnClear = new Button();
            settingsPanel = new Panel();
            chkJs = new CheckBox();
            chkAntiBlock = new CheckBox();
            cmbDelay = new ComboBox();
            cmbTimeout = new ComboBox();
            selectorPanel = new Panel();
            txtCss = new TextBox();
            txtXPath = new TextBox();
            btnTest = new Button();
            btnAuto = new Button();
            actionPanel = new Panel();
            btnScrape = new Button();
            btnStop = new Button();
            btnPause = new Button();
            btnRetry = new Button();
            btnExport = new Button();
            resultGrid = new DataGridView();
            logPanel = new Panel();
            lblStatus = new Label();
            progressBar = new ProgressBar();
            txtLogs = new TextBox();
            mediaPanel = new Panel();
            elementPanel = new Panel();
            topBar.SuspendLayout();
            panelhtml.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            modePanel.SuspendLayout();
            urlPanel.SuspendLayout();
            settingsPanel.SuspendLayout();
            selectorPanel.SuspendLayout();
            actionPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)resultGrid).BeginInit();
            logPanel.SuspendLayout();
            SuspendLayout();
            // 
            // topBar
            // 
            topBar.BackColor = Color.FromArgb(30, 30, 30);
            topBar.Controls.Add(lblTitle);
            topBar.Controls.Add(lblStatusConn);
            topBar.Controls.Add(btnSettings);
            topBar.Dock = DockStyle.Top;
            topBar.Location = new Point(0, 0);
            topBar.Name = "topBar";
            topBar.Size = new Size(884, 45);
            topBar.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(10, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(100, 23);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "\U0001f9f0 WebScraper Suite Pro";
            // 
            // lblStatusConn
            // 
            lblStatusConn.ForeColor = Color.Lime;
            lblStatusConn.Location = new Point(250, 12);
            lblStatusConn.Name = "lblStatusConn";
            lblStatusConn.Size = new Size(100, 23);
            lblStatusConn.TabIndex = 1;
            lblStatusConn.Text = "\U0001f7e2 API Connected";
            // 
            // btnSettings
            // 
            btnSettings.BackColor = Color.White;
            btnSettings.Location = new Point(750, 8);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(75, 23);
            btnSettings.TabIndex = 2;
            btnSettings.Text = "⚙ Settings";
            btnSettings.UseVisualStyleBackColor = false;
            btnSettings.Click += btnSettings_Click;
            // 
            // panelhtml
            // 
            panelhtml.Controls.Add(btnLoad);
            panelhtml.Controls.Add(dataGridView1);
            panelhtml.Controls.Add(btnExport1);
            panelhtml.Controls.Add(btnExtract);
            panelhtml.Controls.Add(progressBar1);
            panelhtml.Controls.Add(txtOutput);
            panelhtml.Controls.Add(txtInput);
            panelhtml.Controls.Add(cmbExportType);
            panelhtml.Controls.Add(cmbSiteType);
            panelhtml.Controls.Add(cmbInputType);
            panelhtml.Controls.Add(label6);
            panelhtml.Controls.Add(label5);
            panelhtml.Controls.Add(label4);
            panelhtml.Controls.Add(label3);
            panelhtml.Controls.Add(label2);
            panelhtml.Location = new Point(2, 103);
            panelhtml.Name = "panelhtml";
            panelhtml.Size = new Size(862, 569);
            panelhtml.TabIndex = 3;
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(184, 238);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(171, 44);
            btnLoad.TabIndex = 11;
            btnLoad.Text = "Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(160, 294);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(589, 272);
            dataGridView1.TabIndex = 10;
            // 
            // btnExport1
            // 
            btnExport1.Location = new Point(591, 240);
            btnExport1.Name = "btnExport1";
            btnExport1.Size = new Size(155, 42);
            btnExport1.TabIndex = 7;
            btnExport1.Text = "Export ";
            btnExport1.UseVisualStyleBackColor = true;
            btnExport1.Click += btnExport1_Click;
            // 
            // btnExtract
            // 
            btnExtract.Location = new Point(399, 240);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new Size(150, 42);
            btnExtract.TabIndex = 8;
            btnExtract.Text = "Extract";
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(163, 167);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(583, 31);
            progressBar1.TabIndex = 6;
            // 
            // txtOutput
            // 
            txtOutput.Location = new Point(278, 138);
            txtOutput.Name = "txtOutput";
            txtOutput.Size = new Size(469, 23);
            txtOutput.TabIndex = 3;
            // 
            // txtInput
            // 
            txtInput.Location = new Point(277, 109);
            txtInput.Name = "txtInput";
            txtInput.Size = new Size(469, 23);
            txtInput.TabIndex = 3;
            // 
            // cmbExportType
            // 
            cmbExportType.FormattingEnabled = true;
            cmbExportType.Location = new Point(277, 80);
            cmbExportType.Name = "cmbExportType";
            cmbExportType.Size = new Size(469, 23);
            cmbExportType.TabIndex = 2;
            // 
            // cmbSiteType
            // 
            cmbSiteType.FormattingEnabled = true;
            cmbSiteType.Location = new Point(277, 49);
            cmbSiteType.Name = "cmbSiteType";
            cmbSiteType.Size = new Size(469, 23);
            cmbSiteType.TabIndex = 2;
            // 
            // cmbInputType
            // 
            cmbInputType.FormattingEnabled = true;
            cmbInputType.Location = new Point(277, 20);
            cmbInputType.Name = "cmbInputType";
            cmbInputType.Size = new Size(469, 23);
            cmbInputType.TabIndex = 2;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(163, 142);
            label6.Name = "label6";
            label6.Size = new Size(45, 15);
            label6.TabIndex = 0;
            label6.Text = "Output";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(163, 112);
            label5.Name = "label5";
            label5.Size = new Size(93, 15);
            label5.TabIndex = 0;
            label5.Text = "URL or File Path ";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(163, 83);
            label4.Name = "label4";
            label4.Size = new Size(102, 15);
            label4.TabIndex = 0;
            label4.Text = "Select Export Type";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(163, 52);
            label3.Name = "label3";
            label3.Size = new Size(88, 15);
            label3.TabIndex = 0;
            label3.Text = "Select Site Type";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(163, 23);
            label2.Name = "label2";
            label2.Size = new Size(97, 15);
            label2.TabIndex = 0;
            label2.Text = "Select Input Type";
            // 
            // modePanel
            // 
            modePanel.Controls.Add(rbAdvanced);
            modePanel.Controls.Add(rbhtml);
            modePanel.Controls.Add(rbMedia);
            modePanel.Controls.Add(rbPicker);
            modePanel.Location = new Point(10, 50);
            modePanel.Name = "modePanel";
            modePanel.Size = new Size(850, 40);
            modePanel.TabIndex = 1;
            // 
            // rbAdvanced
            // 
            rbAdvanced.Checked = true;
            rbAdvanced.Location = new Point(10, 3);
            rbAdvanced.Name = "rbAdvanced";
            rbAdvanced.Size = new Size(104, 24);
            rbAdvanced.TabIndex = 0;
            rbAdvanced.TabStop = true;
            rbAdvanced.Text = "Advanced Scraper";
            // 
            // rbhtml
            // 
            rbhtml.Location = new Point(184, 3);
            rbhtml.Name = "rbhtml";
            rbhtml.Size = new Size(167, 24);
            rbhtml.TabIndex = 1;
            rbhtml.Text = "html or url to json or csv";
            // 
            // rbMedia
            // 
            rbMedia.Location = new Point(471, 3);
            rbMedia.Name = "rbMedia";
            rbMedia.Size = new Size(104, 24);
            rbMedia.TabIndex = 1;
            rbMedia.Text = "Media Scraper";
            // 
            // rbPicker
            // 
            rbPicker.Location = new Point(711, 3);
            rbPicker.Name = "rbPicker";
            rbPicker.Size = new Size(104, 24);
            rbPicker.TabIndex = 2;
            rbPicker.Text = "Element Picker";
            rbPicker.CheckedChanged += rbPicker_CheckedChanged;
            // 
            // urlPanel
            // 
            urlPanel.Controls.Add(label1);
            urlPanel.Controls.Add(txtUrl);
            urlPanel.Controls.Add(btnPaste);
            urlPanel.Controls.Add(comboBox1);
            urlPanel.Controls.Add(btnClear);
            urlPanel.Location = new Point(10, 100);
            urlPanel.Name = "urlPanel";
            urlPanel.Size = new Size(850, 50);
            urlPanel.TabIndex = 2;
            // 
            // label1
            // 
            label1.Location = new Point(10, 0);
            label1.Name = "label1";
            label1.Size = new Size(237, 23);
            label1.TabIndex = 3;
            label1.Text = "🌐 URL or HTLM FILE PATH INPUT SECTION ";
            // 
            // txtUrl
            // 
            txtUrl.Location = new Point(10, 24);
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(565, 23);
            txtUrl.TabIndex = 0;
            // 
            // btnPaste
            // 
            btnPaste.Location = new Point(581, 3);
            btnPaste.Name = "btnPaste";
            btnPaste.Size = new Size(75, 23);
            btnPaste.TabIndex = 1;
            btnPaste.Text = "Paste";
            // 
            // comboBox1
            // 
            comboBox1.Location = new Point(743, 3);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(104, 23);
            comboBox1.TabIndex = 3;
            comboBox1.Text = "History";
            // 
            // btnClear
            // 
            btnClear.Location = new Point(662, 2);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(75, 23);
            btnClear.TabIndex = 2;
            btnClear.Text = "Clear";
            // 
            // settingsPanel
            // 
            settingsPanel.Controls.Add(chkJs);
            settingsPanel.Controls.Add(chkAntiBlock);
            settingsPanel.Controls.Add(cmbDelay);
            settingsPanel.Controls.Add(cmbTimeout);
            settingsPanel.Location = new Point(10, 150);
            settingsPanel.Name = "settingsPanel";
            settingsPanel.Size = new Size(850, 50);
            settingsPanel.TabIndex = 3;
            // 
            // chkJs
            // 
            chkJs.Location = new Point(0, 0);
            chkJs.Name = "chkJs";
            chkJs.Size = new Size(104, 24);
            chkJs.TabIndex = 0;
            chkJs.Text = "Use JavaScript";
            // 
            // chkAntiBlock
            // 
            chkAntiBlock.Location = new Point(150, 0);
            chkAntiBlock.Name = "chkAntiBlock";
            chkAntiBlock.Size = new Size(104, 24);
            chkAntiBlock.TabIndex = 1;
            chkAntiBlock.Text = "Anti-Block Mode";
            // 
            // cmbDelay
            // 
            cmbDelay.Location = new Point(350, 0);
            cmbDelay.Name = "cmbDelay";
            cmbDelay.Size = new Size(120, 23);
            cmbDelay.TabIndex = 2;
            // 
            // cmbTimeout
            // 
            cmbTimeout.Location = new Point(500, 0);
            cmbTimeout.Name = "cmbTimeout";
            cmbTimeout.Size = new Size(120, 23);
            cmbTimeout.TabIndex = 3;
            // 
            // selectorPanel
            // 
            selectorPanel.Controls.Add(txtCss);
            selectorPanel.Controls.Add(txtXPath);
            selectorPanel.Controls.Add(btnTest);
            selectorPanel.Controls.Add(btnAuto);
            selectorPanel.Location = new Point(10, 210);
            selectorPanel.Name = "selectorPanel";
            selectorPanel.Size = new Size(850, 70);
            selectorPanel.TabIndex = 4;
            // 
            // txtCss
            // 
            txtCss.Location = new Point(0, 0);
            txtCss.Name = "txtCss";
            txtCss.PlaceholderText = "CSS Selector";
            txtCss.Size = new Size(400, 23);
            txtCss.TabIndex = 0;
            // 
            // txtXPath
            // 
            txtXPath.Location = new Point(0, 35);
            txtXPath.Name = "txtXPath";
            txtXPath.PlaceholderText = "XPath Selector";
            txtXPath.Size = new Size(400, 23);
            txtXPath.TabIndex = 1;
            // 
            // btnTest
            // 
            btnTest.Location = new Point(420, 0);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(75, 23);
            btnTest.TabIndex = 2;
            btnTest.Text = "Test";
            // 
            // btnAuto
            // 
            btnAuto.Location = new Point(500, 0);
            btnAuto.Name = "btnAuto";
            btnAuto.Size = new Size(75, 23);
            btnAuto.TabIndex = 3;
            btnAuto.Text = "Auto";
            // 
            // actionPanel
            // 
            actionPanel.Controls.Add(btnScrape);
            actionPanel.Controls.Add(btnStop);
            actionPanel.Controls.Add(btnPause);
            actionPanel.Controls.Add(btnRetry);
            actionPanel.Controls.Add(btnExport);
            actionPanel.Location = new Point(10, 286);
            actionPanel.Name = "actionPanel";
            actionPanel.Size = new Size(850, 40);
            actionPanel.TabIndex = 5;
            // 
            // btnScrape
            // 
            btnScrape.Location = new Point(0, 0);
            btnScrape.Name = "btnScrape";
            btnScrape.Size = new Size(75, 23);
            btnScrape.TabIndex = 0;
            btnScrape.Text = "SCRAPE";
            btnScrape.Click += btnScrape_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(100, 0);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(75, 23);
            btnStop.TabIndex = 1;
            btnStop.Text = "STOP";
            // 
            // btnPause
            // 
            btnPause.Location = new Point(200, 0);
            btnPause.Name = "btnPause";
            btnPause.Size = new Size(75, 23);
            btnPause.TabIndex = 2;
            btnPause.Text = "PAUSE";
            // 
            // btnRetry
            // 
            btnRetry.Location = new Point(300, 0);
            btnRetry.Name = "btnRetry";
            btnRetry.Size = new Size(75, 23);
            btnRetry.TabIndex = 3;
            btnRetry.Text = "RETRY";
            // 
            // btnExport
            // 
            btnExport.Location = new Point(400, 0);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(75, 23);
            btnExport.TabIndex = 4;
            btnExport.Text = "EXPORT";
            // 
            // resultGrid
            // 
            resultGrid.Location = new Point(10, 340);
            resultGrid.Name = "resultGrid";
            resultGrid.Size = new Size(850, 180);
            resultGrid.TabIndex = 6;
            // 
            // logPanel
            // 
            logPanel.Controls.Add(lblStatus);
            logPanel.Controls.Add(progressBar);
            logPanel.Controls.Add(txtLogs);
            logPanel.Location = new Point(10, 530);
            logPanel.Name = "logPanel";
            logPanel.Size = new Size(850, 120);
            logPanel.TabIndex = 7;
            // 
            // lblStatus
            // 
            lblStatus.Location = new Point(0, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(100, 23);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Status: READY";
            // 
            // progressBar
            // 
            progressBar.Location = new Point(0, 25);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(300, 23);
            progressBar.TabIndex = 1;
            // 
            // txtLogs
            // 
            txtLogs.Location = new Point(0, 50);
            txtLogs.Multiline = true;
            txtLogs.Name = "txtLogs";
            txtLogs.Size = new Size(800, 60);
            txtLogs.TabIndex = 2;
            // 
            // mediaPanel
            // 
            mediaPanel.Location = new Point(10, 100);
            mediaPanel.Name = "mediaPanel";
            mediaPanel.Size = new Size(850, 250);
            mediaPanel.TabIndex = 8;
            mediaPanel.Visible = false;
            // 
            // elementPanel
            // 
            elementPanel.Location = new Point(10, 100);
            elementPanel.Name = "elementPanel";
            elementPanel.Size = new Size(850, 250);
            elementPanel.TabIndex = 9;
            elementPanel.Visible = false;
            // 
            // Form1
            // 
            ClientSize = new Size(884, 681);
            Controls.Add(panelhtml);
            Controls.Add(actionPanel);
            Controls.Add(topBar);
            Controls.Add(modePanel);
            Controls.Add(urlPanel);
            Controls.Add(settingsPanel);
            Controls.Add(selectorPanel);
            Controls.Add(resultGrid);
            Controls.Add(logPanel);
            Controls.Add(mediaPanel);
            Controls.Add(elementPanel);
            Name = "Form1";
            Text = "WebScraper Suite Pro";
            topBar.ResumeLayout(false);
            panelhtml.ResumeLayout(false);
            panelhtml.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            modePanel.ResumeLayout(false);
            urlPanel.ResumeLayout(false);
            urlPanel.PerformLayout();
            settingsPanel.ResumeLayout(false);
            selectorPanel.ResumeLayout(false);
            selectorPanel.PerformLayout();
            actionPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)resultGrid).EndInit();
            logPanel.ResumeLayout(false);
            logPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel topBar;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblStatusConn;
        private System.Windows.Forms.Button btnSettings;

        private System.Windows.Forms.Panel modePanel;
        private System.Windows.Forms.RadioButton rbAdvanced;
        private System.Windows.Forms.RadioButton rbMedia;
        private System.Windows.Forms.RadioButton rbPicker;

        private System.Windows.Forms.Panel urlPanel;
        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.Button btnPaste;
        private System.Windows.Forms.Button btnClear;

        private System.Windows.Forms.Panel settingsPanel;
        private System.Windows.Forms.CheckBox chkJs;
        private System.Windows.Forms.CheckBox chkAntiBlock;
        private System.Windows.Forms.ComboBox cmbDelay;
        private System.Windows.Forms.ComboBox cmbTimeout;

        private System.Windows.Forms.Panel selectorPanel;
        private System.Windows.Forms.TextBox txtCss;
        private System.Windows.Forms.TextBox txtXPath;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Button btnAuto;

        private System.Windows.Forms.Panel actionPanel;
        private System.Windows.Forms.Button btnScrape;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnRetry;
        private System.Windows.Forms.Button btnExport;

        private System.Windows.Forms.DataGridView resultGrid;

        private System.Windows.Forms.Panel logPanel;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox txtLogs;

        private System.Windows.Forms.Panel mediaPanel;
        private System.Windows.Forms.Panel elementPanel;
        private RadioButton rbhtml;
        private Label label1;
        private ComboBox comboBox1;
        private Panel panelhtml;
        private ComboBox cmbExportType;
        private ComboBox cmbSiteType;
        private ComboBox cmbInputType;
        private Label label2;
        private DataGridView dataGridView1;
        private Button btnExport1;
        private Button btnExtract;
        private ProgressBar progressBar1;
        private TextBox txtInput;
        private Label label5;
        private Label label4;
        private Label label3;
        private TextBox txtOutput;
        private Label label6;
        private Button btnLoad;
    }
}