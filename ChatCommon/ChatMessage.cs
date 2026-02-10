using System;
namespace CharCommon;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserFrom { get; set; } = string.Empty;
    public string UserTo { get; set; } = string.Empty;
    public ChatMessageContentType ContentType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ContentName { get; set; } = string.Empty;
}