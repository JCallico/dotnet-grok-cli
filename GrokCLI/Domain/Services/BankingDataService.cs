using GrokCLI.Domain.Models;

namespace GrokCLI.Domain.Services
{
    public interface IBankingDataService
    {
        List<Account> GetAccounts();
        List<Transaction> GetTransactions(string? accountId = null);
        List<Payee> GetPayees();
        Account? GetAccount(string accountId);
        Payee? GetPayee(string payeeId);
        Transaction AddTransaction(Transaction transaction);
        void UpdateAccountBalance(string accountId, decimal newBalance);
        void InitializeData();
    }

    public class BankingDataService : IBankingDataService
    {
        private List<Account> _accounts = new();
        private List<Transaction> _transactions = new();
        private List<Payee> _payees = new();
        private readonly object _lock = new();

        public BankingDataService()
        {
            InitializeData();
        }

        public void InitializeData()
        {
            lock (_lock)
            {
                // Initialize Accounts
                _accounts = new List<Account>
                {
                    new Account
                    {
                        Id = "acc-001",
                        Name = "Primary Checking",
                        Type = AccountType.Checking,
                        Balance = 2500.75m,
                        AccountNumber = "****1234",
                        CreatedDate = DateTime.Now.AddYears(-2),
                        IsActive = true
                    },
                    new Account
                    {
                        Id = "acc-002",
                        Name = "Emergency Savings",
                        Type = AccountType.Savings,
                        Balance = 15000.00m,
                        AccountNumber = "****5678",
                        CreatedDate = DateTime.Now.AddYears(-2),
                        IsActive = true
                    },
                    new Account
                    {
                        Id = "acc-003",
                        Name = "Vacation Fund",
                        Type = AccountType.Savings,
                        Balance = 3250.50m,
                        AccountNumber = "****9012",
                        CreatedDate = DateTime.Now.AddMonths(-8),
                        IsActive = true
                    }
                };

                // Initialize Payees
                _payees = new List<Payee>
                {
                    new Payee
                    {
                        Id = "payee-001",
                        Name = "Electric Company",
                        AccountNumber = "123456789",
                        RoutingNumber = "987654321",
                        Email = "billing@electricco.com",
                        Phone = "(555) 123-4567",
                        IsActive = true,
                        CreatedDate = DateTime.Now.AddMonths(-18)
                    },
                    new Payee
                    {
                        Id = "payee-002",
                        Name = "Internet Service Provider",
                        AccountNumber = "987654321",
                        RoutingNumber = "123456789",
                        Email = "bills@isp.com",
                        Phone = "(555) 987-6543",
                        IsActive = true,
                        CreatedDate = DateTime.Now.AddMonths(-20)
                    },
                    new Payee
                    {
                        Id = "payee-003",
                        Name = "Rent Management Company",
                        AccountNumber = "456789123",
                        RoutingNumber = "789123456",
                        Email = "payments@rentco.com",
                        Phone = "(555) 456-7890",
                        IsActive = true,
                        CreatedDate = DateTime.Now.AddMonths(-24)
                    },
                    new Payee
                    {
                        Id = "payee-004",
                        Name = "John Doe",
                        AccountNumber = "111222333",
                        RoutingNumber = "444555666",
                        Email = "john.doe@email.com",
                        Phone = "(555) 111-2222",
                        IsActive = true,
                        CreatedDate = DateTime.Now.AddMonths(-6)
                    }
                };

                // Initialize Transactions
                _transactions = new List<Transaction>();
                
                // Add some historical transactions for checking account
                var checkingAccount = _accounts.First(a => a.Id == "acc-001");
                var initialBalance = 3000.00m;
                
                AddHistoricalTransaction("acc-001", TransactionType.Deposit, 1500.00m, "Salary Deposit", initialBalance + 1500.00m, DateTime.Now.AddDays(-30));
                AddHistoricalTransaction("acc-001", TransactionType.Payment, -850.00m, "Rent Payment", initialBalance + 1500.00m - 850.00m, DateTime.Now.AddDays(-28), "payee-003");
                AddHistoricalTransaction("acc-001", TransactionType.Payment, -125.50m, "Electric Bill", initialBalance + 1500.00m - 850.00m - 125.50m, DateTime.Now.AddDays(-25), "payee-001");
                AddHistoricalTransaction("acc-001", TransactionType.Payment, -75.25m, "Internet Bill", checkingAccount.Balance, DateTime.Now.AddDays(-22), "payee-002");
                
                // Add some historical transactions for savings accounts
                AddHistoricalTransaction("acc-002", TransactionType.Deposit, 500.00m, "Monthly Savings", 15000.00m, DateTime.Now.AddDays(-30));
                AddHistoricalTransaction("acc-003", TransactionType.Deposit, 250.50m, "Vacation Savings", 3250.50m, DateTime.Now.AddDays(-15));
            }
        }

        private void AddHistoricalTransaction(string accountId, TransactionType type, decimal amount, string description, decimal balanceAfter, DateTime date, string? payeeId = null)
        {
            _transactions.Add(new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = accountId,
                Type = type,
                Amount = amount,
                Description = description,
                PayeeId = payeeId,
                Date = date,
                BalanceAfter = balanceAfter
            });
        }

        public List<Account> GetAccounts()
        {
            lock (_lock)
            {
                return _accounts.Where(a => a.IsActive).ToList();
            }
        }

        public List<Transaction> GetTransactions(string? accountId = null)
        {
            lock (_lock)
            {
                var transactions = _transactions.AsQueryable();
                if (!string.IsNullOrEmpty(accountId))
                {
                    transactions = transactions.Where(t => t.AccountId == accountId);
                }
                return transactions.OrderByDescending(t => t.Date).ToList();
            }
        }

        public List<Payee> GetPayees()
        {
            lock (_lock)
            {
                return _payees.Where(p => p.IsActive).ToList();
            }
        }

        public Account? GetAccount(string accountId)
        {
            lock (_lock)
            {
                return _accounts.FirstOrDefault(a => a.Id == accountId && a.IsActive);
            }
        }

        public Payee? GetPayee(string payeeId)
        {
            lock (_lock)
            {
                return _payees.FirstOrDefault(p => p.Id == payeeId && p.IsActive);
            }
        }

        public Transaction AddTransaction(Transaction transaction)
        {
            lock (_lock)
            {
                transaction.Id = Guid.NewGuid().ToString();
                transaction.Date = DateTime.Now;
                _transactions.Add(transaction);
                return transaction;
            }
        }

        public void UpdateAccountBalance(string accountId, decimal newBalance)
        {
            lock (_lock)
            {
                var account = _accounts.FirstOrDefault(a => a.Id == accountId);
                if (account != null)
                {
                    account.Balance = newBalance;
                }
            }
        }
    }
}
