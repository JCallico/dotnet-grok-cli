using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using GrokCLI.Core.Models;
using GrokCLI.Core.Settings;

namespace GrokCLI.Core.Infrastructure.Http
{
    public class GrokApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly GrokApiSettings _settings;
        private readonly string _apiKey;

        public GrokApiClient(HttpClient httpClient, GrokApiSettings settings, string apiKey)
        {
            _httpClient = httpClient;
            _settings = settings;
            _apiKey = apiKey;
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> SendMessageAsync(List<Message> messages)
        {
            var payload = new
            {
                model = _settings.Model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                max_tokens = _settings.MaxTokens,
                temperature = _settings.Temperature
            };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("chat/completions", content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("Empty response from Grok API");

            var result = JsonConvert.DeserializeObject<GrokApiResponse>(responseString);
            if (result == null || result.choices == null || result.choices.Count == 0)
                throw new Exception("Invalid response from Grok API");
            return result.choices[0].message.content;
        }

        public async IAsyncEnumerable<string> SendMessageStreamAsync(List<Message> messages)
        {
            var payload = new
            {
                model = _settings.Model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                max_tokens = _settings.MaxTokens,
                temperature = _settings.Temperature,
                stream = true
            };
            
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = content
            };
            
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Skip the "data: " prefix
                if (line.StartsWith("data: "))
                {
                    var jsonData = line.Substring(6);
                    
                    // Check for end of stream
                    if (jsonData == "[DONE]")
                        break;
                    
                    var contentChunk = ParseStreamChunk(jsonData);
                    if (!string.IsNullOrEmpty(contentChunk))
                    {
                        yield return contentChunk;
                    }
                }
            }
        }

        private string? ParseStreamChunk(string jsonData)
        {
            try
            {
                var streamResponse = JsonConvert.DeserializeObject<GrokStreamResponse>(jsonData);
                if (streamResponse?.choices != null && streamResponse.choices.Count > 0)
                {
                    var delta = streamResponse.choices[0].delta;
                    return delta?.content;
                }
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // Skip malformed JSON lines
            }
            return null;
        }

        public async Task<GrokFunctionCallResponse> SendMessageWithFunctionsAsync(List<Message> messages, List<Tool> tools)
        {
            var payload = new
            {
                model = _settings.Model,
                messages = messages.Select(SerializeMessage).ToList(),
                max_tokens = _settings.MaxTokens,
                temperature = _settings.Temperature,
                tools = tools.Select(t => new
                {
                    type = t.Type,
                    function = new
                    {
                        name = t.Function.Name,
                        description = t.Function.Description,
                        parameters = t.Function.Parameters
                    }
                }).ToList(),
                tool_choice = "auto"
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            
            // Enhanced debug logging for grok-3
            if (Environment.GetEnvironmentVariable("GROK_DEBUG") == "true")
            {
                Console.WriteLine($"🔍 Debug - Sending function calling request to grok-3:");
                Console.WriteLine($"   Model: {_settings.Model}");
                Console.WriteLine($"   Functions: {string.Join(", ", tools.Select(t => t.Function.Name))}");
                Console.WriteLine($"   Messages: {messages.Count}");
                Console.WriteLine($"🔍 Full payload:");
                Console.WriteLine(JsonConvert.SerializeObject(payload, Formatting.Indented));
            }
            
            var response = await _httpClient.PostAsync("chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🚨 Function calling API Error {response.StatusCode}: {errorContent}");
                
                // Parse error response for better error handling
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(errorContent);
                    var errorMessage = errorResponse?.error?.message?.ToString() ?? errorContent;
                    throw new Exception($"Grok API Error: {errorMessage}");
                }
                catch (JsonException)
                {
                    throw new Exception($"Function calling API Error {response.StatusCode}: {errorContent}");
                }
            }
            
            var responseString = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(responseString))
                throw new Exception("Empty response from Grok API");

            // Enhanced debug logging for response
            if (Environment.GetEnvironmentVariable("GROK_DEBUG") == "true")
            {
                Console.WriteLine($"🔍 Debug - Received response from grok-3: {responseString.Length} characters");
            }

            var result = JsonConvert.DeserializeObject<GrokFunctionCallResponse>(responseString);
            if (result == null || result.choices == null || result.choices.Count == 0)
                throw new Exception("Invalid response from Grok API");
                
            return result;
        }

        public async IAsyncEnumerable<GrokStreamChunk> SendMessageWithFunctionsStreamAsync(List<Message> messages, List<Tool> tools)
        {
            var payload = new
            {
                model = _settings.Model,
                messages = messages.Select(SerializeMessage).ToList(),
                max_tokens = _settings.MaxTokens,
                temperature = _settings.Temperature,
                tools = tools.Select(t => new
                {
                    type = t.Type,
                    function = new
                    {
                        name = t.Function.Name,
                        description = t.Function.Description,
                        parameters = t.Function.Parameters
                    }
                }).ToList(),
                tool_choice = "auto",
                stream = true
            };
            
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = content
            };
            
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                if (line.StartsWith("data: "))
                {
                    var jsonData = line.Substring(6);
                    
                    if (jsonData == "[DONE]")
                        break;
                    
                    var streamChunk = ParseFunctionStreamChunk(jsonData);
                    if (streamChunk != null)
                    {
                        yield return streamChunk;
                    }
                }
            }
        }

        private object SerializeMessage(Message message)
        {
            var baseObj = new Dictionary<string, object>
            {
                ["role"] = message.Role,
                ["content"] = message.Content ?? ""
            };

            if (message is FunctionCallMessage fcm && fcm.ToolCalls != null)
            {
                baseObj["tool_calls"] = fcm.ToolCalls.Select(tc => new
                {
                    id = tc.Id,
                    type = tc.Type,
                    function = new
                    {
                        name = tc.Function.Name,
                        arguments = tc.Function.Arguments
                    }
                }).ToList();
            }

            if (message is ToolMessage tm)
            {
                baseObj["tool_call_id"] = tm.ToolCallId;
            }

            return baseObj;
        }

        private GrokStreamChunk? ParseFunctionStreamChunk(string jsonData)
        {
            try
            {
                var streamResponse = JsonConvert.DeserializeObject<GrokFunctionStreamResponse>(jsonData);
                if (streamResponse?.choices != null && streamResponse.choices.Count > 0)
                {
                    var choice = streamResponse.choices[0];
                    return new GrokStreamChunk
                    {
                        Content = choice.delta?.content,
                        ToolCalls = choice.delta?.tool_calls,
                        FinishReason = choice.finish_reason
                    };
                }
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // Skip malformed JSON lines
            }
            return null;
        }

        // ...existing code...

        private class GrokApiResponse
        {
            public List<Choice> choices { get; set; } = new();
        }
        
        private class GrokStreamResponse
        {
            public List<StreamChoice> choices { get; set; } = new();
        }

        public class GrokFunctionCallResponse
        {
            public List<FunctionChoice> choices { get; set; } = new();
        }

        public class GrokFunctionStreamResponse
        {
            public List<FunctionStreamChoice> choices { get; set; } = new();
        }

        public class GrokStreamChunk
        {
            public string? Content { get; set; }
            public List<ToolCall>? ToolCalls { get; set; }
            public string? FinishReason { get; set; }
        }
        
        private class Choice
        {
            public ChoiceMessage message { get; set; } = new();
        }

        public class FunctionChoice
        {
            public FunctionChoiceMessage message { get; set; } = new();
            public string? finish_reason { get; set; }
        }

        public class FunctionChoiceMessage
        {
            public string? content { get; set; }
            public List<ToolCall>? tool_calls { get; set; }
        }

        public class FunctionStreamChoice
        {
            public FunctionDelta delta { get; set; } = new();
            public string? finish_reason { get; set; }
        }

        public class FunctionDelta
        {
            public string? content { get; set; }
            public List<ToolCall>? tool_calls { get; set; }
        }
        
        private class StreamChoice
        {
            public Delta delta { get; set; } = new();
        }
        
        private class ChoiceMessage
        {
            public string content { get; set; } = string.Empty;
        }
        
        private class Delta
        {
            public string? content { get; set; }
        }
    }
}
