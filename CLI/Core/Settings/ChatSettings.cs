namespace GrokCLI.Core.Settings
{
    public class ChatSettings
    {
        public int MaxHistoryLength { get; set; } = 50;
        public bool AutoSaveHistory { get; set; } = true;
        public string LogsDirectory { get; set; } = "logs";
        public string HistoryFilePrefix { get; set; } = "chat_history";
    }
}
