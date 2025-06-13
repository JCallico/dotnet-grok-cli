using GrokCLI.Core.Abstractions;
using GrokCLI.Domain.Services;
using Newtonsoft.Json;

namespace GrokCLI.Domain.Functions
{
    /// <summary>
    /// Function to list and filter bank accounts
    /// </summary>
    [Function("list_accounts", "List all bank accounts or filter by account type")]
    public class ListAccountsFunction : FunctionBase
    {
        private readonly IBankingDataService _bankingService;

        public ListAccountsFunction(IBankingDataService bankingService)
        {
            _bankingService = bankingService;
        }

        public ListAccountsFunction() : this(BankingServiceProvider.Instance) { }

        public override async Task<string> ExecuteAsync(string arguments)
        {
            try
            {
                var args = JsonConvert.DeserializeObject<ListAccountsArgs>(arguments);
                if (args == null) return "Invalid arguments for list accounts function";

                var accounts = _bankingService.GetAccounts();

                // Filter by account type if specified
                if (!string.IsNullOrEmpty(args.AccountType))
                {
                    if (Enum.TryParse<Domain.Models.AccountType>(args.AccountType, true, out var accountType))
                    {
                        accounts = accounts.Where(a => a.Type == accountType).ToList();
                    }
                }

                var result = new
                {
                    accounts = accounts.Select(a => new
                    {
                        id = a.Id,
                        name = a.Name,
                        type = a.Type.ToString(),
                        balance = a.Balance,
                        account_number = a.AccountNumber,
                        created_date = a.CreatedDate.ToString("yyyy-MM-dd")
                    }).ToList(),
                    total_accounts = accounts.Count,
                    total_balance = accounts.Sum(a => a.Balance)
                };

                return await Task.FromResult(JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            catch (Exception ex)
            {
                return $"Error listing accounts: {ex.Message}";
            }
        }

        private class ListAccountsArgs
        {
            [JsonProperty("account_type")]
            [FunctionParameter("Filter by account type (checking, savings)", 
                EnumValues = new[] { "checking", "savings" })]
            public string? AccountType { get; set; }
        }
    }
}
