using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Scribe.Types
{
    public class Message
    {
        public string Role { get; }
        public string Content { get; }

        public Message(string role, string content)
        {
            if (role != "system" && role != "user" && role != "assistant")
            {
                throw new ArgumentException("Role must be 'system', 'user', or 'assistant'.");
            }

            Role = role;
            Content = content;
        }
    }
}
