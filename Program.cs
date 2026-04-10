using System;
using System.Windows.Forms;
using FinanceTracker.Database;
using FinanceTracker.Forms;

namespace FinanceTracker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize database (creates tables, seeds data)
            var _ = DatabaseManager.Instance;

            // Show Login form — only launch MainForm on successful login
            var login = new LoginForm();
            if (login.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainForm());
            }
        }
    }
}
