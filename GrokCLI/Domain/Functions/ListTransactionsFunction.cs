using GrokCLI.Core.Abstractions;
using GrokCLI.Domain.Services;
using Newtonsoft.Json;

namespace GrokCLI.Domain.Functions
{
    /// <summary>
    /// Function to list and filter transactions for accounts
    /// </summary>
    [Function("list_transactions", "List transactions for all accounts or a specific account with optional filtering")]
    public class ListTransactionsFunction : FunctionBase
    {
        private readonly IBankingDataService _bankingService;

        public ListTransactionsFunction(IBankingDataService bankingService)
        {
            _bankingService = bankingService;
        }

        public ListTransactionsFunction() : this(BankingServiceProvider.Instance) { }

        public override async Task<string> ExecuteAsync(string arguments)
        {
            try
            {
                var args = JsonConvert.DeserializeObject<ListTransactionsArgs>(arguments);
                if (args == null) return "Invalid arguments for list transactions function";

                var transactions = _bankingService.GetTransactions(args.AccountId);

                // Filter by transaction type if specified
                if (!string.IsNullOrEmpty(args.TransactionType))
                {
                    if (Enum.TryParse<Domain.Models.TransactionType>(args.TransactionType, true, out var transactionType))
                    {
                        transactions = transactions.Where(t => t.Type == transactionType).ToList();
                    }
                }

                // Filter by date range if specified
                if (args.StartDate.HasValue)
                {
                    transactions = transactions.Where(t => t.Date >= args.StartDate.Value).ToList();
                }

                if (args.EndDate.HasValue)
                {
                    transactions = transactions.Where(t => t.Date <= args.EndDate.Value).ToList();
                }

                // Limit results if specified
                if (args.Limit.HasValue && args.Limit > 0)
                {
                    transactions = transactions.Take(args.Limit.Value).ToList();
                }

                var accounts = _bankingService.GetAccounts();
                var payees = _bankingService.GetPayees();

                var result = new
                {
                    transactions = transactions.Select(t => new
                    {
                        id = t.Id,
                        account_id = t.AccountId,
                        account_name = accounts.FirstOrDefault(a => a.Id == t.AccountId)?.Name ?? "Unknown",
                        type = t.Type.ToString(),
                        amount = t.Amount,
                        description = t.Description,
                        payee_name = !string.IsNullOrEmpty(t.PayeeId) ? 
                            payees.FirstOrDefault(p => p.Id == t.PayeeId)?.Name : null,
                        to_account_name = !string.IsNullOrEmpty(t.ToAccountId) ? 
                            accounts.FirstOrDefault(a => a.Id == t.ToAccountId)?.Name : null,
                        date = t.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                        balance_after = t.BalanceAfter
                    }).ToList(),
                    total_transactions = transactions.Count,
                    filter_applied = new
                    {
                        account_id = args.AccountId,
                        transaction_type = args.TransactionType,
                        start_date = args.StartDate?.ToString("yyyy-MM-dd"),
                        end_date = args.EndDate?.ToString("yyyy-MM-dd"),
                        limit = args.Limit
                    }
                };

                return await Task.FromResult(JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            catch (Exception ex)
            {
                return $"Error listing transactions: {ex.Message}";
            }
        }

        private class ListTransactionsArgs
        {
            [JsonProperty("account_id")]
            [FunctionParameter("Filter by specific account ID (optional)")]
            public string? AccountId { get; set; }

            [JsonProperty("transaction_type")]
            [FunctionParameter("Filter by transaction type", 
                EnumValues = new[] { "deposit", "withdrawal", "transfer", "payment" })]
            public string? TransactionType { get; set; }

            [JsonProperty("start_date")]
            [FunctionParameter("Start date for filtering (YYYY-MM-DD format)")]
            public DateTime? StartDate { get; set; }

            [JsonProperty("end_date")]
            [FunctionParameter("End date for filtering (YYYY-MM-DD format)")]
            public DateTime? EndDate { get; set; }

            [JsonProperty("limit")]
            [FunctionParameter("Maximum number of transactions to return")]
            public int? Limit { get; set; }
        }
    }
}
