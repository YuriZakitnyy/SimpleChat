using CharCommon;
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.Core.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly object s_lock = new();
        private const int MaxMessages = 1000;
        private static readonly List<ChatMessage> s_messages = new();

        public async Task SendMessage(ChatMessage message)
        {
            lock (s_lock)
            {
                Console.WriteLine($"Received message from {message.UserFrom} to {message.UserTo}: {message.Id} {message.ContentType} {message.ContentName}");
                s_messages.Add(message);
                if (s_messages.Count > MaxMessages)
                {
                    s_messages.RemoveRange(0, s_messages.Count - MaxMessages);
                }
            }

            await Clients.All.SendAsync("ReceiveMessage", message);
        }

        public Task<string> Ping()
        {
            Console.WriteLine($"Ping received from connection: {Context.ConnectionId}");
            return Task.FromResult("Pong");
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public Task<IEnumerable<ChatMessage>> ListMessages(DateTime date, string user)
        {
            ChatMessage[] snapshot;
            lock (s_lock)
            {
                snapshot = s_messages
                    .Where(m => m.Timestamp >= date
                                && (string.IsNullOrEmpty(user)
                                    || m.UserFrom == user
                                    || m.UserTo == user))
                    .ToArray();
            }

            return Task.FromResult<IEnumerable<ChatMessage>>(snapshot);
        }
    }
}