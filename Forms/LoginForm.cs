using System;
using System.Drawing;
using System.Windows.Forms;
using FinanceTracker.Database;

namespace FinanceTracker.Forms
{
    public class LoginForm : Form
    {
        private static readonly Color C_BG = Color.FromArgb(10, 12, 20);
        private static readonly Color C_PANEL = Color.FromArgb(18, 21, 33);
        private static readonly Color C_ACCENT = Color.FromArgb(99, 102, 241);
        private static readonly Color C_TEXT = Color.FromArgb(241, 245, 249);
        private static readonly Color C_MUTED = Color.FromArgb(148, 163, 184);
        private static readonly Color C_INPUT_BG = Color.FromArgb(28, 32, 50);
        private static readonly Color C_ERROR = Color.Red;

        private TextBox _txtEmail;
        private TextBox _txtPassword;
        private Label _lblError;
        private CheckBox _chkShow;

        public LoginForm()
        {
            BuildForm();
        }

        private void BuildForm()
        {
            Text = "FinanceTrack — Sign In";
            Size = new Size(960, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = C_BG;

            var right = new Panel
            {
                Bounds = new Rectangle(420, 0, 540, 600),
                BackColor = C_PANEL
            };
            Controls.Add(right);

            int lx = 80;
            int fw = 380;

            right.Controls.Add(new Label
            {
                Text = "Welcome back",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(lx, 60),
                AutoSize = true
            });

            // Email
            AddLabel(right, "Email", lx, 140);
            _txtEmail = MakeInput(right, lx, 165, fw, false);

            // Password
            AddLabel(right, "Password", lx, 230);
            _txtPassword = MakeInput(right, lx, 255, fw, true);

            // Show password
            _chkShow = new CheckBox
            {
                Text = "Show password",
                ForeColor = C_MUTED,
                Location = new Point(lx, 300),
                AutoSize = true
            };

            _chkShow.CheckedChanged += (s, e) =>
            {
                _txtPassword.UseSystemPasswordChar = !_chkShow.Checked;
            };

            right.Controls.Add(_chkShow);

            // Error
            _lblError = new Label
            {
                ForeColor = C_ERROR,
                Location = new Point(lx, 330),
                Width = fw
            };
            right.Controls.Add(_lblError);

            // Login button
            var btnLogin = new Button
            {
                Text = "Sign In",
                Location = new Point(lx, 360),
                Size = new Size(fw, 45),
                BackColor = C_ACCENT,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += OnLogin;
            right.Controls.Add(btnLogin);

            // Register button
            var btnRegister = new Button
            {
                Text = "Create Account",
                Location = new Point(lx, 420),
                Size = new Size(fw, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = C_INPUT_BG,
                ForeColor = C_TEXT
            };
            btnRegister.Click += OpenRegister;
            right.Controls.Add(btnRegister);

            _txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    btnLogin.PerformClick();
            };
        }

        // ✅ LOGIN FIXED
        private void OnLogin(object sender, EventArgs e)
        {
            string email = _txtEmail.Text.Trim();
            string password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email))
            {
                _lblError.Text = "Enter email";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                _lblError.Text = "Enter password";
                return;
            }

            var result = UserManager.Instance.Login(email, password);

            if (result == LoginResult.Success)
            {
                MessageBox.Show("Login successful!");
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblError.Text = "Invalid login";
            }
        }

        // REGISTER NAVIGATION
        private void OpenRegister(object sender, EventArgs e)
        {
            using (var reg = new RegisterForm())
            {
                if (reg.ShowDialog() == DialogResult.OK)
                {
                    _txtEmail.Text = reg.RegisteredEmail;
                }
            }
        }

        // ✅ CLEAN INPUT (NO PLACEHOLDER)
        private TextBox MakeInput(Control parent, int x, int y, int w, bool isPassword)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Width = w,
                ForeColor = Color.Black,
                BackColor = Color.White,
                UseSystemPasswordChar = isPassword
            };

            parent.Controls.Add(txt);
            return txt;
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                ForeColor = C_MUTED,
                Location = new Point(x, y),
                AutoSize = true
            });
        }
    }
}
