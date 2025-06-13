using GrokCLI.Core.Models;
using GrokCLI.Core.Settings;
using GrokCLI.Core.Services;
using GrokCLI.Core.Handlers;
using GrokCLI.Core.Infrastructure.Http;
using GrokCLI.Core.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<GrokCLI.Core.Settings.GrokApiSettings>()
    .AddEnvironmentVariables()
    .Build();

var grokApiKey = Environment.GetEnvironmentVariable("GROK_API_KEY")
    ?? config["GrokApi:ApiKey"];

if (string.IsNullOrWhiteSpace(grokApiKey))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Grok API key not found. Please add it using the .NET Secret Manager:");
    Console.WriteLine();
    Console.WriteLine("  dotnet user-secrets init");
    Console.WriteLine("  dotnet user-secrets set GrokApi:ApiKey <your-grok-api-key>");
    Console.WriteLine();
    Console.WriteLine("Or set the GROK_API_KEY environment variable.");
    Console.ResetColor();
    return;
}

var provider = ServiceConfigurator.Configure();
var grokClient = provider.GetService(typeof(GrokApiClient)) as GrokApiClient;
var chatService = provider.GetService(typeof(ChatService)) as ChatService;
var functionHandler = provider.GetService(typeof(IFunctionHandler)) as IFunctionHandler;

if (grokClient == null || chatService == null || functionHandler == null)
{
    Console.WriteLine("Failed to initialize services. Exiting.");
    return;
}

// Initialize the function handler to discover and load all functions
await functionHandler.InitializeAsync();

Console.WriteLine("🤖 Grok CLI Chat Agent with Function Calling");
Console.WriteLine("Type 'exit' to quit, 'functions' to see available functions.\n");
Console.WriteLine($"📝 Chat history will be saved to: {chatService.GetHistoryFilePath()}");
Console.WriteLine($"🕒 Session started at: {chatService.GetSessionStartTime():yyyy-MM-dd HH:mm:ss}\n");

// Get available tools for function calling
var availableFunctions = functionHandler.GetAvailableFunctions();
var tools = availableFunctions.Select(f => new Tool
{
    Type = "function",
    Function = f
}).ToList();

Console.WriteLine($"🔧 {tools.Count} functions available: {string.Join(", ", availableFunctions.Select(f => f.Name))}\n");

// Start a new chat session
chatService.StartNewChat();

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Trim().ToLower() == "exit") break;
    
    if (input.Trim().ToLower() == "functions")
    {
        Console.WriteLine("\n🔧 Available Functions:");
        foreach (var func in availableFunctions)
        {
            Console.WriteLine($"  • {func.Name}: {func.Description}");
            if (func.Parameters.Properties.Any())
            {
                Console.WriteLine($"    Parameters: {string.Join(", ", func.Parameters.Properties.Keys)}");
                var requiredParams = func.Parameters.Required;
                if (requiredParams.Any())
                {
                    Console.WriteLine($"    Required: {string.Join(", ", requiredParams)}");
                }
            }
        }
        Console.WriteLine();
        continue;
    }
    
    // Save user message immediately
    chatService.AddUserMessage(input);
    
    Console.Write("🤖 Grok: ");
    Console.ForegroundColor = ConsoleColor.Cyan;
    
    await ProcessMessageWithFunctions(grokClient, chatService, functionHandler, tools);
    
    Console.ResetColor();
    Console.WriteLine("\n");
}

Console.WriteLine("Chat session ended. History saved to logs folder.");
Console.WriteLine($"📁 Final history file: {chatService.GetHistoryFilePath()}");

async Task ProcessMessageWithFunctions(GrokApiClient client, ChatService service, IFunctionHandler handler, List<Tool> availableTools)
{
    var currentMessages = service.GetCurrentChat().Messages;
    var fullResponse = new StringBuilder();
    
    try
    {
        // Try function calling with streaming
        var functionResponse = await client.SendMessageWithFunctionsAsync(currentMessages, availableTools);
        
        if (functionResponse.choices.Count > 0)
        {
            var choice = functionResponse.choices[0];
            var message = choice.message;
            
            // Check if the model wants to call functions
            if (message.tool_calls != null && message.tool_calls.Count > 0)
            {
                // Display assistant's thinking if any
                if (!string.IsNullOrEmpty(message.content))
                {
                    Console.Write(message.content);
                    fullResponse.Append(message.content);
                }
                
                Console.WriteLine($"\n🔧 Calling {message.tool_calls.Count} function(s):");
                
                // Add assistant message with tool calls to conversation
                var assistantMessage = new FunctionCallMessage("assistant", message.content ?? "", message.tool_calls);
                service.GetCurrentChat().Messages.Add(assistantMessage);
                
                // Execute each function call
                foreach (var toolCall in message.tool_calls)
                {
                    Console.WriteLine($"   📞 {toolCall.Function.Name}");
                    
                    try
                    {
                        var functionResult = await handler.ExecuteAsync(toolCall.Function.Name, toolCall.Function.Arguments);
                        
                        // Add function result to conversation
                        var toolMessage = new ToolMessage(toolCall.Id, functionResult);
                        service.GetCurrentChat().Messages.Add(toolMessage);
                        
                        Console.WriteLine($"   ✅ Completed");
                    }
                    catch (Exception ex)
                    {
                        var errorResult = $"Error executing function: {ex.Message}";
                        var toolMessage = new ToolMessage(toolCall.Id, errorResult);
                        service.GetCurrentChat().Messages.Add(toolMessage);
                        
                        Console.WriteLine($"   ❌ Error: {errorResult}");
                    }
                }
                
                // Get the final response after function execution
                Console.WriteLine("\n🧠 Processing function results...");
                
                try
                {
                    var finalResponse = await client.SendMessageWithFunctionsAsync(service.GetCurrentChat().Messages, availableTools);
                    if (finalResponse.choices.Count > 0 && !string.IsNullOrEmpty(finalResponse.choices[0].message.content))
                    {
                        var finalContent = finalResponse.choices[0].message.content;
                        Console.Write(finalContent);
                        fullResponse.Append($"\n{finalContent}");
                        
                        // Add final assistant response
                        service.GetCurrentChat().Messages.Add(new Message("assistant", finalContent ?? ""));
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No response after function execution.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error getting final response: {ex.Message}");
                    var fallbackMessage = "I executed the requested functions, but encountered an error generating the final response.";
                    Console.Write(fallbackMessage);
                    fullResponse.Append($"\n{fallbackMessage}");
                    service.GetCurrentChat().Messages.Add(new Message("assistant", fallbackMessage));
                }
            }
            else
            {
                // No function calls, just regular response
                if (!string.IsNullOrEmpty(message.content))
                {
                    Console.Write(message.content);
                    fullResponse.Append(message.content);
                    
                    service.GetCurrentChat().Messages.Add(new Message("assistant", message.content));
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Function calling failed: {ex.Message}");
        Console.WriteLine("🔄 Falling back to regular streaming...");
        
        // Fallback to regular streaming
        await ProcessRegularMessage(client, service);
    }
    
    // Save the conversation state
    service.SaveHistory();
}

async Task ProcessRegularMessage(GrokApiClient client, ChatService service)
{
    var fullResponse = new StringBuilder();
    var hasStreamedContent = false;
    
    try
    {
        await foreach (var chunk in client.SendMessageStreamAsync(service.GetCurrentChat().Messages))
        {
            Console.Write(chunk);
            fullResponse.Append(chunk);
            hasStreamedContent = true;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Streaming failed: {ex.Message}");
        Console.WriteLine("🔄 Falling back to regular API call...");
        
        // Final fallback to regular API call
        var response = await client.SendMessageAsync(service.GetCurrentChat().Messages);
        Console.Write(response);
        fullResponse.Append(response);
        hasStreamedContent = true;
    }
    
    if (hasStreamedContent && fullResponse.Length > 0)
    {
        service.GetCurrentChat().Messages.Add(new Message("assistant", fullResponse.ToString()));
    }
}
