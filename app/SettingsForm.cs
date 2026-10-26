// Sozlamalar oynasi: umumiy sozlamalar, istisno dasturlar, fayllarni o'tkazish, statistika.
// Har bir o'zgarish darhol saqlanadi va SettingsChanged orqali qo'llanadi.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace UzKlaviatura
{
    public sealed class SettingsForm : Form
    {
        public const int TabGeneral = 0, TabApps = 1, TabFiles = 2, TabStats = 3;

        readonly Settings settings;
        readonly LearningCoach coach;
        public event Action SettingsChanged;

        readonly TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        CheckBox enabledBox, onlyUzbekBox, learningBox, autoStartBox;
        TextBox hotkeyBox;
        Label hotkeyStatus;
        ListBox appsList;
        ComboBox runningApps;
        ListView filesList;
        CheckBox overwriteBox;
        Button convertButton;
        ListView statsList;

        public SettingsForm(Settings settings, LearningCoach coach, string version)
        {
            this.settings = settings;
            this.coach = coach;
            Text = "Yangi alifbo yordamçisi — sozlamalar (v" + version + ")";
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(600, 470);
            MinimumSize = new Size(560, 440);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            tabs.TabPages.Add(BuildGeneral());
            tabs.TabPages.Add(BuildApps());
            tabs.TabPages.Add(BuildFiles());
            tabs.TabPages.Add(BuildStats());
            tabs.SelectedIndexChanged += delegate { if (tabs.SelectedIndex == TabStats) RefreshStats(); };
            Controls.Add(tabs);
        }

        public void ShowTab(int index)
        {
            tabs.SelectedIndex = index;
            if (index == TabStats) RefreshStats();
        }

        // Tray yoki tezkor tugma holatni o'zgartirganda oynani yangilash.
        public void SyncFromSettings()
        {
            enabledBox.Checked = settings.Enabled;
            appsList.Items.Clear();
            foreach (string app in settings.ExcludedApps) appsList.Items.Add(app);
        }

        void Changed()
        {
            settings.Save();
            var handler = SettingsChanged;
            if (handler != null) handler();
        }

        // --- Umumiy ---------------------------------------------------------------

        TabPage BuildGeneral()
        {
            var page = new TabPage("Umumiy") { Padding = new Padding(14) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };

            enabledBox = Check("Avto-almaştiriş: sh → ş, ch → ç, o' → ö, g' → ğ", settings.Enabled,
                v => settings.Enabled = v);
            onlyUzbekBox = Check("Faqat Özbek klaviaturasida işlaş (inglizça va rusça yozuvga tegmaydi)", settings.OnlyUzbekLayout,
                v => settings.OnlyUzbekLayout = v);
            learningBox = Check("Örganiş rejimi: yangi harflarni qaysi tugma bilan yoziş haqida maslahat beriş", settings.LearningMode,
                v => settings.LearningMode = v);
            autoStartBox = new CheckBox { Text = "Windows bilan birga işga tuşiriş", AutoSize = true, Checked = Settings.AutoStart, Margin = new Padding(0, 4, 0, 4) };
            autoStartBox.CheckedChanged += delegate { Settings.AutoStart = autoStartBox.Checked; };

            flow.Controls.Add(enabledBox);
            flow.Controls.Add(onlyUzbekBox);
            flow.Controls.Add(learningBox);
            flow.Controls.Add(autoStartBox);

            flow.Controls.Add(new Label { Text = "Avto-almaştirişni yoqiş/öçiriş uçun tezkor tugma:", AutoSize = true, Margin = new Padding(0, 18, 0, 4) });
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            hotkeyBox = new TextBox { Width = 180, ReadOnly = true, BackColor = SystemColors.Window, Text = Settings.FormatHotkey(settings.Hotkey) };
            hotkeyBox.KeyDown += OnHotkeyKeyDown;
            hotkeyBox.PreviewKeyDown += (s, e) => e.IsInputKey = true;
            var clear = new Button { Text = "Öçiriş", AutoSize = true };
            clear.Click += delegate { SetHotkey(Keys.None); };
            row.Controls.Add(hotkeyBox);
            row.Controls.Add(clear);
            flow.Controls.Add(row);
            hotkeyStatus = new Label { AutoSize = true, ForeColor = SystemColors.GrayText, Text = "Maydonni bosib, yangi birikmani bosing (masalan Ctrl+Alt+Y)." };
            flow.Controls.Add(hotkeyStatus);

            flow.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(540, 0), Margin = new Padding(0, 22, 0, 0), ForeColor = SystemColors.GrayText,
                Text = "Maslahat: almaştiriş kerak bölmagan sözda (masalan inglizça «shop») darhol Backspace bosing — «sh» qaytadi."
            });
            flow.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(540, 0), Margin = new Padding(0, 12, 0, 0), ForeColor = SystemColors.GrayText,
                Text = "Bu oynani yopsangiz ham yordamçi işlayveradi: soat yonidagi yaşil «Ş» belgisi (körinmasa — ^ tugmasini bosing). " +
                       "Oynani qayta oçiş uçun Start menyusidan «Yangi alifbo yordamçisi» ni tanlang."
            });
            page.Controls.Add(flow);
            return page;
        }

        CheckBox Check(string text, bool value, Action<bool> apply)
        {
            var box = new CheckBox { Text = text, AutoSize = true, Checked = value, Margin = new Padding(0, 4, 0, 4) };
            box.CheckedChanged += delegate { apply(box.Checked); Changed(); };
            return box;
        }

        void OnHotkeyKeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            Keys code = e.KeyCode;
            if (code == Keys.ControlKey || code == Keys.ShiftKey || code == Keys.Menu || code == Keys.LWin || code == Keys.RWin) return;
            if (code == Keys.Escape) return;
            if (!e.Control && !e.Alt)
            {
                hotkeyStatus.Text = "Ctrl yoki Alt bilan birga bosing.";
                return;
            }
            SetHotkey(e.Modifiers | code);
        }

        void SetHotkey(Keys keys)
        {
            settings.Hotkey = keys;
            hotkeyBox.Text = keys == Keys.None ? "(yöq)" : Settings.FormatHotkey(keys);
            Changed();
        }

        public void ShowHotkeyResult(bool ok)
        {
            hotkeyStatus.Text = ok ? "Saqlandi." : "Bu birikma boşqa dastur tomonidan band. Boşqasini tanlang.";
        }

        // --- Dasturlar -------------------------------------------------------------

        TabPage BuildApps()
        {
            var page = new TabPage("Dasturlar") { Padding = new Padding(14) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(540, 0), Margin = new Padding(0, 0, 0, 8),
                Text = "Bu dasturlarda avto-almaştiriş işlamaydi (masalan öyinlar, kod muharrirlari, terminal)."
            }, 0, 0);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 0), 2);

            runningApps = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown };
            runningApps.DropDown += delegate { FillRunningApps(); };
            var add = new Button { Text = "Qöşiş", AutoSize = true };
            add.Click += delegate { AddApp(runningApps.Text); runningApps.Text = ""; };
            layout.Controls.Add(runningApps, 0, 1);
            layout.Controls.Add(add, 1, 1);

            appsList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            foreach (string app in settings.ExcludedApps) appsList.Items.Add(app);
            layout.Controls.Add(appsList, 0, 2);

            var side = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(0) };
            var browse = new Button { Text = "Tanlaş...", AutoSize = true, Width = 110 };
            browse.Click += delegate
            {
                using (var dlg = new OpenFileDialog { Filter = "Dasturlar (*.exe)|*.exe", Title = "Dasturni tanlang" })
                    if (dlg.ShowDialog(this) == DialogResult.OK) AddApp(dlg.FileName);
            };
            var remove = new Button { Text = "Olib taşlaş", AutoSize = true, Width = 110 };
            remove.Click += delegate
            {
                if (appsList.SelectedItem == null) return;
                settings.ExcludedApps.Remove((string)appsList.SelectedItem);
                appsList.Items.Remove(appsList.SelectedItem);
                Changed();
            };
            side.Controls.Add(browse);
            side.Controls.Add(remove);
            layout.Controls.Add(side, 1, 2);

            layout.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(540, 0), ForeColor = SystemColors.GrayText, Margin = new Padding(0, 8, 0, 0),
                Text = "Tez yöl: tray menyusidagi «… dasturida öçiriş» bandi oxirgi yozgan dasturingizni röyxatga qöşadi."
            }, 0, 3);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 3), 2);

            page.Controls.Add(layout);
            return page;
        }

        void FillRunningApps()
        {
            runningApps.Items.Clear();
            var names = new SortedSet<string>();
            foreach (Process p in Process.GetProcesses())
            {
                try { if (p.MainWindowHandle != IntPtr.Zero) names.Add(p.ProcessName.ToLowerInvariant() + ".exe"); }
                catch (InvalidOperationException) { }
                finally { p.Dispose(); }
            }
            foreach (string n in names) runningApps.Items.Add(n);
        }

        void AddApp(string name)
        {
            name = Settings.NormalizeApp(name);
            if (name.Length == 0 || settings.ExcludedApps.Contains(name)) return;
            settings.ExcludedApps.Add(name);
            appsList.Items.Add(name);
            Changed();
        }

        // --- Fayllar ---------------------------------------------------------------

        TabPage BuildFiles()
        {
            var page = new TabPage("Fayllar") { Padding = new Padding(14) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(560, 0), Margin = new Padding(0, 0, 0, 8),
                Text = "Fayllarni şu yerga sudrab taşlang yoki «Fayl qöşiş» ni bosing. Word (.docx) da formatlaş saqlanadi. " +
                       "Qöllanadi: .docx " + string.Join(" ", FileConverter.TextExtensions)
            });

            filesList = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, AllowDrop = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
            filesList.Columns.Add("Fayl", 360);
            filesList.Columns.Add("Holat", 170);
            filesList.DragEnter += (s, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            filesList.DragDrop += (s, e) => AddPaths((string[])e.Data.GetData(DataFormats.FileDrop));
            layout.Controls.Add(filesList);

            overwriteBox = new CheckBox
            {
                AutoSize = true, Margin = new Padding(0, 8, 0, 4),
                Text = "Asl faylni almaştiriş (belgilanmasa, «… (yangi alifbo)» nomli nusxa yaratiladi)"
            };
            layout.Controls.Add(overwriteBox);

            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0) };
            var addFiles = new Button { Text = "Fayl qöşiş...", AutoSize = true };
            addFiles.Click += delegate
            {
                using (var dlg = new OpenFileDialog { Multiselect = true, Filter = "Matn va Word fayllari|*.docx;*.txt;*.md;*.csv;*.srt|Barça fayllar|*.*" })
                    if (dlg.ShowDialog(this) == DialogResult.OK) AddPaths(dlg.FileNames);
            };
            var addFolder = new Button { Text = "Papka qöşiş...", AutoSize = true };
            addFolder.Click += delegate
            {
                using (var dlg = new FolderBrowserDialog { Description = "Papkadagi barça mos fayllar qöşiladi" })
                    if (dlg.ShowDialog(this) == DialogResult.OK) AddPaths(new[] { dlg.SelectedPath });
            };
            var clearList = new Button { Text = "Tozalaş", AutoSize = true };
            clearList.Click += delegate { filesList.Items.Clear(); };
            convertButton = new Button { Text = "Ögiriş", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            convertButton.Click += delegate { ConvertFiles(); };
            buttons.Controls.Add(addFiles);
            buttons.Controls.Add(addFolder);
            buttons.Controls.Add(clearList);
            buttons.Controls.Add(convertButton);
            layout.Controls.Add(buttons);

            page.Controls.Add(layout);
            return page;
        }

        void AddPaths(IEnumerable<string> paths)
        {
            var existing = new HashSet<string>(filesList.Items.Cast<ListViewItem>().Select(i => i.Name), StringComparer.OrdinalIgnoreCase);
            foreach (string path in paths)
            {
                IEnumerable<string> files = Directory.Exists(path)
                    ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    : new[] { path };
                foreach (string f in files)
                {
                    if (!FileConverter.IsSupported(f) || existing.Contains(f)) continue;
                    if (Path.GetFileName(f).StartsWith("~$")) continue;   // Word'ning vaqtinchalik fayli
                    existing.Add(f);
                    var item = new ListViewItem(new[] { f, "Kutilmoqda" }) { Name = f };
                    filesList.Items.Add(item);
                }
            }
        }

        void ConvertFiles()
        {
            if (filesList.Items.Count == 0) return;
            if (overwriteBox.Checked && MessageBox.Show(this,
                    "Asl fayllar almaştiriladi va buni qaytarib bölmaydi. Davom etasizmi?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            convertButton.Enabled = false;
            Cursor = Cursors.WaitCursor;
            int ok = 0, failed = 0;
            try
            {
                foreach (ListViewItem item in filesList.Items)
                {
                    try
                    {
                        int changes = FileConverter.Convert(item.Name, FileConverter.OutputPath(item.Name, overwriteBox.Checked));
                        item.SubItems[1].Text = changes + " ta özgariş";
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        item.SubItems[1].Text = "Xato: " + (ex is IOException ? "fayl band (yopib qayta urinib köring)" : ex.Message);
                        failed++;
                    }
                    Application.DoEvents();
                }
            }
            finally
            {
                Cursor = Cursors.Default;
                convertButton.Enabled = true;
            }
            MessageBox.Show(this, string.Format("Tayyor: {0} ta fayl ögirildi{1}.", ok, failed > 0 ? ", " + failed + " ta xato" : ""),
                Text, MessageBoxButtons.OK, failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        // --- Statistika ------------------------------------------------------------

        TabPage BuildStats()
        {
            var page = new TabPage("Statistika") { Padding = new Padding(14) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label
            {
                AutoSize = true, MaximumSize = new Size(560, 0), Margin = new Padding(0, 0, 0, 8),
                Text = "Yordamçi eski yozuvni necha marta almaştirgani. Raqamlar kamaysa — yangi tugmalarga örganyapsiz."
            });
            statsList = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
            statsList.Columns.Add("Harf", 70);
            statsList.Columns.Add("Eski yozuv", 90);
            statsList.Columns.Add("Bugun", 80);
            statsList.Columns.Add("Jami", 80);
            statsList.Columns.Add("Tugma", 200);
            layout.Controls.Add(statsList);

            var reset = new Button { Text = "Statistikani tozalaş", AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            reset.Click += delegate { coach.Reset(); RefreshStats(); };
            layout.Controls.Add(reset);
            page.Controls.Add(layout);
            return page;
        }

        void RefreshStats()
        {
            statsList.Items.Clear();
            foreach (var pair in coach.Total)
            {
                string l = pair.Key;
                statsList.Items.Add(new ListViewItem(new[]
                {
                    l, LearningCoach.Old(l), coach.Today[l].ToString(), pair.Value.ToString(), LearningCoach.Shortcut(l)
                }));
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Yopilganda yo'q qilinmaydi: keyingi safar tez ochiladi va fayllar ro'yxati saqlanadi.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
