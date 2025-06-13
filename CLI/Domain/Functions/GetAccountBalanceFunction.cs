 using GrokCLI.Core.Abstractions;
using GrokCLI.Domain.Services;
using Newtonsoft.Json;

namespace GrokCLI.Domain.Functions
{
    /// <summary>
    /// Function to get account balance and summary information
    /// </summary>
    [Function("get_account_balance", "Get balance and summary information for a specific account or all accounts")]
    public class GetAccountBalanceFunction : FunctionBase
    {
        private readonly IBankingDataService _bankingService;

        public GetAccountBalanceFunction(IBankingDataService bankingService)
        {
            _bankingService = bankingService;
        }

        public GetAccountBalanceFunction() : this(BankingServiceProvider.Instance) { }

        public override async Task<string> ExecuteAsync(string arguments)
        {
            try
            {
                var args = JsonConvert.DeserializeObject<GetAccountBalanceArgs>(arguments);
                if (args == null) return "Invalid arguments for get account balance function";

                if (!string.IsNullOrEmpty(args.AccountId))
                {
                    // Get specific account balance
                    var account = _bankingService.GetAccount(args.AccountId);
                    if (account == null)
                    {
                        return "Account not found";
                    }

                    // Get recent transactions for this account
                    var recentTransactions = _bankingService.GetTransactions(args.AccountId)
                        .Take(5)
                        .ToList();

                    var result = new
                    {
                        account = new
                        {
                            id = account.Id,
                            name = account.Name,
                            type = account.Type.ToString(),
                            balance = account.Balance,
                            account_number = account.AccountNumber,
                            created_date = account.CreatedDate.ToString("yyyy-MM-dd")
                        },
                        recent_transactions = recentTransactions.Select(t => new
                        {
                            id = t.Id,
                            type = t.Type.ToString(),
                            amount = t.Amount,
                            description = t.Description,
                            date = t.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                            balance_after = t.BalanceAfter
                        }).ToList()
                    };

                    return await Task.FromResult(JsonConvert.SerializeObject(result, Formatting.Indented));
                }
                else
                {
                    // Get all account balances
                    var accounts = _bankingService.GetAccounts();
                    var totalBalance = accounts.Sum(a => a.Balance);
                    var checkingBalance = accounts.Where(a => a.Type == Domain.Models.AccountType.Checking).Sum(a => a.Balance);
                    var savingsBalance = accounts.Where(a => a.Type == Domain.Models.AccountType.Savings).Sum(a => a.Balance);

                    var result = new
                    {
                        summary = new
                        {
                            total_balance = totalBalance,
                            checking_balance = checkingBalance,
                            savings_balance = savingsBalance,
                            total_accounts = accounts.Count
                        },
                        accounts = accounts.Select(a => new
                        {
                            id = a.Id,
                            name = a.Name,
                            type = a.Type.ToString(),
                            balance = a.Balance,
                            account_number = a.AccountNumber
                        }).ToList()
                    };

                    return await Task.FromResult(JsonConvert.SerializeObject(result, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                return $"Error getting account balance: {ex.Message}";
            }
        }

        private class GetAccountBalanceArgs
        {
            [JsonProperty("account_id")]
            [FunctionParameter("Account ID to get balance for (optional - if not provided, returns all account balances)")]
            public string? AccountId { get; set; }
        }
    }
}
