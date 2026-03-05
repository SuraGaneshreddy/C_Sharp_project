using System.Data.SQLite;

namespace PersonalFinanceTracker.Data
{
    public class DatabaseHelper
    {
        private static string connectionString =
            "Data Source=finance.db;Version=3;";

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(connectionString);
        }

        public static void InitializeDatabase()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                string userTable =
                @"CREATE TABLE IF NOT EXISTS Users(
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT,
                    Password TEXT
                );";

                string transactionTable =
                @"CREATE TABLE IF NOT EXISTS Transactions(
                    TransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT,
                    Amount REAL,
                    Category TEXT,
                    Type TEXT,
                    Date TEXT
                );";

                string categoryTable =
                @"CREATE TABLE IF NOT EXISTS Categories(
                CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT
                );";

                SQLiteCommand cmd = new SQLiteCommand(userTable, conn);
                cmd.ExecuteNonQuery();

                cmd = new SQLiteCommand(transactionTable, conn);
                cmd.ExecuteNonQuery();

                cmd = new SQLiteCommand(categoryTable, conn);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
