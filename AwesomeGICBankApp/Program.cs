using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.DependencyInjection;
using System;

public class Program
{
    public static void Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("bankapp");
            })
            .AddScoped<IUserInterface, ConsoleUserInterface>()
            .AddScoped<ITransactionProcessor, TransactionProcessor>()
            .AddScoped<IInterestRuleManager, InterestRuleManager>()
            .AddScoped<IStatementPrinter, StatementPrinter>()
            .AddScoped<IAccountService, AccountService>()
            .AddScoped<IApplication, AwesomeGICBankApplication>()
            .AddScoped<ITransactionRepository, TransactionRepository>()
            .AddScoped<IAccountRepository, AccountRepository>()
            .AddScoped<AwesomeGICBankApplication>()
            .BuildServiceProvider();

        using (var scope = serviceProvider.CreateScope())
        {
            var app = scope.ServiceProvider.GetRequiredService<IApplication>();
            app.Run();
        }
    }
}

public interface IApplication
{
    void Run();
}
public interface IUserInterface
{
    string ReadInput();
    void DisplayMessage(string message);
}

public class ConsoleUserInterface : IUserInterface
{
    public string ReadInput()
    {
        return Console.ReadLine()?.Trim()?.ToUpper();
    }

    public void DisplayMessage(string message)
    {
        Console.WriteLine(message);
    }
}

public interface ITransactionProcessor
{
    void InputTransactions();
}

public class TransactionProcessor : ITransactionProcessor
{
    private readonly IUserInterface userInterface;
    private readonly IAccountService accountService;

    public TransactionProcessor(IUserInterface userInterface, IAccountService accountService)
    {
        this.userInterface = userInterface;
        this.accountService = accountService;
    }

    public void InputTransactions()
    {
        while (true)
        {
            userInterface.DisplayMessage("Please enter transaction details in <Date> <Account> <Type> <Amount> format");
            userInterface.DisplayMessage("(or enter blank to go back to the main menu):");
            userInterface.DisplayMessage("> ");
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
                    userInterface.DisplayMessage("Invalid date format. Please use YYYYMMdd format.");
                    continue; // Continue to the next iteration to allow the user to input again.
                }

                if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
                {
                    userInterface.DisplayMessage("Invalid amount. Please enter a positive number.");
                    continue; // Continue to the next iteration to allow the user to input again.
                }

                // Use the accountService to create and interact with the account
                var account = accountService.CreateAccountIfNotExist(accountNumber);

                var isFirstTransactionWithdraw = accountService.IsFirstWithdrawal(account.AccountNumber, type);
                if (isFirstTransactionWithdraw)
                {
                    userInterface.DisplayMessage("The first transaction on an account cannot be a withdrawal.");
                    continue; // Continue to the next iteration to allow the user to input again.
                }
                if (type == "D")
                {
                    userInterface.DisplayMessage("D");
                    accountService.Deposit(account, dateObj, amount);
                }
                else if (type == "W")
                {
                    userInterface.DisplayMessage("W");
                    accountService.Withdraw(account, dateObj, amount);
                }
                else
                {
                    userInterface.DisplayMessage("Invalid transaction type. Use 'D' for deposit or 'W' for withdrawal.");
                    continue; // Continue to the next iteration to allow the user to input again.
                }

                userInterface.DisplayMessage("Transaction recorded successfully.");
            }
            else
            {
                userInterface.DisplayMessage("Invalid input format. Please use <Date> <Account> <Type> <Amount>.");
            }
        }
    }

}

public interface IInterestRuleManager
{
    void DefineInterestRules();
}

public class InterestRuleManager : IInterestRuleManager
{
    private readonly IUserInterface userInterface;
    private readonly ApplicationDbContext dbContext;

    public InterestRuleManager(ApplicationDbContext dbContext, IUserInterface userInterface)
    {
        this.dbContext = dbContext;
        this.userInterface = userInterface;
    }

    public void DefineInterestRules()
    {
        userInterface.DisplayMessage("Please enter interest rule details in <Date> <RuleId> <Rate in %>");
        userInterface.DisplayMessage("(or enter blank to go back to the main menu):");
        userInterface.DisplayMessage("> ");

        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return;

        var ruleDetails = input.Split(' ');
        if (ruleDetails.Length != 3)
        {
            userInterface.DisplayMessage("Invalid input format. Please enter details in the correct format.");
            return;
        }

        if (!DateTime.TryParseExact(ruleDetails[0], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            userInterface.DisplayMessage("Invalid date format. Please use YYYYMMdd format.");
            return;
        }

        var ruleId = ruleDetails[1];
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            userInterface.DisplayMessage("RuleId cannot be empty.");
            return;
        }

        if (!decimal.TryParse(ruleDetails[2], out var rate) || rate <= 0 || rate >= 100)
        {
            userInterface.DisplayMessage("Invalid interest rate. Rate should be greater than 0 and less than 100.");
            return;
        }

        // Check if there are existing rules on the same day
        var existingRuleOnDate = dbContext.InterestRules
            .Where(r => r.Date == date)
            .OrderByDescending(r => r.Date)
            .FirstOrDefault();

        if (existingRuleOnDate != null)
        {
            userInterface.DisplayMessage("An interest rule already exists for this date. The latest rule will be kept.");
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

        userInterface.DisplayMessage("Interest rules:");
        userInterface.DisplayMessage("| Date     | RuleId | Rate (%) |");
        foreach (var rule in interestRules)
        {
            userInterface.DisplayMessage($"| {rule.Date:yyyyMMdd} | {rule.RuleId} | {rule.Rate:F2} |");
        }
    }
}

// Statement printing
public interface IStatementPrinter
{
    void PrintStatement();
}

public class StatementPrinter : IStatementPrinter
{
    private readonly ApplicationDbContext dbContext;
    private readonly IUserInterface userInterface;

    public StatementPrinter(ApplicationDbContext dbContext, IUserInterface userInterface)
    {
        this.dbContext = dbContext;
        this.userInterface = userInterface;
    }

    public void PrintStatement()
    {
        userInterface.DisplayMessage("Please enter account and month to generate the statement <Account> <Year><Month>");
        userInterface.DisplayMessage("(or enter blank to go back to main menu):");
        userInterface.DisplayMessage("> ");

        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return;

        var statementDetails = input.Split(' ');
        if (statementDetails.Length != 2)
        {
            userInterface.DisplayMessage("Invalid input format. Please enter account and month in the correct format.");
            return;
        }

        var accountNumber = statementDetails[0];
        var yearMonth = statementDetails[1];

        if (!int.TryParse(yearMonth.Substring(0, 4), out var year) ||
            !int.TryParse(yearMonth.Substring(4, 2), out var month))
        {
            userInterface.DisplayMessage("Invalid year and month format. Please use YYYYMM format.");
            return;
        }

        // Calculate and print the account statement
        PrintAccountStatement(new Account(accountNumber), year, month);
        userInterface.DisplayMessage("Transaction recorded successfully.");
    }

    private void PrintAccountStatement(Account account, int year, int month)
    {
        userInterface.DisplayMessage($"Account: {account.AccountNumber}");
        userInterface.DisplayMessage("| Date     | Txn Id      | Type | Amount | Balance |");

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

            userInterface.DisplayMessage($"| {transaction.Date:yyyyMMdd} | {transaction.TransactionId} | {transaction.Type}    | {transaction.Amount:F2}  | {balance:F2}  |");
        }

        // Calculate and apply interest for the month
        var annualInterestRate = GetInterestRateForDate(account.AccountNumber, startDate);
        if (annualInterestRate > 0)
        {
            var numDays = DateTime.DaysInMonth(year, month);
            // Calculate the daily interest rate
            decimal dailyInterestRate = annualInterestRate / 365;

            // Calculate the interest for the specified number of days
            decimal interest = balance * dailyInterestRate * numDays;
            // Display the interest transaction
            userInterface.DisplayMessage($"| {endDate:yyyyMMdd} |             | I    | {interest:F2}  | {balance + interest:F2}  |");
        }
    }

    /*Interest rules:
        | Date     | RuleId | Rate(%) |
        | 20230101 | RULE01 |     1.95 |
        | 20230520 | RULE02 |     1.90 |
        | 20230615 | RULE03 |     2.20 |
    */
    private decimal GetInterestRateForDate(string accountNumber, DateTime date)
    {
        var applicableRules = dbContext.InterestRules
            .Where(r => r.Date <= date)
            .OrderByDescending(r => r.Date)
            .ToList();

        decimal interestRate = 0;

        foreach (var rule in applicableRules)
        {
            // Check if the next rule's start date is after the transaction date
            var nextRule = applicableRules.FirstOrDefault(r => r.Date > rule.Date);
            if (nextRule == null || (nextRule.Date > date))
            {
                interestRate = rule.Rate;
                break; // Use the latest applicable rule
            }
        }

        return interestRate;
    }

}

public class AwesomeGICBankApplication : IApplication
{
    private readonly ApplicationDbContext dbContext;
    private readonly IUserInterface userInterface;
    private readonly ITransactionProcessor transactionProcessor;
    private readonly IInterestRuleManager interestRuleManager;
    private readonly IStatementPrinter iStatementPrinter;

    public AwesomeGICBankApplication(ApplicationDbContext dbContext, IUserInterface userInterface,
        ITransactionProcessor transactionProcessor, IInterestRuleManager interestRuleManager, IStatementPrinter iStatementPrinter)
    {
        this.dbContext = dbContext;
        this.userInterface = userInterface;
        this.transactionProcessor = transactionProcessor;
        this.interestRuleManager = interestRuleManager;
        this.iStatementPrinter = iStatementPrinter;
    }

    public void Run()
    {
        while (true)
        {
            userInterface.DisplayMessage("Welcome to AwesomeGIC Bank! What would you like to do?");
            userInterface.DisplayMessage("[T] Input transactions");
            userInterface.DisplayMessage("[I] Define interest rules");
            userInterface.DisplayMessage("[P] Print statement");
            userInterface.DisplayMessage("[Q] Quit");
            userInterface.DisplayMessage("> ");

            var input = userInterface.ReadInput();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            switch (input)
            {
                case "T":
                    this.transactionProcessor.InputTransactions();
                    break;
                case "I":
                    this.interestRuleManager.DefineInterestRules();
                    break;
                case "P":
                    iStatementPrinter.PrintStatement();
                    break;
                case "Q":
                    Quit();
                    return;
                default:
                    userInterface.DisplayMessage("Invalid input. Please try again.");
                    break;
            }
        }
    }

    public void Quit()
    {
        userInterface.DisplayMessage("Thank you for banking with AwesomeGIC Bank.");
        userInterface.DisplayMessage("Have a nice day!");
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

    public Account(string accountNumber)
    {
        AccountNumber = accountNumber;
    }
}

public interface ITransactionRepository
{
    void AddTransaction(Transaction transaction);
    decimal CalculateBalance(string accountNumber);
    bool IsFirstWithdrawal(string accountNumber, string transactionType);
    string GenerateTransactionId(string accountNumber, DateTime date);
}

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext context;

    public TransactionRepository(ApplicationDbContext dbContext)
    {
        context = dbContext;
    }

    public void AddTransaction(Transaction transaction)
    {
        context.Transactions.Add(transaction);
        context.SaveChanges();
    }

    public decimal CalculateBalance(string accountNumber)
    {
        var deposits = context.Transactions
            .Where(t => t.AccountNumber == accountNumber && t.Type == "D")
            .Sum(t => t.Amount);

        var withdrawals = context.Transactions
            .Where(t => t.AccountNumber == accountNumber && t.Type == "W")
            .Sum(t => t.Amount);

        return deposits - withdrawals;
    }

    public bool IsFirstWithdrawal(string accountNumber, string transactionType)
    {
        var transactionCount = context.Transactions
            .Where(t => t.AccountNumber == accountNumber)
            .Count();

        return transactionCount == 0 && transactionType == "W";
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

public interface IAccountRepository
{
    Account AddAccount(string accountNumber);
    Account RetriveAccount(string accountNumber);
}

public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext context;

    public AccountRepository(ApplicationDbContext dbContext)
    {
        context = dbContext;
    }

    public Account AddAccount(string accountNumber)
    {
        var account = new Account(accountNumber);
        context.Accounts.Add(account);
        context.SaveChanges();
        return account;
    }

    public Account? RetriveAccount(string accountNumber)
    {
        var account = context.Accounts
            .Where(t => t.AccountNumber == accountNumber).AsEnumerable().FirstOrDefault();
        if (account != null && account.AccountId > 0)
        {
            return account;
        }
        else
        {
            return null;
        }
    }
}

public interface IAccountService
{
    Account CreateAccountIfNotExist(string accountNumber);
    void Deposit(Account account, DateTime date, decimal amount);
    void Withdraw(Account account, DateTime date, decimal amount);
    bool IsFirstWithdrawal(string accountNumber, string transactionType);
}


public class AccountService : IAccountService
{
    private readonly ITransactionRepository transactionRepository;
    private readonly IAccountRepository accountRepository;

    public AccountService(ITransactionRepository transactionRepository, IAccountRepository accountRepository)
    {
        this.transactionRepository = transactionRepository;
        this.accountRepository = accountRepository;
    }

    public Account CreateAccountIfNotExist(string accountNumber)
    {
        // Check and create the account logic here
        var account = this.accountRepository.RetriveAccount(accountNumber);

        if (account == null || account.AccountId > 0)
        {
            account = this.accountRepository.AddAccount(accountNumber);
        }
        return account;
    }

    public void Deposit(Account account, DateTime date, decimal amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("Invalid deposit amount.");
            return;
        }

        try
        {
            var transactionId = this.transactionRepository.GenerateTransactionId(account.AccountNumber, date);

            var transactionObj = new Transaction
            {
                TransactionId = transactionId,
                Date = date,
                AccountNumber = account.AccountNumber,
                Type = "D",
                Amount = amount
            };
            this.transactionRepository.AddTransaction(transactionObj);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.InnerException?.Message);
        }
    }

    public void Withdraw(Account account, DateTime date, decimal amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("Invalid withdrawal amount.");
            return;
        }

        var balance = this.transactionRepository.CalculateBalance(account.AccountNumber);
        if (balance < amount)
        {
            Console.WriteLine("Insufficient balance.");
            return;
        }

        var transactionId = this.transactionRepository.GenerateTransactionId(account.AccountNumber, date);
        var transaction = new Transaction
        {
            TransactionId = transactionId,
            Date = date,
            AccountNumber = account.AccountNumber,
            Type = "W",
            Amount = amount
        };
        this.transactionRepository.AddTransaction(transaction);

    }

    public bool IsFirstWithdrawal(string accountNumber, string transactionType)
    {
        var transactionCount = transactionRepository.IsFirstWithdrawal(accountNumber, transactionType);
        return transactionCount;
    }
}
