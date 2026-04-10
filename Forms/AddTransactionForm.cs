using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FinanceTracker.Database;
using FinanceTracker.Models;

namespace FinanceTracker.Forms
{
    public class AddTransactionForm : Form
    {
        private readonly int _editId;
        private ComboBox _cmbAccount, _cmbCategory, _cmbType;
        private TextBox _txtAmount, _txtDescription, _txtNotes;
        private DateTimePicker _dtpDate;

        private readonly Color C_BG    = Color.FromArgb(22, 25, 37);
        private readonly Color C_CARD  = Color.FromArgb(30, 34, 50);
        private readonly Color C_ACCENT= Color.FromArgb(99, 102, 241);
        private readonly Color C_TEXT  = Color.FromArgb(241, 245, 249);
        private readonly Color C_MUTED = Color.FromArgb(148, 163, 184);
        private readonly Color C_BORDER= Color.FromArgb(51, 65, 85);

        public AddTransactionForm(int editId = 0)
        {
            _editId = editId;
            SetupUI();
            if (editId > 0) LoadForEdit(editId);
        }

        private void SetupUI()
        {
            Text = _editId == 0 ? "Add Transaction" : "Edit Transaction";
            Size = new Size(460, 540);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = C_BG;
            ForeColor = C_TEXT;
            Font = new Font("Segoe UI", 9.5f);

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 20, 28, 20) };

            int y = 0;
            Label AddLabel(string text)
            {
                var lbl = new Label { Text = text, ForeColor = C_MUTED, Font = new Font("Segoe UI", 8.5f), AutoSize = true, Location = new Point(0, y) };
                panel.Controls.Add(lbl);
                y += 20;
                return lbl;
            }
            Control AddControl(Control ctrl)
            {
                ctrl.Location = new Point(0, y);
                ctrl.Width = 400;
                panel.Controls.Add(ctrl);
                y += ctrl.Height + 12;
                return ctrl;
            }

            // Type radio-style via ComboBox
            AddLabel("Transaction Type");
            _cmbType = CreateCombo(new[] { "Expense", "Income" }, 0);
            _cmbType.SelectedIndexChanged += (s, e) => RefreshCategories();
            AddControl(_cmbType);

            AddLabel("Account");
            _cmbAccount = CreateCombo(Array.Empty<string>(), -1);
            AddControl(_cmbAccount);

            AddLabel("Category");
            _cmbCategory = CreateCombo(Array.Empty<string>(), -1);
            AddControl(_cmbCategory);

            AddLabel("Amount");
            _txtAmount = CreateTextBox("0.00");
            AddControl(_txtAmount);

            AddLabel("Description");
            _txtDescription = CreateTextBox("e.g. Grocery run");
            AddControl(_txtDescription);

            AddLabel("Date");
            _dtpDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Height = 34,
                BackColor = C_CARD,
                ForeColor = C_TEXT,
                CalendarForeColor = C_TEXT,
                CalendarMonthBackground = C_CARD
            };
            AddControl(_dtpDate);

            AddLabel("Notes (optional)");
            _txtNotes = CreateTextBox("Any additional info...");
            _txtNotes.Height = 60;
            _txtNotes.Multiline = true;
            AddControl(_txtNotes);

            // Buttons
            var btnRow = new Panel { Location = new Point(0, y), Width = 400, Height = 44 };
            var btnSave = new Button
            {
                Text = _editId == 0 ? "💾  Save Transaction" : "✏️  Update Transaction",
                BackColor = C_ACCENT,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 38),
                Location = new Point(0, 0),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += Save;

            var btnCancel = new Button
            {
                Text = "Cancel",
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = C_TEXT,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(210, 0),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => Close();

            btnRow.Controls.Add(btnSave);
            btnRow.Controls.Add(btnCancel);
            panel.Controls.Add(btnRow);

            Controls.Add(panel);

            RefreshAccounts();
            RefreshCategories();
        }

        private void RefreshAccounts()
        {
            var accounts = DatabaseManager.Instance.GetAccounts();
            _cmbAccount.Items.Clear();
            foreach (var a in accounts) _cmbAccount.Items.Add(a);
            if (_cmbAccount.Items.Count > 0) _cmbAccount.SelectedIndex = 0;
        }

        private void RefreshCategories()
        {
            string type = _cmbType.SelectedItem?.ToString() ?? "Expense";
            var cats = DatabaseManager.Instance.GetCategories(type);
            _cmbCategory.Items.Clear();
            foreach (var c in cats) _cmbCategory.Items.Add(c);
            if (_cmbCategory.Items.Count > 0) _cmbCategory.SelectedIndex = 0;
        }

        private void LoadForEdit(int id)
        {
            var txns = DatabaseManager.Instance.GetTransactions();
            var t = txns.FirstOrDefault(x => x.Id == id);
            if (t == null) return;

            _cmbType.SelectedItem = t.Type;
            RefreshCategories();

            foreach (Account a in _cmbAccount.Items)
                if (a.Id == t.AccountId) { _cmbAccount.SelectedItem = a; break; }

            foreach (Category c in _cmbCategory.Items)
                if (c.Id == t.CategoryId) { _cmbCategory.SelectedItem = c; break; }

            _txtAmount.Text = t.Amount.ToString("F2");
            _txtDescription.Text = t.Description;
            _dtpDate.Value = t.Date;
            _txtNotes.Text = t.Notes;
        }

        private void Save(object sender, EventArgs e)
        {
            if (!decimal.TryParse(_txtAmount.Text, out decimal amount) || amount <= 0)
            { MessageBox.Show("Please enter a valid amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(_txtDescription.Text))
            { MessageBox.Show("Please enter a description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_cmbAccount.SelectedItem == null)
            { MessageBox.Show("Please select an account.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_cmbCategory.SelectedItem == null)
            { MessageBox.Show("Please select a category.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var t = new Transaction
            {
                Id = _editId,
                AccountId = ((Account)_cmbAccount.SelectedItem).Id,
                CategoryId = ((Category)_cmbCategory.SelectedItem).Id,
                Amount = amount,
                Type = _cmbType.SelectedItem.ToString(),
                Description = _txtDescription.Text.Trim(),
                Date = _dtpDate.Value.Date,
                Notes = _txtNotes.Text.Trim()
            };

            DatabaseManager.Instance.SaveTransaction(t);
            DialogResult = DialogResult.OK;
            Close();
        }

        private ComboBox CreateCombo(string[] items, int idx)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = 34,
                BackColor = Color.FromArgb(30, 34, 50),
                ForeColor = C_TEXT,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f)
            };
            cmb.Items.AddRange(items);
            if (idx >= 0 && idx < items.Length) cmb.SelectedIndex = idx;
            return cmb;
        }

        private TextBox CreateTextBox(string placeholder)
        {
            var txt = new TextBox
            {
                Height = 34,
                BackColor = Color.FromArgb(30, 34, 50),
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            return txt;
        }
    }
}
