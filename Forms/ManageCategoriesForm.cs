using System;
using System.Drawing;
using System.Windows.Forms;
using FinanceTracker.Database;
using FinanceTracker.Models;

namespace FinanceTracker.Forms
{
    public class ManageCategoriesForm : Form
    {
        private DataGridView _grid;
        private ComboBox _cmbType;
        private TextBox _txtName, _txtIcon;
        private Panel _pnlColor;
        private string _selectedColor = "#E74C3C";

        private readonly Color C_BG    = Color.FromArgb(15, 17, 26);
        private readonly Color C_CARD  = Color.FromArgb(30, 34, 50);
        private readonly Color C_ACCENT= Color.FromArgb(99, 102, 241);
        private readonly Color C_TEXT  = Color.FromArgb(241, 245, 249);
        private readonly Color C_MUTED = Color.FromArgb(148, 163, 184);
        private readonly Color C_BORDER= Color.FromArgb(51, 65, 85);

        public ManageCategoriesForm() { SetupUI(); RefreshGrid(); }

        private void SetupUI()
        {
            BackColor = C_BG; ForeColor = C_TEXT;
            Font = new Font("Segoe UI", 9.5f);

            // Left panel - add form
            var formPanel = new Panel { Width = 300, Dock = DockStyle.Left, BackColor = Color.FromArgb(22,25,37), Padding = new Padding(20) };
            formPanel.Paint += (s,e) => e.Graphics.DrawLine(new Pen(C_BORDER), formPanel.Width-1, 0, formPanel.Width-1, formPanel.Height);

            int y = 10;
            void AddLbl(string t) { formPanel.Controls.Add(new Label { Text=t, ForeColor=C_MUTED, AutoSize=true, Location=new Point(20,y) }); y+=22; }
            void AddCtrl(Control c, int h=34) { c.Location=new Point(20,y); c.Width=250; formPanel.Controls.Add(c); y+=h+10; }

            AddLbl("Category Type");
            _cmbType = MakeCombo(new[]{"Expense","Income"});
            AddCtrl(_cmbType);

            AddLbl("Name");
            _txtName = MakeTxt();
            AddCtrl(_txtName);

            AddLbl("Icon (Emoji)");
            _txtIcon = MakeTxt(); _txtIcon.Text = "💰";
            AddCtrl(_txtIcon);

            AddLbl("Color");
            _pnlColor = new Panel { Height=34, BackColor=ColorTranslator.FromHtml(_selectedColor), Cursor=Cursors.Hand };
            _pnlColor.Click += PickColor;
            AddCtrl(_pnlColor);

            var btnAdd = MakeBtn("➕  Add Category", C_ACCENT, Color.White);
            btnAdd.Location = new Point(20, y); btnAdd.Width = 250;
            btnAdd.Click += AddCategory;
            formPanel.Controls.Add(btnAdd);

            // Right panel - grid
            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = C_CARD,
                GridColor = C_BORDER,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 36 },
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_TEXT,
                EnableHeadersVisualStyles = false
            };
            StyleGrid(_grid);
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Id", HeaderText="ID", Visible=false });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Icon", HeaderText="", FillWeight=8 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Name", HeaderText="Name", FillWeight=40 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Type", HeaderText="Type", FillWeight=20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Color", HeaderText="Color", FillWeight=20 });

            var menu = new ContextMenuStrip();
            menu.BackColor = C_CARD; menu.ForeColor = C_TEXT;
            var del = new ToolStripMenuItem("🗑️  Delete");
            del.Click += (s,e) => {
                if (_grid.SelectedRows.Count == 0) return;
                int id = (int)_grid.SelectedRows[0].Cells["Id"].Value;
                if (MessageBox.Show("Delete category?","Confirm",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes) {
                    DatabaseManager.Instance.DeleteCategory(id); RefreshGrid();
                }
            };
            menu.Items.Add(del);
            _grid.ContextMenuStrip = menu;

            rightPanel.Controls.Add(_grid);
            Controls.Add(rightPanel);
            Controls.Add(formPanel);
        }

        private void PickColor(object s, EventArgs e)
        {
            using var dlg = new ColorDialog();
            if (dlg.ShowDialog() == DialogResult.OK) {
                _selectedColor = $"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}";
                _pnlColor.BackColor = dlg.Color;
            }
        }

        private void AddCategory(object s, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text)) { MessageBox.Show("Enter a name."); return; }
            var c = new Category { Name=_txtName.Text.Trim(), Type=_cmbType.SelectedItem.ToString(), Color=_selectedColor, Icon=_txtIcon.Text.Trim() };
            DatabaseManager.Instance.SaveCategory(c);
            _txtName.Clear(); RefreshGrid();
        }

        private void RefreshGrid()
        {
            _grid.Rows.Clear();
            foreach (var c in DatabaseManager.Instance.GetCategories())
                _grid.Rows.Add(c.Id, c.Icon, c.Name, c.Type, c.Color);
        }

        private void StyleGrid(DataGridView g)
        {
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40,44,62);
            g.ColumnHeadersDefaultCellStyle.ForeColor = C_MUTED;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI",9f,FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(40,44,62);
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.DefaultCellStyle.BackColor = C_CARD;
            g.DefaultCellStyle.ForeColor = C_TEXT;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60,99,102,241);
            g.DefaultCellStyle.SelectionForeColor = Color.White;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35,39,55);
        }

        private ComboBox MakeCombo(string[] items) => new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, BackColor=C_CARD, ForeColor=C_TEXT, FlatStyle=FlatStyle.Flat, Items={items[0],items[1]}, SelectedIndex=0 };
        private TextBox MakeTxt() => new TextBox { BackColor=C_CARD, ForeColor=C_TEXT, BorderStyle=BorderStyle.FixedSingle };
        private Button MakeBtn(string t, Color bg, Color fg) { var b=new Button{Text=t,BackColor=bg,ForeColor=fg,FlatStyle=FlatStyle.Flat,Height=36,Cursor=Cursors.Hand,Font=new Font("Segoe UI",9.5f,FontStyle.Bold)}; b.FlatAppearance.BorderSize=0; return b; }
    }
}
