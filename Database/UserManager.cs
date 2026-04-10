using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace FinanceTracker.Database
{
    /// <summary>
    /// Handles user authentication — stored in the same SQLite DB.
    /// Passwords are SHA-256 hashed with a per-user salt.
    /// </summary>
    public class UserManager
    {
        private static UserManager _instance;
        public static UserManager Instance => _instance ?? (_instance = new UserManager());

        // Currently logged-in user (set after successful login)
        public int    CurrentUserId       { get; private set; }
        public string CurrentUserName     { get; private set; }
        public string CurrentUserEmail    { get; private set; }
        public string CurrentUserInitials { get; private set; }
        public bool   IsLoggedIn          => CurrentUserId > 0;

        private UserManager()
        {
            EnsureUsersTable();
        }

        private SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(DatabaseManager.Instance.GetConnectionString());
            conn.Open();
            return conn;
        }

        private void EnsureUsersTable()
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                        FullName    TEXT NOT NULL,
                        Email       TEXT NOT NULL UNIQUE COLLATE NOCASE,
                        PasswordHash TEXT NOT NULL,
                        Salt        TEXT NOT NULL,
                        CreatedAt   TEXT NOT NULL DEFAULT (datetime('now'))
                    );";
                cmd.ExecuteNonQuery();
            }
        }

        // ── Register ─────────────────────────────────────────────────────────────
        public RegisterResult Register(string fullName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName))  return RegisterResult.EmptyName;
            if (string.IsNullOrWhiteSpace(email))     return RegisterResult.EmptyEmail;
            if (!email.Contains("@"))                 return RegisterResult.InvalidEmail;
            if (string.IsNullOrWhiteSpace(password))  return RegisterResult.EmptyPassword;
            if (password.Length < 6)                  return RegisterResult.WeakPassword;

            using (var conn = GetConnection())
            {
                // Check if email exists
                var check = conn.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM Users WHERE Email=@e";
                check.Parameters.AddWithValue("@e", email.Trim());
                long count = (long)check.ExecuteScalar();
                if (count > 0) return RegisterResult.EmailTaken;

                string salt = GenerateSalt();
                string hash = HashPassword(password, salt);

                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Users (FullName,Email,PasswordHash,Salt) VALUES(@n,@e,@h,@s)";
                cmd.Parameters.AddWithValue("@n", fullName.Trim());
                cmd.Parameters.AddWithValue("@e", email.Trim().ToLower());
                cmd.Parameters.AddWithValue("@h", hash);
                cmd.Parameters.AddWithValue("@s", salt);
                cmd.ExecuteNonQuery();
            }
            return RegisterResult.Success;
        }

        // ── Login ────────────────────────────────────────────────────────────────
        public LoginResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))    return LoginResult.EmptyEmail;
            if (string.IsNullOrWhiteSpace(password)) return LoginResult.EmptyPassword;

            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id,FullName,Email,PasswordHash,Salt FROM Users WHERE Email=@e";
                cmd.Parameters.AddWithValue("@e", email.Trim().ToLower());
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return LoginResult.NotFound;

                    string salt      = r["Salt"].ToString();
                    string stored    = r["PasswordHash"].ToString();
                    string attempted = HashPassword(password, salt);

                    if (attempted != stored) return LoginResult.WrongPassword;

                    // Set session
                    CurrentUserId    = r.GetInt32(r.GetOrdinal("Id"));
                    CurrentUserName  = r["FullName"].ToString();
                    CurrentUserEmail = r["Email"].ToString();
                    string[] parts   = CurrentUserName.Split(' ');
                    CurrentUserInitials = parts.Length >= 2
                        ? $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper()
                        : CurrentUserName.Substring(0, Math.Min(2, CurrentUserName.Length)).ToUpper();
                    return LoginResult.Success;
                }
            }
        }

        public void Logout()
        {
            CurrentUserId    = 0;
            CurrentUserName  = null;
            CurrentUserEmail = null;
            CurrentUserInitials = null;
        }

        // ── Crypto ───────────────────────────────────────────────────────────────
        private static string GenerateSalt()
        {
            byte[] bytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string HashPassword(string password, string salt)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hash  = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }

    public enum RegisterResult { Success, EmptyName, EmptyEmail, InvalidEmail, EmptyPassword, WeakPassword, EmailTaken }
    public enum LoginResult    { Success, EmptyEmail, EmptyPassword, NotFound, WrongPassword }
}
