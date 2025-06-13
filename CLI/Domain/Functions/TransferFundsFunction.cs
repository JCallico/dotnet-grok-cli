using GrokCLI.Core.Abstractions;
using GrokCLI.Domain.Services;
using GrokCLI.Domain.Models;
using Newtonsoft.Json;

namespace GrokCLI.Domain.Functions
{
    /// <summary>
    /// Function to transfer money between accounts
    /// </summary>
    [Function("transfer_funds", "Transfer money between your accounts")]
    public class TransferFundsFunction : FunctionBase
    {
        private readonly IBankingDataService _bankingService;

        public TransferFundsFunction(IBankingDataService bankingService)
        {
            _bankingService = bankingService;
        }

        public TransferFundsFunction() : this(BankingServiceProvider.Instance) { }

        public override async Task<string> ExecuteAsync(string arguments)
        {
            try
            {
                var args = JsonConvert.DeserializeObject<TransferFundsArgs>(arguments);
                if (args == null) return "Invalid arguments for transfer funds function";

                // Validate accounts
                var fromAccount = _bankingService.GetAccount(args.FromAccountId);
                if (fromAccount == null)
                {
                    return "Source account not found";
                }

                var toAccount = _bankingService.GetAccount(args.ToAccountId);
                if (toAccount == null)
                {
                    return "Destination account not found";
                }

                if (args.FromAccountId == args.ToAccountId)
                {
                    return "Cannot transfer to the same account";
                }

                // Validate amount
                if (args.Amount <= 0)
                {
                    return "Transfer amount must be greater than zero";
                }

                // Check sufficient balance
                if (fromAccount.Balance < args.Amount)
                {
                    return $"Insufficient funds in source account. Available balance: ${fromAccount.Balance:F2}";
                }

                // Store the original balances before making changes
                var previousFromBalance = fromAccount.Balance;
                var previousToBalance = toAccount.Balance;

                // Create outgoing transaction (from account)
                var outgoingTransaction = new Transaction
                {
                    AccountId = args.FromAccountId,
                    Type = TransactionType.Transfer,
                    Amount = -args.Amount, // Negative for outgoing transfer
                    Description = args.Description ?? $"Transfer to {toAccount.Name}",
                    ToAccountId = args.ToAccountId
                };

                // Create incoming transaction (to account)
                var incomingTransaction = new Transaction
                {
                    AccountId = args.ToAccountId,
                    Type = TransactionType.Transfer,
                    Amount = args.Amount, // Positive for incoming transfer
                    Description = args.Description ?? $"Transfer from {fromAccount.Name}",
                    ToAccountId = args.FromAccountId
                };

                // Calculate new balances
                var newFromBalance = previousFromBalance - args.Amount;
                var newToBalance = previousToBalance + args.Amount;

                outgoingTransaction.BalanceAfter = newFromBalance;
                incomingTransaction.BalanceAfter = newToBalance;

                // Add transactions and update balances
                var addedOutgoingTransaction = _bankingService.AddTransaction(outgoingTransaction);
                var addedIncomingTransaction = _bankingService.AddTransaction(incomingTransaction);
                
                _bankingService.UpdateAccountBalance(args.FromAccountId, newFromBalance);
                _bankingService.UpdateAccountBalance(args.ToAccountId, newToBalance);

                var result = new
                {
                    success = true,
                    transfer_details = new
                    {
                        from_account = new
                        {
                            id = fromAccount.Id,
                            name = fromAccount.Name,
                            account_number = fromAccount.AccountNumber,
                            previous_balance = previousFromBalance,
                            new_balance = newFromBalance,
                            transaction_id = addedOutgoingTransaction.Id
                        },
                        to_account = new
                        {
                            id = toAccount.Id,
                            name = toAccount.Name,
                            account_number = toAccount.AccountNumber,
                            previous_balance = previousToBalance,
                            new_balance = newToBalance,
                            transaction_id = addedIncomingTransaction.Id
                        },
                        amount = args.Amount,
                        description = outgoingTransaction.Description,
                        date = addedOutgoingTransaction.Date.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                };

                return await Task.FromResult(JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            catch (Exception ex)
            {
                return $"Error transferring funds: {ex.Message}";
            }
        }

        private class TransferFundsArgs
        {
            [JsonProperty("from_account_id")]
            [FunctionParameter("ID of the account to transfer from", IsRequired = true)]
            public string FromAccountId { get; set; } = string.Empty;

            [JsonProperty("to_account_id")]
            [FunctionParameter("ID of the account to transfer to", IsRequired = true)]
            public string ToAccountId { get; set; } = string.Empty;

            [JsonProperty("amount")]
            [FunctionParameter("Transfer amount", IsRequired = true)]
            public decimal Amount { get; set; }

            [JsonProperty("description")]
            [FunctionParameter("Transfer description (optional)")]
            public string? Description { get; set; }
        }
    }
}
