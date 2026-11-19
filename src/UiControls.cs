using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ImageTextMaker
{
    // ---------------------------------------------------------------------
    // Design system
    // ---------------------------------------------------------------------
    internal static class UiTheme
    {
        // Requested palette
        public static readonly Color Bg = Color.FromArgb(245, 246, 248);      // #F5F6F8
        public static readonly Color Card = Color.White;                      // #FFFFFF
        public static readonly Color Line = Color.FromArgb(226, 232, 240);    // subtle border
        public static readonly Color LineSoft = Color.FromArgb(238, 242, 248);
        public static readonly Color Ink = Color.FromArgb(32, 43, 56);        // #202B38
        public static readonly Color Muted = Color.FromArgb(113, 128, 150);   // #718096
        public static readonly Color Primary = Color.FromArgb(36, 95, 141);   // #245F8D
        public static readonly Color PrimaryDark = Color.FromArgb(27, 74, 110);
        public static readonly Color PrimarySoft = Color.FromArgb(235, 243, 249);
        public static readonly Color Accent = Color.FromArgb(245, 158, 11);   // amber / warning
        public static readonly Color Soft = Color.FromArgb(248, 250, 252);

        public static readonly Color Ok = Color.FromArgb(22, 163, 74);
        public static readonly Color OkSoft = Color.FromArgb(236, 253, 245);
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);
        public static readonly Color DangerSoft = Color.FromArgb(254, 242, 242);
        public static readonly Color Warn = Color.FromArgb(217, 119, 6);
        public static readonly Color WarnSoft = Color.FromArgb(255, 251, 235);
        public static readonly Color Info = Color.FromArgb(36, 95, 141);
        public static readonly Color InfoSoft = Color.FromArgb(235, 243, 249);

        public const int Radius = 9;
        public const int InputHeight = 34;
        public const int ButtonHeight = 34;

        private static PrivateFontCollection _fonts;
        private static FontFamily _regular;
        private static FontFamily _bold;

        private static void LoadFonts()
        {
            if (_fonts != null) return;
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string reg = System.IO.Path.Combine(baseDir, "fonts", "Tajawal-Regular.ttf");
                string bold = System.IO.Path.Combine(baseDir, "fonts", "Tajawal-Bold.ttf");
                _fonts = new PrivateFontCollection();
                if (System.IO.File.Exists(reg)) _fonts.AddFontFile(reg);
                if (System.IO.File.Exists(bold)) _fonts.AddFontFile(bold);
                if (_fonts.Families.Length > 0) _regular = _fonts.Families[0];
                if (_fonts.Families.Length > 1) _bold = _fonts.Families[1];
            }
            catch
            {
                _fonts = null;
            }
        }

        public static FontFamily RegularFamily
        {
            get { LoadFonts(); return _regular ?? new FontFamily("Tahoma"); }
        }

        public static FontFamily BoldFamily
        {
            get { LoadFonts(); return _bold ?? _regular ?? new FontFamily("Tahoma"); }
        }

        public static Font F(float size, bool bold)
        {
            return new Font(bold ? BoldFamily : RegularFamily, size,
                bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
        }

        // Reused by painters: creating a private font on every paint is expensive.
        private static readonly System.Collections.Generic.Dictionary<string, Font> _fontCache =
            new System.Collections.Generic.Dictionary<string, Font>();

        public static Font Cached(float size, bool bold)
        {
            string key = size.ToString(System.Globalization.CultureInfo.InvariantCulture) + (bold ? "b" : "r");
            Font f;
            if (!_fontCache.TryGetValue(key, out f))
            {
                f = F(size, bold);
                _fontCache[key] = f;
            }
            return f;
        }

        private static readonly StringFormat _rtlWrap = Rtl(false);
        private static readonly StringFormat _rtlNoWrap = Rtl(true);

        public static StringFormat CachedRtl(bool noWrap)
        {
            return noWrap ? _rtlNoWrap : _rtlWrap;
        }

        public static GraphicsPath RoundPath(Rectangle r, int radius)
        {
            int d = Math.Max(2, Math.Min(radius, Math.Min(r.Width, r.Height) / 2));
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static StringFormat Rtl(bool noWrap)
        {
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            sf.FormatFlags = StringFormatFlags.DirectionRightToLeft;
            if (noWrap) sf.FormatFlags |= StringFormatFlags.NoWrap;
            return sf;
        }

        // Shared GDI+ drawing objects. Painters must never dispose these.
        private static readonly System.Collections.Generic.Dictionary<int, SolidBrush> _brushCache =
            new System.Collections.Generic.Dictionary<int, SolidBrush>();
        public static SolidBrush CachedBrush(Color c)
        {
            int key = c.ToArgb();
            SolidBrush b;
            if (!_brushCache.TryGetValue(key, out b))
            {
                b = new SolidBrush(c);
                _brushCache[key] = b;
            }
            return b;
        }

        private static readonly System.Collections.Generic.Dictionary<long, Pen> _penCache =
            new System.Collections.Generic.Dictionary<long, Pen>();
        public static Pen CachedPen(Color c, float width)
        {
            long key = ((long)c.ToArgb() << 16) | (long)(width * 100f);
            Pen p;
            if (!_penCache.TryGetValue(key, out p))
            {
                p = new Pen(c, width);
                _penCache[key] = p;
            }
            return p;
        }

        private struct PathKey
        {
            public readonly Rectangle R;
            public readonly int Radius;
            public PathKey(Rectangle r, int radius) { R = r; Radius = radius; }
            public override bool Equals(object o)
            {
                if (!(o is PathKey)) return false;
                PathKey k = (PathKey)o;
                return R.Equals(k.R) && Radius == k.Radius;
            }
            public override int GetHashCode() { return R.GetHashCode() ^ (Radius * 397); }
        }
        private static readonly System.Collections.Generic.Dictionary<PathKey, GraphicsPath> _pathCache =
            new System.Collections.Generic.Dictionary<PathKey, GraphicsPath>();
        public static GraphicsPath CachedPath(Rectangle r, int radius)
        {
            PathKey key = new PathKey(r, radius);
            GraphicsPath p;
            if (!_pathCache.TryGetValue(key, out p))
            {
                if (_pathCache.Count > 400) _pathCache.Clear();
                p = RoundPath(r, radius);
                _pathCache[key] = p;
            }
            return p;
        }
    }

    // Flat surface with one subtle border. No heavy shadows / nested cards.
    internal class Surface : Panel
    {
        private static readonly SolidBrush CardBrush = new SolidBrush(UiTheme.Card);
        private static readonly Pen LinePen = new Pen(UiTheme.Line);
        private static readonly SolidBrush ShadowA = new SolidBrush(Color.FromArgb(12, 30, 45, 60));
        private static readonly SolidBrush ShadowB = new SolidBrush(Color.FromArgb(16, 25, 40, 55));

        public Surface()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Padding = new Padding(12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            int w = Math.Max(4, Width);
            int h = Math.Max(4, Height);
            Rectangle r = new Rectangle(2, 0, Math.Max(1, w - 5), Math.Max(1, h - 5));

            // Thin shadow band just under the card: same lift, far cheaper than full paths.
            int bandH = Math.Min(4, h - r.Bottom);
            if (bandH >= 2)
            {
                e.Graphics.FillRectangle(ShadowA,
                    new Rectangle(r.Left + 1, r.Bottom, Math.Max(1, r.Width - 2), bandH / 2));
                e.Graphics.FillRectangle(ShadowB,
                    new Rectangle(r.Left + 2, r.Bottom + bandH / 2, Math.Max(1, r.Width - 4), bandH - bandH / 2));
            }

            GraphicsPath path = UiTheme.CachedPath(r, UiTheme.Radius);
            e.Graphics.FillPath(CardBrush, path);
            e.Graphics.DrawPath(LinePen, path);
        }
    }

    // Kept under the old name so existing code keeps compiling.
    internal class CardPanel : Surface { }

    // Light rounded track used behind segmented controls.
    internal class SoftPanel : Panel
    {
        public SoftPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            Rectangle r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            e.Graphics.FillPath(UiTheme.CachedBrush(UiTheme.Soft), UiTheme.CachedPath(r, 7));
        }
    }

    // Modern segmented control segment.
    internal class ModeTab : Panel
    {
        private bool _active;

        public ModeTab(string title)
        {
            Text = title;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            MouseMove += delegate { Invalidate(); };
            MouseLeave += delegate { Invalidate(); };
        }

        public bool Active
        {
            get { return _active; }
            set { _active = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            bool hover = !_active && ClientRectangle.Contains(PointToClient(MousePosition));
            Rectangle r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            if (_active || hover)
            {
                GraphicsPath path = UiTheme.CachedPath(r, 7);
                e.Graphics.FillPath(UiTheme.CachedBrush(_active ? UiTheme.Card : UiTheme.LineSoft), path);
                if (_active)
                    e.Graphics.DrawPath(UiTheme.CachedPen(UiTheme.Line, 1f), path);
            }
            // Cached font/format/brush are shared and must never be disposed here.
            e.Graphics.DrawString(Text, UiTheme.Cached(11.5f, true),
                UiTheme.CachedBrush(_active ? UiTheme.Primary : UiTheme.Muted), r, UiTheme.CachedRtl(true));
        }
    }

    internal class PillLabel : Panel
    {
        public enum Kind { Idle, Processing, Done, Error }

        private Kind _kind = Kind.Idle;

        public PillLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Height = 24;
            Width = 122;
            Text = "جاهز";
            SetKind(Kind.Idle);
        }

        public void SetKind(Kind kind)
        {
            _kind = kind;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            Color bg, fg;
            switch (_kind)
            {
                case Kind.Processing: bg = UiTheme.InfoSoft; fg = UiTheme.Info; break;
                case Kind.Done: bg = UiTheme.OkSoft; fg = UiTheme.Ok; break;
                case Kind.Error: bg = UiTheme.DangerSoft; fg = UiTheme.Danger; break;
                default: bg = UiTheme.Soft; fg = UiTheme.Muted; break;
            }
            Rectangle r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            e.Graphics.FillPath(UiTheme.CachedBrush(bg), UiTheme.CachedPath(r, r.Height / 2));
            e.Graphics.DrawString(Text, UiTheme.Cached(10, true), UiTheme.CachedBrush(fg), r, UiTheme.CachedRtl(true));
        }
    }

    internal class ProgressPanel : Panel
    {
        private int _value;

        public ProgressPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }

        public int Value
        {
            get { return _value; }
            set { _value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            Rectangle r = new Rectangle(0, Height / 2 - 3, Width - 1, 6);
            if (r.Height < 3) r.Height = 3;
            e.Graphics.FillPath(UiTheme.CachedBrush(UiTheme.LineSoft), UiTheme.CachedPath(r, 3));
            if (_value > 0)
            {
                int w = Math.Max(6, (int)((r.Width * _value) / 100f));
                Rectangle f = new Rectangle(r.Left, r.Top, Math.Min(w, r.Width), r.Height);
                e.Graphics.FillPath(UiTheme.CachedBrush(UiTheme.Primary), UiTheme.CachedPath(f, 3));
            }
        }
    }

    // Rounded border around a borderless text box.
    internal class InputHost : Panel
    {
        private readonly Control _inner;
        private bool _focused;

        public InputHost(Control inner)
        {
            _inner = inner;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Padding = new Padding(9, 2, 9, 2);
            Height = UiTheme.InputHeight;
            TextBoxBase tb = inner as TextBoxBase;
            if (tb != null) tb.BorderStyle = BorderStyle.None;
            inner.Dock = DockStyle.Fill;
            inner.Enter += delegate { _focused = true; Invalidate(); };
            inner.Leave += delegate { _focused = false; Invalidate(); };
            Controls.Add(inner);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            Rectangle r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            GraphicsPath path = UiTheme.CachedPath(r, 7);
            e.Graphics.FillPath(UiTheme.CachedBrush(UiTheme.Card), path);
            e.Graphics.DrawPath(UiTheme.CachedPen(_focused ? UiTheme.Primary : UiTheme.Line, _focused ? 1.5f : 1f), path);
        }
    }

    internal class HintTextBox : TextBox
    {
        private string _hint = "";
        private bool _hintShown;
        private bool _busy;

        public HintTextBox()
        {
            ForeColor = UiTheme.Ink;
            BorderStyle = BorderStyle.None;
        }

        public override string Text
        {
            get { return _hintShown ? string.Empty : base.Text; }
            set
            {
                _busy = true;
                _hintShown = false;
                base.Text = value ?? string.Empty;
                ForeColor = UiTheme.Ink;
                _busy = false;
                if (!Focused && string.IsNullOrEmpty(base.Text)) ShowHint();
            }
        }

        public string Hint
        {
            get { return _hint; }
            set
            {
                _hint = value ?? "";
                if (_hintShown)
                {
                    HideHint();
                    if (string.IsNullOrEmpty(base.Text)) ShowHint();
                }
                else if (!Focused && string.IsNullOrEmpty(base.Text))
                {
                    ShowHint();
                }
            }
        }

        private void ShowHint()
        {
            if (_busy || _hintShown || string.IsNullOrEmpty(_hint)) return;
            _busy = true;
            _hintShown = true;
            base.Text = _hint;
            ForeColor = UiTheme.Muted;
            _busy = false;
        }

        private void HideHint()
        {
            if (!_hintShown) return;
            _busy = true;
            _hintShown = false;
            base.Text = string.Empty;
            ForeColor = UiTheme.Ink;
            _busy = false;
        }

        protected override void OnEnter(EventArgs e)
        {
            HideHint();
            base.OnEnter(e);
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            if (string.IsNullOrEmpty(base.Text)) ShowHint();
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (_busy) return;
            if (_hintShown)
            {
                if (base.Text != _hint)
                {
                    _hintShown = false;
                    ForeColor = UiTheme.Ink;
                }
            }
            else if (!Focused && string.IsNullOrEmpty(base.Text))
            {
                ShowHint();
            }
        }
    }

    internal class DropZone : Panel
    {
        private static readonly Pen DashNormal = CreateDash(UiTheme.Line);
        private static readonly Pen DashHot = CreateDash(UiTheme.Primary);

        private static Pen CreateDash(Color c)
        {
            Pen p = new Pen(c, 1.5f);
            p.DashStyle = DashStyle.Dash;
            return p;
        }

        private PictureBox _preview;
        private Image _image;
        private string _hint;

        public DropZone(string hint)
        {
            _hint = hint;
            AllowDrop = true;
            Cursor = Cursors.Hand;
            BackColor = UiTheme.Soft;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            _preview = new PictureBox();
            _preview.Dock = DockStyle.Fill;
            _preview.SizeMode = PictureBoxSizeMode.Zoom;
            _preview.Visible = false;
            Controls.Add(_preview);
        }

        public Image Preview
        {
            get { return _image; }
            set
            {
                if (_preview.Image != null && !object.ReferenceEquals(_preview.Image, value))
                    _preview.Image.Dispose();
                _image = value;
                _preview.Image = value;
                _preview.Visible = value != null;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            bool hot = ClientRectangle.Contains(PointToClient(MousePosition));
            Rectangle r = new Rectangle(1, 1, Math.Max(1, Width - 2), Math.Max(1, Height - 2));
            GraphicsPath path = UiTheme.CachedPath(r, 9);
            e.Graphics.FillPath(UiTheme.CachedBrush(hot ? UiTheme.PrimarySoft : UiTheme.Soft), path);
            e.Graphics.DrawPath(hot ? DashHot : DashNormal, path);
            if (_image != null) return;
            int cx = Width / 2;
            Pen icon = UiTheme.CachedPen(UiTheme.Muted, 1.6f);
            Rectangle box = new Rectangle(cx - 18, Height / 2 - 34, 36, 26);
            e.Graphics.DrawRectangle(icon, box);
            e.Graphics.DrawLine(icon, cx - 16, box.Bottom - 2, cx - 4, box.Top + 9);
            e.Graphics.DrawLine(icon, cx + 3, box.Bottom - 2, cx + 14, box.Top + 14);
            e.Graphics.DrawString("اسحب الصورة هنا", UiTheme.Cached(11, true), UiTheme.CachedBrush(UiTheme.Ink),
                new RectangleF(0, Height / 2 + 2, Width, 24), UiTheme.CachedRtl(false));
            e.Graphics.DrawString(_hint, UiTheme.Cached(9.5f, false), UiTheme.CachedBrush(UiTheme.Muted),
                new RectangleF(0, Height / 2 + 26, Width, 20), UiTheme.CachedRtl(false));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _preview != null && _preview.Image != null)
            {
                _preview.Image.Dispose();
                _preview.Image = null;
            }
            base.Dispose(disposing);
        }
    }

    // Custom painted button with hover / pressed / disabled / loading states.
    internal class RoundedButton : Button
    {
        public enum BStyle { Primary, Soft, Ghost, Danger }

        private BStyle _style = BStyle.Soft;
        private bool _hover;
        private bool _pressed;
        private bool _loading;
        private string _loadingText = "شغال...";
        private Timer _spin;

        public RoundedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            RightToLeft = RightToLeft.Yes;
            TextAlign = ContentAlignment.MiddleCenter;
            AutoSize = false;
            MouseEnter += delegate { _hover = true; Invalidate(); };
            MouseLeave += delegate { _hover = false; _pressed = false; Invalidate(); };
            MouseDown += delegate { _pressed = true; Invalidate(); };
            MouseUp += delegate { _pressed = false; Invalidate(); };
        }

        public bool Loading
        {
            get { return _loading; }
            set
            {
                if (_loading == value) return;
                _loading = value;
                if (value)
                {
                    _spin = new Timer();
                    _spin.Interval = 180;
                    _spin.Tick += delegate { Invalidate(); };
                    _spin.Start();
                }
                else if (_spin != null)
                {
                    _spin.Stop();
                    _spin.Dispose();
                    _spin = null;
                }
                Invalidate();
            }
        }

        public string LoadingText
        {
            get { return _loadingText; }
            set { _loadingText = value ?? "شغال..."; Invalidate(); }
        }

        public void ApplyPrimary()
        {
            _style = BStyle.Primary;
            ForeColor = Color.White;
            Font = UiTheme.F(11.5f, true);
            Height = UiTheme.ButtonHeight;
            Invalidate();
        }

        public void ApplySoft()
        {
            _style = BStyle.Soft;
            ForeColor = UiTheme.Primary;
            Font = UiTheme.F(10.5f, true);
            Height = UiTheme.ButtonHeight;
            Invalidate();
        }

        public void ApplyGhost()
        {
            _style = BStyle.Ghost;
            ForeColor = UiTheme.Muted;
            Font = UiTheme.F(10.5f, true);
            Height = UiTheme.ButtonHeight;
            Invalidate();
        }

        public void ApplyDanger()
        {
            _style = BStyle.Danger;
            ForeColor = UiTheme.Danger;
            Font = UiTheme.F(10.5f, true);
            Height = UiTheme.ButtonHeight;
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRegion();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width < 4 || Height < 4) return;
            using (GraphicsPath path = UiTheme.RoundPath(new Rectangle(0, 0, Width, Height), 7))
                Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            Rectangle r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            bool enabled = Enabled && !_loading;
            Color fill, text, border = UiTheme.Line;

            switch (_style)
            {
                case BStyle.Primary:
                    fill = !enabled ? Color.FromArgb(178, 193, 205)
                         : _pressed ? UiTheme.PrimaryDark
                         : _hover ? Color.FromArgb(29, 81, 121) : UiTheme.Primary;
                    text = Color.White;
                    break;
                case BStyle.Danger:
                    fill = !enabled ? UiTheme.Soft : (_hover ? UiTheme.DangerSoft : UiTheme.Card);
                    text = !enabled ? UiTheme.Muted : UiTheme.Danger;
                    border = UiTheme.Line;
                    break;
                case BStyle.Soft:
                    fill = !enabled ? UiTheme.Soft : (_hover ? UiTheme.LineSoft : UiTheme.PrimarySoft);
                    text = !enabled ? UiTheme.Muted : UiTheme.Primary;
                    break;
                default:
                    fill = !enabled ? UiTheme.Soft : (_hover ? UiTheme.Soft : UiTheme.Card);
                    text = !enabled ? UiTheme.Muted : ForeColor;
                    break;
            }

            GraphicsPath path = UiTheme.CachedPath(r, 7);
            e.Graphics.FillPath(UiTheme.CachedBrush(fill), path);
            if (_style == BStyle.Primary && enabled)
            {
                // Soft inner shade along the bottom so the main button feels raised.
                Rectangle band = new Rectangle(r.Left + 1, r.Bottom - 5, Math.Max(1, r.Width - 2), 5);
                e.Graphics.FillPath(UiTheme.CachedBrush(Color.FromArgb(26, 12, 28, 46)), UiTheme.CachedPath(band, 5));
            }
            if (_style == BStyle.Ghost || _style == BStyle.Danger)
                e.Graphics.DrawPath(UiTheme.CachedPen(border, 1f), path);

            string label = _loading ? _loadingText : Text;
            if (_loading)
            {
                int dots = (Environment.TickCount / 240) % 4;
                label = _loadingText + new string('.', dots);
                text = UiTheme.Muted;
            }

            Rectangle textRect = r;
            if (_loading) textRect = new Rectangle(r.Left + 14, r.Top, r.Width - 14, r.Height);
            // Never dispose the control's own Font — it is reused on every paint.
            Font f = Font ?? UiTheme.Cached(10.5f, true);
            e.Graphics.DrawString(label, f, UiTheme.CachedBrush(text), textRect, UiTheme.CachedRtl(true));
            if (_loading)
            {
                int a = Environment.TickCount / 60;
                e.Graphics.DrawArc(UiTheme.CachedPen(UiTheme.Primary, 2f),
                    r.Left + 4, r.Top + (r.Height - 12) / 2, 12, 12, a, 250);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _spin != null) { _spin.Dispose(); _spin = null; }
            base.Dispose(disposing);
        }
    }

    internal class NumberBadge : Panel
    {
        public int Number;
        private bool _filled;

        public NumberBadge(int number)
        {
            Number = number;
            Width = 30;
            Height = 26;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }

        public void SetFilled(bool filled)
        {
            if (_filled == filled) return;
            _filled = filled;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            Rectangle r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            e.Graphics.FillPath(UiTheme.CachedBrush(_filled ? UiTheme.Primary : UiTheme.PrimarySoft),
                UiTheme.CachedPath(r, 6));
            e.Graphics.DrawString(Number.ToString(), UiTheme.Cached(10, true),
                UiTheme.CachedBrush(_filled ? Color.White : UiTheme.Primary), r, UiTheme.CachedRtl(true));
        }
    }

    internal enum CardStatus { Empty, Incomplete, Processing, Done, Error }

    internal class StatusPill : Panel
    {
        private CardStatus _status = CardStatus.Empty;

        public StatusPill()
        {
            Width = 96;
            Height = 24;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }

        public void SetStatus(CardStatus status)
        {
            if (_status == status) return;
            _status = status;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            Color bg, fg;
            string label;
            switch (_status)
            {
                case CardStatus.Processing: bg = UiTheme.InfoSoft; fg = UiTheme.Info; label = "شغال"; break;
                case CardStatus.Done: bg = UiTheme.OkSoft; fg = UiTheme.Ok; label = "خلصت"; break;
                case CardStatus.Error: bg = UiTheme.DangerSoft; fg = UiTheme.Danger; label = "خطأ"; break;
                case CardStatus.Incomplete: bg = UiTheme.WarnSoft; fg = UiTheme.Warn; label = "ناقصة"; break;
                default: bg = UiTheme.Soft; fg = UiTheme.Muted; label = "فاضية"; break;
            }
            Rectangle r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            e.Graphics.FillPath(UiTheme.CachedBrush(bg), UiTheme.CachedPath(r, r.Height / 2));
            e.Graphics.FillEllipse(UiTheme.CachedBrush(fg), r.Left + 7, r.Top + (r.Height - 6) / 2, 6, 6);
            e.Graphics.DrawString(label, UiTheme.Cached(9.5f, true), UiTheme.CachedBrush(fg),
                new RectangleF(r.Left, r.Top, r.Width - 12, r.Height), UiTheme.CachedRtl(true));
        }
    }

    // Compact, collapsible image editor row.
    internal class BatchCard : Surface
    {
        public const int ExpandedHeight = 96;
        public const int CollapsedHeight = 44;

        public HintTextBox TitleBox;
        public HintTextBox TextBox;
        public RoundedButton CopyButton;
        public RoundedButton ClearButton;

        private readonly NumberBadge _badge;
        private readonly StatusPill _status;
        private readonly RoundedButton _toggle;
        private readonly Panel _fields;
        private bool _collapsed;

        public event EventHandler ContentChanged;
        public event EventHandler CopyRequested;
        public event EventHandler ClearRequested;
        public event EventHandler HeightChanged;

        public BatchCard(int number)
        {
            Padding = new Padding(8);
            Height = ExpandedHeight;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.Margin = Padding.Empty;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _badge = new NumberBadge(number);
            _badge.Dock = DockStyle.Fill;
            _status = new StatusPill();
            _status.Dock = DockStyle.Fill;

            CopyButton = new RoundedButton();
            CopyButton.Text = "انسخ النص";
            CopyButton.ApplySoft();
            CopyButton.Dock = DockStyle.Fill;
            CopyButton.Click += delegate(object s, EventArgs e) { if (CopyRequested != null) CopyRequested(this, e); };

            ClearButton = new RoundedButton();
            ClearButton.Text = "مسح";
            ClearButton.ApplyGhost();
            ClearButton.Dock = DockStyle.Fill;
            ClearButton.Click += delegate(object s, EventArgs e) { if (ClearRequested != null) ClearRequested(this, e); };

            _toggle = new RoundedButton();
            _toggle.Text = "▴";
            _toggle.Font = new Font("Segoe UI", 9f);
            _toggle.ApplyGhost();
            _toggle.Dock = DockStyle.Fill;
            _toggle.Click += delegate { Collapsed = !Collapsed; };

            TableLayoutPanel tools = new TableLayoutPanel();
            tools.Dock = DockStyle.Fill;
            tools.ColumnCount = 6;
            tools.RowCount = 1;
            tools.Margin = Padding.Empty;
            tools.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30f));
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84f));
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64f));
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30f));
            tools.Controls.Add(_badge, 0, 0);
            tools.Controls.Add(_status, 1, 0);
            tools.Controls.Add(CopyButton, 3, 0);
            tools.Controls.Add(ClearButton, 4, 0);
            tools.Controls.Add(_toggle, 5, 0);
            root.Controls.Add(tools, 0, 0);

            TableLayoutPanel fields = new TableLayoutPanel();
            fields.Dock = DockStyle.Fill;
            fields.ColumnCount = 2;
            fields.RowCount = 1;
            fields.Margin = Padding.Empty;
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));

            TitleBox = new HintTextBox();
            TitleBox.Hint = "اسم الملف من غير .png";
            TitleBox.Font = UiTheme.F(10, false);
            fields.Controls.Add(Field("العنوان (اسم الملف)", TitleBox), 0, 0);

            TextBox = new HintTextBox();
            TextBox.Multiline = true;
            TextBox.AcceptsReturn = true;
            TextBox.ScrollBars = ScrollBars.Vertical;
            TextBox.Hint = "النص اللي هيظهر جوّه الصورة...";
            TextBox.Font = UiTheme.F(10, false);
            fields.Controls.Add(Field("النص جوّه الصورة", TextBox), 1, 0);

            _fields = new Panel();
            _fields.Dock = DockStyle.Fill;
            _fields.Margin = Padding.Empty;
            _fields.Controls.Add(fields);
            root.Controls.Add(_fields, 0, 1);

            Controls.Add(root);

            TitleBox.TextChanged += delegate { NotifyChanged(); };
            TextBox.TextChanged += delegate { NotifyChanged(); };
            NotifyChanged();
        }

        private static Panel Field(string label, Control box)
        {
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.Margin = new Padding(0, 0, 6, 0);
            t.ColumnCount = 1;
            t.RowCount = 2;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 16f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label l = new Label();
            l.Text = label;
            l.Dock = DockStyle.Fill;
            l.Font = UiTheme.F(9, true);
            l.ForeColor = UiTheme.Muted;
            l.TextAlign = ContentAlignment.MiddleRight;
            t.Controls.Add(l, 0, 0);

            InputHost host = new InputHost(box);
            host.Dock = DockStyle.Fill;
            host.Margin = Padding.Empty;
            t.Controls.Add(host, 0, 1);
            return t;
        }

        public bool Collapsed
        {
            get { return _collapsed; }
            set
            {
                if (_collapsed == value) return;
                _collapsed = value;
                _fields.Visible = !value;
                _toggle.Text = value ? "▾" : "▴";
                Height = value ? CollapsedHeight : ExpandedHeight;
                if (HeightChanged != null) HeightChanged(this, EventArgs.Empty);
            }
        }

        public void SetStatus(CardStatus status)
        {
            _status.SetStatus(status);
        }

        private void NotifyChanged()
        {
            bool hasText = TextBox.Text.Length > 0;
            bool hasTitle = TitleBox.Text.Length > 0;
            _badge.SetFilled(hasText);
            _status.SetStatus(hasText ? CardStatus.Done
                : (hasTitle ? CardStatus.Incomplete : CardStatus.Empty));
            if (ContentChanged != null) ContentChanged(this, EventArgs.Empty);
        }

        public int Number { get { return _badge.Number; } }
        public string TitleValue { get { return TitleBox.Text; } }
        public string TextValue { get { return TextBox.Text; } }

        public void Clear()
        {
            TitleBox.Text = "";
            TextBox.Text = "";
            _badge.SetFilled(false);
            _status.SetStatus(CardStatus.Empty);
        }
    }

    // Single scrolling region. Content height is set explicitly from its rows.
    internal class ScrollHost : Panel
    {
        public TableLayoutPanel Content;

        public ScrollHost()
        {
            AutoScroll = true;
            BackColor = UiTheme.Card;
            Content = new TableLayoutPanel();
            Content.Dock = DockStyle.Top;
            Content.ColumnCount = 1;
            Content.RowCount = 1;
            Content.Margin = Padding.Empty;
            Content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            Controls.Add(Content);
        }

        public void FitContent()
        {
            int h = 0;
            for (int i = 0; i < Content.RowStyles.Count; i++)
                if (Content.RowStyles[i].SizeType == SizeType.Absolute)
                    h += (int)Content.RowStyles[i].Height;
            if (h > 0) Content.Height = h;
            SizeWidth();
        }

        private void SizeWidth()
        {
            int reserve = VScroll ? SystemInformation.VerticalScrollBarWidth + 8 : 6;
            Content.Width = Math.Max(10, ClientSize.Width - reserve);
        }

        public void SetRowHeight(int row, int height)
        {
            if (row >= 0 && row < Content.RowStyles.Count)
                Content.RowStyles[row].Height = height;
            FitContent();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            SizeWidth();
        }
    }
}
