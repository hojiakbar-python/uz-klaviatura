// Tray ilovasi: "Yangi alifbo yordamçisi".
// Interfeys matnlari ataylab yangi alifboda yozilgan.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace UzKlaviatura
{
    static class Program
    {
        public const string Version = "0.2.0";

        static Settings settings;
        static KeyboardHook hook;
        static LearningCoach coach;
        static HotkeyManager hotkey;
        static NotifyIcon tray;
        static SettingsForm settingsForm;
        static ToolStripMenuItem enabledItem, appItem;
        static Icon iconOn, iconOff;

        [STAThread]
        static void Main(string[] args)
        {
            bool created;
            using (var mutex = new Mutex(true, @"Local\UzKlaviatura.Yordamchi", out created))
            {
                if (!created) return;   // allaqachon ishlayapti

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var ui = new WindowsFormsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(ui);

                settings = Settings.Load();
                coach = new LearningCoach();
                hook = new KeyboardHook();
                hotkey = new HotkeyManager();
                ApplySettings();

                // Ilmoq ichida og'ir ish qilinmaydi: hisob va maslahat keyinroq, UI navbatida.
                hook.Converted += text => ui.Post(_ => coach.OnConverted(text, settings.LearningMode), null);
                coach.Tip += (title, text) => tray.ShowBalloonTip(6000, title, text, ToolTipIcon.Info);
                hotkey.Pressed += delegate
                {
                    SetEnabled(!settings.Enabled);
                    tray.ShowBalloonTip(1500, "Yangi alifbo yordamçisi",
                        settings.Enabled ? "Avto-almaştiriş yoqildi." : "Avto-almaştiriş öçirildi.", ToolTipIcon.None);
                };

                iconOn = MakeIcon(Color.FromArgb(0, 135, 90));
                iconOff = MakeIcon(Color.FromArgb(130, 130, 130));

                enabledItem = new ToolStripMenuItem("Avto-almaştiriş", null, delegate { SetEnabled(!settings.Enabled); });
                appItem = new ToolStripMenuItem("", null, delegate { ToggleLastApp(); });
                var settingsItem = new ToolStripMenuItem("Sozlamalar...", null, delegate { ShowSettings(SettingsForm.TabGeneral); });
                settingsItem.Font = new Font(settingsItem.Font, FontStyle.Bold);

                var menu = new ContextMenuStrip();
                menu.Items.Add(enabledItem);
                menu.Items.Add(appItem);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Buferdagi matnni yangi alifboga ögiriş", null, delegate { ConvertClipboard(); });
                menu.Items.Add("Fayllarni ögiriş...", null, delegate { ShowSettings(SettingsForm.TabFiles); });
                menu.Items.Add("Statistika", null, delegate { ShowSettings(SettingsForm.TabStats); });
                menu.Items.Add(settingsItem);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Çiqiş", null, delegate { Application.Exit(); });
                menu.Opening += delegate { UpdateAppItem(); };

                tray = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
                tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) SetEnabled(!settings.Enabled); };
                UpdateUi();
                if (args.Contains("--settings")) ShowSettings(SettingsForm.TabGeneral);

                Application.Run();

                coach.Save();
                tray.Visible = false;
                tray.Dispose();
                hotkey.Dispose();
                hook.Dispose();
            }
        }

        static void ApplySettings()
        {
            hook.Enabled = settings.Enabled;
            hook.OnlyUzbekLayout = settings.OnlyUzbekLayout;
            hook.ExcludedApps = new System.Collections.Generic.HashSet<string>(settings.ExcludedApps, StringComparer.OrdinalIgnoreCase);
            bool ok = hotkey.Register(settings.Hotkey);
            if (settingsForm != null) settingsForm.ShowHotkeyResult(ok);
        }

        static void SetEnabled(bool value)
        {
            settings.Enabled = value;
            settings.Save();
            ApplySettings();
            UpdateUi();
            if (settingsForm != null) settingsForm.SyncFromSettings();
        }

        static void UpdateUi()
        {
            enabledItem.Checked = settings.Enabled;
            tray.Icon = settings.Enabled ? iconOn : iconOff;
            string hk = settings.Hotkey == Keys.None ? "" : " (" + Settings.FormatHotkey(settings.Hotkey) + ")";
            tray.Text = (settings.Enabled ? "Yangi alifbo yordamçisi: yoqilgan" : "Yangi alifbo yordamçisi: öçirilgan") + hk;
        }

        static void UpdateAppItem()
        {
            string app = hook.LastAppName;
            appItem.Visible = app != null;
            if (app == null) return;
            appItem.Text = settings.ExcludedApps.Contains(app)
                ? "«" + app + "» dasturida yana yoqiş"
                : "«" + app + "» dasturida öçiriş";
        }

        static void ToggleLastApp()
        {
            string app = hook.LastAppName;
            if (app == null) return;
            if (!settings.ExcludedApps.Remove(app)) settings.ExcludedApps.Add(app);
            settings.Save();
            ApplySettings();
            if (settingsForm != null) settingsForm.SyncFromSettings();
        }

        static void ShowSettings(int tab)
        {
            if (settingsForm == null)
            {
                settingsForm = new SettingsForm(settings, coach, Version);
                settingsForm.SettingsChanged += delegate { ApplySettings(); UpdateUi(); };
            }
            settingsForm.ShowTab(tab);
            settingsForm.Show();
            if (settingsForm.WindowState == FormWindowState.Minimized) settingsForm.WindowState = FormWindowState.Normal;
            settingsForm.Activate();
        }

        static void ConvertClipboard()
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    tray.ShowBalloonTip(3000, "Bufer böş", "Avval matnni nusxalang (Ctrl+C).", ToolTipIcon.Info);
                    return;
                }
                Clipboard.SetText(Imlo.Convert(Clipboard.GetText()));
                tray.ShowBalloonTip(3000, "Tayyor", "Matn yangi alifboga ögirildi. Endi Ctrl+V bilan qöying.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                tray.ShowBalloonTip(3000, "Xato", ex.Message, ToolTipIcon.Error);
            }
        }

        static Icon MakeIcon(Color background)
        {
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            using (var brush = new SolidBrush(background))
            using (var font = new Font("Segoe UI", 17, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.FillEllipse(brush, 1, 1, 30, 30);
                var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Ş", font, Brushes.White, new RectangleF(0, -1, 32, 32), format);
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
    }
}
