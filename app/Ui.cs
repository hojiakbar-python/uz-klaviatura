// Sozlamalar oynasi uchun ranglar va o'zimiz chizadigan yengil boshqaruv elementlari.
// O'lchamlar 96 DPI uchun yoziladi va Theme.S() orqali ekran DPI'siga moslanadi.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UzKlaviatura
{
    static class Theme
    {
        public static readonly Color Brand = Color.FromArgb(0, 135, 90);
        public static readonly Color BrandHover = Color.FromArgb(0, 116, 77);
        public static readonly Color BrandPressed = Color.FromArgb(0, 98, 65);
        public static readonly Color BrandSoft = Color.FromArgb(224, 243, 235);
        public static readonly Color BrandBorder = Color.FromArgb(178, 222, 202);
        public static readonly Color Back = Color.FromArgb(246, 248, 247);
        public static readonly Color Sidebar = Color.FromArgb(236, 242, 239);
        public static readonly Color SidebarHover = Color.FromArgb(225, 233, 229);
        public static readonly Color Surface = Color.White;
        public static readonly Color Border = Color.FromArgb(222, 228, 225);
        public static readonly Color Text = Color.FromArgb(24, 31, 28);
        public static readonly Color Muted = Color.FromArgb(96, 107, 102);
        public static readonly Color Off = Color.FromArgb(158, 167, 163);
        public static readonly Color Danger = Color.FromArgb(190, 45, 30);

        public static readonly Font Body = new Font("Segoe UI", 9.75f);
        public static readonly Font Small = new Font("Segoe UI", 8.75f);
        public static readonly Font Strong = new Font("Segoe UI Semibold", 9.75f);
        public static readonly Font Big = new Font("Segoe UI Semibold", 12.5f);
        public static readonly Font Heading = new Font("Segoe UI Semibold", 17f);
        public static readonly Font Number = new Font("Segoe UI Semibold", 15f);
        public static readonly Font Letter = new Font("Segoe UI Semibold", 22f);

        static float scale;
        public static float Scale
        {
            get
            {
                if (scale == 0)
                    using (var g = Graphics.FromHwnd(IntPtr.Zero)) scale = g.DpiX / 96f;
                return scale;
            }
        }

        public static int S(int px) { return (int)Math.Round(px * Scale); }
        public static Padding S(int left, int top, int right, int bottom) { return new Padding(S(left), S(top), S(right), S(bottom)); }

        static string iconFamily;
        // Windows 11 da "Segoe Fluent Icons", Windows 10 da "Segoe MDL2 Assets".
        public static Font Icons(float size)
        {
            if (iconFamily == null)
                using (var f = new Font("Segoe Fluent Icons", 10f))
                    iconFamily = f.Name == "Segoe Fluent Icons" ? f.Name : "Segoe MDL2 Assets";
            return new Font(iconFamily, size);
        }

        public static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRound(Graphics g, Color fill, Color border, RectangleF r, float radius)
        {
            using (var path = Round(r, radius))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                g.FillPath(brush, path);
                if (border != Color.Empty) g.DrawPath(pen, path);
            }
        }
    }

    static class Ui
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        const int EM_SETCUEBANNER = 0x1501, CB_SETCUEBANNER = 0x1703, DWMWA_CAPTION_COLOR = 35;

        // Bo'sh maydonda kulrang namuna matn.
        public static void SetCue(Control control, string text)
        {
            int msg = control is ComboBox ? CB_SETCUEBANNER : EM_SETCUEBANNER;
            control.HandleCreated += delegate { SendMessage(control.Handle, msg, (IntPtr)1, text); };
        }

        // Windows 11: sarlavha satrini oyna foniga moslash (eski tizimlar e'tiborsiz qoldiradi).
        public static void SetCaptionColor(Form form, Color color)
        {
            try
            {
                int value = ColorTranslator.ToWin32(color);
                DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref value, 4);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        public static Control Gap(int height) { return new Panel { Height = Theme.S(height) }; }

        public static WrapLabel Text(string text, Font font, Color color)
        {
            return new WrapLabel { Text = text, Font = font, ForeColor = color };
        }

        public static WrapLabel Heading(string text) { return Text(text, Theme.Heading, Theme.Text); }

        public static WrapLabel Subtitle(string text)
        {
            var label = Text(text, Theme.Body, Theme.Muted);
            label.Padding = Theme.S(1, 2, 0, 0);
            return label;
        }

        // Yuqoridan pastga tartibda joylash. Dock'da oxirgi qo'shilgan birinchi joylanadi, shuning uchun teskari qo'shamiz.
        public static void Stack(Control parent, params Control[] items)
        {
            for (int i = items.Length - 1; i >= 0; i--)
            {
                items[i].Dock = DockStyle.Top;
                parent.Controls.Add(items[i]);
            }
        }

        // Pastki qism: ro'yxatdagi oxirgi element eng pastda turadi.
        public static void StackBottom(Control parent, params Control[] items)
        {
            foreach (Control item in items)
            {
                item.Dock = DockStyle.Bottom;
                parent.Controls.Add(item);
            }
        }
    }

    // Kengligiga qarab balandligini o'zi tanlaydigan (ko'p qatorli) yozuv.
    sealed class WrapLabel : Label
    {
        public WrapLabel()
        {
            AutoSize = false;
            UseMnemonic = false;
            ForeColor = Theme.Muted;
        }

        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); Fit(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Fit(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); Fit(); }
        protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); Fit(); }

        void Fit()
        {
            int width = Width - Padding.Horizontal;
            if (width <= 0) return;
            int h = TextRenderer.MeasureText(Text.Length == 0 ? " " : Text, Font, new Size(width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height + Padding.Vertical + 1;
            if (h != Height) Height = h;
        }
    }

    // Yumaloq burchakli oq kartochka. AutoHeight: balandlik ichidagi (Dock=Top) elementlar yig'indisiga teng.
    sealed class Card : Panel
    {
        public Color BorderColor = Theme.Border;
        public bool AutoHeight = true;

        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Theme.Surface;
            Padding = Theme.S(18, 12, 18, 12);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme.FillRound(g, BackColor, BorderColor, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Theme.S(10));
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (!AutoHeight) return;
            int h = Padding.Vertical;
            foreach (Control c in Controls) if (c.Dock == DockStyle.Top) h += c.Height;
            if (Height != h) Height = h;
        }
    }

    // Yoqish/o'chirish tugmasi (Windows 11 uslubida), silliq harakat bilan.
    sealed class ToggleSwitch : Control
    {
        bool isOn;
        float knob;   // 0 = chapda, 1 = o'ngda
        readonly Timer timer = new Timer { Interval = 15 };
        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            Size = new Size(Theme.S(44), Theme.S(24));
            Cursor = Cursors.Hand;
            TabStop = true;
            timer.Tick += delegate
            {
                float target = isOn ? 1 : 0;
                knob += (target - knob) * 0.35f;
                if (Math.Abs(target - knob) < 0.02f) { knob = target; timer.Stop(); }
                Invalidate();
            };
        }

        public bool Checked
        {
            get { return isOn; }
            set
            {
                if (isOn == value) return;
                isOn = value;
                if (IsHandleCreated && Visible) timer.Start(); else knob = value ? 1 : 0;
                Invalidate();
                var handler = CheckedChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e) { Focus(); Checked = !Checked; base.OnClick(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { Checked = !Checked; e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float inset = Theme.S(2);
            var track = new RectangleF(inset, inset, Width - 2 * inset - 1, Height - 2 * inset - 1);
            Color color = Theme.Mix(Theme.Off, Theme.Brand, knob);
            if (!Enabled) color = Theme.Mix(color, Color.White, 0.5f);
            Theme.FillRound(g, color, Color.Empty, track, track.Height / 2);
            if (Focused && ShowFocusCues)
                using (var path = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), Height / 2f))
                using (var pen = new Pen(Theme.Text, 1.5f)) g.DrawPath(pen, path);

            float gap = Theme.S(3);
            float d = track.Height - 2 * gap;
            float x = track.X + gap + (track.Width - 2 * gap - d) * knob;
            g.FillEllipse(Brushes.White, x, track.Y + gap, d, d);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }
    }

    // Sozlama qatori: chapda sarlavha va izoh, o'ngda boshqaruv elementi (odatda ToggleSwitch).
    sealed class SettingRow : Panel
    {
        readonly Label title, desc;
        public readonly Control Trailing;
        public bool Divider;
        Color? badge;

        public SettingRow(string titleText, string descText, Control trailing, bool big = false)
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            Height = Theme.S(big ? 58 : 56);
            title = new Label { Text = titleText, AutoSize = true, UseMnemonic = false, Font = big ? Theme.Big : Theme.Strong, ForeColor = Theme.Text };
            desc = new Label { Text = descText, AutoSize = true, UseMnemonic = false, ForeColor = Theme.Muted };
            Trailing = trailing;
            trailing.SizeChanged += delegate { PerformLayout(); };   // AutoSize panel keyinroq o'lchanadi
            Controls.Add(title);
            Controls.Add(desc);
            Controls.Add(trailing);
            // Qatorning matnini bosish ham tugmani almashtiradi.
            if (trailing is ToggleSwitch)
                foreach (Control c in new Control[] { this, title, desc })
                    c.Click += delegate { Switch.Checked = !Switch.Checked; };
        }

        public static SettingRow WithSwitch(string titleText, string descText, bool value, bool big = false)
        {
            return new SettingRow(titleText, descText, new ToggleSwitch { Checked = value }, big);
        }

        public ToggleSwitch Switch { get { return (ToggleSwitch)Trailing; } }
        public string TitleText { set { title.Text = value; PerformLayout(); } }
        public string DescText { set { desc.Text = value; PerformLayout(); } }

        // Chapdagi "Ş" doirasi (asosiy yoqish/o'chirish qatorida).
        public Color? Badge
        {
            set { badge = value; PerformLayout(); Invalidate(); }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (Trailing == null) return;   // konstruktor tugamagan
            int left = badge.HasValue ? Theme.S(54) : 0;
            Trailing.Location = new Point(Width - Trailing.Width, (Height - Trailing.Height) / 2);
            int textHeight = title.Height + desc.Height;
            int y = (Height - textHeight) / 2;
            title.Location = new Point(left - Theme.S(2), y);
            desc.Location = new Point(left - Theme.S(2), y + title.Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            if (Divider)
                using (var pen = new Pen(Theme.Border)) g.DrawLine(pen, 0, 0, Width, 0);
            if (!badge.HasValue) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int d = Theme.S(40);
            var circle = new Rectangle(0, (Height - d) / 2, d, d);
            using (var brush = new SolidBrush(badge.Value)) g.FillEllipse(brush, circle);
            TextRenderer.DrawText(g, "Ş", Theme.Big, circle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    // Yumaloq tekis tugma: asosiy (yashil) yoki oddiy (oq, chegarali).
    sealed class FlatButton : Button
    {
        readonly bool primary;
        bool hover, down;

        public FlatButton(string text, bool primary = false)
        {
            this.primary = primary;
            Text = text;
            UseMnemonic = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Cursor = Cursors.Hand;
            Font = primary ? Theme.Strong : Theme.Body;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int w = TextRenderer.MeasureText(Text, Font).Width + Theme.S(30);
            return new Size(Math.Max(w, Theme.S(88)), Theme.S(34));
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill, border, text;
            if (primary)
            {
                fill = !Enabled ? Theme.Mix(Theme.Brand, Color.White, 0.55f) : down ? Theme.BrandPressed : hover ? Theme.BrandHover : Theme.Brand;
                border = Color.Empty;
                text = Color.White;
            }
            else
            {
                fill = !Enabled ? Theme.Back : down ? Color.FromArgb(230, 235, 232) : hover ? Color.FromArgb(241, 245, 243) : Theme.Surface;
                border = hover && Enabled ? Color.FromArgb(196, 205, 200) : Theme.Border;
                text = Enabled ? Theme.Text : Theme.Off;
            }
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Theme.FillRound(g, fill, border, r, Theme.S(6));
            if (Focused && ShowFocusCues)
                using (var path = Theme.Round(RectangleF.Inflate(r, -Theme.S(3), -Theme.S(3)), Theme.S(4)))
                using (var pen = new Pen(primary ? Color.White : Theme.Brand)) g.DrawPath(pen, path);
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    // Chap paneldagi bo'lim tugmasi.
    sealed class NavButton : Control
    {
        readonly string glyph;
        readonly Font iconFont = Theme.Icons(11.5f);
        bool selected, hover;

        public NavButton(string glyph, string text)
        {
            this.glyph = glyph;
            Text = text;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Height = Theme.S(40);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { OnClick(EventArgs.Empty); e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            if (selected) Theme.FillRound(g, Theme.Surface, Theme.Border, r, Theme.S(8));
            else if (hover) Theme.FillRound(g, Theme.SidebarHover, Color.Empty, r, Theme.S(8));
            if (selected)
                Theme.FillRound(g, Theme.Brand, Color.Empty, new RectangleF(Theme.S(5), Height * 0.3f, Theme.S(3), Height * 0.4f), Theme.S(2));
            if (Focused && ShowFocusCues && !selected)
                using (var path = Theme.Round(RectangleF.Inflate(r, -1, -1), Theme.S(7)))
                using (var pen = new Pen(Theme.Brand)) g.DrawPath(pen, path);

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, glyph, iconFont, new Rectangle(Theme.S(16), 0, Theme.S(22), Height),
                selected ? Theme.Brand : Theme.Muted, flags | TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, Text, selected ? Theme.Strong : Theme.Body, new Rectangle(Theme.S(48), 0, Width - Theme.S(52), Height),
                Theme.Text, flags | TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) iconFont.Dispose();
            base.Dispose(disposing);
        }
    }

    // Chap panel tepasidagi belgi va nom.
    sealed class BrandHeader : Control
    {
        readonly string version;

        public BrandHeader(string version)
        {
            this.version = version;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = Theme.S(52);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int d = Theme.S(38);
            var circle = new Rectangle(Theme.S(4), (Height - d) / 2, d, d);
            using (var brush = new SolidBrush(Theme.Brand)) g.FillEllipse(brush, circle);
            TextRenderer.DrawText(g, "Ş", Theme.Big, circle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            int x = circle.Right + Theme.S(10);
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(g, "Yangi alifbo", Theme.Big, new Rectangle(x, Height / 2 - Theme.S(21), Width - x, Theme.S(24)), Theme.Text, flags);
            TextRenderer.DrawText(g, "yordamçisi · v" + version, Theme.Small, new Rectangle(x, Height / 2 + Theme.S(2), Width - x, Theme.S(18)), Theme.Muted, flags);
        }
    }

    // Fayllar ro'yxati bo'shligida: "shu yerga sudrab tashlang" maydoni.
    sealed class DropZone : Control
    {
        readonly Font iconFont = Theme.Icons(26f);
        readonly string hint;
        bool active;

        public DropZone(string hint)
        {
            this.hint = hint;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public bool Active
        {
            set { if (active != value) { active = value; Invalidate(); } }
        }

        protected override void OnMouseEnter(EventArgs e) { Active = true; base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { Active = false; base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(Theme.S(4), Theme.S(4), Width - Theme.S(8) - 1, Height - Theme.S(8) - 1);
            using (var path = Theme.Round(r, Theme.S(8)))
            using (var brush = new SolidBrush(active ? Theme.BrandSoft : Theme.Back))
            using (var pen = new Pen(active ? Theme.Brand : Color.FromArgb(190, 200, 195), Theme.S(1)) { DashStyle = DashStyle.Dash })
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            int mid = Height / 2;
            var center = TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, "", iconFont, new Rectangle(0, mid - Theme.S(64), Width, Theme.S(44)), Theme.Brand, center | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "Fayllarni şu yerga sudrab taşlang", Theme.Big, new Rectangle(0, mid - Theme.S(14), Width, Theme.S(26)), Theme.Text, center);
            TextRenderer.DrawText(g, "yoki bosib tanlang", Theme.Body, new Rectangle(0, mid + Theme.S(14), Width, Theme.S(20)), Theme.Muted, center);
            TextRenderer.DrawText(g, hint, Theme.Small, new Rectangle(Theme.S(12), mid + Theme.S(40), Width - Theme.S(24), Theme.S(18)), Theme.Muted, center | TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) iconFont.Dispose();
            base.Dispose(disposing);
        }
    }

    // Statistika: bitta harf uchun kartochka.
    sealed class StatTile : Control
    {
        public readonly string Letter;
        readonly string old, shortcut;
        public int Today, Total;

        public StatTile(string letter, string old, string shortcut)
        {
            Letter = letter;
            this.old = old;
            this.shortcut = shortcut;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(Theme.S(160), Theme.S(134));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme.FillRound(g, Theme.Surface, Theme.Border, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Theme.S(10));

            int p = Theme.S(14);
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, Letter, Theme.Letter, new Point(p - Theme.S(1), p - Theme.S(8)), Theme.Brand, flags);

            // O'ng tepada: "sh → ş" yorlig'i.
            string pill = old + " → " + Letter;
            Size ps = TextRenderer.MeasureText(pill, Theme.Small, Size.Empty, flags);
            var pr = new RectangleF(Width - p - ps.Width - Theme.S(16), p, ps.Width + Theme.S(16), Theme.S(22));
            Theme.FillRound(g, Theme.BrandSoft, Color.Empty, pr, pr.Height / 2);
            TextRenderer.DrawText(g, pill, Theme.Small, Rectangle.Round(pr), Theme.BrandPressed,
                flags | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            int y = Theme.S(58);
            int x2 = Width / 2 + Theme.S(4);
            TextRenderer.DrawText(g, "Bugun", Theme.Small, new Point(p, y), Theme.Muted, flags);
            TextRenderer.DrawText(g, Today.ToString(), Theme.Number, new Point(p - Theme.S(1), y + Theme.S(15)), Theme.Text, flags);
            TextRenderer.DrawText(g, "Jami", Theme.Small, new Point(x2, y), Theme.Muted, flags);
            TextRenderer.DrawText(g, Total.ToString(), Theme.Number, new Point(x2 - Theme.S(1), y + Theme.S(15)), Theme.Text, flags);

            using (var pen = new Pen(Theme.Border)) g.DrawLine(pen, p, Height - Theme.S(36), Width - p, Height - Theme.S(36));
            TextRenderer.DrawText(g, shortcut, Theme.Small, new Rectangle(p, Height - Theme.S(28), Width - 2 * p, Theme.S(18)), Theme.Muted,
                flags | TextFormatFlags.EndEllipsis);
        }
    }
}
