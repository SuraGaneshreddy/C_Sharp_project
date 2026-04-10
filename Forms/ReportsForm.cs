using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FinanceTracker.Database;

namespace FinanceTracker.Forms
{
    public class ReportsForm : Form
    {
        private TabControl _tabs;
        private ComboBox _cmbMonth, _cmbYear;
        private Panel _piePanel, _barPanel, _trendPanel;
        private DataTable _expenseDt, _incomeDt, _trendDt;

        private readonly Color C_BG    = Color.FromArgb(15, 17, 26);
        private readonly Color C_CARD  = Color.FromArgb(30, 34, 50);
        private readonly Color C_ACCENT= Color.FromArgb(99, 102, 241);
        private readonly Color C_GREEN = Color.FromArgb(52, 211, 153);
        private readonly Color C_RED   = Color.FromArgb(248, 113, 113);
        private readonly Color C_TEXT  = Color.FromArgb(241, 245, 249);
        private readonly Color C_MUTED = Color.FromArgb(148, 163, 184);
        private readonly Color C_BORDER= Color.FromArgb(51, 65, 85);
        private readonly Color C_YELLOW= Color.FromArgb(251, 191, 36);

        public ReportsForm() { SetupUI(); LoadReport(); }

        private void SetupUI()
        {
            BackColor = C_BG; ForeColor = C_TEXT; Font = new Font("Segoe UI", 9.5f);

            // Top filter bar
            var topBar = new Panel { Dock=DockStyle.Top, Height=56, BackColor=Color.FromArgb(22,25,37), Padding=new Padding(16,10,16,0) };
            topBar.Paint += (s,e)=>e.Graphics.DrawLine(new Pen(C_BORDER),0,55,topBar.Width,55);

            _cmbMonth = MakeCombo(new[]{"Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"});
            _cmbMonth.SelectedIndex = DateTime.Now.Month-1; _cmbMonth.Location=new Point(0,8); _cmbMonth.Width=80;
            _cmbYear = MakeCombo(new[]{"2024","2025","2026"});
            _cmbYear.SelectedIndex=1; _cmbYear.Location=new Point(90,8); _cmbYear.Width=72;
            var btnLoad = MakeBtn("📊 Load Report", C_ACCENT, Color.White);
            btnLoad.Location=new Point(174,6); btnLoad.Size=new Size(140,36);
            btnLoad.Click+=(s,e)=>LoadReport();
            topBar.Controls.AddRange(new Control[]{_cmbMonth,_cmbYear,btnLoad});
            Controls.Add(topBar);

            // Tabs
            _tabs = new TabControl { Dock=DockStyle.Fill, Font=new Font("Segoe UI",9.5f) };
            _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            _tabs.DrawItem += DrawTab;
            _tabs.ItemSize = new Size(130, 36);
            _tabs.SizeMode = TabSizeMode.Fixed;

            var tabExpPie = new TabPage("Expense Breakdown") { BackColor=C_BG, BorderStyle=BorderStyle.None };
            var tabIncomePie = new TabPage("Income Sources") { BackColor=C_BG };
            var tabTrend = new TabPage("Monthly Trend") { BackColor=C_BG };
            var tabSummary = new TabPage("Summary Table") { BackColor=C_BG };

            _piePanel = new Panel { Dock=DockStyle.Fill, BackColor=C_BG };
            _piePanel.Paint += DrawExpensePie;
            tabExpPie.Controls.Add(_piePanel);

            _barPanel = new Panel { Dock=DockStyle.Fill, BackColor=C_BG };
            _barPanel.Paint += DrawIncomeBar;
            tabIncomePie.Controls.Add(_barPanel);

            _trendPanel = new Panel { Dock=DockStyle.Fill, BackColor=C_BG };
            _trendPanel.Paint += DrawTrendChart;
            tabTrend.Controls.Add(_trendPanel);

            // Summary table tab
            var summaryGrid = BuildSummaryGrid();
            summaryGrid.Name = "summaryGrid";
            tabSummary.Controls.Add(summaryGrid);

            _tabs.TabPages.Add(tabExpPie);
            _tabs.TabPages.Add(tabIncomePie);
            _tabs.TabPages.Add(tabTrend);
            _tabs.TabPages.Add(tabSummary);
            _tabs.SelectedIndexChanged += (s,e)=>_tabs.SelectedTab.Invalidate();
            Controls.Add(_tabs);
        }

        private void DrawTab(object s, DrawItemEventArgs e)
        {
            bool selected = e.Index == _tabs.SelectedIndex;
            using var br = new SolidBrush(selected ? C_CARD : Color.FromArgb(22,25,37));
            e.Graphics.FillRectangle(br, e.Bounds);
            using var fBr = new SolidBrush(selected ? C_TEXT : C_MUTED);
            e.Graphics.DrawString(_tabs.TabPages[e.Index].Text, new Font("Segoe UI",9f,selected?FontStyle.Bold:FontStyle.Regular), fBr, e.Bounds, new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center});
            if (selected) e.Graphics.DrawLine(new Pen(C_ACCENT,2), e.Bounds.X, e.Bounds.Bottom-2, e.Bounds.Right, e.Bounds.Bottom-2);
        }

        private void LoadReport()
        {
            int month = _cmbMonth.SelectedIndex + 1;
            int year = int.Parse(_cmbYear.SelectedItem.ToString());
            _expenseDt = DatabaseManager.Instance.GetCategoryBreakdown("Expense", month, year);
            _incomeDt  = DatabaseManager.Instance.GetCategoryBreakdown("Income", month, year);
            _trendDt   = DatabaseManager.Instance.GetMonthlyTrend(6);
            _piePanel.Invalidate(); _barPanel.Invalidate(); _trendPanel.Invalidate();
            RefreshSummaryGrid(month, year);
        }

        // ─── PIE CHART ───────────────────────────────────────────────────────────
        private void DrawExpensePie(object s, PaintEventArgs e)
        {
            DrawPieChart(e.Graphics, _piePanel.ClientRectangle, _expenseDt, "Expense Breakdown by Category", C_RED);
        }

        private void DrawIncomeBar(object s, PaintEventArgs e)
        {
            DrawPieChart(e.Graphics, _barPanel.ClientRectangle, _incomeDt, "Income Breakdown by Source", C_GREEN);
        }

        private void DrawPieChart(Graphics g, Rectangle bounds, DataTable dt, string title, Color baseColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(C_BG);

            // Title
            using (var br = new SolidBrush(C_TEXT))
                g.DrawString(title, new Font("Segoe UI",13f,FontStyle.Bold), br, 24, 20);

            if (dt == null || dt.Rows.Count == 0)
            {
                using var br = new SolidBrush(C_MUTED);
                g.DrawString("No data for selected period.", new Font("Segoe UI",11f), br, 24, 70);
                return;
            }

            decimal total = 0;
            foreach (DataRow row in dt.Rows) total += Convert.ToDecimal(row["Total"]);
            if (total == 0) return;

            var colors = new[]
            {
                Color.FromArgb(99,102,241), Color.FromArgb(248,113,113), Color.FromArgb(52,211,153),
                Color.FromArgb(251,191,36), Color.FromArgb(167,139,250), Color.FromArgb(249,168,212),
                Color.FromArgb(94,234,212), Color.FromArgb(253,224,71),  Color.FromArgb(196,181,253),
                Color.FromArgb(134,239,172), Color.FromArgb(147,197,253), Color.FromArgb(252,165,165)
            };

            int pieX = 60, pieY = 60, pieSize = Math.Min(bounds.Width/2-80, bounds.Height-120);
            float startAngle = -90f;
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                decimal val = Convert.ToDecimal(row["Total"]);
                float sweep = (float)(val / total) * 360f;
                Color c;
                try { c = ColorTranslator.FromHtml(row["Color"].ToString()); } catch { c = colors[i % colors.Length]; }

                using (var br = new SolidBrush(c))
                    g.FillPie(br, pieX, pieY, pieSize, pieSize, startAngle, sweep);
                using (var p = new Pen(C_BG, 2))
                    g.DrawPie(p, pieX, pieY, pieSize, pieSize, startAngle, sweep);

                // Label inside slice (if big enough)
                if (sweep > 20)
                {
                    double mid = (startAngle + sweep / 2.0) * Math.PI / 180;
                    float lx = pieX + pieSize/2 + (float)(Math.Cos(mid) * pieSize/3.5f);
                    float ly = pieY + pieSize/2 + (float)(Math.Sin(mid) * pieSize/3.5f);
                    string pctStr = $"{val/total*100:F1}%";
                    using (var br = new SolidBrush(Color.White))
                        g.DrawString(pctStr, new Font("Segoe UI",8f,FontStyle.Bold), br, lx-18, ly-7);
                }

                startAngle += sweep; i++;
            }

            // Hole (donut)
            int holeSize = pieSize / 3;
            int holeX = pieX + (pieSize - holeSize) / 2;
            int holeY = pieY + (pieSize - holeSize) / 2;
            using (var br = new SolidBrush(C_BG)) g.FillEllipse(br, holeX, holeY, holeSize, holeSize);
            using (var br = new SolidBrush(C_TEXT))
            using (var sf = new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
                g.DrawString(total.ToString("C0"), new Font("Segoe UI",10f,FontStyle.Bold), br, new Rectangle(holeX,holeY,holeSize,holeSize), sf);

            // Legend
            int legX = pieX + pieSize + 40, legY = pieY + 10;
            i = 0;
            foreach (DataRow row in dt.Rows)
            {
                Color c;
                try { c = ColorTranslator.FromHtml(row["Color"].ToString()); } catch { c = colors[i % colors.Length]; }
                decimal val = Convert.ToDecimal(row["Total"]);
                using (var br = new SolidBrush(c))
                    g.FillRectangle(br, legX, legY + i * 26, 14, 14);
                using (var br = new SolidBrush(C_TEXT))
                    g.DrawString($"{row["Name"]}  {val:C}", new Font("Segoe UI",9f), br, legX + 20, legY + i * 26);
                i++;
            }
        }

        // ─── TREND CHART ─────────────────────────────────────────────────────────
        private void DrawTrendChart(object s, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(C_BG);
            using (var br = new SolidBrush(C_TEXT))
                g.DrawString("Income vs Expenses — Last 6 Months", new Font("Segoe UI",13f,FontStyle.Bold), br, 24, 20);

            if (_trendDt == null || _trendDt.Rows.Count == 0)
            {
                using var br = new SolidBrush(C_MUTED);
                g.DrawString("No data available.", new Font("Segoe UI",11f), br, 24, 70); return;
            }

            var bounds = _trendPanel.ClientRectangle;
            int padL=80, padR=30, padT=60, padB=60;
            int chartW = bounds.Width - padL - padR;
            int chartH = bounds.Height - padT - padB;

            decimal maxVal = 0;
            foreach (DataRow r in _trendDt.Rows)
            {
                decimal inc = Convert.ToDecimal(r["Income"]), exp = Convert.ToDecimal(r["Expense"]);
                if (inc > maxVal) maxVal = inc; if (exp > maxVal) maxVal = exp;
            }
            if (maxVal == 0) maxVal = 1;

            // Grid lines
            int gridLines = 5;
            for (int gi=0; gi<=gridLines; gi++)
            {
                int gy = padT + chartH - (int)(chartH * gi / (double)gridLines);
                using (var p = new Pen(Color.FromArgb(40,255,255,255), 1) { DashStyle=DashStyle.Dash })
                    g.DrawLine(p, padL, gy, padL+chartW, gy);
                decimal lval = maxVal * gi / gridLines;
                using (var br = new SolidBrush(C_MUTED))
                using (var sf = new StringFormat{Alignment=StringAlignment.Far})
                    g.DrawString(lval.ToString("C0"), new Font("Segoe UI",8f), br, padL-4, gy-8, sf);
            }

            int n = _trendDt.Rows.Count;
            float step = n > 1 ? (float)chartW / (n - 1) : chartW;

            var incPts = new PointF[n];
            var expPts = new PointF[n];
            for (int idx=0; idx<n; idx++)
            {
                var row = _trendDt.Rows[idx];
                float x = padL + idx * step;
                float incY = padT + chartH - (float)(Convert.ToDecimal(row["Income"]) / maxVal * chartH);
                float expY = padT + chartH - (float)(Convert.ToDecimal(row["Expense"]) / maxVal * chartH);
                incPts[idx] = new PointF(x, incY);
                expPts[idx] = new PointF(x, expY);
                // X labels
                using (var br = new SolidBrush(C_MUTED))
                using (var sf = new StringFormat{Alignment=StringAlignment.Center})
                    g.DrawString(row["Month"].ToString(), new Font("Segoe UI",8f), br, x, padT+chartH+8);
            }

            if (n > 1)
            {
                using (var p = new Pen(C_GREEN, 3){LineJoin=LineJoin.Round}) g.DrawLines(p, incPts);
                using (var p = new Pen(C_RED, 3){LineJoin=LineJoin.Round}) g.DrawLines(p, expPts);
            }
            foreach (var pt in incPts)
                using (var br = new SolidBrush(C_GREEN)) g.FillEllipse(br, pt.X-5, pt.Y-5, 10, 10);
            foreach (var pt in expPts)
                using (var br = new SolidBrush(C_RED)) g.FillEllipse(br, pt.X-5, pt.Y-5, 10, 10);

            // Legend
            int lx=padL+chartW-180, ly=padT;
            using (var br = new SolidBrush(C_GREEN)) g.FillRectangle(br, lx, ly, 16, 16);
            using (var br = new SolidBrush(C_TEXT)) g.DrawString("Income", new Font("Segoe UI",9f), br, lx+20, ly);
            ly += 24;
            using (var br = new SolidBrush(C_RED)) g.FillRectangle(br, lx, ly, 16, 16);
            using (var br = new SolidBrush(C_TEXT)) g.DrawString("Expenses", new Font("Segoe UI",9f), br, lx+20, ly);
        }

        // ─── SUMMARY TABLE ───────────────────────────────────────────────────────
        private DataGridView BuildSummaryGrid()
        {
            var g = new DataGridView
            {
                Dock=DockStyle.Fill, BackgroundColor=C_CARD, GridColor=C_BORDER,
                BorderStyle=BorderStyle.None, RowHeadersVisible=false, AllowUserToAddRows=false,
                ReadOnly=true, SelectionMode=DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight=40, RowTemplate={Height=36},
                Font=new Font("Segoe UI",9.5f), ForeColor=C_TEXT, EnableHeadersVisualStyles=false, Name="summaryGrid"
            };
            g.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(40,44,62);
            g.ColumnHeadersDefaultCellStyle.ForeColor=C_MUTED;
            g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9f,FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor=Color.FromArgb(40,44,62);
            g.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;
            g.DefaultCellStyle.BackColor=C_CARD; g.DefaultCellStyle.ForeColor=C_TEXT;
            g.DefaultCellStyle.SelectionBackColor=Color.FromArgb(60,99,102,241);
            g.DefaultCellStyle.SelectionForeColor=Color.White;
            g.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(35,39,55);
            g.Columns.Add(new DataGridViewTextBoxColumn{Name="Month",HeaderText="Month",FillWeight=20});
            g.Columns.Add(new DataGridViewTextBoxColumn{Name="Income",HeaderText="Total Income",FillWeight=20,DefaultCellStyle=new DataGridViewCellStyle{Alignment=DataGridViewContentAlignment.MiddleRight}});
            g.Columns.Add(new DataGridViewTextBoxColumn{Name="Expense",HeaderText="Total Expenses",FillWeight=20,DefaultCellStyle=new DataGridViewCellStyle{Alignment=DataGridViewContentAlignment.MiddleRight}});
            g.Columns.Add(new DataGridViewTextBoxColumn{Name="Savings",HeaderText="Net Savings",FillWeight=20,DefaultCellStyle=new DataGridViewCellStyle{Alignment=DataGridViewContentAlignment.MiddleRight}});
            g.Columns.Add(new DataGridViewTextBoxColumn{Name="Rate",HeaderText="Savings Rate",FillWeight=20,DefaultCellStyle=new DataGridViewCellStyle{Alignment=DataGridViewContentAlignment.MiddleRight}});
            return g;
        }

        private void RefreshSummaryGrid(int month, int year)
        {
            var grids = _tabs.TabPages[3].Controls.Find("summaryGrid", true);
            if (grids.Length == 0) return;
            var g = (DataGridView)grids[0];
            g.Rows.Clear();
            if (_trendDt == null) return;
            foreach (DataRow row in _trendDt.Rows)
            {
                decimal inc = Convert.ToDecimal(row["Income"]);
                decimal exp = Convert.ToDecimal(row["Expense"]);
                decimal sav = inc - exp;
                string rate = inc > 0 ? $"{sav/inc*100:F1}%" : "N/A";
                g.Rows.Add(row["Month"], inc.ToString("C"), exp.ToString("C"), sav.ToString("C"), rate);
            }
        }

        private ComboBox MakeCombo(string[] items){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,BackColor=C_CARD,ForeColor=C_TEXT,FlatStyle=FlatStyle.Flat};foreach(var i in items)c.Items.Add(i);return c;}
        private Button MakeBtn(string t,Color bg,Color fg){var b=new Button{Text=t,BackColor=bg,ForeColor=fg,FlatStyle=FlatStyle.Flat,Height=36,Cursor=Cursors.Hand,Font=new Font("Segoe UI",9.5f,FontStyle.Bold)};b.FlatAppearance.BorderSize=0;return b;}
    }
}
