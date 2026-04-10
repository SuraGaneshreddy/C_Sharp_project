using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FinanceTracker.Forms
{
    /// <summary>
    /// Polished custom TextBox with:
    ///   - Rounded corners drawn via GDI+
    ///   - Greyed placeholder text
    ///   - Coloured border that glows on focus
    ///   - Password masking toggle
    /// </summary>
    public class RoundedTextBox : Panel
    {
        // ── Public Properties ─────────────────────────────────────────────────────
        public string PlaceholderText    { get; set; } = "";
        public Color  BorderColor        { get; set; } = Color.FromArgb(51, 65, 85);
        public Color  FocusBorderColor   { get; set; } = Color.FromArgb(99, 102, 241);
        public int    CornerRadius       { get; set; } = 10;

        /// <summary>
        /// Use 'new' to intentionally hide Panel.Text and redirect to inner TextBox.
        /// (Fixes CS0114 warning)
        /// </summary>
        public new string Text
        {
            get => _innerTxt.Text;
            set => _innerTxt.Text = value;
        }

        public new Font Font
        {
            get => _innerTxt.Font;
            set => _innerTxt.Font = value;
        }

        public new Color ForeColor
        {
            get => _innerTxt.ForeColor;
            set => _innerTxt.ForeColor = value;
        }

        public new Color BackColor
        {
            get => base.BackColor;
            set
            {
                base.BackColor      = value;
                _innerTxt.BackColor = value;
            }
        }

        public bool IsPassword
        {
            get => _innerTxt.UseSystemPasswordChar;
            set
            {
                _innerTxt.UseSystemPasswordChar = value;
                _innerTxt.Invalidate();
            }
        }

        /// <summary>Expose inner TextBox for event binding (e.g. TextChanged).</summary>
        public TextBox InnerTextBox => _innerTxt;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly TextBox _innerTxt;
        private bool             _focused;
        private readonly Color   _placeholderColor = Color.FromArgb(100, 148, 163, 184);

        // ── Constructor ───────────────────────────────────────────────────────────
        public RoundedTextBox()
        {
            DoubleBuffered = true;
            Padding        = new Padding(12, 0, 12, 0);

            _innerTxt = new TextBox
            {
                BorderStyle             = BorderStyle.None,
                BackColor               = Color.FromArgb(30, 35, 54),
                ForeColor               = Color.FromArgb(241, 245, 249),
                Font                    = new Font("Segoe UI", 11f),
                UseSystemPasswordChar   = false,
                Multiline               = false,
                TabStop                 = true
            };

            _innerTxt.GotFocus   += (s, e) => { _focused = true;  Invalidate(); };
            _innerTxt.LostFocus  += (s, e) => { _focused = false; Invalidate(); };
            _innerTxt.TextChanged+= (s, e) => Invalidate();   // refresh placeholder visibility

            Controls.Add(_innerTxt);
            Resize += (s, e) => PositionInner();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            PositionInner();
        }

        private void PositionInner()
        {
            // Guard: OnLayout fires before constructor finishes creating _innerTxt
            if (_innerTxt == null) return;
            const int pad = 14;
            int h = _innerTxt.PreferredHeight;
            _innerTxt.SetBounds(pad, (Height - h) / 2, Width - pad * 2, h);
        }

        // ── Painting ─────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Guard: can fire before _innerTxt is created
            if (_innerTxt == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Rounded background
            using (var path = MakeRoundedPath(0, 0, Width - 1, Height - 1, CornerRadius))
            using (var br   = new SolidBrush(base.BackColor))
                g.FillPath(br, path);

            // Border — glows when focused
            using (var path = MakeRoundedPath(0, 0, Width - 1, Height - 1, CornerRadius))
            {
                if (_focused)
                {
                    // Outer glow ring
                    using (var glowPen = new Pen(Color.FromArgb(55, FocusBorderColor), 5))
                        g.DrawPath(glowPen, path);
                }
                Color borderC = _focused ? FocusBorderColor : BorderColor;
                using (var pen = new Pen(borderC, _focused ? 2f : 1f))
                    g.DrawPath(pen, path);
            }

            // Placeholder text (shown when inner text box is empty)
            if (string.IsNullOrEmpty(_innerTxt.Text))
            {
                using (var br = new SolidBrush(_placeholderColor))
                    g.DrawString(
                        PlaceholderText,
                        _innerTxt.Font,
                        br,
                        new RectangleF(_innerTxt.Left, _innerTxt.Top,
                                       _innerTxt.Width, _innerTxt.Height),
                        new StringFormat { LineAlignment = StringAlignment.Center });
            }
        }

        // Clicking anywhere on the panel focuses the inner TextBox
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            _innerTxt.Focus();
        }

        // ── Helper ────────────────────────────────────────────────────────────────
        private static GraphicsPath MakeRoundedPath(int x, int y, int w, int h, int r)
        {
            if (r < 1) r = 1;
            var path = new GraphicsPath();
            path.AddArc(x,             y,             r * 2, r * 2, 180, 90);
            path.AddArc(x + w - r * 2, y,             r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0,   90);
            path.AddArc(x,             y + h - r * 2, r * 2, r * 2, 90,  90);
            path.CloseFigure();
            return path;
        }
    }
}
