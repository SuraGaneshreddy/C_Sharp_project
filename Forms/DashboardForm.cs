using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace PersonalFinanceTracker.Forms
{
    public partial class DashboardForm : Form
    {
        public DashboardForm()
        {
            InitializeComponent();

            this.Text = "Dashboard";
            this.Size = new Size(900, 550);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lbl = new Label();
            lbl.Text = "Welcome to Personal Finance Tracker";
            lbl.Font = new Font("Segoe UI", 16,
                FontStyle.Bold);
            lbl.AutoSize = true;
            lbl.Location = new Point(180, 200);

            Controls.Add(lbl);
        }
    }
}
