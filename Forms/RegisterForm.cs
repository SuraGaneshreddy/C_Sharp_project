using System;
using System.Drawing;
using System.Windows.Forms;
using FinanceTracker.Database;

namespace FinanceTracker.Forms
{
    public class RegisterForm : Form
    {
        private static readonly Color C_TEXT = Color.Black;
        private static readonly Color C_ERROR = Color.Red;

        private TextBox _txtName, _txtEmail, _txtPassword, _txtConfirm;
        private Label _lblError;

        public string RegisteredEmail { get; private set; }

        public RegisterForm()
        {
            BuildForm();
        }

        private void BuildForm()
        {
            Text = "Register";
            Size = new Size(500, 550);
            StartPosition = FormStartPosition.CenterScreen;

            int x = 50, w = 380, y = 40, gap = 70;

            // 🔹 FULL NAME
            AddLabel("Full Name", x, y);
            _txtName = AddInput(x, y + 25, w);

            // 🔹 EMAIL
            AddLabel("Email Address", x, y += gap);
            _txtEmail = AddInput(x, y + 25, w);

            // 🔹 PASSWORD
            AddLabel("Password", x, y += gap);
            _txtPassword = AddInput(x, y + 25, w, true);

            // 🔹 CONFIRM PASSWORD
            AddLabel("Confirm Password", x, y += gap);
            _txtConfirm = AddInput(x, y + 25, w, true);

            // 🔹 ERROR LABEL
            _lblError = new Label
            {
                ForeColor = C_ERROR,
                Location = new Point(x, y + 70),
                Width = w
            };
            Controls.Add(_lblError);

            // 🔹 REGISTER BUTTON
            var btn = new Button
            {
                Text = "Create Account",
                Location = new Point(x, y + 100),
                Size = new Size(w, 40),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White
            };
            btn.Click += OnRegister;
            Controls.Add(btn);
        }

        // ✅ REGISTER LOGIC
        private void OnRegister(object sender, EventArgs e)
        {
            string name = _txtName.Text.Trim();
            string email = _txtEmail.Text.Trim();
            string pwd = _txtPassword.Text;
            string conf = _txtConfirm.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                _lblError.Text = "Enter your name";
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                _lblError.Text = "Enter your email";
                return;
            }

            if (string.IsNullOrWhiteSpace(pwd))
            {
                _lblError.Text = "Enter password";
                return;
            }

            if (pwd != conf)
            {
                _lblError.Text = "Passwords do not match";
                return;
            }

            var result = UserManager.Instance.Register(name, email, pwd);
            MessageBox.Show(result.ToString());

            if (result == RegisterResult.Success)
            {
                RegisteredEmail = email;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                switch (result)
                {
                    case RegisterResult.EmailTaken:
                        _lblError.Text = "Email already exists";
                        break;

                    case RegisterResult.InvalidEmail:
                        _lblError.Text = "Invalid email format";
                        break;

                    case RegisterResult.WeakPassword:
                        _lblError.Text = "Password must be at least 6 characters";
                        break;

                    case RegisterResult.EmptyName:
                        _lblError.Text = "Name is required";
                        break;

                    case RegisterResult.EmptyEmail:
                        _lblError.Text = "Email is required";
                        break;

                    case RegisterResult.EmptyPassword:
                        _lblError.Text = "Password is required";
                        break;

                    default:
                        _lblError.Text = "Something went wrong";
                        break;
                }
            }
        }

        // ✅ INPUT FIELD
        private TextBox AddInput(int x, int y, int w, bool isPassword = false)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Width = w,
                ForeColor = C_TEXT
            };

            if (isPassword)
                txt.UseSystemPasswordChar = true;

            Controls.Add(txt);
            return txt;
        }

        // ✅ LABEL
        private void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            });
        }
    }
}
