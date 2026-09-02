using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace adan
{
    public static class AppSettings
    {
        public static string CityName = "Oued Zem";
        public static int TimeOffsetHours = 1;

        public static int AdjFajr = 0;
        public static int AdjSunrise = 0;
        public static int AdjDhuhr = 0;
        public static int AdjAsr = 0;
        public static int AdjMaghrib = 0;
        public static int AdjIsha = 0;

        public static int PreFajrAlarmMinutes = 0;

        private static string CfgPath
        {
            get
            {
                string f = ResolveAppFolder();
                return f + "\\adan.cfg";
            }
        }

        private static string ResolveAppFolder()
        {
            string folder = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().GetName().CodeBase);

            if (folder == null) return "";
            if (folder.StartsWith("file:///"))
                return folder.Substring(8).Replace('/', '\\');
            if (folder.StartsWith("file:\\"))
                return folder.Substring(6);
            return folder;
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(CfgPath)) return;

                StreamReader sr = new StreamReader(CfgPath);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    int eq = line.IndexOf('=');
                    if (eq < 1) continue;

                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();

                    switch (k)
                    {
                        case "City": CityName = v; break;
                        case "TZ": TimeOffsetHours = int.Parse(v); break;
                        case "AdjFajr": AdjFajr = int.Parse(v); break;
                        case "AdjSunrise": AdjSunrise = int.Parse(v); break;
                        case "AdjDhuhr": AdjDhuhr = int.Parse(v); break;
                        case "AdjAsr": AdjAsr = int.Parse(v); break;
                        case "AdjMaghrib": AdjMaghrib = int.Parse(v); break;
                        case "AdjIsha": AdjIsha = int.Parse(v); break;
                        case "PreFajrAlarm": PreFajrAlarmMinutes = int.Parse(v); break;
                    }
                }
                sr.Close();
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                StreamWriter sw = new StreamWriter(CfgPath, false);
                sw.WriteLine("City=" + CityName);
                sw.WriteLine("TZ=" + TimeOffsetHours);
                sw.WriteLine("AdjFajr=" + AdjFajr);
                sw.WriteLine("AdjSunrise=" + AdjSunrise);
                sw.WriteLine("AdjDhuhr=" + AdjDhuhr);
                sw.WriteLine("AdjAsr=" + AdjAsr);
                sw.WriteLine("AdjMaghrib=" + AdjMaghrib);
                sw.WriteLine("AdjIsha=" + AdjIsha);
                sw.WriteLine("PreFajrAlarm=" + PreFajrAlarmMinutes);
                sw.Close();
            }
            catch { }
        }

        public static int[] GetAdjustments()
        {
            return new int[] { AdjFajr, AdjSunrise, AdjDhuhr, AdjAsr, AdjMaghrib, AdjIsha };
        }
    }

    public class SettingsForm : Form
    {
        private static readonly string[] PRAYER_NAMES = { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };

        private Button btnGMT;
        private Label lblGMTStatus;
        private TextBox[] adjBoxes = new TextBox[6];
        private TextBox preFajrBox;
        private Button btnSave;
        private Button btnCancel;
        private Button btnTest;
        private Button btnAlarm;
        private Form1 _main;

        public SettingsForm(Form1 main)
        {
            _main = main;
            this.Text = "Settings";
            this.BackColor = SystemColors.Control;

            this.ClientSize = new Size(240, 280);

            // 1. Timezone Section
            Label lGMT = new Label();
            lGMT.Text = "Timezone:";
            lGMT.Font = new Font("Tahoma", 8, FontStyle.Regular);
            lGMT.Location = new Point(5, 12);
            lGMT.Size = new Size(60, 18);
            this.Controls.Add(lGMT);

            lblGMTStatus = new Label();
            lblGMTStatus.Font = new Font("Tahoma", 8, FontStyle.Bold);
            lblGMTStatus.Location = new Point(65, 12);
            lblGMTStatus.Size = new Size(70, 18);
            this.Controls.Add(lblGMTStatus);

            btnGMT = new Button();
            btnGMT.Font = new Font("Tahoma", 7, FontStyle.Regular);
            btnGMT.Location = new Point(145, 8);
            btnGMT.Size = new Size(85, 24);
            btnGMT.Click += new EventHandler(OnToggleGMT);
            this.Controls.Add(btnGMT);
            UpdateGMTLabel();

            // Separator 1
            Panel sep = new Panel();
            sep.Location = new Point(5, 38);
            sep.Size = new Size(230, 1);
            sep.BackColor = Color.FromArgb(180, 185, 200);
            this.Controls.Add(sep);

            // 2. Adjustments Header
            Label lAdj = new Label();
            lAdj.Text = "Manual adjustments (minutes, +/-)";
            lAdj.Font = new Font("Tahoma", 7, FontStyle.Regular);
            lAdj.ForeColor = Color.FromArgb(80, 80, 80);
            lAdj.Location = new Point(5, 44);
            lAdj.Size = new Size(230, 14);
            this.Controls.Add(lAdj);

            // 3. Prayer Adjustments List
            int[] curAdj = AppSettings.GetAdjustments();
            for (int i = 0; i < 6; i++)
            {
                int yPos = 60 + (i * 23);

                Label lName = new Label();
                lName.Text = PRAYER_NAMES[i] + ":";
                lName.Font = new Font("Tahoma", 8, FontStyle.Regular);
                lName.Location = new Point(5, yPos + 2);
                lName.Size = new Size(55, 18);
                this.Controls.Add(lName);

                adjBoxes[i] = new TextBox();
                adjBoxes[i].Text = curAdj[i].ToString();
                adjBoxes[i].Font = new Font("Tahoma", 8, FontStyle.Regular);
                adjBoxes[i].Location = new Point(60, yPos);
                adjBoxes[i].Size = new Size(32, 20);
                this.Controls.Add(adjBoxes[i]);

                Label lMin = new Label();
                lMin.Text = "min";
                lMin.Font = new Font("Tahoma", 7, FontStyle.Regular);
                lMin.ForeColor = Color.Gray;
                lMin.Location = new Point(94, yPos + 3);
                lMin.Size = new Size(24, 14);
                this.Controls.Add(lMin);

                Button bMinus = new Button();
                bMinus.Text = "-";
                bMinus.Font = new Font("Tahoma", 8, FontStyle.Bold);
                bMinus.Tag = i;
                bMinus.Location = new Point(130, yPos - 1);
                bMinus.Size = new Size(45, 22);
                bMinus.Click += new EventHandler(OnMinus);
                this.Controls.Add(bMinus);

                Button bPlus = new Button();
                bPlus.Text = "+";
                bPlus.Font = new Font("Tahoma", 8, FontStyle.Bold);
                bPlus.Tag = i;
                bPlus.Location = new Point(185, yPos - 1);
                bPlus.Size = new Size(45, 22);
                bPlus.Click += new EventHandler(OnPlus);
                this.Controls.Add(bPlus);
            }

            // Separator 2
            Panel sep2 = new Panel();
            sep2.Location = new Point(5, 202);
            sep2.Size = new Size(230, 1);
            sep2.BackColor = Color.FromArgb(180, 185, 200);
            this.Controls.Add(sep2);

            // 4. Pre-Fajr Section
            Label lPreFajrHead = new Label();
            lPreFajrHead.Text = "Fajr offset (min): - before, + after, 0 off";
            lPreFajrHead.Font = new Font("Tahoma", 6, FontStyle.Regular);
            lPreFajrHead.ForeColor = Color.FromArgb(80, 80, 80);
            lPreFajrHead.Location = new Point(5, 208);
            lPreFajrHead.Size = new Size(230, 12);
            this.Controls.Add(lPreFajrHead);

            Label lPreFajr = new Label();
            lPreFajr.Text = "Alert:";
            lPreFajr.Font = new Font("Tahoma", 7, FontStyle.Regular);
            lPreFajr.Location = new Point(5, 224);
            lPreFajr.Size = new Size(35, 16);
            this.Controls.Add(lPreFajr);

            preFajrBox = new TextBox();
            preFajrBox.Text = AppSettings.PreFajrAlarmMinutes.ToString();
            preFajrBox.Font = new Font("Tahoma", 8, FontStyle.Regular);
            preFajrBox.Location = new Point(60, 222);
            preFajrBox.Size = new Size(32, 20);
            this.Controls.Add(preFajrBox);

            Label lPreFajrMin = new Label();
            lPreFajrMin.Text = "min";
            lPreFajrMin.Font = new Font("Tahoma", 7, FontStyle.Regular);
            lPreFajrMin.ForeColor = Color.Gray;
            lPreFajrMin.Location = new Point(94, 225);
            lPreFajrMin.Size = new Size(24, 14);
            this.Controls.Add(lPreFajrMin);

            Button bPreFajrMinus = new Button();
            bPreFajrMinus.Text = "-";
            bPreFajrMinus.Font = new Font("Tahoma", 8, FontStyle.Bold);
            bPreFajrMinus.Location = new Point(130, 221);
            bPreFajrMinus.Size = new Size(45, 22);
            bPreFajrMinus.Click += new EventHandler(OnPreFajrMinus);
            this.Controls.Add(bPreFajrMinus);

            Button bPreFajrPlus = new Button();
            bPreFajrPlus.Text = "+";
            bPreFajrPlus.Font = new Font("Tahoma", 8, FontStyle.Bold);
            bPreFajrPlus.Location = new Point(185, 221);
            bPreFajrPlus.Size = new Size(45, 22);
            bPreFajrPlus.Click += new EventHandler(OnPreFajrPlus);
            this.Controls.Add(bPreFajrPlus);

            // Separator 3
            Panel sep3 = new Panel();
            sep3.Location = new Point(5, 248);
            sep3.Size = new Size(230, 1);
            sep3.BackColor = Color.FromArgb(180, 185, 200);
            this.Controls.Add(sep3);

            // 5. Action Buttons (4 buttons evenly spaced across 240px)
            btnTest = new Button();
            btnTest.Text = "Adan";
            btnTest.Font = new Font("Tahoma", 8, FontStyle.Regular);
            btnTest.Location = new Point(4, 252);
            btnTest.Size = new Size(55, 24);
            btnTest.Click += new EventHandler(OnTest);
            this.Controls.Add(btnTest);

            btnAlarm = new Button();
            btnAlarm.Text = "Alarm";
            btnAlarm.Font = new Font("Tahoma", 8, FontStyle.Regular);
            btnAlarm.Location = new Point(63, 252);
            btnAlarm.Size = new Size(55, 24);
            btnAlarm.Click += new EventHandler(OnTestAlarm);
            this.Controls.Add(btnAlarm);

            btnSave = new Button();
            btnSave.Text = "Save";
            btnSave.Font = new Font("Tahoma", 8, FontStyle.Bold);
            btnSave.Location = new Point(122, 252);
            btnSave.Size = new Size(55, 24);
            btnSave.Click += new EventHandler(OnSave);
            this.Controls.Add(btnSave);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Font = new Font("Tahoma", 8, FontStyle.Regular);
            btnCancel.Location = new Point(181, 252);
            btnCancel.Size = new Size(55, 24);
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Click += new EventHandler(OnCancel);
            this.Controls.Add(btnCancel);
        }

        private void UpdateGMTLabel()
        {
            if (AppSettings.TimeOffsetHours == 1)
            {
                lblGMTStatus.Text = "GMT+1 ON";
                lblGMTStatus.ForeColor = Color.FromArgb(0, 100, 0);
                btnGMT.Text = "Turn OFF";
            }
            else
            {
                lblGMTStatus.Text = "GMT+1 OFF";
                lblGMTStatus.ForeColor = Color.FromArgb(180, 0, 0);
                btnGMT.Text = "Turn ON";
            }
        }

        private void OnToggleGMT(object sender, EventArgs e)
        {
            AppSettings.TimeOffsetHours =
                (AppSettings.TimeOffsetHours == 1) ? 0 : 1;
            UpdateGMTLabel();
        }

        private static int SafeParseInt(string s)
        {
            try { return int.Parse(s.Trim()); }
            catch { return 0; }
        }

        private void OnPlus(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            int v = SafeParseInt(adjBoxes[idx].Text);
            adjBoxes[idx].Text = (v + 1).ToString();
        }

        private void OnMinus(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            int v = SafeParseInt(adjBoxes[idx].Text);
            adjBoxes[idx].Text = (v - 1).ToString();
        }

        private void OnPreFajrPlus(object sender, EventArgs e)
        {
            int v = SafeParseInt(preFajrBox.Text) + 10;
            if (v > 120) v = 120;
            preFajrBox.Text = v.ToString();
        }

        private void OnPreFajrMinus(object sender, EventArgs e)
        {
            int v = SafeParseInt(preFajrBox.Text) - 10;
            if (v < -120) v = -120;
            preFajrBox.Text = v.ToString();
        }

        private void OnTest(object sender, EventArgs e)
        {
            _main.TestSound();
        }

        private void OnTestAlarm(object sender, EventArgs e)
        {
            _main.TestAlarmSound();
        }

        private void OnSave(object sender, EventArgs e)
        {
            try
            {
                int[] adj = new int[6];
                for (int i = 0; i < 6; i++)
                    adj[i] = SafeParseInt(adjBoxes[i].Text);

                AppSettings.AdjFajr = adj[0];
                AppSettings.AdjSunrise = adj[1];
                AppSettings.AdjDhuhr = adj[2];
                AppSettings.AdjAsr = adj[3];
                AppSettings.AdjMaghrib = adj[4];
                AppSettings.AdjIsha = adj[5];
                AppSettings.PreFajrAlarmMinutes = SafeParseInt(preFajrBox.Text);
                AppSettings.Save();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch
            {
                MessageBox.Show("Invalid values.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);
            }
        }

        private void OnCancel(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    public class AdhanPopup : Form
    {
        private System.Windows.Forms.Timer _autoClose;

        public AdhanPopup(string prayerName)
            : this("Prayer Time", prayerName + "   " + DateTime.Now.ToString("HH:mm"), "Allahu Akbar")
        {
        }

        public AdhanPopup(string title, string message, string subtitle)
        {
            this.Text = "Adhan";
            this.ClientSize = new Size(220, 120);
            this.BackColor = Color.FromArgb(0, 0, 128);
            this.TopMost = true;
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            Label star = new Label();
            star.Text = "*";
            star.Font = new Font("Tahoma", 22, FontStyle.Bold);
            star.ForeColor = Color.Gold;
            star.Location = new Point(10, 10);
            star.Size = new Size(38, 44);
            this.Controls.Add(star);

            Label l1 = new Label();
            l1.Text = title;
            l1.Font = new Font("Tahoma", 9, FontStyle.Bold);
            l1.ForeColor = Color.White;
            l1.Location = new Point(55, 12);
            l1.Size = new Size(155, 20);
            this.Controls.Add(l1);

            Label l2 = new Label();
            l2.Text = message;
            l2.Font = new Font("Tahoma", 9, FontStyle.Bold);
            l2.ForeColor = Color.Gold;
            l2.Location = new Point(55, 34);
            l2.Size = new Size(155, 22);
            this.Controls.Add(l2);

            Label l3 = new Label();
            l3.Text = subtitle;
            l3.Font = new Font("Tahoma", 7, FontStyle.Regular);
            l3.ForeColor = Color.FromArgb(180, 200, 255);
            l3.Location = new Point(55, 58);
            l3.Size = new Size(155, 18);
            this.Controls.Add(l3);

            Button btnOK = new Button();
            btnOK.Text = "Dismiss";
            btnOK.Font = new Font("Tahoma", 8, FontStyle.Regular);
            btnOK.Location = new Point(70, 85);
            btnOK.Size = new Size(80, 26);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Click += new EventHandler(OnDismiss);
            this.Controls.Add(btnOK);

            _autoClose = new System.Windows.Forms.Timer();
            _autoClose.Interval = 60000;
            _autoClose.Tick += new EventHandler(OnAutoClose);
            _autoClose.Enabled = true;
        }

        private void OnDismiss(object sender, EventArgs e)
        {
            _autoClose.Enabled = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnAutoClose(object sender, EventArgs e)
        {
            _autoClose.Enabled = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (_autoClose != null)
            {
                _autoClose.Enabled = false;
                _autoClose.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}