using PersonalFinanceTracker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PersonalFinanceTracker.Forms
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "Personal Finance Tracker";
            this.Size = new Size(900, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 60);

            Panel panelLogin = new Panel();
            panelLogin.Size = new Size(350, 400);
            panelLogin.BackColor = Color.White;
            panelLogin.Location = new Point(
                (this.Width - panelLogin.Width) / 2,
                (this.Height - panelLogin.Height) / 2);

            Label lblTitle = new Label();
            lblTitle.Text = "USER LOGIN";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.Location = new Point(90, 30);
            lblTitle.AutoSize = true;

            Label lblUser = new Label();
            lblUser.Text = "Username";
            lblUser.Location = new Point(40, 100);

            TextBox txtUsername = new TextBox();
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(260, 25);
            txtUsername.Location = new Point(40, 125);

            Label lblPass = new Label();
            lblPass.Text = "Password";
            lblPass.Location = new Point(40, 170);

            TextBox txtPassword = new TextBox();
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(260, 25);
            txtPassword.Location = new Point(40, 195);
            txtPassword.UseSystemPasswordChar = true;

            Button btnLogin = new Button();
            btnLogin.Text = "Login";
            btnLogin.Size = new Size(260, 40);
            btnLogin.Location = new Point(40, 245);
            btnLogin.AutoSize = true;
            btnLogin.BackColor = Color.RoyalBlue;
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Click += BtnLogin_Click;

            Button btnRegister = new Button();
            btnRegister.Text = "Create Account";
            btnRegister.Size = new Size(260, 35);
            btnRegister.Location = new Point(40, 300);
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.Click += BtnRegister_Click;

            panelLogin.Controls.Add(lblTitle);
            panelLogin.Controls.Add(lblUser);
            panelLogin.Controls.Add(txtUsername);
            panelLogin.Controls.Add(lblPass);
            panelLogin.Controls.Add(txtPassword);
            panelLogin.Controls.Add(btnLogin);
            panelLogin.Controls.Add(btnRegister);

            this.Controls.Add(panelLogin);
        }
        private void BtnRegister_Click(object sender, EventArgs e)
        {
            RegisterForm reg = new RegisterForm();
            reg.ShowDialog();
        }
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            TextBox user =
              (TextBox)this.Controls.Find("txtUsername", true)[0];

            TextBox pass =
              (TextBox)this.Controls.Find("txtPassword", true)[0];

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query =
                "SELECT COUNT(*) FROM Users WHERE Username=@u AND Password=@p";

                SQLiteCommand cmd =
                    new SQLiteCommand(query, conn);

                cmd.Parameters.AddWithValue("@u", user.Text);
                cmd.Parameters.AddWithValue("@p", pass.Text);

                int count =
                    Convert.ToInt32(cmd.ExecuteScalar());

                if (count > 0)
                {
                    MessageBox.Show("Login Success");

                    DashboardForm dash =
                        new DashboardForm();

                    dash.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Invalid Login");
                }
            }
        }
    }
}
