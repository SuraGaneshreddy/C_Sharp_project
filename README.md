# 💰 Personal Finance & Expense Tracker
### A Full-Featured Desktop App — C# .NET Framework 4.8 + SQLite

---

## 📁 Project Structure

```
FinanceTracker/
├── FinanceTracker.csproj          ← Project file (Visual Studio)
├── packages.config                ← NuGet packages (SQLite)
├── Program.cs                     ← Entry point
│
├── Database/
│   └── DatabaseManager.cs         ← All SQLite DB logic (singleton)
│
├── Models/
│   ├── Transaction.cs             ← Transaction model
│   ├── Category.cs                ← Category model
│   ├── Budget.cs                  ← Budget model (with % calculations)
│   └── Account.cs                 ← Account / wallet model
│
└── Forms/
    ├── MainForm.cs                ← Dashboard + navigation shell
    ├── AddTransactionForm.cs      ← Add / Edit transaction dialog
    ├── ManageCategoriesForm.cs    ← Categories CRUD
    ├── ManageBudgetsForm.cs       ← Budget setting + progress bars
    ├── ManageAccountsForm.cs      ← Account management cards
    └── ReportsForm.cs             ← Donut chart, bar chart, trend line, summary table
```

---

## 🚀 How to Open & Run

### Prerequisites
- **Visual Studio 2019 / 2022** (Community edition is free)
- **.NET Framework 4.8** (already installed on Windows 10/11)
- **NuGet** (built into Visual Studio)

### Steps

1. **Open the solution** — Open `FinanceTracker.csproj` in Visual Studio.

2. **Restore NuGet packages** — Right-click the solution → **Restore NuGet Packages**.  
   This downloads `System.Data.SQLite` automatically.  
   *(Or: Tools → NuGet Package Manager → Package Manager Console → `Update-Package -reinstall`)*

3. **Build** — Press `Ctrl+Shift+B` or Build → Build Solution.

4. **Run** — Press `F5` (debug) or `Ctrl+F5` (without debugger).

> **First launch**: The app auto-creates the SQLite database at:
> `C:\Users\<YourName>\AppData\Roaming\FinanceTracker\finance.db`
> and seeds 15 default categories and 3 demo accounts.

---

## ✨ Features

| Module | What you can do |
|---|---|
| **Dashboard** | Monthly income/expense/savings cards, recent transactions, budget progress |
| **Transactions** | Add, edit, delete; filter by month/year/type; export to CSV |
| **Budgets** | Set monthly spend limits per category; animated progress bars with over-budget alerts |
| **Accounts** | Multiple wallets (Checking, Savings, Cash, Credit); auto-balance updates on every transaction |
| **Categories** | Custom income/expense categories with emoji icon and colour picker |
| **Reports** | Donut chart (expense breakdown), income chart, 6-month trend line, savings-rate table |

---

## 🗄️ Database Schema (SQLite)

```sql
Accounts    (Id, Name, Type, Balance, Currency, Color, IsActive)
Categories  (Id, Name, Type, Color, Icon, IsActive)
Transactions(Id, AccountId→, CategoryId→, Amount, Type, Description, Date, Notes)
Budgets     (Id, CategoryId→, LimitAmount, Month, Year)   -- UNIQUE per category/month
```

- Foreign keys enforced via `PRAGMA foreign_keys = ON`
- Transactions atomically update Account balance inside a DB transaction
- Budget spent amounts are computed via a correlated sub-query (no stored redundancy)

---

## 🎨 UI Design

Dark theme built entirely with **GDI+ / WinForms** — no third-party UI library:
- Sidebar navigation with active highlight
- Summary KPI cards with coloured accent bars
- Donut chart, line trend chart drawn with `Graphics.FillPie` / `DrawLines`
- Budget progress bars drawn with `Panel.Paint`
- `DataGridView` with custom owner-draw column colouring (green income / red expense)

---

## 📦 Dependencies

| Package | Version | Purpose |
|---|---|---|
| System.Data.SQLite.Core | 1.0.117.0 | Embedded SQLite database driver |

All other code uses the standard .NET 4.8 BCL (System.Windows.Forms, System.Drawing, System.Data).

---

## 💡 Extending the App

- **Recurring transactions**: Add a `IsRecurring` flag + `RecurFrequency` to Transactions table.
- **Currency conversion**: Store exchange rates in a `Currencies` table and convert on display.
- **Cloud sync**: Replace `DatabaseManager` with an API client; keep the same model/form layer.
- **Charts library**: Drop in `LiveCharts` or `OxyPlot` for interactive charts without changing the data layer.

---

*Built with ❤️ using C# .NET Framework 4.8 + SQLite*
