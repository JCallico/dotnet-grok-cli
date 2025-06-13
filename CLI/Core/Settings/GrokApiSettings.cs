namespace GrokCLI.Core.Settings
{
    public class GrokApiSettings
    {
        public string BaseUrl { get; set; } = "https://api.x.ai/v1";
        public string Model { get; set; } = "grok-3";
        public int MaxTokens { get; set; } = 4000;
        public double Temperature { get; set; } = 0.7;
    }
}
