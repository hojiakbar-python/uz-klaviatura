// Tray ilovasi: "Yangi alifbo yordamçisi".
// Interfeys matnlari ataylab yangi alifboda yozilgan.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UzKlaviatura
{
    static class Program
    {
        const string SettingsKey = @"Software\UzKlaviatura";

        static KeyboardHook hook;
        static NotifyIcon tray;
        static ToolStripMenuItem enabledItem, onlyUzbekItem;
        static Icon iconOn, iconOff;

        [STAThread]
        static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, @"Local\UzKlaviatura.Yordamchi", out created))
            {
                if (!created) return;   // allaqachon ishlayapti

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                hook = new KeyboardHook();
                hook.Enabled = ReadSetting("Enabled", true);
                hook.OnlyUzbekLayout = ReadSetting("OnlyUzbekLayout", true);

                iconOn = MakeIcon(Color.FromArgb(0, 135, 90));
                iconOff = MakeIcon(Color.FromArgb(130, 130, 130));

                enabledItem = new ToolStripMenuItem("Avto-almaştiriş (sh → ş, o' → ö)", null, delegate { SetEnabled(!hook.Enabled); });
                onlyUzbekItem = new ToolStripMenuItem("Faqat Özbek klaviaturasida", null, delegate
                {
                    hook.OnlyUzbekLayout = !hook.OnlyUzbekLayout;
                    WriteSetting("OnlyUzbekLayout", hook.OnlyUzbekLayout);
                    UpdateUi();
                });

                var menu = new ContextMenuStrip();
                menu.Items.Add(enabledItem);
                menu.Items.Add(onlyUzbekItem);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Buferdagi matnni yangi alifboga ögiriş", null, delegate { ConvertClipboard(); });
                menu.Items.Add("Yordam", null, delegate { ShowHelp(); });
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Çiqiş", null, delegate { Application.Exit(); });

                tray = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
                tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) SetEnabled(!hook.Enabled); };
                UpdateUi();

                Application.Run();

                tray.Visible = false;
                tray.Dispose();
                hook.Dispose();
            }
        }

        static void SetEnabled(bool value)
        {
            hook.Enabled = value;
            WriteSetting("Enabled", value);
            UpdateUi();
        }

        static void UpdateUi()
        {
            enabledItem.Checked = hook.Enabled;
            onlyUzbekItem.Checked = hook.OnlyUzbekLayout;
            tray.Icon = hook.Enabled ? iconOn : iconOff;
            tray.Text = hook.Enabled ? "Yangi alifbo yordamçisi: yoqilgan" : "Yangi alifbo yordamçisi: öçirilgan";
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

        static void ShowHelp()
        {
            MessageBox.Show(
                "Odatdagidek yozavering, dastur özi almaştiradi:\n" +
                "   sh → ş     ch → ç     o' → ö     g' → ğ     ma'no → maʼno\n\n" +
                "Almaştiriş kerak bölmasa, darhol Backspace bosing: \"sh\" qaytadi.\n" +
                "Tray belgisini bosib, yordamçini yoqiş/öçiriş mumkin.\n\n" +
                "Klaviaturaning özida ham yangi harflar bor:\n" +
                "   AltGr + o / g / s / c  →  ö / ğ / ş / ç\n" +
                "   AltGr + '  →  ʼ (tutuq)\n" +
                "   \"Özbek (yangi alifbo)\" tartibida:  [ → ö   ] → ğ   ; → ş   \\ → ç",
                "Yangi alifbo yordamçisi", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        static bool ReadSetting(string name, bool fallback)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(SettingsKey))
            {
                object value = key == null ? null : key.GetValue(name);
                return value is int ? (int)value != 0 : fallback;
            }
        }

        static void WriteSetting(string name, bool value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(SettingsKey))
                key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
        }
    }
}
