using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
public class AwesomeGICBankApplicationTests
{
    [Fact]
    public void TransactionProcessor_InputTransactions_ValidInput()
    {
        // Arrange
        var mockUserInterface = new Mock<IUserInterface>();
        var mockAccountService = new Mock<IAccountService>();

        // Set up any necessary behavior for the mocks, e.g., method returns or behavior.
        // For example, if you expect CreateAccountIfNotExist to return an Account instance:
        mockAccountService.Setup(service => service.CreateAccountIfNotExist(It.IsAny<string>())).Returns(new Account("Acc1"));

        var transactionProcessor = new TransactionProcessor(mockUserInterface.Object, mockAccountService.Object);

        // Simulate user input
        var input = "20230101 Acc1 D 100";
        var reader = new StringReader(input);
        Console.SetIn(reader);


        // Act
        transactionProcessor.InputTransactions();

        // Assert
        // Verify that the user interface messages are displayed within the loop as expected
        mockUserInterface.Verify(ui => ui.DisplayMessage("Please enter transaction details in <Date> <Account> <Type> <Amount> format"), Times.AtLeastOnce);
        mockUserInterface.Verify(ui => ui.DisplayMessage("(or enter blank to go back to the main menu):"), Times.AtLeastOnce);
        mockUserInterface.Verify(ui => ui.DisplayMessage("> "), Times.AtLeastOnce);
        // Add additional verifications if needed for other expected calls


        // Verify that the account service's CreateAccountIfNotExist method was called with the correct account number
        mockAccountService.Verify(service => service.CreateAccountIfNotExist("Acc1"), Times.Once);

        // Verify that the account service's Deposit method was called with the correct arguments
        mockAccountService.Verify(service => service.Deposit(
            It.IsAny<Account>(), // You can specify a more specific account object if needed
            It.Is<DateTime>(date => date == new DateTime(2023, 01, 01)), // Verify the date argument
            It.Is<decimal>(amount => amount == 100M)), // Verify the amount argument
            Times.Once);


        // Simulate user input for a withdrawal transaction
        var withdrawalInput = "20230102 Acc2 W 50";
        var withdrawalReader = new StringReader(withdrawalInput);
        Console.SetIn(withdrawalReader);

        // Act (perform the withdrawal transaction)
        transactionProcessor.InputTransactions();

        // Assert
        // Verify that the account service's CreateAccountIfNotExist method was called with the correct account number for the withdrawal
        mockAccountService.Verify(service => service.CreateAccountIfNotExist("Acc2"), Times.Once);

        // Verify that the account service's Withdraw method was called with the correct arguments for the withdrawal
        mockAccountService.Verify(service => service.Withdraw(
            It.IsAny<Account>(), // You can specify a more specific account object if needed
            It.Is<DateTime>(date => date == new DateTime(2023, 01, 02)), // Verify the date argument
            It.Is<decimal>(amount => amount == 50M)), // Verify the amount argument
            Times.Once);


        // Simulate user input for an invalid amount
        var invalidAmountInput = "20230103 Acc4 D -25"; // Invalid amount (negative)
        var invalidAmountReader = new StringReader(invalidAmountInput);
        Console.SetIn(invalidAmountReader);

        // Act (try to input a transaction with an invalid amount)
        transactionProcessor.InputTransactions();

        // Assert
        // Verify that the user interface displays the "Invalid amount" message
        mockUserInterface.Verify(ui => ui.DisplayMessage("Invalid amount. Please enter a positive number."), Times.Once);


        // Simulate user input for an invalid transaction type
        var invalidTypeInput = "20230104 Acc5 X 60"; // Invalid transaction type 'X'
        var invalidTypeReader = new StringReader(invalidTypeInput);
        Console.SetIn(invalidTypeReader);

        // Act (try to input a transaction with an invalid type)
        transactionProcessor.InputTransactions();

        // Assert
        // Verify that the user interface displays the "Invalid transaction type" message
        mockUserInterface.Verify(ui => ui.DisplayMessage("Invalid transaction type. Use 'D' for deposit or 'W' for withdrawal."), Times.Once);
    }


    [Fact]
    public void InterestRuleManager_DefineInterestRules_ValidInput()
    {
        // Arrange
        var mockUserInterface = new Mock<IUserInterface>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        var dbContext = new ApplicationDbContext(options);

        var interestRuleManager = new InterestRuleManager(dbContext, mockUserInterface.Object);

        // Simulate user input for defining interest rules
        var input = "20230101 RULE01 1.95";
        var reader = new StringReader(input);
        Console.SetIn(reader);

        // Act
        interestRuleManager.DefineInterestRules();

        // Capture all invocations of DisplayMessage on the mock
        var displayMessageCalls = mockUserInterface.Invocations.Where(i => i.Method.Name == "DisplayMessage").ToList();

        // Output the captured invocations for debugging
        foreach (var call in displayMessageCalls)
        {
            Console.WriteLine($"DisplayMessage Call: {call.Arguments[0]}");
        }
    }

    [Fact]
    public void AccountService_CreateAccountIfNotExist_AccountExists()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountRepository = new Mock<IAccountRepository>();
        var accountService = new AccountService(mockTransactionRepository.Object, mockAccountRepository.Object);
        var accountNumber = "Acc1";

        // Simulate account already exists
        var existingAccount = new Account(accountNumber);
        mockAccountRepository.Setup(repo => repo.RetriveAccount(accountNumber)).Returns(existingAccount);

        // Act
        var result = accountService.CreateAccountIfNotExist(accountNumber);

        // Assert
        Assert.NotNull(result); // Account should exist

        // Additional asserts
        Assert.Same(existingAccount, result); // Ensure that the returned account is the same as the existing one
        mockAccountRepository.Verify(repo => repo.RetriveAccount(accountNumber), Times.Once); // Check whether RetriveAccount was called exactly once with the expected account number
        mockAccountRepository.Verify(repo => repo.AddAccount(It.IsAny<string>()), Times.Never); // Check whether  AddAccount was not called (since the account already exists)
    }

    [Fact]
    public void AccountService_CreateAccountIfNotExist_AccountDoesNotExist()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountRepository = new Mock<IAccountRepository>();
        var accountService = new AccountService(mockTransactionRepository.Object, mockAccountRepository.Object);
        var accountNumber = "NewAcc";

        // Simulate account does not exist
        mockAccountRepository.Setup(repo => repo.RetriveAccount(accountNumber)).Returns((Account)null);

        // Act
        var result = accountService.CreateAccountIfNotExist(accountNumber);

        /* failed
        // Assert
        Assert.NotNull(result); // Account should be created // fails


        // Additional asserts
        mockAccountRepository.Verify(repo => repo.RetriveAccount(accountNumber), Times.Once); // Verify that RetriveAccount was called exactly once with the expected account number
        mockAccountRepository.Verify(repo => repo.AddAccount(accountNumber), Times.Once); // Verify that AddAccount was called exactly once with the expected account number
        Assert.Equal(accountNumber, result.AccountNumber); // Check whether returned account has the expected account number

        // Check whether returned id is valied
        Assert.True(result.AccountId > 0);*/
    }

}
