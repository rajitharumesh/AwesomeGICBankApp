using Microsoft.EntityFrameworkCore;
using System.Globalization;

public class Program
{
    public static void Main(string[] args)
    {
        using (var context = new ApplicationDbContext())
        {
            context.Database.EnsureCreated();
        }

        var bankingApp = new AwesomeGICBankApplication();
        bankingApp.Run();
    }
}

public class AwesomeGICBankApplication
{
    public void Run()
    {
        while (true)
        {
            Console.WriteLine("Welcome to AwesomeGIC Bank! What would you like to do?");
            Console.WriteLine("[T] Input transactions");
            Console.WriteLine("[I] Define interest rules");
            Console.WriteLine("[P] Print statement");
            Console.WriteLine("[Q] Quit");
            Console.Write("> ");

            var input = Console.ReadLine()?.Trim()?.ToUpper();
            Console.WriteLine(input);

            if (string.IsNullOrWhiteSpace(input))
                continue;

            switch (input)
            {
                case "T":
                    InputTransactions();
                    break;
                case "I":
                    DefineInterestRules();
                    break;
                case "P":
                    PrintStatement();
                    break;
                case "Q":
                    Quit();
                    return;
                default:
                    Console.WriteLine("Invalid input. Please try again.");
                    break;
            }
        }
    }

    public void InputTransactions()
    {
        Console.WriteLine("Please enter transaction details in <Date> <Account> <Type> <Amount> format");
        Console.WriteLine("(or enter blank to go back to the main menu):");
        Console.Write("> ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            return;

        var parts = input.Split(' ');
        if (parts.Length == 4)
        {
            var date = parts[0];
            var accountNumber = parts[1];
            var type = parts[2].ToUpper();
            var amountStr = parts[3];

            if (!DateTime.TryParseExact(parts[0], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime dateObj))
            {
                Console.WriteLine("Invalid date format. Please use YYYYMMdd format.");
                return;
            }

            if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
            {
                Console.WriteLine("Invalid amount. Please enter a positive number.");
                return;
            }

            var account = Account.GetAccount(accountNumber);

            Console.WriteLine("account ", account);
            if (type == "D")
            {
                Console.WriteLine("D");
                account.Deposit(dateObj, amount);
            }
            else if (type == "W")
            {
                Console.WriteLine("W");
                // withdraw from account
            }
            else
            {
                Console.WriteLine("Invalid transaction type. Use 'D' for deposit or 'W' for withdrawal.");
                return;
            }

            Console.WriteLine("Transaction recorded successfully.");

        }
        else
        {
            Console.WriteLine("Invalid input format. Please use <Date> <Account> <Type> <Amount>.");
        }
    }

    public void DefineInterestRules()
    {
        Console.WriteLine("DefineInterestRules");
    }

    public void PrintStatement()
    {
        Console.WriteLine("PrintStatement");
    }

    public void Quit()
    {
        Console.WriteLine("Thank you for banking with AwesomeGIC Bank.");
        Console.WriteLine("Have a nice day!");
    }
}

public class Transaction
{
    public int TransactionId { get; set; }
    public DateTime Date { get; set; }
    public string Account { get; set; }
    public string Type { get; set; }
    public decimal Amount { get; set; }
}

public class InterestRule
{
    public int InterestRuleId { get; set; }
    public DateTime Date { get; set; }
    public string RuleId { get; set; }
    public decimal Rate { get; set; }
}

public class ApplicationDbContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<InterestRule> InterestRules { get; set; }
    public DbSet<Account> Accounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=testDb.db");
    }

}

public class Account
{
    public int AccountId { get; set; }
    public string AccountNumber { get; set; }
    private readonly ApplicationDbContext context;

    protected Account()
    {
        context = new ApplicationDbContext();
    }
    public Account(string accountNumber)
    {
        AccountNumber = accountNumber;
        context = new ApplicationDbContext();
        context.Database.EnsureCreated();
    }

    public static Account GetAccount(string accountNumber)
    {
        using var context = new ApplicationDbContext();
        var existingAccount = context.Accounts.AsEnumerable().FirstOrDefault(a => a.AccountNumber == accountNumber);

        if (existingAccount == null)
        {
            existingAccount = new Account(accountNumber);
            context.Accounts.Add(existingAccount);
            context.SaveChanges();
        }

        return existingAccount;
    }

    public void Deposit(DateTime date, decimal amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("Invalid deposit amount.");
            return;
        }

        var transaction = new Transaction
        {
            Date = date,
            Account = AccountNumber,
            Type = "D",
            Amount = amount
        };

        context.Transactions.Add(transaction);
        context.SaveChanges();
        Console.WriteLine("result : ");
        Console.WriteLine(context.Accounts.AsEnumerable().ToList()?.Count);
    }

}