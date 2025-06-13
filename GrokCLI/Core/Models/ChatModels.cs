namespace GrokCLI.Core.Models
{
    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }

        public Message(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    public class ChatMessage
    {
        public string Id { get; set; }
        public List<Message> Messages { get; set; }
        public DateTime Timestamp { get; set; }

        public ChatMessage()
        {
            Id = Guid.NewGuid().ToString();
            Messages = new List<Message>();
            Timestamp = DateTime.Now;
        }
    }

    public class ChatHistory
    {
        public List<ChatMessage> Conversations { get; set; } = new List<ChatMessage>();
    }
}
