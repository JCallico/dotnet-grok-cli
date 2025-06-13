using System.ComponentModel.DataAnnotations;

namespace GrokCLI.Domain.Models
{
    public enum AccountType
    {
        Checking,
        Savings
    }

    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Transfer,
        Payment
    }

    public class Account
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public decimal Balance { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class Transaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AccountId { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? PayeeId { get; set; }
        public string? ToAccountId { get; set; }
        public DateTime Date { get; set; }
        public decimal BalanceAfter { get; set; }
    }

    public class Payee
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string RoutingNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
    }
}
