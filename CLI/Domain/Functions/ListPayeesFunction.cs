using GrokCLI.Core.Abstractions;
using GrokCLI.Domain.Services;
using Newtonsoft.Json;

namespace GrokCLI.Domain.Functions
{
    /// <summary>
    /// Function to list and filter payees
    /// </summary>
    [Function("list_payees", "List all payees or filter by name")]
    public class ListPayeesFunction : FunctionBase
    {
        private readonly IBankingDataService _bankingService;

        public ListPayeesFunction(IBankingDataService bankingService)
        {
            _bankingService = bankingService;
        }

        public ListPayeesFunction() : this(BankingServiceProvider.Instance) { }

        public override async Task<string> ExecuteAsync(string arguments)
        {
            try
            {
                var args = JsonConvert.DeserializeObject<ListPayeesArgs>(arguments);
                if (args == null) return "Invalid arguments for list payees function";

                var payees = _bankingService.GetPayees();

                // Filter by name if specified
                if (!string.IsNullOrEmpty(args.NameFilter))
                {
                    payees = payees.Where(p => p.Name.Contains(args.NameFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var result = new
                {
                    payees = payees.Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        account_number = p.AccountNumber,
                        routing_number = p.RoutingNumber,
                        email = p.Email,
                        phone = p.Phone,
                        created_date = p.CreatedDate.ToString("yyyy-MM-dd")
                    }).ToList(),
                    total_payees = payees.Count,
                    filter_applied = args.NameFilter
                };

                return await Task.FromResult(JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            catch (Exception ex)
            {
                return $"Error listing payees: {ex.Message}";
            }
        }

        private class ListPayeesArgs
        {
            [JsonProperty("name_filter")]
            [FunctionParameter("Filter payees by name (partial match)")]
            public string? NameFilter { get; set; }
        }
    }
}
