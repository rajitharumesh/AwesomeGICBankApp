using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Xunit;
public class AwesomeGICBankApplicationTests : IDisposable
{
    private readonly StringWriter consoleOutput;
    private readonly TextWriter originalConsoleOutput;

    public AwesomeGICBankApplicationTests()
    {
        // Redirect console output to capture it for testing
        consoleOutput = new StringWriter();
        originalConsoleOutput = Console.Out;
        Console.SetOut(consoleOutput);
    }

    public void Dispose()
    {
        // Restore the original console output
        Console.SetOut(originalConsoleOutput);
        consoleOutput.Dispose();
    }

    [Fact]
    public void InputTransactions_ValidInput_Deposit()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Act
        using (var reader = new StringReader("20230101 12345 D 100\n"))
        {
            Console.SetIn(reader);
            app.InputTransactions();
        }

        // Assert
        var transactions = dbContext.Transactions.ToList();
        Assert.Single(transactions);
        Assert.Equal("D", transactions[0].Type);
        Assert.Equal(100.0m, transactions[0].Amount);
    }

    [Fact]
    public void DefineInterestRules_ValidInput_AddNewRule()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Act
        using (var reader = new StringReader("20230101 RULE01 5\n"))
        {
            Console.SetIn(reader);
            app.DefineInterestRules();
        }

        // Assert
        var interestRules = dbContext.InterestRules.ToList();
        Assert.Single(interestRules);
        Assert.Equal("RULE01", interestRules[0].RuleId);
        Assert.Equal(5.0m, interestRules[0].Rate);
    }

    [Fact]
    public void InputTransactions_InvalidDate_Format()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Act
        using (var reader = new StringReader("2023-01-01 12345 D 100\n"))
        {
            Console.SetIn(reader);
            app.InputTransactions();
        }

        // Assert
        Assert.Empty(dbContext.Transactions.ToList());
        Assert.Contains("Invalid date format", consoleOutput.ToString());
    }

    [Fact]
    public void InputTransactions_InvalidAmount_Negative()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Act
        using (var reader = new StringReader("20230101 12345 D -100\n"))
        {
            Console.SetIn(reader);
            app.InputTransactions();
        }

        // Assert
        Assert.Empty(dbContext.Transactions.ToList());
        Assert.Contains("Invalid amount. Please enter a positive number.", consoleOutput.ToString());
    }

    [Fact]
    public void InputTransactions_InvalidTransactionType()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Act
        using (var reader = new StringReader("20230101 12345 X 100\n"))
        {
            Console.SetIn(reader);
            app.InputTransactions();
        }

        // Assert
        Assert.Empty(dbContext.Transactions.ToList());
        Assert.Contains("Invalid transaction type. Use 'D' for deposit or 'W' for withdrawal.", consoleOutput.ToString());
    }

    [Fact]
    public void DefineInterestRules_DuplicateDate_UpdateExistingRule()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Add an initial rule
        dbContext.InterestRules.Add(new InterestRule { Date = new DateTime(2023, 01, 01), RuleId = "RULE01", Rate = 3 });
        dbContext.SaveChanges();

        // Act
        using (var reader = new StringReader("20230101 RULE02 4\n"))
        {
            Console.SetIn(reader);
            app.DefineInterestRules();
        }

        // Assert
        var interestRules = dbContext.InterestRules.ToList();
        Assert.Single(interestRules);
        Assert.Equal("RULE02", interestRules[0].RuleId);
        Assert.Equal(4.0m, interestRules[0].Rate);
        Assert.Contains("An interest rule already exists for this date. The latest rule will be kept.", consoleOutput.ToString());
    }

    [Fact]
    public void PrintStatement_ValidInput_PrintsStatement()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        var transaction1 = new Transaction
        {
            TransactionId = "2009776b-5ccd-4fcc-9100-97fa611099eb",
            Date = new DateTime(2023, 01, 01),
            AccountNumber = "12345",
            Type = "D",
            Amount = 100
        };

        var transaction2 = new Transaction
        {
            TransactionId = "7f205fca-16b4-4815-b324-4f74e8ec1cbc",
            Date = new DateTime(2023, 01, 02),
            AccountNumber = "12345",
            Type = "W",
            Amount = 50
        };


        dbContext.Transactions.Add(transaction1);
        dbContext.Transactions.Add(transaction2);
        dbContext.SaveChanges();

        // Act
        using (var reader = new StringReader("12345 202301\n"))
        {
            Console.SetIn(reader);
            app.PrintStatement();
        }

        // Assert
        Assert.Equal(2, dbContext.Transactions?.Count());
    }

    [Fact]
    public void DefineInterestRules_ValidInput_CreatesInterestRule()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Act
        using (var reader = new StringReader("20230101 RULE04 3.5\n"))
        {
            Console.SetIn(reader);
            app.DefineInterestRules();
        }

        // Assert
        var interestRule = dbContext.InterestRules.FirstOrDefault(rule => rule.RuleId == "RULE04");
        Assert.NotNull(interestRule);
        Assert.Equal(new DateTime(2023, 01, 01), interestRule.Date);
        Assert.Equal("RULE04", interestRule.RuleId);
        Assert.Equal(3.5m, interestRule.Rate);
    }

    [Fact]
    public void PrintStatement_InvalidInput_ShowsErrorMessage()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Act
        using (var reader = new StringReader("InvalidInput\n"))
        {
            Console.SetIn(reader);
            app.PrintStatement();
        }

        // Assert
        Assert.Contains("Invalid input format.", consoleOutput.ToString());
    }

    [Fact]
    public void InputTransactions_InvalidDate_ShowsErrorMessage()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Act
        using (var reader = new StringReader("20230132 12345 D 100\n"))
        {
            Console.SetIn(reader);
            app.InputTransactions();
        }

        // Assert
        Assert.Contains("Invalid date format. Please use YYYYMMdd format.", consoleOutput.ToString());
    }

    [Fact]
    public void InputTransactions_InvalidAmount_ShowsErrorMessage()
    {
        // Arrange
        var dbContext = CreateTestDbContext();
        var app = new AwesomeGICBankApplication(dbContext);

        // Act
        using (var reader = new StringReader("20230101 12345 D -50\n"))
        {
            Console.SetIn(reader);
            app.InputTransactions();
        }

        // Assert
        Assert.Contains("Invalid amount. Please enter a positive number.", consoleOutput.ToString());
    }

    private ApplicationDbContext CreateTestDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
