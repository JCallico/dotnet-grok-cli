using GrokCLI.Core.Abstractions;
using GrokCLI.Core.Models;
using GrokCLI.Core.Services;
using Newtonsoft.Json;
using System.Text.Json;

namespace GrokCLI.Core.Handlers
{
    public interface IFunctionHandler
    {
        Task<string> ExecuteAsync(string functionName, string arguments);
        List<FunctionDefinition> GetAvailableFunctions();
        Task InitializeAsync();
    }

    public class FunctionHandler : IFunctionHandler
    {
        private readonly Dictionary<string, IFunction> _functions;
        private readonly List<FunctionDefinition> _functionDefinitions;
        private readonly IFunctionDiscoveryService _discoveryService;

        public FunctionHandler(IFunctionDiscoveryService discoveryService)
        {
            _functions = new Dictionary<string, IFunction>();
            _functionDefinitions = new List<FunctionDefinition>();
            _discoveryService = discoveryService;
        }

        public async Task InitializeAsync()
        {
            await RegisterFunctionsAsync();
        }

        private async Task RegisterFunctionsAsync()
        {
            try
            {
                var discoveredFunctions = await _discoveryService.DiscoverFunctionsAsync();
                
                _functions.Clear();
                _functionDefinitions.Clear();

                foreach (var function in discoveredFunctions)
                {
                    var definition = function.GetDefinition();
                    _functions[definition.Name] = function;
                    _functionDefinitions.Add(definition);
                }

                Console.WriteLine($"Registered {_functions.Count} functions: {string.Join(", ", _functions.Keys)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering functions: {ex.Message}");
            }
        }

        public async Task<string> ExecuteAsync(string functionName, string arguments)
        {
            if (_functions.TryGetValue(functionName, out var function))
            {
                try
                {
                    return await function.ExecuteAsync(arguments);
                }
                catch (Exception ex)
                {
                    return $"Error executing function {functionName}: {ex.Message}";
                }
            }
            
            return $"Function {functionName} not found";
        }

        public List<FunctionDefinition> GetAvailableFunctions()
        {
            return _functionDefinitions;
        }

    }
}
