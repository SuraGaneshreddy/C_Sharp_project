using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FinanceTracker.Database;
using FinanceTracker.Models;

namespace FinanceTracker.Forms
{
    public partial class MainForm : Form
    {
        private Panel _sidebar;
        private Panel _contentPanel;
        private Panel _headerPanel;
        private DataGridView _transactionGrid;
        private ComboBox _cmbFilterMonth, _cmbFilterYear, _cmbFilterType;
        private Button _btnActiveNav;

        // Color palette
        private readonly Color C_BG       = Color.FromArgb(15, 17, 26);
        private readonly Color C_SIDEBAR  = Color.FromArgb(22, 25, 37);
        private readonly Color C_CARD     = Color.FromArgb(30, 34, 50);
        private readonly Color C_ACCENT   = Color.FromArgb(99, 102, 241);
        private readonly Color C_GREEN    = Color.FromArgb(52, 211, 153);
        private readonly Color C_RED      = Color.FromArgb(248, 113, 113);
        private readonly Color C_TEXT     = Color.FromArgb(241, 245, 249);
        private readonly Color C_MUTED    = Color.FromArgb(148, 163, 184);
        private readonly Color C_BORDER   = Color.FromArgb(51, 65, 85);
        private readonly Color C_YELLOW   = Color.FromArgb(251, 191, 36);

        public MainForm()
        {
            InitializeComponent();
            SetupUI();
            LoadDashboard();
        }

        private void InitializeComponent()
        {
            Text = "💰 Personal Finance Tracker";
            Size = new Size(1280, 800);
            MinimumSize = new Size(1024, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = C_BG;
            ForeColor = C_TEXT;
            Font = new Font("Segoe UI", 9.5f);
            DoubleBuffered = true;
        }

        private void SetupUI()
        {
            // ── Sidebar ──────────────────────────────────────────────────────────
            _sidebar = new Panel
            {
                Width = 220,
                Dock = DockStyle.Left,
                BackColor = C_SIDEBAR,
                Padding = new Padding(0, 0, 0, 0)
            };
            _sidebar.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(C_BORDER, 1), _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
            };

            // Logo area
            var logoPanel = new Panel { Height = 72, Dock = DockStyle.Top, BackColor = C_SIDEBAR };
            var lblLogo = new Label
            {
                Text = "FinanceTrack",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblSubLogo = new Label
            {
                Text = "Personal Finance Manager",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = C_MUTED,
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };
            logoPanel.Controls.Add(lblSubLogo);
            logoPanel.Controls.Add(lblLogo);
            logoPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(C_BORDER, 1), 20, 71, 200, 71);
            };

            // Nav buttons
            var navContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 16, 12, 0) };
            var navItems = new[]
            {
                ("🏠  Dashboard",     (Action)LoadDashboard),
                ("💳  Transactions",  (Action)LoadTransactions),
                ("📊  Budgets",       (Action)LoadBudgets),
                ("🏦  Accounts",      (Action)LoadAccounts),
                ("📋  Categories",    (Action)LoadCategories),
                ("📈  Reports",       (Action)LoadReports),
            };

            int navY = 0;
            Button firstBtn = null;
            foreach (var (label, action) in navItems)
            {
                var btn = CreateNavButton(label);
                btn.Top = navY;
                navY += 48;
                var capturedAction = action;
                var capturedBtn = btn;
                btn.Click += (s, e) =>
                {
                    SetActiveNav(capturedBtn);
                    _contentPanel.Controls.Clear();
                    capturedAction();
                };
                navContainer.Controls.Add(btn);
                firstBtn ??= btn;
            }

            // Separator line
            var sep = new Panel { Top = navY + 8, Height = 1, Left = 0, Width = 196, BackColor = C_BORDER };
            navContainer.Controls.Add(sep);
            navY += 24;

            // Settings / DB path button
            var btnDb = CreateNavButton("🗃️  Database Info");
            btnDb.Top = navY;
            btnDb.Click += (s, e) =>
            {
                MessageBox.Show($"Database Location:\n{DatabaseManager.Instance.GetDatabasePath()}",
                    "Database Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            navContainer.Controls.Add(btnDb);

            _sidebar.Controls.Add(navContainer);
            _sidebar.Controls.Add(logoPanel);

            // ── Header ───────────────────────────────────────────────────────────
            _headerPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = C_SIDEBAR,
                Padding = new Padding(20, 0, 20, 0)
            };
            _headerPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(C_BORDER, 1), 0, 59, _headerPanel.Width, 59);
            };

            var lblPageTitle = new Label
            {
                Name = "lblPageTitle",
                Text = "Dashboard",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = C_TEXT,
                AutoSize = true,
                Location = new Point(20, 14)
            };

            var btnAdd = CreateButton("+ Add Transaction", C_ACCENT, Color.White);
            btnAdd.Size = new Size(160, 36);
            btnAdd.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnAdd.Location = new Point(_headerPanel.Width - 180, 12);
            btnAdd.Click += (s, e) => OpenAddTransaction();

            _headerPanel.Controls.Add(lblPageTitle);
            _headerPanel.Controls.Add(btnAdd);
            _headerPanel.Resize += (s, e) => btnAdd.Left = _headerPanel.Width - 180;

            // ── Content area ─────────────────────────────────────────────────────
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG,
                Padding = new Padding(24, 20, 24, 20),
                AutoScroll = true
            };

            // ── Layout ───────────────────────────────────────────────────────────
            var mainContainer = new Panel { Dock = DockStyle.Fill };
            var rightSide = new Panel { Dock = DockStyle.Fill };
            rightSide.Controls.Add(_contentPanel);
            rightSide.Controls.Add(_headerPanel);
            mainContainer.Controls.Add(rightSide);
            mainContainer.Controls.Add(_sidebar);

            Controls.Add(mainContainer);

            _btnActiveNav = firstBtn;
            if (firstBtn != null) SetActiveNav(firstBtn);
        }

        private Button CreateNavButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.None,
                Width = 196,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = C_MUTED,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                Left = 0
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 99, 102, 241);
            btn.MouseEnter += (s, e) => { if (btn != _btnActiveNav) btn.ForeColor = C_TEXT; };
            btn.MouseLeave += (s, e) => { if (btn != _btnActiveNav) btn.ForeColor = C_MUTED; };
            return btn;
        }

        private void SetActiveNav(Button btn)
        {
            if (_btnActiveNav != null)
            {
                _btnActiveNav.BackColor = Color.Transparent;
                _btnActiveNav.ForeColor = C_MUTED;
            }
            _btnActiveNav = btn;
            btn.BackColor = Color.FromArgb(60, 99, 102, 241);
            btn.ForeColor = C_TEXT;

            var title = Controls.Find("lblPageTitle", true);
            if (title.Length > 0)
                ((Label)title[0]).Text = btn.Text.Trim().Substring(btn.Text.IndexOf(' ')).Trim();
        }

        // ─── DASHBOARD ───────────────────────────────────────────────────────────

        private void LoadDashboard()
        {
            _contentPanel.Controls.Clear();
            var now = DateTime.Now;
            var (income, expense, balance) = DatabaseManager.Instance.GetMonthlySummary(now.Month, now.Year);

            // Summary cards row
            var cardsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 120,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 0, 0, 20),
                BackColor = Color.Transparent
            };

            cardsPanel.Controls.Add(CreateSummaryCard("💵 Monthly Income", income.ToString("C"), C_GREEN, "↑ This Month"));
            cardsPanel.Controls.Add(CreateSummaryCard("💸 Monthly Expenses", expense.ToString("C"), C_RED, "↓ This Month"));
            cardsPanel.Controls.Add(CreateSummaryCard("💰 Net Savings", balance.ToString("C"), balance >= 0 ? C_ACCENT : C_RED, balance >= 0 ? "✓ Positive" : "⚠ Negative"));

            // Accounts row
            var accounts = DatabaseManager.Instance.GetAccounts();
            decimal totalBalance = 0;
            foreach (var a in accounts) totalBalance += a.Balance;
            cardsPanel.Controls.Add(CreateSummaryCard("🏦 Total Net Worth", totalBalance.ToString("C"), C_YELLOW, $"{accounts.Count} account(s)"));

            _contentPanel.Controls.Add(cardsPanel);

            // Recent Transactions
            var lblRecent = new Label
            {
                Text = "Recent Transactions",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = C_TEXT,
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };
            _contentPanel.Controls.Add(lblRecent);

            var grid = CreateTransactionGrid();
            grid.Dock = DockStyle.Top;
            grid.Height = 320;
            var txns = DatabaseManager.Instance.GetTransactions(DateTime.Now.AddDays(-30), DateTime.Now);
            PopulateGrid(grid, txns);
            _contentPanel.Controls.Add(grid);

            // Budget overview
            var lblBudget = new Label
            {
                Text = "Budget Overview — " + now.ToString("MMMM yyyy"),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = C_TEXT,
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 16, 0, 0)
            };
            _contentPanel.Controls.Add(lblBudget);

            var budgets = DatabaseManager.Instance.GetBudgets(now.Month, now.Year);
            if (budgets.Count == 0)
            {
                var lbl = new Label { Text = "No budgets set for this month. Go to Budgets to add some.", ForeColor = C_MUTED, Dock = DockStyle.Top, Height = 40 };
                _contentPanel.Controls.Add(lbl);
            }
            else
            {
                foreach (var b in budgets)
                    _contentPanel.Controls.Add(CreateBudgetBar(b));
            }

            // Re-order (controls are added bottom-up in Fill layout)
            ReverseControlsOrder(_contentPanel);
        }

        private Panel CreateSummaryCard(string title, string value, Color accent, string subtitle)
        {
            var card = new Panel
            {
                Width = 240,
                Height = 100,
                BackColor = C_CARD,
                Margin = new Padding(0, 0, 16, 0)
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(C_BORDER, 1))
                    g.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                using (var b = new SolidBrush(accent))
                    g.FillRectangle(b, 0, 0, 4, card.Height);
            };

            var lblTitle = new Label { Text = title, ForeColor = C_MUTED, Font = new Font("Segoe UI", 9f), Location = new Point(16, 16), AutoSize = true };
            var lblValue = new Label { Text = value, ForeColor = C_TEXT, Font = new Font("Segoe UI", 18f, FontStyle.Bold), Location = new Point(14, 36), AutoSize = true };
            var lblSub = new Label { Text = subtitle, ForeColor = accent, Font = new Font("Segoe UI", 8.5f), Location = new Point(16, 75), AutoSize = true };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblSub);
            return card;
        }

        private Panel CreateBudgetBar(Budget b)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 4) };
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int barY = 36, barH = 8;
                int totalW = panel.Width - 24;

                // Background track
                using (var br = new SolidBrush(C_CARD))
                    g.FillRectangle(br, 12, barY, totalW, barH);

                // Fill
                double pct = Math.Min(b.PercentageUsed / 100.0, 1.0);
                var fillColor = b.IsOverBudget ? C_RED : (b.PercentageUsed > 75 ? C_YELLOW : C_GREEN);
                using (var br = new SolidBrush(fillColor))
                    g.FillRectangle(br, 12, barY, (int)(totalW * pct), barH);

                // Labels
                string left = $"{b.CategoryName}  ({b.SpentAmount:C} / {b.LimitAmount:C})";
                string right = $"{b.PercentageUsed:F0}%";
                using (var sf = new StringFormat()) {
                    g.DrawString(left, new Font("Segoe UI", 9f), new SolidBrush(C_TEXT), 12, 12);
                    sf.Alignment = StringAlignment.Far;
                    g.DrawString(right, new Font("Segoe UI", 9f, FontStyle.Bold), new SolidBrush(fillColor), totalW + 12, 12, sf);
                }
            };
            return panel;
        }

        // ─── TRANSACTIONS ─────────────────────────────────────────────────────────

        private void LoadTransactions()
        {
            _contentPanel.Controls.Clear();

            // Filter toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };

            _cmbFilterType = CreateCombo(new[] { "All", "Income", "Expense" }, 0);
            _cmbFilterType.Location = new Point(0, 10);
            _cmbFilterType.Width = 110;

            _cmbFilterMonth = CreateCombo(new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" }, DateTime.Now.Month - 1);
            _cmbFilterMonth.Location = new Point(120, 10);
            _cmbFilterMonth.Width = 80;

            _cmbFilterYear = CreateCombo(new[] { "2022","2023","2024","2025","2026" }, 4);
            _cmbFilterYear.Location = new Point(210, 10);
            _cmbFilterYear.Width = 75;

            var btnFilter = CreateButton("🔍 Filter", C_ACCENT, Color.White);
            btnFilter.Size = new Size(90, 32);
            btnFilter.Location = new Point(298, 10);
            btnFilter.Click += (s, e) => RefreshTransactionGrid();

            var btnExport = CreateButton("⬇ Export CSV", C_CARD, C_TEXT);
            btnExport.Size = new Size(110, 32);
            btnExport.Location = new Point(400, 10);
            btnExport.Click += (s, e) => ExportCsv();

            toolbar.Controls.Add(_cmbFilterType);
            toolbar.Controls.Add(_cmbFilterMonth);
            toolbar.Controls.Add(_cmbFilterYear);
            toolbar.Controls.Add(btnFilter);
            toolbar.Controls.Add(btnExport);
            _contentPanel.Controls.Add(toolbar);

            _transactionGrid = CreateTransactionGrid();
            _transactionGrid.Dock = DockStyle.Fill;
            _transactionGrid.ContextMenuStrip = CreateTransactionContextMenu();
            _contentPanel.Controls.Add(_transactionGrid);

            ReverseControlsOrder(_contentPanel);
            RefreshTransactionGrid();
        }

        private void RefreshTransactionGrid()
        {
            if (_transactionGrid == null) return;
            int month = (_cmbFilterMonth?.SelectedIndex ?? DateTime.Now.Month - 1) + 1;
            int year = int.Parse(_cmbFilterYear?.Text ?? DateTime.Now.Year.ToString());
            string type = _cmbFilterType?.SelectedItem?.ToString() == "All" ? null : _cmbFilterType?.SelectedItem?.ToString();

            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var txns = DatabaseManager.Instance.GetTransactions(from, to, type: type);
            PopulateGrid(_transactionGrid, txns);
        }

        private DataGridView CreateTransactionGrid()
        {
            var grid = new DataGridView
            {
                BackgroundColor = C_CARD,
                GridColor = C_BORDER,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 38 },
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_TEXT,
                EnableHeadersVisualStyles = false,
                MultiSelect = false,
                ScrollBars = ScrollBars.Vertical
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 44, 62);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = C_MUTED;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(40, 44, 62);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            grid.DefaultCellStyle.BackColor = C_CARD;
            grid.DefaultCellStyle.ForeColor = C_TEXT;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 99, 102, 241);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 39, 55);

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50, Tag = "id" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", FillWeight = 12 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", FillWeight = 30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", FillWeight = 18 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Account", HeaderText = "Account", FillWeight = 18 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", FillWeight = 10 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", FillWeight = 14,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            grid.Columns["Id"].Visible = false;

            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (grid.Columns[e.ColumnIndex].Name == "Amount" ||
                    grid.Columns[e.ColumnIndex].Name == "Type")
                {
                    if (e.Value == null) return;
                    var row = grid.Rows[e.RowIndex];
                    bool isIncome = row.Cells["Type"].Value?.ToString() == "Income";
                    e.Handled = true;
                    e.PaintBackground(e.CellBounds, true);
                    Color c = isIncome ? C_GREEN : C_RED;
                    using (var br = new SolidBrush(c))
                    using (var sf = new StringFormat { Alignment = grid.Columns[e.ColumnIndex].Name == "Amount" ? StringAlignment.Far : StringAlignment.Near, LineAlignment = StringAlignment.Center })
                        e.Graphics.DrawString(e.Value.ToString(), grid.DefaultCellStyle.Font, br, e.CellBounds, sf);
                }
            };

            return grid;
        }

        private void PopulateGrid(DataGridView grid, List<Transaction> txns)
        {
            grid.Rows.Clear();
            foreach (var t in txns)
            {
                string amount = t.Type == "Income" ? $"+{t.Amount:C}" : $"-{t.Amount:C}";
                grid.Rows.Add(t.Id, t.Date.ToString("dd MMM yyyy"), t.Description, t.CategoryName, t.AccountName, t.Type, amount);
            }
        }

        private ContextMenuStrip CreateTransactionContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = C_CARD;
            menu.ForeColor = C_TEXT;

            var edit = new ToolStripMenuItem("✏️  Edit Transaction");
            edit.Click += (s, e) =>
            {
                if (_transactionGrid.SelectedRows.Count == 0) return;
                int id = (int)_transactionGrid.SelectedRows[0].Cells["Id"].Value;
                OpenAddTransaction(id);
            };
            var delete = new ToolStripMenuItem("🗑️  Delete Transaction");
            delete.Click += (s, e) =>
            {
                if (_transactionGrid.SelectedRows.Count == 0) return;
                int id = (int)_transactionGrid.SelectedRows[0].Cells["Id"].Value;
                if (MessageBox.Show("Delete this transaction?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DatabaseManager.Instance.DeleteTransaction(id);
                    RefreshTransactionGrid();
                }
            };
            menu.Items.Add(edit);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(delete);
            return menu;
        }

        private void OpenAddTransaction(int editId = 0)
        {
            var form = new AddTransactionForm(editId);
            if (form.ShowDialog() == DialogResult.OK)
                RefreshTransactionGrid();
        }

        private void ExportCsv()
        {
            using (var dlg = new SaveFileDialog { Filter = "CSV Files|*.csv", FileName = "transactions.csv" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                int month = (_cmbFilterMonth?.SelectedIndex ?? DateTime.Now.Month - 1) + 1;
                int year = int.Parse(_cmbFilterYear?.Text ?? DateTime.Now.Year.ToString());
                var from = new DateTime(year, month, 1);
                var to = from.AddMonths(1).AddDays(-1);
                var txns = DatabaseManager.Instance.GetTransactions(from, to);
                using (var sw = new System.IO.StreamWriter(dlg.FileName))
                {
                    sw.WriteLine("Date,Description,Category,Account,Type,Amount,Notes");
                    foreach (var t in txns)
                        sw.WriteLine($"{t.Date:yyyy-MM-dd},{Quote(t.Description)},{t.CategoryName},{t.AccountName},{t.Type},{t.Amount},{Quote(t.Notes)}");
                }
                MessageBox.Show($"Exported {txns.Count} transactions to {dlg.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string Quote(string s) => $"\"{s?.Replace("\"", "\"\"")}\"";

        // ─── BUDGETS ──────────────────────────────────────────────────────────────

        private void LoadBudgets()
        {
            _contentPanel.Controls.Clear();
            var form = new ManageBudgetsForm();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(form);
            form.Show();
        }

        // ─── ACCOUNTS ─────────────────────────────────────────────────────────────

        private void LoadAccounts()
        {
            _contentPanel.Controls.Clear();
            var form = new ManageAccountsForm();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(form);
            form.Show();
        }

        // ─── CATEGORIES ───────────────────────────────────────────────────────────

        private void LoadCategories()
        {
            _contentPanel.Controls.Clear();
            var form = new ManageCategoriesForm();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(form);
            form.Show();
        }

        // ─── REPORTS ──────────────────────────────────────────────────────────────

        private void LoadReports()
        {
            _contentPanel.Controls.Clear();
            var form = new ReportsForm();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(form);
            form.Show();
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────────

        private Button CreateButton(string text, Color back, Color fore)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private ComboBox CreateCombo(string[] items, int selectedIndex)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = C_CARD,
                ForeColor = C_TEXT,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            cmb.Items.AddRange(items);
            cmb.SelectedIndex = selectedIndex;
            return cmb;
        }

        private void ReverseControlsOrder(Panel panel)
        {
            var controls = new Control[panel.Controls.Count];
            panel.Controls.CopyTo(controls, 0);
            panel.Controls.Clear();
            for (int i = controls.Length - 1; i >= 0; i--)
                panel.Controls.Add(controls[i]);
        }
    }
}
