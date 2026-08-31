using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

namespace adan
{
    public partial class Form1 : Form
    {
        [DllImport("coredll.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("CoreDll.DLL", EntryPoint = "PlaySound", SetLastError = true)]
        private static extern int PlaySoundCE(string szSound, IntPtr hMod, int flags);

        private const int SND_ASYNC = 0x0001;
        private const int SND_FILENAME = 0x20000;
        private const int SND_PURGE = 0x0040;
        private const int SND_LOOP = 0x0008;

        private static readonly string[] NAMES = { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };
        private const int FORM_W = 480;
        private const int FORM_H = 640;

        private string _dateStr = "";
        private string _nextStr = "";
        private bool _muted = false;
        private int _lastIdx = -1;
        private DateTime _lastCalcDate = DateTime.MinValue;
        private bool _preFajrFired = false;

        private PrayerCalculator _calc;
        private PrayerTime[] _prayers;
        private System.Windows.Forms.Timer _ticker;
        private string _appFolder = "";
        private string _wavPath = "";
        private Bitmap _homeImage;
        private Bitmap _muteImage;

        public Form1()
        {
            AppSettings.Load();
            _calc = new PrayerCalculator();
            _appFolder = ResolveAppFolder();
            _wavPath = Path.Combine(_appFolder, "adhan.wav");

            LoadAssets();
            BuildUI();
            RefreshPrayers();

            RegisterHotKey(this.Handle, 1001, 0, 0x70); // F1
            RegisterHotKey(this.Handle, 1002, 0, 0x71); // F2
            RegisterHotKey(this.Handle, 1003, 0, 0x72); // F3

            _ticker = new System.Windows.Forms.Timer();
            _ticker.Interval = 15000;
            _ticker.Tick += new EventHandler(OnTick);
            _ticker.Enabled = true;
        }

        private void LoadAssets()
        {
            try
            {
                string homePath = Path.Combine(_appFolder, "home.jpg");
                if (File.Exists(homePath)) _homeImage = new Bitmap(homePath);

                string mutePath = Path.Combine(_appFolder, "mute.jpg");
                if (File.Exists(mutePath)) _muteImage = new Bitmap(mutePath);
            }
            catch { }
        }

        private void BuildUI()
        {
            this.Text = "Prayer Times";
            this.ClientSize = new Size(FORM_W, FORM_H);
            this.Menu = null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_homeImage != null) e.Graphics.DrawImage(_homeImage, 0, 0);
            if (_prayers == null) return;

            // Mute color logic: Red if muted, Black if active
            Color statusColor = _muted ? Color.Red : Color.LightGreen;

            using (Font timeFont = new Font("Tahoma", 12, FontStyle.Regular))
            using (Font statusFont = new Font("Tahoma", 10, FontStyle.Regular))
            using (Font dateFont = new Font("Tahoma", 10, FontStyle.Regular))
            using (Brush blackBrush = new SolidBrush(Color.White))
            using (Brush alertBrush = new SolidBrush(statusColor))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;

                e.Graphics.DrawString(_dateStr, dateFont, blackBrush, new RectangleF(0, 96, FORM_W, 40), sf);

                // This text will turn RED when muted
                e.Graphics.DrawString(_nextStr, statusFont, alertBrush, new RectangleF(0, 140, FORM_W, 40), sf);

                for (int i = 0; i < 6; i++)
                {
                    string timeStr = _prayers[i].Time.ToString("HH:mm");
                    e.Graphics.DrawString(timeStr, timeFont, blackBrush, 30, 190 + (i * 56));
                }

                // Visual Icon/Square in the top left corner (X:20, Y:130)
                if (_muted)
                {
                    if (_muteImage != null)
                        e.Graphics.DrawImage(_muteImage, 8, 12);
                    else
                        e.Graphics.FillRectangle(alertBrush, 20, 130, 24, 24);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.F1) OpenSettings();
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.F2) ToggleMute();
            base.OnKeyDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.X < 80 && e.Y < 80)
            {
                ToggleMute();
            }
            else if (e.Y >= 460)
            {
                OpenSettings();
            }

            base.OnMouseUp(e);
        }

        private void OpenSettings()
        {
            using (SettingsForm sf = new SettingsForm(this))
            {
                if (sf.ShowDialog() == DialogResult.OK) RefreshPrayers();
            }
        }

        private void ToggleMute()
        {
            _muted = !_muted;
            this.Invalidate();
        }

        public void TestSound() { PlayAdhan(); }

        private void PlayAdhan()
        {
            if (!File.Exists(_wavPath)) return;
            PlaySoundCE(null, IntPtr.Zero, SND_PURGE);
            PlaySoundCE(_wavPath, IntPtr.Zero, SND_ASYNC | SND_FILENAME);
        }

        private void PlayPreFajrSound()
        {
            // Optional distinct reminder sound, looped continuously (short beep files
            // repeat until StopSound() is called). Falls back to a single adhan.wav
            // play if no separate reminder sound is provided.
            string preFajrPath = Path.Combine(_appFolder, "prefajr.wav");
            PlaySoundCE(null, IntPtr.Zero, SND_PURGE);
            if (File.Exists(preFajrPath))
                PlaySoundCE(preFajrPath, IntPtr.Zero, SND_ASYNC | SND_LOOP | SND_FILENAME);
            else if (File.Exists(_wavPath))
                PlaySoundCE(_wavPath, IntPtr.Zero, SND_ASYNC | SND_FILENAME);
        }

        private void StopSound()
        {
            PlaySoundCE(null, IntPtr.Zero, SND_PURGE);
        }

        private void OnTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            // Recalculate at day rollover so tomorrow's Fajr/pre-Fajr alarms fire correctly.
            if (now.Date != _lastCalcDate)
            {
                _lastCalcDate = now.Date;
                _prayers = _calc.Calculate(now);
                _lastIdx = -1;
                _preFajrFired = false;
            }

            // Fajr alert offset: negative = before Fajr, positive = after Fajr, 0 = off
            if (AppSettings.PreFajrAlarmMinutes != 0 && !_preFajrFired)
            {
                DateTime alarmTime = _prayers[0].Time.AddMinutes(AppSettings.PreFajrAlarmMinutes);
                double preDiff = (now - alarmTime).TotalMinutes;
                if (preDiff >= 0.0 && preDiff < 1.0)
                {
                    _preFajrFired = true;
                    bool isBefore = AppSettings.PreFajrAlarmMinutes < 0;
                    int mins = Math.Abs(AppSettings.PreFajrAlarmMinutes);

                    if (!_muted) PlayPreFajrSound();
                    using (AdhanPopup pop = new AdhanPopup(
                        isBefore ? "Pre-Fajr Reminder" : "Post-Fajr Reminder",
                        "Fajr at " + _prayers[0].Time.ToString("HH:mm"),
                        isBefore ? mins + " min to go - time to prepare" : mins + " min since Fajr"))
                    { pop.ShowDialog(); }
                    StopSound();
                }
            }

            for (int i = 0; i < 6; i++)
            {
                if (i == 1) continue;
                double diff = (now - _prayers[i].Time).TotalMinutes;
                if (diff >= 0.0 && diff < 1.0 && _lastIdx != i)
                {
                    _lastIdx = i;
                    if (!_muted) PlayAdhan();
                    using (AdhanPopup pop = new AdhanPopup(NAMES[i])) { pop.ShowDialog(); }
                }
            }
            UpdateUI();
        }

        private void RefreshPrayers()
        {
            _prayers = _calc.Calculate(DateTime.Now);
            _lastCalcDate = DateTime.Now.Date;
            _lastIdx = -1;
            _preFajrFired = false;
            UpdateUI();
        }

        private void UpdateUI()
        {
            DateTime now = DateTime.Now;
            _dateStr = now.ToString("ddd, MMM d yyyy");
            int idx = -1;
            for (int i = 0; i < 6; i++) if (_prayers[i].Time > now) { idx = i; break; }

            if (idx >= 0)
            {
                TimeSpan d = _prayers[idx].Time - now;
                _nextStr = "Next: " + NAMES[idx] + " in " + d.Hours + "h " + d.Minutes + "m";
            }
            this.Invalidate();
        }

        private static string ResolveAppFolder()
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (folder.StartsWith("file:///")) return folder.Substring(8).Replace('/', '\\');
            return folder;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(FORM_W, FORM_H);
            this.Name = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
