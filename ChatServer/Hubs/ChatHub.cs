using ChatCommon;
using ChatServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.Core.Hubs
{
    public class ChatHub : Hub
    {
        private readonly FirebaseMessagingService _firebaseMessaging;
        private readonly DeviceTokenStore _deviceTokenStore;
        private readonly MessageStore _messageStore;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            FirebaseMessagingService firebaseMessaging, 
            DeviceTokenStore deviceTokenStore,
            MessageStore messageStore,
            ILogger<ChatHub> logger)
        {
            _firebaseMessaging = firebaseMessaging;
            _deviceTokenStore = deviceTokenStore;
            _messageStore = messageStore;
            _logger = logger;
        }

        public async Task SendMessage(ChatMessage message)
        {
            Console.WriteLine($"Received message from {message.UserFrom} to {message.UserTo}: {message.Id} {message.ContentType} {message.ContentName}");
            _messageStore.AddMessage(message);

            await Clients.All.SendAsync("ReceiveMessage", message);

            // Send push notification to offline/background users
            await SendPushNotificationAsync(message);
        }

        public async Task<bool> SendMessage2(ChatMessage message)
        {
            Console.WriteLine($"Received message from {message.UserFrom} to {message.UserTo}: {message.Id} {message.ContentType} {message.ContentName}");
            _messageStore.AddMessage(message);

            await Clients.All.SendAsync("ReceiveMessage", message);
            
            // Send push notification
            await SendPushNotificationAsync(message);
            
            return true;
        }

        public Task<string> Ping()
        {
            Console.WriteLine($"Ping received from connection: {Context.ConnectionId}");
            return Task.FromResult("Pong");
        }

        public Task<bool> RegisterDeviceToken(string deviceId, string deviceToken, string platform = "Android")
        {
            _deviceTokenStore.RegisterDevice(deviceId, deviceToken, Context.ConnectionId, platform);
            _logger.LogInformation($"Device token registered for {deviceId}");
            return Task.FromResult(true);
        }

        public Task UnregisterDeviceToken(string userName, string deviceToken)
        {
            _deviceTokenStore.UnregisterDevice(userName, deviceToken);
            _logger.LogInformation($"Device token unregistered for {userName}");
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }

        public Task<IEnumerable<ChatMessage>> ListMessages(DateTime date, string user)
        {
            return Task.FromResult(_messageStore.ListMessages(date, user));
        }

        public Task<IEnumerable<ChatMessage>> ListMessagesAndroid(string date, string user)
        {
            DateTime dateTime = DateTime.Parse(date);
            return ListMessages(dateTime, user);
        }

        public Task<ChatMessagesChunk> ListMessagesNext(string lastId, string user)
        {
            return Task.FromResult(_messageStore.ListMessagesNext(lastId, user));
        }

        private async Task SendPushNotificationAsync(ChatMessage message)
        {
            try
            {
                // Get message preview
                string messagePreview = message.ContentType switch
                {
                    ChatMessageContentType.Text => message.Message.Length > 50 
                        ? message.Message.Substring(0, 50) + "..." 
                        : message.Message,
                    ChatMessageContentType.Image => "Image",
                    ChatMessageContentType.File => $"File {message.ContentName}",
                    ChatMessageContentType.Emogi => "Emoji",
                    _ => "New message"
                };

                var data = new Dictionary<string, string>
                {
                    { "messageId", message.Id },
                    { "userFrom", message.UserFrom },
                    { "contentType", ((int)message.ContentType).ToString() },
                    { "timestamp", message.Timestamp.ToString("O") }
                };

                // Send to specific user if UserTo is specified
                if (!string.IsNullOrEmpty(message.UserTo))
                {
                    var tokens = _deviceTokenStore.GetDeviceTokens(message.UserTo);
                    if (tokens.Any())
                    {
                        await _firebaseMessaging.SendMulticastNotificationAsync(
                            tokens,
                            $"New message from {message.UserFrom}",
                            messagePreview,
                            data
                        );
                    }
                }
                else
                {
                    // Broadcast to all devices except sender
                    var allTokens = _deviceTokenStore.GetAllDeviceTokens();
                    var senderTokens = _deviceTokenStore.GetDeviceTokens(message.UserFrom);
                    var recipientTokens = allTokens.Except(senderTokens).ToList();

                    if (recipientTokens.Any())
                    {
                        foreach(var token in recipientTokens)
                        {
                            await _firebaseMessaging.SendNotificationAsync(token, $"New message from {message.UserFrom}", messagePreview, data);
                        }
                        /*await _firebaseMessaging.SendMulticastNotificationAsync(
                            recipientTokens,
                            $"New message from {message.UserFrom}",
                            messagePreview,
                            data
                        );*/
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification");
            }
        }
    }
}