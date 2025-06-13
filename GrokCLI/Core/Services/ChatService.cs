using GrokCLI.Core.Models;
using GrokCLI.Core.Settings;
using Newtonsoft.Json;

namespace GrokCLI.Core.Services
{
    public class ChatService
    {
        private readonly ChatSettings _settings;
        private readonly string _historyFilePath;
        private readonly DateTime _sessionStartTime;
        public ChatHistory History { get; private set; }

        public ChatService(ChatSettings settings)
        {
            _settings = settings;
            _sessionStartTime = DateTime.Now;
            
            // Create logs directory if it doesn't exist
            if (!Directory.Exists(_settings.LogsDirectory))
            {
                Directory.CreateDirectory(_settings.LogsDirectory);
            }
            
            // Generate timestamped filename
            var timestamp = _sessionStartTime.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"{_settings.HistoryFilePrefix}_{timestamp}.json";
            _historyFilePath = Path.Combine(_settings.LogsDirectory, filename);
            
            History = LoadHistory();
        }

        public void AddUserMessage(string userInput)
        {
            var message = new Message("user", userInput);
            AddMessageToCurrentChat(message);
            SaveHistory();
        }

        public void AddAssistantMessage(string assistantResponse)
        {
            var message = new Message("assistant", assistantResponse);
            AddMessageToCurrentChat(message);
            SaveHistory();
        }

        public void StartNewChat()
        {
            var newChat = new ChatMessage();
            History.Conversations.Add(newChat);
            SaveHistory();
        }

        public ChatMessage GetCurrentChat()
        {
            if (History.Conversations.Count == 0)
            {
                StartNewChat();
            }
            return History.Conversations.Last();
        }

        private void AddMessageToCurrentChat(Message message)
        {
            var currentChat = GetCurrentChat();
            currentChat.Messages.Add(message);
            
            // Maintain history length limit
            if (History.Conversations.Count > _settings.MaxHistoryLength)
            {
                History.Conversations.RemoveAt(0);
            }
        }

        public void SaveHistory()
        {
            if (_settings.AutoSaveHistory)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(History, Formatting.Indented);
                    File.WriteAllText(_historyFilePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to save chat history: {ex.Message}");
                }
            }
        }

        public ChatHistory LoadHistory()
        {
            if (File.Exists(_historyFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_historyFilePath);
                    return JsonConvert.DeserializeObject<ChatHistory>(json) ?? new ChatHistory();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load chat history: {ex.Message}");
                }
            }
            return new ChatHistory();
        }

        public string GetHistoryFilePath() => _historyFilePath;
        public DateTime GetSessionStartTime() => _sessionStartTime;
    }
}
