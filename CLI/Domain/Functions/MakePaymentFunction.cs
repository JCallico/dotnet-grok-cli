using GrokCLI.Core.Abstractions;
using GrokCLI.Domain.Services;
using GrokCLI.Domain.Models;
using Newtonsoft.Json;

namespace GrokCLI.Domain.Functions
{
    /// <summary>
    /// Function to make a payment from an account to a payee
    /// </summary>
    [Function("make_payment", "Make a payment from a specified account to a payee")]
    public class MakePaymentFunction : FunctionBase
    {
        private readonly IBankingDataService _bankingService;

        public MakePaymentFunction(IBankingDataService bankingService)
        {
            _bankingService = bankingService;
        }

        public MakePaymentFunction() : this(BankingServiceProvider.Instance) { }

        public override async Task<string> ExecuteAsync(string arguments)
        {
            try
            {
                var args = JsonConvert.DeserializeObject<MakePaymentArgs>(arguments);
                if (args == null) return "Invalid arguments for make payment function";

                // Validate account
                var account = _bankingService.GetAccount(args.FromAccountId);
                if (account == null)
                {
                    return "Account not found";
                }

                // Validate payee
                var payee = _bankingService.GetPayee(args.PayeeId);
                if (payee == null)
                {
                    return "Payee not found";
                }

                // Validate amount
                if (args.Amount <= 0)
                {
                    return "Payment amount must be greater than zero";
                }

                // Check sufficient balance
                if (account.Balance < args.Amount)
                {
                    return $"Insufficient funds. Available balance: ${account.Balance:F2}";
                }

                // Store the original balance before making changes
                var previousBalance = account.Balance;

                // Create transaction
                var transaction = new Transaction
                {
                    AccountId = args.FromAccountId,
                    Type = TransactionType.Payment,
                    Amount = -args.Amount, // Negative for outgoing payment
                    Description = args.Description ?? $"Payment to {payee.Name}",
                    PayeeId = args.PayeeId
                };

                // Update account balance
                var newBalance = previousBalance - args.Amount;
                transaction.BalanceAfter = newBalance;

                // Add transaction and update balance
                var addedTransaction = _bankingService.AddTransaction(transaction);
                _bankingService.UpdateAccountBalance(args.FromAccountId, newBalance);

                var result = new
                {
                    success = true,
                    transaction_id = addedTransaction.Id,
                    payment_details = new
                    {
                        from_account = new
                        {
                            id = account.Id,
                            name = account.Name,
                            account_number = account.AccountNumber
                        },
                        to_payee = new
                        {
                            id = payee.Id,
                            name = payee.Name,
                            account_number = payee.AccountNumber
                        },
                        amount = args.Amount,
                        description = transaction.Description,
                        date = addedTransaction.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                        previous_balance = previousBalance,
                        new_balance = newBalance
                    }
                };

                return await Task.FromResult(JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            catch (Exception ex)
            {
                return $"Error making payment: {ex.Message}";
            }
        }

        private class MakePaymentArgs
        {
            [JsonProperty("from_account_id")]
            [FunctionParameter("ID of the account to pay from", IsRequired = true)]
            public string FromAccountId { get; set; } = string.Empty;

            [JsonProperty("payee_id")]
            [FunctionParameter("ID of the payee to pay", IsRequired = true)]
            public string PayeeId { get; set; } = string.Empty;

            [JsonProperty("amount")]
            [FunctionParameter("Payment amount", IsRequired = true)]
            public decimal Amount { get; set; }

            [JsonProperty("description")]
            [FunctionParameter("Payment description (optional)")]
            public string? Description { get; set; }
        }
    }
}
