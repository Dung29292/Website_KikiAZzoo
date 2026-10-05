using System.Collections.Generic;

namespace KikiAZZoo.Models
{
    public class ChatRequest
    {
        public string Message { get; set; }
        public List<ChatMessage> History { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; } // "user" or "model"
        public string Content { get; set; }
    }
}
