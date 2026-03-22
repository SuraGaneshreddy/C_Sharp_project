using System;
using System.Windows.Forms;
using PersonalFinanceTracker.Data;
using PersonalFinanceTracker.Forms;

namespace PersonalFinanceTracker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DatabaseHelper.InitializeDatabase();

            Application.Run(new LoginForm());
        }
    }
}
