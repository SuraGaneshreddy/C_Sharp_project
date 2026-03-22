using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using PersonalFinanceTracker.Data;
using Microsoft.VisualBasic;

namespace PersonalFinanceTracker.Forms
{
    public class DashboardForm : Form
    {
        Label lblIncome, lblExpense, lblBalance;

        public DashboardForm()
        {
            BuildUI();

            LoadSummary();
            LoadSummaryChart();
            LoadCategoryChart();
            LoadTransactions();
        }

        private void BuildUI()
        {
            this.Text = "Finance Dashboard";
            this.Size = new Size(1000, 550);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Title
            Label title = new Label()
            {
                Text = "Finance Overview",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(title);

            // Summary Labels
            lblIncome = new Label() { Location = new Point(20, 80), AutoSize = true };
            lblExpense = new Label() { Location = new Point(20, 110), AutoSize = true };
            lblBalance = new Label()
            {
                Location = new Point(20, 140),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            this.Controls.Add(lblIncome);
            this.Controls.Add(lblExpense);
            this.Controls.Add(lblBalance);

            // Buttons
            Button btnAdd = new Button()
            {
                Text = "Add Transaction",
                Location = new Point(20, 200),
                Width = 150
            };
            btnAdd.Click += OpenAddExpense;

            Button btnCategory = new Button()
            {
                Text = "New Category",
                Location = new Point(20, 240),
                Width = 150
            };
            btnCategory.Click += OpenCategory;

            Button btnExport = new Button()
            {
                Text = "Export Report",
                Location = new Point(20, 280),
                Width = 150
            };

            this.Controls.Add(btnAdd);
            this.Controls.Add(btnCategory);
            this.Controls.Add(btnExport);

            // -------- Chart 1 (Income vs Expense) --------
            Chart chartSummary = new Chart();
            chartSummary.Name = "chartSummary";
            chartSummary.Size = new Size(300, 250);
            chartSummary.Location = new Point(250, 50);

            chartSummary.ChartAreas.Add(new ChartArea());
            Series s1 = new Series();
            s1.ChartType = SeriesChartType.Pie;
            chartSummary.Series.Add(s1);

            this.Controls.Add(chartSummary);

            // -------- Chart 2 (Category) --------
            Chart chartCategory = new Chart();
            chartCategory.Name = "chartCategory";
            chartCategory.Size = new Size(300, 250);
            chartCategory.Location = new Point(600, 50);

            chartCategory.ChartAreas.Add(new ChartArea());
            Series s2 = new Series();
            s2.ChartType = SeriesChartType.Pie;
            chartCategory.Series.Add(s2);

            this.Controls.Add(chartCategory);

            // -------- DataGrid --------
            DataGridView grid = new DataGridView();
            grid.Name = "gridTransactions";
            grid.Location = new Point(20, 320);
            grid.Size = new Size(940, 180);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            grid.CellClick += Grid_CellClick;

            this.Controls.Add(grid);
        }

        // ================= SUMMARY =================

        private void LoadSummary()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                double income = 0;
                double expense = 0;

                SQLiteCommand cmd;

                cmd = new SQLiteCommand(
                    "SELECT IFNULL(SUM(Amount),0) FROM Transactions WHERE Type='Income'", conn);
                income = Convert.ToDouble(cmd.ExecuteScalar());

                cmd = new SQLiteCommand(
                    "SELECT IFNULL(SUM(Amount),0) FROM Transactions WHERE Type='Expense'", conn);
                expense = Convert.ToDouble(cmd.ExecuteScalar());

                lblIncome.Text = "Income: " + income;
                lblExpense.Text = "Expense: " + expense;
                lblBalance.Text = "Balance: " + (income - expense);
            }
        }

        // ================= CHART 1 =================

        private void LoadSummaryChart()
        {
            Chart chart = (Chart)this.Controls.Find("chartSummary", true)[0];
            chart.Series[0].Points.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query =
                @"SELECT Type, SUM(Amount)
                  FROM Transactions
                  GROUP BY Type";

                SQLiteCommand cmd = new SQLiteCommand(query, conn);
                SQLiteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    chart.Series[0].Points.AddXY(
                        reader["Type"].ToString(),
                        Convert.ToDouble(reader[1])
                    );
                }
            }

            chart.Series[0].IsValueShownAsLabel = true;
        }

        // ================= CHART 2 =================

        private void LoadCategoryChart()
        {
            Chart chart = (Chart)this.Controls.Find("chartCategory", true)[0];
            chart.Series[0].Points.Clear();

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
                    chart.Series[0].Points.AddXY(
                        reader["Category"].ToString(),
                        Convert.ToDouble(reader[1])
                    );
                }
            }

            chart.Series[0].IsValueShownAsLabel = true;
        }

        // ================= TABLE =================

        private void LoadTransactions()
        {
            DataGridView grid =
            (DataGridView)this.Controls.Find("gridTransactions", true)[0];

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query =
                "SELECT TransactionId, Date, Title, Category, Amount, Type FROM Transactions";

                SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                grid.DataSource = dt;
            }

            // Add Delete button once
            if (!grid.Columns.Contains("Delete"))
            {
                DataGridViewButtonColumn btnDelete = new DataGridViewButtonColumn();
                btnDelete.Name = "Delete";
                btnDelete.Text = "Delete";
                btnDelete.UseColumnTextForButtonValue = true;

                grid.Columns.Add(btnDelete);
            }
        }

        // ================= DELETE =================

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView grid = (DataGridView)sender;

            if (e.RowIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "Delete")
            {
                int id = Convert.ToInt32(
                    grid.Rows[e.RowIndex].Cells["TransactionId"].Value);

                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string query =
                    "DELETE FROM Transactions WHERE TransactionId=@id";

                    SQLiteCommand cmd = new SQLiteCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);

                    cmd.ExecuteNonQuery();
                }

                // Refresh everything
                LoadTransactions();
                LoadSummary();
                LoadSummaryChart();
                LoadCategoryChart();
            }
        }

        // ================= BUTTONS =================

        private void OpenAddExpense(object sender, EventArgs e)
        {
            AddExpenseForm form = new AddExpenseForm();
            form.ShowDialog();

            LoadTransactions();
            LoadSummary();
            LoadSummaryChart();
            LoadCategoryChart();
        }

        private void OpenCategory(object sender, EventArgs e)
        {
            string name = Interaction.InputBox(
                "Enter category", "New Category", "");

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

            MessageBox.Show("Category added!");
        }
    }
}
