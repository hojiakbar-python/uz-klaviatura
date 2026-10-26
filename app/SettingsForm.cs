// Sozlamalar oynasi: chapda bo'limlar, o'ngda tanlangan bo'lim (umumiy, istisno dasturlar,
// fayllarni o'tkazish, statistika). Har bir o'zgarish darhol saqlanadi va SettingsChanged orqali qo'llanadi.
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

        readonly List<NavButton> nav = new List<NavButton>();
        readonly List<Control> pages = new List<Control>();
        Card hero;
        SettingRow enabledRow;
        TextBox hotkeyBox;
        Label hotkeyStatus;
        ListBox appsList;
        Label appsEmpty;
        ComboBox runningApps;
        FlatButton removeApp;
        ListView filesList;
        DropZone dropZone;
        SettingRow overwriteRow;
        FlatButton convertButton, clearFiles;
        Label statsSummary;
        readonly List<StatTile> tiles = new List<StatTile>();

        public SettingsForm(Settings settings, LearningCoach coach, string version)
        {
            this.settings = settings;
            this.coach = coach;
            Text = "Yangi alifbo yordamçisi — sozlamalar";
            AutoScaleMode = AutoScaleMode.None;   // o'lchamlar Theme.S() bilan qo'lda moslanadi
            Font = Theme.Body;
            ForeColor = Theme.Text;
            BackColor = Theme.Back;
            ClientSize = new Size(Theme.S(820), Theme.S(580));
            MinimumSize = new Size(Theme.S(820), Theme.S(560));   // statistikada 3 ta kartochka sig'ishi uchun
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            SuspendLayout();
            var content = new Panel { Dock = DockStyle.Fill };
            pages.Add(BuildGeneral());
            pages.Add(BuildApps());
            pages.Add(BuildFiles());
            pages.Add(BuildStats());
            foreach (Control page in pages)
            {
                page.Dock = DockStyle.Fill;
                page.Padding = Theme.S(28, 22, 28, 20);
                page.Visible = false;
                content.Controls.Add(page);
            }

            var sidebar = new Panel { Dock = DockStyle.Left, Width = Theme.S(212), BackColor = Theme.Sidebar, Padding = Theme.S(12, 14, 12, 16) };
            sidebar.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border)) e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };
            var sideItems = new List<Control> { new BrandHeader(version), Ui.Gap(18) };
            string[,] sections = { { "", "Umumiy" }, { "", "Dasturlar" }, { "", "Fayllar" }, { "", "Statistika" } };
            for (int i = 0; i < pages.Count; i++)
            {
                int index = i;
                var button = new NavButton(sections[i, 0], sections[i, 1]);
                button.Click += delegate { ShowTab(index); };
                nav.Add(button);
                sideItems.Add(button);
                sideItems.Add(Ui.Gap(4));
            }
            Ui.Stack(sidebar, sideItems.ToArray());
            Ui.StackBottom(sidebar, Ui.Text("Oynani yopsangiz ham yordamçi işlayveradi: soat yonidagi yaşil «Ş» belgisi " +
                                            "(körinmasa — ^ tugmasini bosing).", Theme.Small, Theme.Muted));

            Controls.Add(content);
            Controls.Add(sidebar);
            ResumeLayout();
            ShowTab(TabGeneral);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Ui.SetCaptionColor(this, Theme.Back);
        }

        public void ShowTab(int index)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].Visible = i == index;
                nav[i].Selected = i == index;
            }
            ActiveControl = nav[index];
            if (index == TabStats) RefreshStats();
        }

        // Tray yoki tezkor tugma holatni o'zgartirganda oynani yangilash.
        public void SyncFromSettings()
        {
            enabledRow.Switch.Checked = settings.Enabled;
            appsList.Items.Clear();
            foreach (string app in settings.ExcludedApps) appsList.Items.Add(app);
            UpdateAppsState();
        }

        void Changed()
        {
            settings.Save();
            var handler = SettingsChanged;
            if (handler != null) handler();
        }

        // --- Umumiy ---------------------------------------------------------------

        Control BuildGeneral()
        {
            var page = new Panel { AutoScroll = true };

            enabledRow = SettingRow.WithSwitch("", "", settings.Enabled, true);
            enabledRow.Switch.CheckedChanged += delegate { settings.Enabled = enabledRow.Switch.Checked; UpdateHero(); Changed(); };
            hero = new Card { Padding = Theme.S(18, 12, 18, 12) };
            Ui.Stack(hero, enabledRow);
            UpdateHero();

            var autoStart = SettingRow.WithSwitch("Windows bilan birga işga tuşiriş", "Kompyuter yoqilganda yordamçi özi işga tuşadi", Settings.AutoStart);
            autoStart.Switch.CheckedChanged += delegate { Settings.AutoStart = autoStart.Switch.Checked; };
            autoStart.Divider = true;
            var options = new Card { Padding = Theme.S(18, 2, 18, 2) };
            Ui.Stack(options,
                Row("Faqat Özbek klaviaturasida işlaş", "Inglizça va rusça yozuvga tegmaydi", settings.OnlyUzbekLayout,
                    v => settings.OnlyUzbekLayout = v, false),
                Row("Örganiş rejimi", "Yangi harflarni qaysi tugma bilan yoziş haqida maslahat beradi", settings.LearningMode,
                    v => settings.LearningMode = v, true),
                autoStart);

            hotkeyBox = new TextBox
            {
                ReadOnly = true, BackColor = Theme.Surface, BorderStyle = BorderStyle.FixedSingle, Font = Theme.Strong,
                TextAlign = HorizontalAlignment.Center, Width = Theme.S(170), Cursor = Cursors.Hand, Text = HotkeyText(settings.Hotkey)
            };
            hotkeyBox.KeyDown += OnHotkeyKeyDown;
            hotkeyBox.PreviewKeyDown += (s, e) => e.IsInputKey = true;
            hotkeyBox.Enter += delegate { hotkeyBox.BackColor = Theme.BrandSoft; };
            hotkeyBox.Leave += delegate { hotkeyBox.BackColor = Theme.Surface; };
            var clear = new FlatButton("Öçiriş") { Margin = Padding.Empty };
            hotkeyBox.Margin = new Padding(0, Math.Max(0, (clear.PreferredSize.Height - hotkeyBox.Height) / 2), Theme.S(8), 0);
            clear.Click += delegate { SetHotkey(Keys.None); };
            var hotkeyControls = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
            hotkeyControls.Controls.Add(hotkeyBox);
            hotkeyControls.Controls.Add(clear);
            hotkeyStatus = Ui.Text("Maydonni bosib, yangi birikmani bosing (masalan Ctrl+Alt+Y).", Theme.Small, Theme.Muted);
            hotkeyStatus.Padding = Theme.S(0, 0, 0, 4);
            var hotkeyCard = new Card { Padding = Theme.S(18, 4, 18, 10) };
            Ui.Stack(hotkeyCard, new SettingRow("Tezkor tugma", "Avto-almaştirişni yoqiş yoki öçiriş", hotkeyControls), hotkeyStatus);

            Ui.Stack(page,
                Ui.Heading("Umumiy"),
                Ui.Subtitle("Yordamçi qanday işlaşini sozlang. Özgarişlar darhol saqlanadi."),
                Ui.Gap(18), hero,
                Ui.Gap(14), options,
                Ui.Gap(14), hotkeyCard,
                Ui.Gap(14),
                Ui.Text("Maslahat: almaştiriş kerak bölmagan sözda (masalan inglizça «shop») darhol Backspace bosing — «sh» qaytadi.",
                    Theme.Small, Theme.Muted));
            return page;
        }

        SettingRow Row(string title, string desc, bool value, Action<bool> apply, bool divider)
        {
            var row = SettingRow.WithSwitch(title, desc, value);
            row.Divider = divider;
            row.Switch.CheckedChanged += delegate { apply(row.Switch.Checked); Changed(); };
            return row;
        }

        void UpdateHero()
        {
            bool on = settings.Enabled;
            enabledRow.TitleText = on ? "Avto-almaştiriş yoqilgan" : "Avto-almaştiriş öçirilgan";
            enabledRow.DescText = on ? "sh → ş,   ch → ç,   o' → ö,   g' → ğ" : "Hozir heç narsa almaştirilmaydi";
            enabledRow.Badge = on ? Theme.Brand : Theme.Off;
            hero.BackColor = on ? Theme.BrandSoft : Theme.Surface;
            hero.BorderColor = on ? Theme.BrandBorder : Theme.Border;
            hero.Invalidate(true);
        }

        static string HotkeyText(Keys keys)
        {
            return keys == Keys.None ? "(yöq)" : Settings.FormatHotkey(keys);
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
            hotkeyBox.Text = HotkeyText(keys);
            Changed();
        }

        public void ShowHotkeyResult(bool ok)
        {
            hotkeyStatus.ForeColor = ok ? Theme.Muted : Theme.Danger;
            hotkeyStatus.Text = ok ? "Saqlandi." : "Bu birikma boşqa dastur tomonidan band. Boşqasini tanlang.";
        }

        // --- Dasturlar -------------------------------------------------------------

        Control BuildApps()
        {
            var page = new Panel();

            runningApps = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = Theme.S(0, 0, 8, 0) };
            Ui.SetCue(runningApps, "Dastur nomi, masalan code.exe — yoki röyxatdan tanlang");
            runningApps.DropDown += delegate { FillRunningApps(); };
            runningApps.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                AddTypedApp();
            };
            var add = new FlatButton("Qöşiş", true) { Margin = Theme.S(0, 0, 8, 0) };
            add.Click += delegate { AddTypedApp(); };
            var browse = new FlatButton("Tanlaş...") { Margin = Padding.Empty };
            browse.Click += delegate
            {
                using (var dlg = new OpenFileDialog { Filter = "Dasturlar (*.exe)|*.exe", Title = "Dasturni tanlang" })
                    if (dlg.ShowDialog(this) == DialogResult.OK) AddApp(dlg.FileName);
            };
            var input = new TableLayoutPanel { Height = Theme.S(36), ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            input.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            input.Controls.Add(runningApps, 0, 0);
            input.Controls.Add(add, 1, 0);
            input.Controls.Add(browse, 2, 0);

            appsList = new ListBox
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, IntegralHeight = false, BackColor = Theme.Surface,
                DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = Theme.S(38)
            };
            foreach (string app in settings.ExcludedApps) appsList.Items.Add(app);
            appsList.DrawItem += DrawAppItem;
            appsList.SelectedIndexChanged += delegate { removeApp.Enabled = appsList.SelectedItem != null; };
            appsList.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) RemoveSelectedApp(); };
            appsEmpty = new Label
            {
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Muted,
                Text = "Hozirça röyxat böş — avto-almaştiriş barça dasturlarda işlaydi."
            };
            var listCard = new Card { AutoHeight = false, Dock = DockStyle.Fill, Padding = Theme.S(6, 6, 6, 6) };
            listCard.Controls.Add(appsList);
            listCard.Controls.Add(appsEmpty);

            removeApp = new FlatButton("Olib taşlaş") { Enabled = false, Margin = Padding.Empty };
            removeApp.Click += delegate { RemoveSelectedApp(); };
            var bottom = new FlowLayoutPanel { Height = Theme.S(34), WrapContents = false, Margin = Padding.Empty };
            bottom.Controls.Add(removeApp);

            page.Controls.Add(listCard);
            Ui.Stack(page,
                Ui.Heading("Dasturlar"),
                Ui.Subtitle("Bu dasturlarda avto-almaştiriş işlamaydi — masalan öyinlar, kod muharrirlari, terminal."),
                Ui.Gap(18), input, Ui.Gap(12));
            Ui.StackBottom(page, Ui.Gap(12), bottom, Ui.Gap(8),
                Ui.Text("Tez yöl: tray menyusidagi «… dasturida öçiriş» bandi oxirgi yozgan dasturingizni röyxatga qöşadi.",
                    Theme.Small, Theme.Muted));
            UpdateAppsState();
            return page;
        }

        void DrawAppItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var back = new SolidBrush(Theme.Surface)) g.FillRectangle(back, e.Bounds);
            if (selected)
                Theme.FillRound(g, Theme.BrandSoft, Color.Empty,
                    new RectangleF(e.Bounds.X + 1, e.Bounds.Y + 1, e.Bounds.Width - 3, e.Bounds.Height - 3), Theme.S(6));

            string name = (string)appsList.Items[e.Index];
            int size = Theme.S(26);
            var badge = new Rectangle(e.Bounds.X + Theme.S(8), e.Bounds.Y + (e.Bounds.Height - size) / 2, size, size);
            Theme.FillRound(g, selected ? Theme.Surface : Theme.BrandSoft, Color.Empty, badge, Theme.S(6));
            TextRenderer.DrawText(g, name.Substring(0, 1).ToUpperInvariant(), Theme.Strong, badge, Theme.Brand,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            var text = new Rectangle(badge.Right + Theme.S(12), e.Bounds.Y, e.Bounds.Width - badge.Right - Theme.S(20), e.Bounds.Height);
            TextRenderer.DrawText(g, name, Theme.Body, text, Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        void UpdateAppsState()
        {
            bool empty = appsList.Items.Count == 0;
            appsList.Visible = !empty;
            appsEmpty.Visible = empty;
            removeApp.Enabled = appsList.SelectedItem != null;
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

        void AddTypedApp()
        {
            AddApp(runningApps.Text);
            runningApps.Text = "";
        }

        void AddApp(string name)
        {
            name = Settings.NormalizeApp(name);
            if (name.Length == 0 || settings.ExcludedApps.Contains(name)) return;
            settings.ExcludedApps.Add(name);
            appsList.Items.Add(name);
            UpdateAppsState();
            Changed();
        }

        void RemoveSelectedApp()
        {
            if (appsList.SelectedItem == null) return;
            settings.ExcludedApps.Remove((string)appsList.SelectedItem);
            appsList.Items.Remove(appsList.SelectedItem);
            UpdateAppsState();
            Changed();
        }

        // --- Fayllar ---------------------------------------------------------------

        Control BuildFiles()
        {
            var page = new Panel();

            filesList = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, AllowDrop = true, BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable, SmallImageList = new ImageList { ImageSize = new Size(1, Theme.S(28)) }
            };
            filesList.Columns.Add("Fayl", Theme.S(300));
            filesList.Columns.Add("Holat", Theme.S(150));
            filesList.Resize += delegate { FitFileColumns(); };
            dropZone = new DropZone("Qöllanadi: .docx " + string.Join(" ", FileConverter.TextExtensions)) { Dock = DockStyle.Fill, AllowDrop = true };
            dropZone.Click += delegate { PickFiles(); };
            var listCard = new Card { AutoHeight = false, Dock = DockStyle.Fill, Padding = Theme.S(6, 6, 6, 6), AllowDrop = true };
            listCard.Controls.Add(filesList);
            listCard.Controls.Add(dropZone);
            foreach (Control c in new Control[] { filesList, dropZone, listCard })
            {
                c.DragEnter += (s, e) =>
                {
                    bool files = e.Data.GetDataPresent(DataFormats.FileDrop);
                    e.Effect = files ? DragDropEffects.Copy : DragDropEffects.None;
                    dropZone.Active = files;
                };
                c.DragLeave += delegate { dropZone.Active = false; };
                c.DragDrop += (s, e) =>
                {
                    dropZone.Active = false;
                    AddPaths((string[])e.Data.GetData(DataFormats.FileDrop));
                };
            }

            overwriteRow = SettingRow.WithSwitch("Asl faylni almaştiriş", "Öçiq bölsa, «… (yangi alifbo)» nomli nusxa yaratiladi", false);
            var optionsCard = new Card { Padding = Theme.S(18, 2, 18, 2) };
            Ui.Stack(optionsCard, overwriteRow);

            var addFiles = new FlatButton("Fayl qöşiş...") { Margin = Theme.S(0, 0, 8, 0) };
            addFiles.Click += delegate { PickFiles(); };
            var addFolder = new FlatButton("Papka qöşiş...") { Margin = Theme.S(0, 0, 8, 0) };
            addFolder.Click += delegate
            {
                using (var dlg = new FolderBrowserDialog { Description = "Papkadagi barça mos fayllar qöşiladi" })
                    if (dlg.ShowDialog(this) == DialogResult.OK) AddPaths(new[] { dlg.SelectedPath });
            };
            clearFiles = new FlatButton("Tozalaş") { Margin = Padding.Empty };
            clearFiles.Click += delegate { filesList.Items.Clear(); UpdateFilesState(); };
            convertButton = new FlatButton("Ögiriş", true) { Margin = Padding.Empty };
            convertButton.Click += delegate { ConvertFiles(); };
            var buttons = new TableLayoutPanel { Height = Theme.S(34), ColumnCount = 5, RowCount = 1, Margin = Padding.Empty };
            for (int i = 0; i < 5; i++) buttons.ColumnStyles.Add(i == 3 ? new ColumnStyle(SizeType.Percent, 100) : new ColumnStyle(SizeType.AutoSize));
            buttons.Controls.Add(addFiles, 0, 0);
            buttons.Controls.Add(addFolder, 1, 0);
            buttons.Controls.Add(clearFiles, 2, 0);
            buttons.Controls.Add(convertButton, 4, 0);

            page.Controls.Add(listCard);
            Ui.Stack(page,
                Ui.Heading("Fayllar"),
                Ui.Subtitle("Fayllardagi matnni yangi alifboga ögiradi. Word (.docx) da formatlaş saqlanadi."),
                Ui.Gap(18));
            Ui.StackBottom(page, Ui.Gap(12), optionsCard, Ui.Gap(12), buttons);
            UpdateFilesState();
            return page;
        }

        void PickFiles()
        {
            using (var dlg = new OpenFileDialog { Multiselect = true, Filter = "Matn va Word fayllari|*.docx;*.txt;*.md;*.csv;*.srt|Barça fayllar|*.*" })
                if (dlg.ShowDialog(this) == DialogResult.OK) AddPaths(dlg.FileNames);
        }

        void FitFileColumns()
        {
            filesList.Columns[0].Width = Math.Max(Theme.S(120), filesList.ClientSize.Width - filesList.Columns[1].Width);
        }

        void UpdateFilesState()
        {
            bool empty = filesList.Items.Count == 0;
            filesList.Visible = !empty;
            dropZone.Visible = empty;
            convertButton.Enabled = !empty;
            clearFiles.Enabled = !empty;
            if (!empty) FitFileColumns();
        }

        static void SetStatus(ListViewItem item, string text, Color color)
        {
            item.SubItems[1].Text = text;
            item.SubItems[1].ForeColor = color;
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
                    var item = new ListViewItem(new[] { f, "Kutilmoqda" }) { Name = f, UseItemStyleForSubItems = false };
                    item.SubItems[1].ForeColor = Theme.Muted;
                    filesList.Items.Add(item);
                }
            }
            UpdateFilesState();
        }

        void ConvertFiles()
        {
            if (filesList.Items.Count == 0) return;
            if (overwriteRow.Switch.Checked && MessageBox.Show(this,
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
                        int changes = FileConverter.Convert(item.Name, FileConverter.OutputPath(item.Name, overwriteRow.Switch.Checked));
                        SetStatus(item, changes + " ta özgariş", Theme.Brand);
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        SetStatus(item, "Xato: " + (ex is IOException ? "fayl band (yopib qayta urinib köring)" : ex.Message), Theme.Danger);
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

        Control BuildStats()
        {
            var page = new Panel();

            statsSummary = Ui.Text("", Theme.Strong, Theme.Text);
            var grid = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
            foreach (string letter in coach.Total.Keys)
            {
                var tile = new StatTile(letter, LearningCoach.Old(letter), LearningCoach.Shortcut(letter)) { Margin = Theme.S(0, 0, 10, 10) };
                tiles.Add(tile);
                grid.Controls.Add(tile);
            }

            var reset = new FlatButton("Statistikani tozalaş") { Margin = Padding.Empty };
            reset.Click += delegate
            {
                if (MessageBox.Show(this, "Barça hisoblar nolga qaytariladi. Davom etasizmi?", Text,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                coach.Reset();
                RefreshStats();
            };
            var bottom = new FlowLayoutPanel { Height = Theme.S(34), WrapContents = false, Margin = Padding.Empty };
            bottom.Controls.Add(reset);

            page.Controls.Add(grid);
            Ui.Stack(page,
                Ui.Heading("Statistika"),
                Ui.Subtitle("Yordamçi eski yozuvni necha marta almaştirgani. Raqamlar kamaysa — yangi tugmalarga örganyapsiz."),
                Ui.Gap(14), statsSummary, Ui.Gap(12));
            Ui.StackBottom(page, Ui.Gap(8), bottom);
            return page;
        }

        void RefreshStats()
        {
            int today = 0, total = 0;
            foreach (StatTile tile in tiles)
            {
                tile.Today = coach.Today[tile.Letter];
                tile.Total = coach.Total[tile.Letter];
                today += tile.Today;
                total += tile.Total;
                tile.Invalidate();
            }
            statsSummary.Text = string.Format("Bugun: {0} ta almaştiriş   ·   Jami: {1} ta", today, total);
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
