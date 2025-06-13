using GrokCLI.Core.Abstractions;
using System.Reflection;

namespace GrokCLI.Core.Services
{
    /// <summary>
    /// Service responsible for discovering and loading function implementations
    /// </summary>
    public interface IFunctionDiscoveryService
    {
        /// <summary>
        /// Discovers all available functions from the Functions folder and assemblies
        /// </summary>
        Task<List<IFunction>> DiscoverFunctionsAsync();

        /// <summary>
        /// Loads a specific function by name
        /// </summary>
        Task<IFunction?> LoadFunctionAsync(string functionName);
    }

    public class FunctionDiscoveryService : IFunctionDiscoveryService
    {
        private readonly string _functionsDirectory;
        private readonly Dictionary<string, Type> _discoveredFunctions;

        public FunctionDiscoveryService()
        {
            _functionsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Functions");
            _discoveredFunctions = new Dictionary<string, Type>();
        }

        public async Task<List<IFunction>> DiscoverFunctionsAsync()
        {
            var functions = new List<IFunction>();
            
            // Discover functions from current assembly
            await DiscoverFromAssemblyAsync(Assembly.GetExecutingAssembly(), functions);
            
            // Discover functions from external assemblies in Functions directory
            if (Directory.Exists(_functionsDirectory))
            {
                await DiscoverFromDirectoryAsync(_functionsDirectory, functions);
            }

            return functions;
        }

        public async Task<IFunction?> LoadFunctionAsync(string functionName)
        {
            if (_discoveredFunctions.TryGetValue(functionName, out var functionType))
            {
                return await CreateFunctionInstanceAsync(functionType);
            }

            // If not in cache, try to discover it
            await DiscoverFunctionsAsync();
            
            if (_discoveredFunctions.TryGetValue(functionName, out functionType))
            {
                return await CreateFunctionInstanceAsync(functionType);
            }

            return null;
        }

        private async Task DiscoverFromAssemblyAsync(Assembly assembly, List<IFunction> functions)
        {
            var functionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IFunction).IsAssignableFrom(t))
                .ToList();

            foreach (var type in functionTypes)
            {
                try
                {
                    var functionAttribute = type.GetCustomAttribute<FunctionAttribute>();
                    if (functionAttribute != null && functionAttribute.IsEnabled)
                    {
                        var instance = await CreateFunctionInstanceAsync(type);
                        if (instance != null)
                        {
                            functions.Add(instance);
                            _discoveredFunctions[functionAttribute.Name] = type;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue with other functions
                    Console.WriteLine($"Error loading function from type {type.Name}: {ex.Message}");
                }
            }
        }

        private async Task DiscoverFromDirectoryAsync(string directory, List<IFunction> functions)
        {
            try
            {
                var assemblyFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);
                
                foreach (var assemblyFile in assemblyFiles)
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(assemblyFile);
                        await DiscoverFromAssemblyAsync(assembly, functions);
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue with other assemblies
                        Console.WriteLine($"Error loading assembly {assemblyFile}: {ex.Message}");
                    }
                }

                // Also look for .cs files that can be compiled dynamically (future enhancement)
                var sourceFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
                if (sourceFiles.Length > 0)
                {
                    // For now, just log that source files were found
                    // Future enhancement: Compile and load .cs files dynamically
                    Console.WriteLine($"Found {sourceFiles.Length} source files in Functions directory. Dynamic compilation not yet implemented.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering functions from directory {directory}: {ex.Message}");
            }
        }

        private async Task<IFunction?> CreateFunctionInstanceAsync(Type type)
        {
            try
            {
                // Try to create instance with parameterless constructor
                var instance = Activator.CreateInstance(type) as IFunction;
                
                // If the function needs async initialization, we could add an interface for that
                // For now, just return the instance
                return await Task.FromResult(instance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating instance of function type {type.Name}: {ex.Message}");
                return null;
            }
        }
    }
}
