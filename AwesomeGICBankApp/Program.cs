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
            var app = new AwesomeGICBankApplication(dbContext);
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
                account.CreateAccountIfNotExist(accountNumber);

                var isFirstTransactionWithdraw = account.CheckIsFirstTransaction(accountNumber, type);
                if (isFirstTransactionWithdraw)
                {
                    Console.WriteLine("The first transaction on an account cannot be a withdrawal.");
                    return;
                }
                if (type == "D")
                {
                    Console.WriteLine("D");
                    account.Deposit(dateObj, amount);
                }
                else if (type == "W")
                {
                    Console.WriteLine("W");
                    account.Withdraw(dateObj, amount);
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
        Console.WriteLine("Please enter interest rule details in <Date> <RuleId> <Rate in %>");
        Console.WriteLine("(or enter blank to go back to the main menu):");
        Console.Write("> ");

        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return;

        var ruleDetails = input.Split(' ');
        if (ruleDetails.Length != 3)
        {
            Console.WriteLine("Invalid input format. Please enter details in the correct format.");
            return;
        }

        if (!DateTime.TryParseExact(ruleDetails[0], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            Console.WriteLine("Invalid date format. Please use YYYYMMdd format.");
            return;
        }

        var ruleId = ruleDetails[1];
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            Console.WriteLine("RuleId cannot be empty.");
            return;
        }

        if (!decimal.TryParse(ruleDetails[2], out var rate) || rate <= 0 || rate >= 100)
        {
            Console.WriteLine("Invalid interest rate. Rate should be greater than 0 and less than 100.");
            return;
        }

        // Check if there are existing rules on the same day
        var existingRuleOnDate = dbContext.InterestRules
            .Where(r => r.Date == date)
            .OrderByDescending(r => r.Date)
            .FirstOrDefault();

        if (existingRuleOnDate != null)
        {
            Console.WriteLine("An interest rule already exists for this date. The latest rule will be kept.");
            existingRuleOnDate.RuleId = ruleId;
            existingRuleOnDate.Rate = rate;
        }
        else
        {
            // Create and save the interest rule
            var interestRule = new InterestRule
            {
                Date = date,
                RuleId = ruleId,
                Rate = rate
            };

            dbContext.InterestRules.Add(interestRule);
        }

        dbContext.SaveChanges();

        // Display all interest rules ordered by date
        var interestRules = dbContext.InterestRules.OrderBy(r => r.Date).ToList();

        Console.WriteLine("Interest rules:");
        Console.WriteLine("| Date     | RuleId | Rate (%) |");
        foreach (var rule in interestRules)
        {
            Console.WriteLine($"| {rule.Date:yyyyMMdd} | {rule.RuleId} | {rule.Rate:F2} |");
        }
    }

    public void PrintStatement()
    {
        Console.WriteLine("Please enter account and month to generate the statement <Account> <Year><Month>");
        Console.WriteLine("(or enter blank to go back to main menu):");
        Console.Write("> ");

        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return;

        var statementDetails = input.Split(' ');
        if (statementDetails.Length != 2)
        {
            Console.WriteLine("Invalid input format. Please enter account and month in the correct format.");
            return;
        }

        var accountNumber = statementDetails[0];
        var yearMonth = statementDetails[1];

        if (!int.TryParse(yearMonth.Substring(0, 4), out var year) ||
            !int.TryParse(yearMonth.Substring(4, 2), out var month))
        {
            Console.WriteLine("Invalid year and month format. Please use YYYYMM format.");
            return;
        }

        // Retrieve the account or create it if it doesn't exist
        var account = new Account(accountNumber, dbContext);
        account.CreateAccountIfNotExist(accountNumber);

        if (account == null)
        {
            Console.WriteLine("Account not found or creation failed.");
            return;
        }

        // Calculate and print the account statement
        PrintAccountStatement(account, year, month);
    }

    private void PrintAccountStatement(Account account, int year, int month)
    {
        Console.WriteLine($"Account: {account.AccountNumber}");
        Console.WriteLine("| Date     | Txn Id      | Type | Amount | Balance |");

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var transactions = dbContext.Transactions
            .Where(t => t.AccountNumber == account.AccountNumber && t.Date >= startDate && t.Date <= endDate)
            .OrderBy(t => t.Date)
            .ToList();

        decimal balance = 0;

        foreach (var transaction in transactions)
        {
            if (transaction.Type == "D")
            {
                balance += transaction.Amount;
            }
            else if (transaction.Type == "W")
            {
                balance -= transaction.Amount;
            }

            Console.WriteLine($"| {transaction.Date:yyyyMMdd} | {transaction.TransactionId} | {transaction.Type}    | {transaction.Amount:F2}  | {balance:F2}  |");
        }

        // Calculate and apply interest for the month
        var interestRate = GetInterestRateForDate(account.AccountNumber, startDate);
        if (interestRate > 0)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var interestAmount = (balance * (interestRate / 100)) * (daysInMonth / 365);

            // Display the interest transaction
            Console.WriteLine($"| {endDate:yyyyMMdd} |             | I    | {interestAmount:F2}  | {balance + interestAmount:F2}  |");
        }
    }

    private decimal GetInterestRateForDate(string accountNumber, DateTime date)
    {
        return dbContext.InterestRules
            .Where(r => r.Date <= date && r.RuleId == "RULE03")
            .OrderByDescending(r => r.Date)
            .Select(r => r.Rate)
            .FirstOrDefault();
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

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

    public Account CreateAccountIfNotExist(string accountNumber)
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

    public decimal CalculateBalance()
    {
        var deposits = context.Transactions
            .Where(t => t.AccountNumber == AccountNumber && t.Type == "D")
            .Sum(t => t.Amount);

        var withdrawals = context.Transactions
            .Where(t => t.AccountNumber == AccountNumber && t.Type == "W")
            .Sum(t => t.Amount);

        return deposits - withdrawals;
    }

    public void Withdraw(DateTime date, decimal amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("Invalid withdrawal amount.");
            return;
        }

        var balance = CalculateBalance();
        if (balance < amount)
        {
            Console.WriteLine("Insufficient balance.");
            return;
        }

        var transactionId = GenerateTransactionId(AccountNumber, date);
        var transaction = new Transaction
        {
            TransactionId = transactionId,
            Date = date,
            AccountNumber = AccountNumber,
            Type = "W",
            Amount = amount
        };

        context.Transactions.Add(transaction);
        context.SaveChanges();


        Console.WriteLine("result : ");

        var test = context.Transactions.AsEnumerable().ToList();
        for (int i = 0; i < test.Count; i++)
        {
            Console.WriteLine(test[i].Amount);
        }
    }

    public bool CheckIsFirstTransaction(string accountNumber, string transactionType)
    {
        // Check if this is the first transaction for the account and it's a withdrawal
        var transactionsount = context.Transactions
                .Where(t => t.AccountNumber == accountNumber).AsEnumerable().ToList().Count;
        if (transactionsount == 0 && transactionType == "W")
        {
            return true;
        }
        return false;
    }


}
