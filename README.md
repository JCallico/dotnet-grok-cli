# Grok CLI Banking Agent

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A command-line interface for interacting with the Grok-3 model with banking function calling support.

## Features

- **Function Calling**: Supports calling predefined banking functions through natural language
- **Account Management**: List and filter checking and savings accounts
- **Transaction History**: View and filter transaction history across all accounts
- **Payee Management**: List and manage payees for payments
- **Payment Processing**: Make payments from accounts to registered payees
- **Fund Transfers**: Transfer money between your own accounts
- **Balance Inquiries**: Get current balances and account summaries
- **Chat History**: Automatically saves conversation history with timestamps
- **Streaming Support**: Real-time response streaming for better user experience
- **Extensible Architecture**: Easy to add new banking functions with automatic discovery

## Setup

1. **Configure API Key**:
   ```bash
   cd GrokCLI
   dotnet user-secrets init
   dotnet user-secrets set GrokApi:ApiKey <your-grok-api-key>
   ```

2. **Build and Run**:
   ```bash
   dotnet build
   dotnet run
   ```

## Usage

### Basic Commands
- Type your questions naturally
- Type `functions` to see available functions
- Type `exit` to quit

### Available Banking Functions

**Account Management:**
- "Show me all my accounts"
- "List only my checking accounts"
- "What are my savings accounts?"

**Balance Inquiries:**
- "What's my account balance?"
- "Show me the balance for my checking account"
- "Give me a summary of all my account balances"

**Transaction History:**
- "Show me my recent transactions"
- "List transactions for my checking account"
- "Show me all payments from last month"
- "What transfers did I make this week?"

**Payee Management:**
- "List all my payees"
- "Show me payees with 'Electric' in the name"
- "Who can I pay?"

**Payment Processing:**
- "Pay $125.50 to Electric Company from my checking account"
- "Make a payment to John Doe for $50"
- "Pay my internet bill"

**Fund Transfers:**
- "Transfer $500 from checking to savings"
- "Move $1000 to my vacation fund"
- "Transfer money between accounts"

## Mock Data

The application includes pre-loaded mock banking data:

**Accounts:**
- Primary Checking (acc-001): $2,500.75
- Emergency Savings (acc-002): $15,000.00  
- Vacation Fund (acc-003): $3,250.50

**Payees:**
- Electric Company
- Internet Service Provider
- Rent Management Company
- John Doe

**Transaction History:**
- Sample transactions including deposits, payments, and transfers
- All new transactions are kept in memory during the session
- Data resets when the application restarts

## Banking Functions

The application provides 6 core banking functions:

1. **list_accounts**: List and filter bank accounts by type
2. **get_account_balance**: Get balance information and recent transactions
3. **list_transactions**: View and filter transaction history with multiple criteria
4. **list_payees**: List and search registered payees
5. **make_payment**: Process payments from accounts to payees
6. **transfer_funds**: Transfer money between your own accounts

## Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "GrokApi": {
    "BaseUrl": "https://api.x.ai/v1/",
    "Model": "grok-3",
    "MaxTokens": 4000,
    "Temperature": 0.7
  },
  "ChatSettings": {
    "MaxHistoryLength": 50,
    "AutoSaveHistory": true,
    "LogsDirectory": "logs",
    "HistoryFilePrefix": "chat_history"
  }
}
```

## Architecture

The application follows a clean architecture pattern with clear separation of concerns:

### Core Layer
- **Core/Abstractions/**: Base classes and interfaces
  - `FunctionBase.cs`: Abstract base class for function implementations
  - `IFunction.cs`: Function interface and attribute definitions
- **Core/Models/**: Pure data models and DTOs
  - `ChatModels.cs`: Chat-related data models (Message, ChatMessage, ChatHistory)
  - `FunctionModels.cs`: Function-related data models (FunctionDefinition, ToolCall, etc.)
- **Core/Settings/**: Configuration classes
  - `GrokApiSettings.cs`: API configuration settings
  - `ChatSettings.cs`: Chat behavior configuration
- **Core/Services/**: Core business services
  - `ChatService.cs`: Manages conversation history and persistence
  - `FunctionDiscoveryService.cs`: Discovers and loads function implementations
- **Core/Handlers/**: Request/response handlers
  - `FunctionHandler.cs`: Manages function execution and registration
- **Core/Infrastructure/**: Infrastructure concerns
  - `Http/GrokApiClient.cs`: Handles API communication with function calling support
  - `DependencyInjection/ServiceConfigurator.cs`: Service registration and DI setup

### Domain Layer
- **Domain/Functions/**: Banking function implementations
  - `ListAccountsFunction.cs`: List and filter bank accounts
  - `GetAccountBalanceFunction.cs`: Get account balances and summaries
  - `ListTransactionsFunction.cs`: View and filter transaction history
  - `ListPayeesFunction.cs`: List and search payees
  - `MakePaymentFunction.cs`: Process payments to payees
  - `TransferFundsFunction.cs`: Transfer money between accounts
- **Domain/Models/**: Banking domain models
  - `BankingModels.cs`: Account, Transaction, and Payee models
- **Domain/Services/**: Banking business services
  - `BankingDataService.cs`: In-memory banking data management
  - `BankingServiceProvider.cs`: Singleton service provider

### Application Entry Point
- **Program.cs**: Main application entry point with interactive chat loop

## Adding New Functions

The application uses automatic function discovery to load all valid functions from the `Domain/Functions/` directory. Functions are automatically discovered when they:

1. Implement the `IFunction` interface or inherit from `FunctionBase`
2. Are decorated with the `[Function]` attribute
3. Have `IsEnabled = true` in the Function attribute (default)
4. Are located in the Functions directory or in the main assembly

### Creating a Banking Function

Follow these steps to create a new banking function:

#### 1. Create a New Function Class

Create a new `.cs` file in `Domain/Functions/` that inherits from `FunctionBase`:

```csharp
[Function("your_banking_function", "Description of banking operation")]
public class YourBankingFunction : FunctionBase
{
    private readonly IBankingDataService _bankingService;

    public YourBankingFunction(IBankingDataService bankingService)
    {
        _bankingService = bankingService;
    }

    public YourBankingFunction() : this(BankingServiceProvider.Instance) { }

    public override async Task<string> ExecuteAsync(string arguments)
    {
        try
        {
            var args = JsonConvert.DeserializeObject<YourFunctionArgs>(arguments);
            if (args == null) return "Invalid arguments";

            // Your banking logic here using _bankingService
            var result = new { /* your result object */ };

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private class YourFunctionArgs
    {
        [JsonProperty("account_id")]
        [FunctionParameter("Account ID for the operation", IsRequired = true)]
        public string AccountId { get; set; } = string.Empty;

        [JsonProperty("amount")]
        [FunctionParameter("Transaction amount")]
        public decimal? Amount { get; set; }
    }
}
```

#### 2. Function Parameter Attributes

The `FunctionParameter` attribute supports these properties:

- **Description**: Required. Describes what the parameter does.
- **IsRequired**: Optional. Set to `true` if the parameter is required.
- **EnumValues**: Optional. Array of allowed string values for the parameter.

#### 3. Build and Test

1. Build the project: `dotnet build`
2. Run the application: `dotnet run`
3. Type `functions` in the chat to see your new function listed
4. Test your function by asking the agent to use it in natural language

### Best Practices

1. **Error Handling**: Always wrap your function logic in try-catch blocks
2. **Validation**: Validate input arguments before processing
3. **Return Format**: Return JSON-serialized objects for structured data
4. **Async Operations**: Use async/await for I/O operations
5. **Documentation**: Use clear descriptions in the Function and FunctionParameter attributes
6. **Parameter Naming**: Use lowercase with underscores for JSON property names to match common API conventions

### Example Banking Functions

See the following banking functions in the `Domain/Functions/` directory:

- **ListAccountsFunction.cs**: Lists and filters bank accounts by type
- **GetAccountBalanceFunction.cs**: Gets account balances and recent transactions
- **ListTransactionsFunction.cs**: Views and filters transaction history
- **ListPayeesFunction.cs**: Lists and searches registered payees
- **MakePaymentFunction.cs**: Processes payments from accounts to payees
- **TransferFundsFunction.cs**: Transfers money between accounts

### Plugin Architecture

To deploy a new function to an existing installation:

1. Compile your function into a separate assembly (DLL)
2. Copy the DLL to the Functions directory of the deployed application
3. Restart the application - the function will be automatically discovered and loaded

This plugin architecture allows for easy extension of the agent's capabilities without modifying the core codebase. The `FunctionBase` class provides automatic parameter discovery using reflection, so you only need to focus on implementing the business logic.

## Project Structure

```
GrokCLI/
├── Core/                              # Core application components
│   ├── Abstractions/                  # Base classes and interfaces
│   ├── Models/                        # Pure data models and DTOs
│   ├── Settings/                      # Configuration classes
│   ├── Services/                      # Core business services
│   ├── Handlers/                      # Request/response handlers
│   └── Infrastructure/                # Infrastructure concerns
│       ├── Http/                      # HTTP clients and API communication
│       └── DependencyInjection/       # Service registration
├── Domain/                            # Domain-specific implementations
│   ├── Functions/                     # Banking function implementations
│   ├── Models/                        # Banking domain models
│   └── Services/                      # Banking business services
├── logs/                              # Chat history files
├── appsettings.json                   # Application configuration
└── Program.cs                         # Application entry point
```

## Chat History

Chat history is automatically saved to the `logs/` directory with timestamped filenames.

## Banking Data Model

The application uses the following banking data model:

### Account Types
- **Checking**: Day-to-day transaction accounts
- **Savings**: Interest-bearing savings accounts

### Transaction Types
- **Deposit**: Money added to an account
- **Withdrawal**: Money removed from an account
- **Transfer**: Money moved between accounts
- **Payment**: Money sent to a payee

### Data Persistence
- All banking data is stored in memory and resets on application restart
- New transactions created during the session are preserved until restart
- Chat history is persisted to disk in the `logs/` directory

### Security Features
- Account numbers are masked (e.g., ****1234)
- No actual banking integration - purely simulated for demonstration
- All data is local and not transmitted anywhere except to the Grok API for processing

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

The MIT License is a permissive license that allows for reuse within proprietary software provided that all copies of the licensed software include a copy of the MIT License terms and the copyright notice.

## Function Parameters

Each banking function accepts specific parameters:

### list_accounts
- `account_type` (optional): "checking" or "savings"

### get_account_balance  
- `account_id` (optional): Specific account ID, or omit for all accounts

### list_transactions
- `account_id` (optional): Filter by account
- `transaction_type` (optional): "deposit", "withdrawal", "transfer", "payment"
- `start_date` (optional): Start date (YYYY-MM-DD)
- `end_date` (optional): End date (YYYY-MM-DD)
- `limit` (optional): Maximum number of results

### list_payees
- `name_filter` (optional): Search by payee name

### make_payment
- `from_account_id` (required): Source account ID
- `payee_id` (required): Target payee ID
- `amount` (required): Payment amount
- `description` (optional): Payment description

### transfer_funds
- `from_account_id` (required): Source account ID
- `to_account_id` (required): Destination account ID
- `amount` (required): Transfer amount
- `description` (optional): Transfer description
