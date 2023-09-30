using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public static void Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("bankapp");
            })
            .AddScoped<AwesomeGICBankApplication>()
            .BuildServiceProvider();

        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var app = new AwesomeGICBankApplication(dbContext); // Pass the context
            app.Run();
        }
    }
}

public class AwesomeGICBankApplication
{
    private readonly ApplicationDbContext dbContext;

    public AwesomeGICBankApplication(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
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
        while (true)
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

                if (!DateTime.TryParseExact(date, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime dateObj))
                {
                    Console.WriteLine("Invalid date format. Please use YYYYMMdd format.");
                    return;
                }

                if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
                {
                    Console.WriteLine("Invalid amount. Please enter a positive number.");
                    return;
                }

                var account = new Account(accountNumber, dbContext);

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
    public int Id { get; set; }
    public string TransactionId { get; set; }

    [Required]
    [DataType(DataType.DateTime)] // Specify the data type as DateTime
    public DateTime Date { get; set; }

    public string AccountNumber { get; set; }
    public string Type { get; set; }


    [Column(TypeName = "decimal(18,2)")] // Specify the data type as decimal(18,2)
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
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseInMemoryDatabase("bankapp");
        }
    }
}

public class Account
{
    public int AccountId { get; set; }
    public string AccountNumber { get; set; }
    private readonly ApplicationDbContext context;

    public Account(string accountNumber, ApplicationDbContext dbContext)
    {
        AccountNumber = accountNumber;
        context = dbContext;
    }

    public Account GetAccount(string accountNumber)
    {
        var existingAccount = context.Accounts.AsEnumerable().FirstOrDefault(a => a.AccountNumber == accountNumber);

        if (existingAccount == null)
        {
            existingAccount = new Account(accountNumber, context);
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

        try
        {
            var transactionId = GenerateTransactionId(AccountNumber, date);

            var transactionObj = new Transaction
            {
                TransactionId = transactionId,
                Date = date,
                AccountNumber = AccountNumber,
                Type = "D",
                Amount = amount
            };

            context.Transactions.Add(transactionObj);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.InnerException?.Message);
        }
        Console.WriteLine("result : ");
        Console.WriteLine(context.Transactions.AsEnumerable().ToList()?.Count);
    }


    public string GenerateTransactionId(string accountNumber, DateTime date)
    {
        int transactionCount = 0;
        try
        {
            var transactions = context.Transactions
                .Where(t => t.AccountNumber == accountNumber)
                .AsEnumerable() // Switch to in-memory LINQ
                .Where(t => t.Date.Date == date.Date)
                .ToList();
            transactionCount = transactions.Count;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.InnerException?.Message);
        }

        // Format the date as YYYYMMdd
        string formattedDate = date.ToString("yyyyMMdd");

        // Generate a unique transaction ID by appending a running number
        string transactionId = $"{formattedDate}-{(transactionCount + 1):D2}";

        return transactionId;
    }

}
