using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FinanceTracker.Forms
{
    /// <summary>
    /// Custom animated button with smooth hover/press transitions,
    /// rounded corners, and IButtonControl (AcceptButton support).
    /// </summary>
    public class AnimatedButton : Control, IButtonControl
    {
        // ── Public Properties ─────────────────────────────────────────────────────
        public Color NormalColor  { get; set; } = Color.FromArgb(99, 102, 241);
        public Color HoverColor   { get; set; } = Color.FromArgb(118, 121, 255);
        public Color PressColor   { get; set; } = Color.FromArgb(79, 82, 210);
        public Color BorderColor  { get; set; } = Color.Transparent;
        public int   CornerRadius { get; set; } = 12;

        // ── IButtonControl (required for Form.AcceptButton) ───────────────────────
        public DialogResult DialogResult { get; set; } = DialogResult.None;

        public void NotifyDefault(bool value) { /* no visual change needed */ }

        public void PerformClick() => OnClick(EventArgs.Empty);

        // ── Animation state ───────────────────────────────────────────────────────
        private bool   _hovered;
        private bool   _pressed;
        private Color  _currentColor;
        private Color  _fromColor;
        private Color  _toColor;
        private double _animProgress;
        private readonly Timer _animTimer;

        public AnimatedButton()
        {
            SetStyle(
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint              |
                ControlStyles.AllPaintingInWmPaint   |
                ControlStyles.DoubleBuffer, true);

            BackColor     = Color.Transparent;
            Cursor        = Cursors.Hand;
            _currentColor = NormalColor;

            _animTimer          = new Timer { Interval = 16 };
            _animTimer.Tick    += AnimTick;
        }

        // ── Paint ─────────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Choose current display color
            Color bg = _animTimer.Enabled
                ? _currentColor
                : (_pressed ? PressColor : (_hovered ? HoverColor : NormalColor));

            using (var path = MakeRoundedPath(0, 0, Width - 1, Height - 1, CornerRadius))
            {
                // Soft drop shadow (skip for transparent buttons)
                if (NormalColor != Color.Transparent && !_pressed)
                {
                    using (var shadowPath = MakeRoundedPath(1, 3, Width - 1, Height - 1, CornerRadius))
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                        g.FillPath(shadowBrush, shadowPath);
                }

                // Button fill
                using (var br = new SolidBrush(bg))
                    g.FillPath(br, path);

                // Optional border
                if (BorderColor != Color.Transparent)
                    using (var pen = new Pen(BorderColor, 1f))
                        g.DrawPath(pen, path);
            }

            // Label text
            using (var br = new SolidBrush(ForeColor))
            using (var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming      = StringTrimming.EllipsisCharacter
            })
                g.DrawString(Text, Font, br, new Rectangle(4, 0, Width - 8, Height), sf);
        }

        // ── Mouse events ──────────────────────────────────────────────────────────
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            AnimateTo(HoverColor);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            _pressed = false;
            AnimateTo(NormalColor);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                AnimateTo(PressColor);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            AnimateTo(_hovered ? HoverColor : NormalColor);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            // Propagate DialogResult to parent form (needed for AcceptButton behaviour)
            if (DialogResult != DialogResult.None)
            {
                var form = FindForm();
                if (form != null) form.DialogResult = DialogResult;
            }
        }

        // Enter / Space key trigger
        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Enter || keyData == Keys.Space) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                PerformClick();
        }

        // ── Color animation ───────────────────────────────────────────────────────
        private void AnimateTo(Color target)
        {
            _fromColor    = _currentColor;
            _toColor      = target;
            _animProgress = 0;
            _animTimer.Start();
        }

        private void AnimTick(object sender, EventArgs e)
        {
            _animProgress += 0.18;
            if (_animProgress >= 1.0)
            {
                _currentColor = _toColor;
                _animTimer.Stop();
            }
            else
            {
                double t = _animProgress;
                _currentColor = Color.FromArgb(
                    Lerp(_fromColor.A, _toColor.A, t),
                    Lerp(_fromColor.R, _toColor.R, t),
                    Lerp(_fromColor.G, _toColor.G, t),
                    Lerp(_fromColor.B, _toColor.B, t));
            }
            Invalidate();
        }

        private static int Lerp(int a, int b, double t) =>
            (int)Math.Round(a + (b - a) * t);

        // ── Helper ────────────────────────────────────────────────────────────────
        private static GraphicsPath MakeRoundedPath(int x, int y, int w, int h, int r)
        {
            if (r < 1) r = 1;
            var path = new GraphicsPath();
            path.AddArc(x,         y,         r * 2, r * 2, 180, 90);
            path.AddArc(x + w - r * 2, y,     r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(x,         y + h - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
