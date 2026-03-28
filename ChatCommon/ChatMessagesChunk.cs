using System.Collections.Generic;

namespace ChatCommon;

public class ChatMessagesChunk
{
    public IEnumerable<ChatMessage>? Messages { get; set; }
    public string? Next { get; set; }
}
