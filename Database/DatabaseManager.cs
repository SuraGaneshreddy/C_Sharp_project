using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using FinanceTracker.Models;

namespace FinanceTracker.Database
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        private readonly string _connectionString;
        private readonly string _dbPath;

        public static DatabaseManager Instance => _instance ?? (_instance = new DatabaseManager());

        private DatabaseManager()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FinanceTracker");
            Directory.CreateDirectory(appDataPath);
            _dbPath = Path.Combine(appDataPath, "finance.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";
            InitializeDatabase();
        }

        // Every public method calls this to get a fresh, already-open connection
        private SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void InitializeDatabase()
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    PRAGMA foreign_keys = ON;

                    CREATE TABLE IF NOT EXISTS Accounts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Type TEXT NOT NULL,
                        Balance REAL NOT NULL DEFAULT 0,
                        Currency TEXT NOT NULL DEFAULT 'USD',
                        Color TEXT NOT NULL DEFAULT '#4A90D9',
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                    );

                    CREATE TABLE IF NOT EXISTS Categories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Type TEXT NOT NULL,
                        Color TEXT NOT NULL DEFAULT '#888888',
                        Icon TEXT NOT NULL DEFAULT '💰',
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );

                    CREATE TABLE IF NOT EXISTS Transactions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountId INTEGER NOT NULL,
                        CategoryId INTEGER NOT NULL,
                        Amount REAL NOT NULL,
                        Type TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        Date TEXT NOT NULL,
                        Notes TEXT,
                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                        FOREIGN KEY (AccountId) REFERENCES Accounts(Id),
                        FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Budgets (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CategoryId INTEGER NOT NULL,
                        LimitAmount REAL NOT NULL,
                        Month INTEGER NOT NULL,
                        Year INTEGER NOT NULL,
                        CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                        FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
                        UNIQUE(CategoryId, Month, Year)
                    );
                ";
                cmd.ExecuteNonQuery();
                SeedDefaultData(conn);
            }
        }

        private void SeedDefaultData(SQLiteConnection conn)
        {
            var check = conn.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM Categories";
            long count = (long)check.ExecuteScalar();
            if (count > 0) return;

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Categories (Name, Type, Color, Icon) VALUES
                ('Salary',        'Income',  '#27AE60', '💼'),
                ('Freelance',     'Income',  '#2ECC71', '💻'),
                ('Investment',    'Income',  '#1ABC9C', '📈'),
                ('Other Income',  'Income',  '#16A085', '💵'),
                ('Food & Dining', 'Expense', '#E74C3C', '🍔'),
                ('Transport',     'Expense', '#E67E22', '🚗'),
                ('Shopping',      'Expense', '#9B59B6', '🛍'),
                ('Utilities',     'Expense', '#3498DB', '💡'),
                ('Health',        'Expense', '#E91E63', '💊'),
                ('Entertainment', 'Expense', '#F39C12', '🎬'),
                ('Education',     'Expense', '#00BCD4', '📚'),
                ('Rent',          'Expense', '#795548', '🏠'),
                ('Insurance',     'Expense', '#607D8B', '🛡'),
                ('Groceries',     'Expense', '#FF5722', '🛒'),
                ('Other',         'Expense', '#95A5A6', '📦');

                INSERT INTO Accounts (Name, Type, Balance, Color) VALUES
                ('Main Checking', 'Checking', 5000.00, '#4A90D9'),
                ('Savings',       'Savings',  12000.00, '#27AE60'),
                ('Cash Wallet',   'Cash',     250.00,   '#F39C12');
            ";
            cmd.ExecuteNonQuery();
        }

        // ─── ACCOUNTS ────────────────────────────────────────────────────────────

        public List<Account> GetAccounts(bool activeOnly = true)
        {
            var list = new List<Account>();
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = activeOnly
                    ? "SELECT * FROM Accounts WHERE IsActive = 1 ORDER BY Name"
                    : "SELECT * FROM Accounts ORDER BY Name";
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        list.Add(MapAccount(reader));
            }
            return list;
        }

        public Account GetAccount(int id)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM Accounts WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? MapAccount(r) : null;
            }
        }

        public void SaveAccount(Account a)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                if (a.Id == 0)
                {
                    cmd.CommandText = @"INSERT INTO Accounts (Name,Type,Balance,Currency,Color,IsActive)
                                        VALUES (@n,@t,@b,@cu,@co,1)";
                }
                else
                {
                    cmd.CommandText = @"UPDATE Accounts SET Name=@n,Type=@t,Balance=@b,
                                        Currency=@cu,Color=@co WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@id", a.Id);
                }
                cmd.Parameters.AddWithValue("@n",  a.Name);
                cmd.Parameters.AddWithValue("@t",  a.Type);
                cmd.Parameters.AddWithValue("@b",  (double)a.Balance);
                cmd.Parameters.AddWithValue("@cu", a.Currency);
                cmd.Parameters.AddWithValue("@co", a.Color);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteAccount(int id)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Accounts SET IsActive=0 WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Account MapAccount(SQLiteDataReader r) => new Account
        {
            Id       = r.GetInt32(r.GetOrdinal("Id")),
            Name     = r["Name"].ToString(),
            Type     = r["Type"].ToString(),
            Balance  = Convert.ToDecimal(r["Balance"]),
            Currency = r["Currency"].ToString(),
            Color    = r["Color"].ToString(),
            IsActive = Convert.ToInt32(r["IsActive"]) == 1
        };

        // ─── CATEGORIES ──────────────────────────────────────────────────────────

        public List<Category> GetCategories(string type = null, bool activeOnly = true)
        {
            var list = new List<Category>();
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                string where = activeOnly ? "WHERE IsActive=1" : "WHERE 1=1";
                if (type != null) where += " AND Type=@type";
                cmd.CommandText = $"SELECT * FROM Categories {where} ORDER BY Type, Name";
                if (type != null) cmd.Parameters.AddWithValue("@type", type);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(MapCategory(r));
            }
            return list;
        }

        public void SaveCategory(Category c)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                if (c.Id == 0)
                    cmd.CommandText = "INSERT INTO Categories (Name,Type,Color,Icon,IsActive) VALUES(@n,@t,@co,@i,1)";
                else
                {
                    cmd.CommandText = "UPDATE Categories SET Name=@n,Type=@t,Color=@co,Icon=@i WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@id", c.Id);
                }
                cmd.Parameters.AddWithValue("@n",  c.Name);
                cmd.Parameters.AddWithValue("@t",  c.Type);
                cmd.Parameters.AddWithValue("@co", c.Color);
                cmd.Parameters.AddWithValue("@i",  c.Icon);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteCategory(int id)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Categories SET IsActive=0 WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Category MapCategory(SQLiteDataReader r) => new Category
        {
            Id       = r.GetInt32(r.GetOrdinal("Id")),
            Name     = r["Name"].ToString(),
            Type     = r["Type"].ToString(),
            Color    = r["Color"].ToString(),
            Icon     = r["Icon"].ToString(),
            IsActive = Convert.ToInt32(r["IsActive"]) == 1
        };

        // ─── TRANSACTIONS ─────────────────────────────────────────────────────────

        public List<Transaction> GetTransactions(DateTime? from = null, DateTime? to = null,
            int? accountId = null, int? categoryId = null, string type = null)
        {
            var list = new List<Transaction>();
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT t.*, a.Name AS AccountName, c.Name AS CategoryName, c.Color AS CategoryColor
                    FROM Transactions t
                    JOIN Accounts a ON t.AccountId = a.Id
                    JOIN Categories c ON t.CategoryId = c.Id
                    WHERE 1=1";

                if (from.HasValue)      { cmd.CommandText += " AND t.Date >= @from"; cmd.Parameters.AddWithValue("@from", from.Value.ToString("yyyy-MM-dd")); }
                if (to.HasValue)        { cmd.CommandText += " AND t.Date <= @to";   cmd.Parameters.AddWithValue("@to",   to.Value.ToString("yyyy-MM-dd")); }
                if (accountId.HasValue) { cmd.CommandText += " AND t.AccountId=@aid";cmd.Parameters.AddWithValue("@aid",  accountId.Value); }
                if (categoryId.HasValue){ cmd.CommandText += " AND t.CategoryId=@cid";cmd.Parameters.AddWithValue("@cid", categoryId.Value); }
                if (type != null)       { cmd.CommandText += " AND t.Type=@type";    cmd.Parameters.AddWithValue("@type", type); }

                cmd.CommandText += " ORDER BY t.Date DESC, t.Id DESC";
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(MapTransaction(r));
            }
            return list;
        }

        // FIX: One connection opened via GetConnection(), no second Open() call
        public void SaveTransaction(Transaction t)
        {
            using (var conn = GetConnection())
            using (var dbTrans = conn.BeginTransaction())
            {
                try
                {
                    var cmd = conn.CreateCommand();

                    if (t.Id == 0)
                    {
                        // New transaction
                        cmd.CommandText = @"
                            INSERT INTO Transactions (AccountId,CategoryId,Amount,Type,Description,Date,Notes)
                            VALUES(@aid,@cid,@amt,@type,@desc,@date,@notes)";
                    }
                    else
                    {
                        // Edit: reverse the old amount from account balance first
                        var old = GetTransactionByIdInternal(conn, t.Id);
                        if (old != null)
                        {
                            decimal reversal = old.Type == "Income" ? -old.Amount : old.Amount;
                            AdjustAccountBalance(conn, old.AccountId, reversal);
                        }

                        cmd.CommandText = @"
                            UPDATE Transactions SET
                                AccountId=@aid, CategoryId=@cid, Amount=@amt,
                                Type=@type, Description=@desc, Date=@date, Notes=@notes
                            WHERE Id=@id";
                        cmd.Parameters.AddWithValue("@id", t.Id);
                    }

                    cmd.Parameters.AddWithValue("@aid",   t.AccountId);
                    cmd.Parameters.AddWithValue("@cid",   t.CategoryId);
                    cmd.Parameters.AddWithValue("@amt",   (double)t.Amount);
                    cmd.Parameters.AddWithValue("@type",  t.Type);
                    cmd.Parameters.AddWithValue("@desc",  t.Description);
                    cmd.Parameters.AddWithValue("@date",  t.Date.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@notes", t.Notes ?? "");
                    cmd.ExecuteNonQuery();

                    // Apply balance change for this transaction
                    decimal balanceChange = t.Type == "Income" ? t.Amount : -t.Amount;
                    AdjustAccountBalance(conn, t.AccountId, balanceChange);

                    dbTrans.Commit();
                }
                catch
                {
                    dbTrans.Rollback();
                    throw;
                }
            }
        }

        // FIX: One connection opened via GetConnection(), no second Open() call
        public void DeleteTransaction(int id)
        {
            using (var conn = GetConnection())
            using (var dbTrans = conn.BeginTransaction())
            {
                try
                {
                    var t = GetTransactionByIdInternal(conn, id);
                    if (t != null)
                    {
                        // Reverse the balance effect
                        decimal reversal = t.Type == "Income" ? -t.Amount : t.Amount;
                        AdjustAccountBalance(conn, t.AccountId, reversal);

                        var cmd = conn.CreateCommand();
                        cmd.CommandText = "DELETE FROM Transactions WHERE Id=@id";
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                    dbTrans.Commit();
                }
                catch
                {
                    dbTrans.Rollback();
                    throw;
                }
            }
        }

        // Uses an already-open connection — does NOT call Open() again
        private Transaction GetTransactionByIdInternal(SQLiteConnection conn, int id)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT t.*, a.Name AS AccountName, c.Name AS CategoryName, c.Color AS CategoryColor
                FROM Transactions t
                JOIN Accounts a ON t.AccountId = a.Id
                JOIN Categories c ON t.CategoryId = c.Id
                WHERE t.Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            using (var r = cmd.ExecuteReader())
                return r.Read() ? MapTransaction(r) : null;
        }

        private void AdjustAccountBalance(SQLiteConnection conn, int accountId, decimal delta)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Accounts SET Balance = Balance + @delta WHERE Id=@id";
            cmd.Parameters.AddWithValue("@delta", (double)delta);
            cmd.Parameters.AddWithValue("@id",    accountId);
            cmd.ExecuteNonQuery();
        }

        private Transaction MapTransaction(SQLiteDataReader r) => new Transaction
        {
            Id           = r.GetInt32(r.GetOrdinal("Id")),
            AccountId    = r.GetInt32(r.GetOrdinal("AccountId")),
            CategoryId   = r.GetInt32(r.GetOrdinal("CategoryId")),
            AccountName  = r["AccountName"].ToString(),
            CategoryName = r["CategoryName"].ToString(),
            CategoryColor= r["CategoryColor"].ToString(),
            Amount       = Convert.ToDecimal(r["Amount"]),
            Type         = r["Type"].ToString(),
            Description  = r["Description"].ToString(),
            Date         = DateTime.Parse(r["Date"].ToString()),
            Notes        = r["Notes"].ToString(),
            CreatedAt    = r["CreatedAt"] != DBNull.Value
                           ? DateTime.Parse(r["CreatedAt"].ToString())
                           : DateTime.Now
        };

        // ─── BUDGETS ──────────────────────────────────────────────────────────────

        public List<Budget> GetBudgets(int month, int year)
        {
            var list = new List<Budget>();
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT b.*, c.Name AS CategoryName, c.Color AS CategoryColor,
                    COALESCE((
                        SELECT SUM(t.Amount) FROM Transactions t
                        WHERE t.CategoryId = b.CategoryId
                          AND t.Type = 'Expense'
                          AND strftime('%m', t.Date) = @month
                          AND strftime('%Y', t.Date) = @year
                    ), 0) AS SpentAmount
                    FROM Budgets b
                    JOIN Categories c ON b.CategoryId = c.Id
                    WHERE b.Month = @m AND b.Year = @y
                    ORDER BY c.Name";
                cmd.Parameters.AddWithValue("@m",     month);
                cmd.Parameters.AddWithValue("@y",     year);
                cmd.Parameters.AddWithValue("@month", month.ToString("D2"));
                cmd.Parameters.AddWithValue("@year",  year.ToString());
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(MapBudget(r));
            }
            return list;
        }

        public void SaveBudget(Budget b)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                if (b.Id == 0)
                    cmd.CommandText = @"INSERT OR REPLACE INTO Budgets (CategoryId,LimitAmount,Month,Year)
                                        VALUES(@cid,@limit,@m,@y)";
                else
                {
                    cmd.CommandText = "UPDATE Budgets SET LimitAmount=@limit WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@id", b.Id);
                }
                cmd.Parameters.AddWithValue("@cid",   b.CategoryId);
                cmd.Parameters.AddWithValue("@limit", (double)b.LimitAmount);
                cmd.Parameters.AddWithValue("@m",     b.Month);
                cmd.Parameters.AddWithValue("@y",     b.Year);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteBudget(int id)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Budgets WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private Budget MapBudget(SQLiteDataReader r) => new Budget
        {
            Id            = r.GetInt32(r.GetOrdinal("Id")),
            CategoryId    = r.GetInt32(r.GetOrdinal("CategoryId")),
            CategoryName  = r["CategoryName"].ToString(),
            CategoryColor = r["CategoryColor"].ToString(),
            LimitAmount   = Convert.ToDecimal(r["LimitAmount"]),
            SpentAmount   = Convert.ToDecimal(r["SpentAmount"]),
            Month         = r.GetInt32(r.GetOrdinal("Month")),
            Year          = r.GetInt32(r.GetOrdinal("Year"))
        };

        // ─── SUMMARY / REPORTS ────────────────────────────────────────────────────

        public (decimal income, decimal expense, decimal balance) GetMonthlySummary(int month, int year)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        COALESCE(SUM(CASE WHEN Type='Income'  THEN Amount ELSE 0 END), 0) AS Income,
                        COALESCE(SUM(CASE WHEN Type='Expense' THEN Amount ELSE 0 END), 0) AS Expense
                    FROM Transactions
                    WHERE strftime('%m', Date) = @m AND strftime('%Y', Date) = @y";
                cmd.Parameters.AddWithValue("@m", month.ToString("D2"));
                cmd.Parameters.AddWithValue("@y", year.ToString());
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        decimal income  = Convert.ToDecimal(r["Income"]);
                        decimal expense = Convert.ToDecimal(r["Expense"]);
                        return (income, expense, income - expense);
                    }
                }
            }
            return (0, 0, 0);
        }

        public DataTable GetCategoryBreakdown(string type, int month, int year)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT c.Name, c.Color, SUM(t.Amount) AS Total
                    FROM Transactions t JOIN Categories c ON t.CategoryId = c.Id
                    WHERE t.Type = @type
                      AND strftime('%m', t.Date) = @m
                      AND strftime('%Y', t.Date) = @y
                    GROUP BY c.Id
                    ORDER BY Total DESC";
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@m",    month.ToString("D2"));
                cmd.Parameters.AddWithValue("@y",    year.ToString());
                var adapter = new SQLiteDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        public DataTable GetMonthlyTrend(int months = 6)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT strftime('%Y-%m', Date) AS Month,
                        SUM(CASE WHEN Type='Income'  THEN Amount ELSE 0 END) AS Income,
                        SUM(CASE WHEN Type='Expense' THEN Amount ELSE 0 END) AS Expense
                    FROM Transactions
                    WHERE Date >= date('now', '-' || @months || ' months')
                    GROUP BY strftime('%Y-%m', Date)
                    ORDER BY Month";
                cmd.Parameters.AddWithValue("@months", months);
                var adapter = new SQLiteDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        public string GetDatabasePath()    => _dbPath;
        public string GetConnectionString() => _connectionString;
    }
}
