using Newtonsoft.Json;

namespace GrokCLI.Core.Models
{
    public class FunctionDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("parameters")]
        public FunctionParameters Parameters { get; set; } = new();
    }

    public class FunctionParameters
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "object";

        [JsonProperty("properties")]
        public Dictionary<string, FunctionProperty> Properties { get; set; } = new();

        [JsonProperty("required")]
        public List<string> Required { get; set; } = new();
    }

    public class FunctionProperty
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("enum", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Enum { get; set; }
    }

    public class FunctionCall
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("arguments")]
        public string Arguments { get; set; } = string.Empty;
    }

    public class ToolCall
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = "function";

        [JsonProperty("function")]
        public FunctionCall Function { get; set; } = new();
    }

    public class Tool
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "function";

        [JsonProperty("function")]
        public FunctionDefinition Function { get; set; } = new();
    }

    public class FunctionCallMessage : Message
    {
        [JsonProperty("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }

        public FunctionCallMessage(string role, string content) : base(role, content)
        {
        }

        public FunctionCallMessage(string role, string content, List<ToolCall> toolCalls) : base(role, content)
        {
            ToolCalls = toolCalls;
        }
    }

    public class ToolMessage : Message
    {
        [JsonProperty("tool_call_id")]
        public string ToolCallId { get; set; } = string.Empty;

        public ToolMessage(string toolCallId, string content) : base("tool", content)
        {
            ToolCallId = toolCallId;
        }
    }
}
