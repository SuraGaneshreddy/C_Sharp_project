using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Windows.Forms.DataVisualization.Charting;
using PersonalFinanceTracker.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.VisualBasic;

namespace PersonalFinanceTracker.Forms
{
    public partial class DashboardForm : Form
    {
        Label lblIncome;
        Label lblExpense;
        Label lblBalance;
        Chart financeChart;

        public DashboardForm()
        {
            InitializeComponent();
            BuildUI();
            LoadSummary();
            LoadMonthlyChart();
            LoadPieChart();
        }

        private void BuildUI()
        {
            this.Text = "Finance Dashboard";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label title = new Label()
            {
                Text = "Finance Overview",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(30, 20),
                AutoSize = true
            };

            lblIncome = new Label()
            {
                Text = "Income: 0",
                Font = new Font("Segoe UI", 12),
                Location = new Point(30, 80),
                AutoSize = true
            };

            lblExpense = new Label()
            {
                Text = "Expense: 0",
                Font = new Font("Segoe UI", 12),
                Location = new Point(30, 120),
                AutoSize = true
            };

            lblBalance = new Label()
            {
                Text = "Balance: 0",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(30, 160),
                AutoSize = true
            };

            financeChart = new Chart();
            financeChart.Location = new Point(300, 80);
            financeChart.Size = new Size(500, 350);

            ChartArea area = new ChartArea();
            financeChart.ChartAreas.Add(area);

            Series series = new Series();
            series.ChartType = SeriesChartType.Pie;

            financeChart.Series.Add(series);

            this.Controls.Add(title);
            this.Controls.Add(lblIncome);
            this.Controls.Add(lblExpense);
            this.Controls.Add(lblBalance);
            this.Controls.Add(financeChart);

            Button btnAddExpense = new Button()
            {
                Text = "Add Transaction",
                Location = new Point(30, 220),
                Width = 150
            };

            btnAddExpense.Click += OpenAddExpense;

            this.Controls.Add(btnAddExpense);

            Button btnCategory = new Button()
            {
                Text = "New Category",
                Location = new Point(30, 250),
                Width = 150
            };

            btnCategory.Click += OpenCategory;

            this.Controls.Add(btnCategory);


            Button btnExport = new Button();
            btnExport.Text = "Export Report";
            btnExport.Location = new Point(30, 300);
            btnExport.Width = 150;

            btnExport.Click += ExportPDF;

            this.Controls.Add(btnExport);
        }

        private void LoadSummary()
        {
            double income = 0;
            double expense = 0;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string incomeQuery =
                "SELECT IFNULL(SUM(Amount),0) FROM Transactions WHERE Type='Income'";

                string expenseQuery =
                "SELECT IFNULL(SUM(Amount),0) FROM Transactions WHERE Type='Expense'";

                SQLiteCommand cmdIncome =
                    new SQLiteCommand(incomeQuery, conn);

                SQLiteCommand cmdExpense =
                    new SQLiteCommand(expenseQuery, conn);

                income = Convert.ToDouble(cmdIncome.ExecuteScalar());
                expense = Convert.ToDouble(cmdExpense.ExecuteScalar());
            }

            double balance = income - expense;

            lblIncome.Text = "Income: " + income;
            lblExpense.Text = "Expense: " + expense;
            lblBalance.Text = "Balance: " + balance;

            financeChart.Series[0].Points.Clear();
            financeChart.Series[0].Points.AddXY("Income", income);
            financeChart.Series[0].Points.AddXY("Expense", expense);
        }
        private void LoadMonthlyChart()
        {
            financeChart.Series[0].Points.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query =
                @"SELECT strftime('%m',Date) as Month,
          SUM(Amount)
          FROM Transactions
          WHERE Type='Expense'
          GROUP BY Month";

                SQLiteCommand cmd = new SQLiteCommand(query, conn);
                SQLiteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    financeChart.Series[0].Points.AddXY(
                        reader["Month"].ToString(),
                        Convert.ToDouble(reader[1])
                    );
                }
            }
        }

        private void OpenAddExpense(object sender, EventArgs e)
        {
            AddExpenseForm form = new AddExpenseForm();
            form.ShowDialog();

            // refresh dashboard
            LoadSummary();
            LoadMonthlyChart();
            LoadPieChart();
        }
        private void ExportPDF(object sender, EventArgs e)
        {
            string path = "FinanceReport.pdf";

            PdfWriter writer = new PdfWriter(path);
            PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            document.Add(new Paragraph("Personal Finance Report"));
            document.Add(new Paragraph(lblIncome.Text));
            document.Add(new Paragraph(lblExpense.Text));
            document.Add(new Paragraph(lblBalance.Text));

            document.Close();

            MessageBox.Show("Report exported successfully!");
        }
        private void OpenCategory(object sender, EventArgs e)
        {
            string name =
            Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new category",
                "Add Category",
                ""
            );

            if (name == "") return;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query =
                "INSERT INTO Categories (Name) VALUES (@name)";

                SQLiteCommand cmd = new SQLiteCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", name);

                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Category added successfully");
        }
        private void LoadPieChart()
        {
            financeChart.Series[0].Points.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query =
                @"SELECT Category, SUM(Amount)
          FROM Transactions
          WHERE Type='Expense'
          GROUP BY Category";

                SQLiteCommand cmd = new SQLiteCommand(query, conn);
                SQLiteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    financeChart.Series[0].Points.AddXY(
                        reader["Category"].ToString(),
                        Convert.ToDouble(reader[1])
                    );
                }
            }
        }
    }
}
