using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FinanceTracker.Database;
using FinanceTracker.Models;

namespace FinanceTracker.Forms
{
    public class ManageBudgetsForm : Form
    {
        private Panel _budgetListPanel;
        private ComboBox _cmbCategory, _cmbMonth, _cmbYear;
        private TextBox _txtLimit;
        private readonly Color C_BG    = Color.FromArgb(15, 17, 26);
        private readonly Color C_CARD  = Color.FromArgb(30, 34, 50);
        private readonly Color C_ACCENT= Color.FromArgb(99, 102, 241);
        private readonly Color C_GREEN = Color.FromArgb(52, 211, 153);
        private readonly Color C_RED   = Color.FromArgb(248, 113, 113);
        private readonly Color C_YELLOW= Color.FromArgb(251, 191, 36);
        private readonly Color C_TEXT  = Color.FromArgb(241, 245, 249);
        private readonly Color C_MUTED = Color.FromArgb(148, 163, 184);
        private readonly Color C_BORDER= Color.FromArgb(51, 65, 85);

        public ManageBudgetsForm() { SetupUI(); RefreshBudgets(); }

        private void SetupUI()
        {
            BackColor = C_BG; ForeColor = C_TEXT; Font = new Font("Segoe UI", 9.5f);

            // Top form strip
            var topPanel = new Panel { Dock=DockStyle.Top, Height=70, BackColor=Color.FromArgb(22,25,37), Padding=new Padding(16,12,16,0) };
            topPanel.Paint += (s,e) => e.Graphics.DrawLine(new Pen(C_BORDER), 0, 69, topPanel.Width, 69);

            _cmbCategory = MakeCombo(); _cmbCategory.Location=new Point(0,8); _cmbCategory.Width=180;
            _txtLimit = new TextBox { Location=new Point(190,8), Width=110, Height=34, BackColor=C_CARD, ForeColor=C_TEXT, BorderStyle=BorderStyle.FixedSingle, Text="500" };
            _cmbMonth = MakeCombo(new[]{"Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"}); _cmbMonth.SelectedIndex=DateTime.Now.Month-1; _cmbMonth.Location=new Point(310,8); _cmbMonth.Width=70;
            _cmbYear = MakeCombo(new[]{"2024","2025","2026"}); _cmbYear.SelectedIndex=1; _cmbYear.Location=new Point(390,8); _cmbYear.Width=70;
            var btnAdd = MakeBtn("+ Set Budget", C_ACCENT, Color.White); btnAdd.Location=new Point(472,6); btnAdd.Size=new Size(120,36); btnAdd.Click+=SetBudget;
            var btnRefresh = MakeBtn("🔄 Refresh", C_CARD, C_TEXT); btnRefresh.Location=new Point(604,6); btnRefresh.Size=new Size(100,36); btnRefresh.Click+=(s,e)=>RefreshBudgets();

            var lblCat = new Label{Text="Category",ForeColor=C_MUTED,AutoSize=true,Location=new Point(0,-2)}; // will be shifted by Padding
            topPanel.Controls.AddRange(new Control[]{_cmbCategory,_txtLimit,_cmbMonth,_cmbYear,btnAdd,btnRefresh});
            Controls.Add(topPanel);

            // Scroll panel for budget bars
            _budgetListPanel = new Panel { Dock=DockStyle.Fill, BackColor=C_BG, AutoScroll=true, Padding=new Padding(16) };
            Controls.Add(_budgetListPanel);

            LoadExpenseCategories();
        }

        private void LoadExpenseCategories()
        {
            var cats = DatabaseManager.Instance.GetCategories("Expense");
            _cmbCategory.Items.Clear();
            foreach (var c in cats) _cmbCategory.Items.Add(c);
            if (_cmbCategory.Items.Count > 0) _cmbCategory.SelectedIndex = 0;
        }

        private void SetBudget(object s, EventArgs e)
        {
            if (_cmbCategory.SelectedItem == null) return;
            if (!decimal.TryParse(_txtLimit.Text, out decimal limit) || limit <= 0) { MessageBox.Show("Enter a valid limit."); return; }
            int month = _cmbMonth.SelectedIndex + 1;
            int year = int.Parse(_cmbYear.SelectedItem.ToString());
            var cat = (Category)_cmbCategory.SelectedItem;
            var b = new Budget { CategoryId=cat.Id, LimitAmount=limit, Month=month, Year=year };
            DatabaseManager.Instance.SaveBudget(b);
            RefreshBudgets();
        }

        private void RefreshBudgets()
        {
            int month = _cmbMonth?.SelectedIndex + 1 ?? DateTime.Now.Month;
            int year = int.Parse(_cmbYear?.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString());
            var budgets = DatabaseManager.Instance.GetBudgets(month, year);
            _budgetListPanel.Controls.Clear();

            if (budgets.Count == 0)
            {
                _budgetListPanel.Controls.Add(new Label { Text="No budgets set for this period.", ForeColor=C_MUTED, AutoSize=true, Location=new Point(16,16) });
                return;
            }

            int y = 16;
            foreach (var b in budgets)
            {
                var card = CreateBudgetCard(b);
                card.Location = new Point(16, y);
                card.Width = _budgetListPanel.Width - 48;
                card.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                _budgetListPanel.Controls.Add(card);
                y += card.Height + 12;
            }
        }

        private Panel CreateBudgetCard(Budget b)
        {
            var pct = Math.Min(b.PercentageUsed / 100.0, 1.0);
            var fillColor = b.IsOverBudget ? C_RED : (b.PercentageUsed > 75 ? C_YELLOW : C_GREEN);

            var card = new Panel { Height=110, BackColor=C_CARD };
            card.Paint += (s,e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                int w = card.Width;

                // Left accent bar
                using (var br = new SolidBrush(fillColor)) g.FillRectangle(br, 0, 0, 4, card.Height);
                // Border
                using (var p = new Pen(C_BORDER)) g.DrawRectangle(p, 0,0,w-1,card.Height-1);

                // Category name & type
                using (var br = new SolidBrush(C_TEXT))
                    g.DrawString($"{b.CategoryName}", new Font("Segoe UI",11f,FontStyle.Bold), br, 18, 14);

                // Amounts
                string amtText = $"Spent: {b.SpentAmount:C}   Limit: {b.LimitAmount:C}   Remaining: {b.RemainingAmount:C}";
                using (var br = new SolidBrush(C_MUTED))
                    g.DrawString(amtText, new Font("Segoe UI",8.5f), br, 18, 40);

                // Progress bar track
                int barX=18, barY=66, barW=w-36, barH=12;
                using (var br = new SolidBrush(Color.FromArgb(51,65,85))) g.FillRectangle(br, barX, barY, barW, barH);
                using (var br = new SolidBrush(fillColor)) g.FillRectangle(br, barX, barY, (int)(barW*pct), barH);

                // Percentage
                string pctStr = $"{b.PercentageUsed:F1}%";
                using (var br = new SolidBrush(fillColor))
                using (var sf = new StringFormat { Alignment=StringAlignment.Far })
                    g.DrawString(pctStr, new Font("Segoe UI",9f,FontStyle.Bold), br, new Rectangle(barX, 83, barW, 18), sf);

                if (b.IsOverBudget)
                    using (var br = new SolidBrush(C_RED))
                        g.DrawString("⚠ Over Budget!", new Font("Segoe UI",8.5f,FontStyle.Bold), br, 18, 83);
            };

            var btnDel = new Button { Text="✕", Size=new Size(28,28), FlatStyle=FlatStyle.Flat, BackColor=Color.Transparent, ForeColor=C_MUTED, Cursor=Cursors.Hand, Anchor=AnchorStyles.Top|AnchorStyles.Right };
            btnDel.FlatAppearance.BorderSize=0;
            btnDel.Click += (s,e) => { DatabaseManager.Instance.DeleteBudget(b.Id); RefreshBudgets(); };
            card.Controls.Add(btnDel);
            card.Resize += (s,e) => btnDel.Location = new Point(card.Width-34, 6);
            btnDel.Location = new Point(card.Width-34, 6);

            return card;
        }

        private ComboBox MakeCombo(string[] items=null)
        {
            var c = new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, BackColor=C_CARD, ForeColor=C_TEXT, FlatStyle=FlatStyle.Flat, Height=34 };
            if (items != null) foreach (var i in items) c.Items.Add(i);
            return c;
        }
        private Button MakeBtn(string t,Color bg,Color fg){var b=new Button{Text=t,BackColor=bg,ForeColor=fg,FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,Font=new Font("Segoe UI",9.5f,FontStyle.Bold)};b.FlatAppearance.BorderSize=0;return b;}
    }
}
