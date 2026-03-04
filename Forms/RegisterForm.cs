using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PersonalFinanceTracker.Data;
using System.Data.SQLite;

namespace PersonalFinanceTracker.Forms
{
    public partial class RegisterForm : Form
    {
        TextBox txtUsername;
        TextBox txtPassword;

        public RegisterForm()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "Register User";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblUser = new Label()
            {
                Text = "Username",
                Location = new Point(40, 50)
            };

            txtUsername = new TextBox()
            {
                Location = new Point(40, 75),
                Width = 280
            };

            Label lblPass = new Label()
            {
                Text = "Password",
                Location = new Point(40, 120)
            };

            txtPassword = new TextBox()
            {
                Location = new Point(40, 145),
                Width = 280,
                UseSystemPasswordChar = true
            };

            Button btnRegister = new Button()
            {
                Text = "Register",
                Location = new Point(40, 200),
                Width = 280,
                Height = 40
            };

            btnRegister.Click += BtnRegister_Click;

            Controls.Add(lblUser);
            Controls.Add(txtUsername);
            Controls.Add(lblPass);
            Controls.Add(txtPassword);
            Controls.Add(btnRegister);
        }
        private void BtnRegister_Click(object sender, EventArgs e)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query =
                "INSERT INTO Users (Username,Password) VALUES (@u,@p)";

                SQLiteCommand cmd =
                    new SQLiteCommand(query, conn);

                cmd.Parameters.AddWithValue("@u", txtUsername.Text);
                cmd.Parameters.AddWithValue("@p", txtPassword.Text);

                cmd.ExecuteNonQuery();

                MessageBox.Show("User Registered Successfully");

                this.Close();
            }
        }
    }
}
