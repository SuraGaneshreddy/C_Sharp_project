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
using Microsoft.VisualBasic;
using PersonalFinanceTracker.Data;

namespace PersonalFinanceTracker.Forms
{
    public partial class AddExpenseForm : Form
    {
        public AddExpenseForm()
        {
            InitializeComponent();
            BuildUI();
            LoadCategories();
        }

        private void BuildUI()
        {
            this.Text = "Add Transaction";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblTitle = new Label()
            {
                Text = "Title",
                Location = new Point(40, 40)
            };

            TextBox txtTitle = new TextBox()
            {
                Name = "txtTitle",
                Location = new Point(150, 40),
                Width = 180
            };

            Label lblAmount = new Label()
            {
                Text = "Amount",
                Location = new Point(40, 80)
            };

            TextBox txtAmount = new TextBox()
            {
                Name = "txtAmount",
                Location = new Point(150, 80),
                Width = 180
            };

            Label lblCategory = new Label()
            {
                Text = "Category",
                Location = new Point(40, 120)
            };

            ComboBox cmbCategory = new ComboBox()
            {
                Name = "cmbCategory",
                Location = new Point(150, 120),
                Width = 180
            };

            cmbCategory.Items.AddRange(new string[]
            {
                "Food","Transport","Rent","Bills","Shopping","Other"
            });

            Label lblType = new Label()
            {
                Text = "Type",
                Location = new Point(40, 160)
            };

            ComboBox cmbType = new ComboBox()
            {
                Name = "cmbType",
                Location = new Point(150, 160),
                Width = 180
            };
            Label lblDate = new Label()
            {
                Text = "Date",
                Location = new Point(40, 200)
            };

            DateTimePicker datePicker = new DateTimePicker()
            {
                Name = "datePicker",
                Location = new Point(150, 200),
                Width = 180,
                Format = DateTimePickerFormat.Short
            };


            cmbType.Items.AddRange(new string[]
            {
                "Income","Expense"
            });

            Button btnSave = new Button()
            {
                Text = "Save",
                Location = new Point(150, 250),
                Width = 180
            };

            btnSave.Click += SaveTransaction;

           

            

            this.Controls.Add(lblTitle);
            this.Controls.Add(txtTitle);
            this.Controls.Add(lblAmount);
            this.Controls.Add(txtAmount);
            this.Controls.Add(lblCategory);
            this.Controls.Add(cmbCategory);
            this.Controls.Add(lblType);
            this.Controls.Add(cmbType);
            this.Controls.Add(lblDate);
            this.Controls.Add(datePicker);
            this.Controls.Add(btnSave);
        }

        private void SaveTransaction(object sender, EventArgs e)
        {
            TextBox title =
            (TextBox)this.Controls.Find("txtTitle", true)[0];

            TextBox amount =
            (TextBox)this.Controls.Find("txtAmount", true)[0];

            ComboBox category =
            (ComboBox)this.Controls.Find("cmbCategory", true)[0];

            ComboBox type =
            (ComboBox)this.Controls.Find("cmbType", true)[0];

            DateTimePicker date =
            (DateTimePicker)this.Controls.Find("datePicker", true)[0];

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query =
                "INSERT INTO Transactions (Title,Amount,Category,Type,Date) VALUES (@t,@a,@c,@type,@d)";

                SQLiteCommand cmd = new SQLiteCommand(query, conn);

                cmd.Parameters.AddWithValue("@t", title.Text);
                cmd.Parameters.AddWithValue("@a", amount.Text);
                cmd.Parameters.AddWithValue("@c", category.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@type", type.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@d", date.Value.ToString("yyyy-MM-dd"));

                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Transaction Saved");

            this.Close();
        }
        private void LoadCategories()
        {
            ComboBox cmb =
            (ComboBox)this.Controls.Find("cmbCategory", true)[0];

            cmb.Items.Clear();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = "SELECT Name FROM Categories";

                SQLiteCommand cmd = new SQLiteCommand(query, conn);
                SQLiteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cmb.Items.Add(reader["Name"].ToString());
                }
            }
        }
        private void AddCategory(object sender, EventArgs e)
        {
            string name =
            Microsoft.VisualBasic.Interaction.InputBox(
                "Enter category name",
                "New Category"
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

            LoadCategories();
        }
    }
}
