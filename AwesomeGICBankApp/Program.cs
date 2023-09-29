using System.Globalization;
public class Program
{
    public static void Main(string[] args)
    {
        var bankingApp = new AwesomeGICBankApp();
        bankingApp.Run();
    }
}

public class AwesomeGICBankApp
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

            if (!DateTime.TryParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                Console.WriteLine("Invalid date format. Please use YYYYMMdd.");
                return;
            }

            if (!decimal.TryParse(amountStr, out var amount) || amount <= 0)
            {
                Console.WriteLine("Invalid amount. Please enter a positive number.");
                return;
            }

            // todo Create if account not exists

            if (type == "D")
            {
                Console.WriteLine("D");
                // deposit to account
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