using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FinanceTracker.Database;
using FinanceTracker.Models;

namespace FinanceTracker.Forms
{
    public class ManageAccountsForm : Form
    {
        private Panel _accountsPanel;
        private TextBox _txtName, _txtBalance;
        private ComboBox _cmbType;
        private Panel _pnlColor;
        private string _selectedColor = "#4A90D9";

        private readonly Color C_BG    = Color.FromArgb(15, 17, 26);
        private readonly Color C_CARD  = Color.FromArgb(30, 34, 50);
        private readonly Color C_ACCENT= Color.FromArgb(99, 102, 241);
        private readonly Color C_TEXT  = Color.FromArgb(241, 245, 249);
        private readonly Color C_MUTED = Color.FromArgb(148, 163, 184);
        private readonly Color C_BORDER= Color.FromArgb(51, 65, 85);

        public ManageAccountsForm() { SetupUI(); RefreshAccounts(); }

        private void SetupUI()
        {
            BackColor = C_BG; ForeColor = C_TEXT; Font = new Font("Segoe UI", 9.5f);

            // Left form
            var formPanel = new Panel { Width=280, Dock=DockStyle.Left, BackColor=Color.FromArgb(22,25,37), Padding=new Padding(20) };
            formPanel.Paint += (s,e) => e.Graphics.DrawLine(new Pen(C_BORDER), formPanel.Width-1, 0, formPanel.Width-1, formPanel.Height);

            int y=10;
            void Lbl(string t){formPanel.Controls.Add(new Label{Text=t,ForeColor=C_MUTED,AutoSize=true,Location=new Point(20,y)});y+=22;}
            void Ctrl(Control c,int h=34){c.Location=new Point(20,y);c.Width=240;formPanel.Controls.Add(c);y+=h+10;}

            Lbl("Account Name");
            _txtName = MakeTxt(); Ctrl(_txtName);
            Lbl("Account Type");
            _cmbType = MakeCombo(new[]{"Checking","Savings","Cash","Credit Card","Investment"}); Ctrl(_cmbType);
            Lbl("Opening Balance");
            _txtBalance = MakeTxt(); _txtBalance.Text="0.00"; Ctrl(_txtBalance);
            Lbl("Color");
            _pnlColor = new Panel{Height=34, BackColor=ColorTranslator.FromHtml(_selectedColor), Cursor=Cursors.Hand};
            _pnlColor.Click += (s,e) => { using var d=new ColorDialog(); if(d.ShowDialog()==DialogResult.OK){_selectedColor=$"#{d.Color.R:X2}{d.Color.G:X2}{d.Color.B:X2}";_pnlColor.BackColor=d.Color;} };
            Ctrl(_pnlColor);

            var btnAdd = MakeBtn("➕  Add Account", C_ACCENT, Color.White);
            btnAdd.Location=new Point(20,y); btnAdd.Width=240;
            btnAdd.Click += AddAccount;
            formPanel.Controls.Add(btnAdd);

            // Right scrollable cards panel
            _accountsPanel = new Panel { Dock=DockStyle.Fill, BackColor=C_BG, AutoScroll=true, Padding=new Padding(16) };
            Controls.Add(_accountsPanel);
            Controls.Add(formPanel);
        }

        private void AddAccount(object s, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text)) { MessageBox.Show("Enter account name."); return; }
            if (!decimal.TryParse(_txtBalance.Text, out decimal bal)) { MessageBox.Show("Enter valid balance."); return; }
            var a = new Account { Name=_txtName.Text.Trim(), Type=_cmbType.SelectedItem.ToString(), Balance=bal, Color=_selectedColor };
            DatabaseManager.Instance.SaveAccount(a);
            _txtName.Clear(); _txtBalance.Text="0.00"; RefreshAccounts();
        }

        private void RefreshAccounts()
        {
            _accountsPanel.Controls.Clear();
            var accounts = DatabaseManager.Instance.GetAccounts(false);
            int y = 16;
            foreach (var a in accounts)
            {
                var card = CreateAccountCard(a);
                card.Location = new Point(16, y);
                card.Width = _accountsPanel.Width - 48;
                card.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                _accountsPanel.Controls.Add(card);
                y += card.Height + 14;
            }
        }

        private Panel CreateAccountCard(Account a)
        {
            Color accent = Color.White;
            try { accent = ColorTranslator.FromHtml(a.Color); } catch { }

            var card = new Panel { Height=90, BackColor=Color.FromArgb(30,34,50) };
            card.Paint += (s,e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                int w = card.Width;
                using (var br = new SolidBrush(accent)) g.FillRectangle(br, 0, 0, 5, card.Height);
                using (var p = new Pen(Color.FromArgb(51,65,85))) g.DrawRectangle(p, 0,0,w-1,card.Height-1);
                using (var br = new SolidBrush(Color.FromArgb(241,245,249)))
                {
                    g.DrawString(a.Name, new Font("Segoe UI",12f,FontStyle.Bold), br, 18, 14);
                    g.DrawString(a.Type, new Font("Segoe UI",9f), new SolidBrush(Color.FromArgb(148,163,184)), 18, 40);
                }
                using (var br = new SolidBrush(a.Balance >= 0 ? Color.FromArgb(52,211,153) : Color.FromArgb(248,113,113)))
                using (var sf = new StringFormat{Alignment=StringAlignment.Far})
                    g.DrawString(a.Balance.ToString("C"), new Font("Segoe UI",14f,FontStyle.Bold), br, new Rectangle(0,14,w-16,30), sf);
                string status = a.IsActive ? "Active" : "Inactive";
                using (var br = new SolidBrush(a.IsActive ? Color.FromArgb(52,211,153) : Color.FromArgb(148,163,184)))
                    g.DrawString($"● {status}", new Font("Segoe UI",8.5f), br, 18, 62);
            };

            var btnDel = new Button{Text="✕",Size=new Size(28,28),FlatStyle=FlatStyle.Flat,BackColor=Color.Transparent,ForeColor=Color.FromArgb(148,163,184),Cursor=Cursors.Hand,Anchor=AnchorStyles.Top|AnchorStyles.Right};
            btnDel.FlatAppearance.BorderSize=0;
            btnDel.Click += (s,e) => {
                if(MessageBox.Show($"Deactivate '{a.Name}'?","Confirm",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes){
                    DatabaseManager.Instance.DeleteAccount(a.Id); RefreshAccounts();
                }
            };
            card.Controls.Add(btnDel);
            card.Resize += (s,e) => btnDel.Location = new Point(card.Width-34, 6);
            btnDel.Location = new Point(card.Width-34, 6);
            return card;
        }

        private ComboBox MakeCombo(string[] items)
        {
            var c = new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Color.FromArgb(30,34,50),ForeColor=Color.FromArgb(241,245,249),FlatStyle=FlatStyle.Flat};
            foreach(var i in items) c.Items.Add(i); c.SelectedIndex=0; return c;
        }
        private TextBox MakeTxt()=>new TextBox{BackColor=Color.FromArgb(30,34,50),ForeColor=Color.FromArgb(241,245,249),BorderStyle=BorderStyle.FixedSingle};
        private Button MakeBtn(string t,Color bg,Color fg){var b=new Button{Text=t,BackColor=bg,ForeColor=fg,FlatStyle=FlatStyle.Flat,Height=36,Cursor=Cursors.Hand,Font=new Font("Segoe UI",9.5f,FontStyle.Bold)};b.FlatAppearance.BorderSize=0;return b;}
    }
}
