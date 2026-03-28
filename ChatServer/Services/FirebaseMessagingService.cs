using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace ChatServer.Services
{
    public class FirebaseMessagingService
    {
        private readonly ILogger<FirebaseMessagingService> _logger;
        private readonly FirebaseApp _firebaseApp;

        public FirebaseMessagingService(ILogger<FirebaseMessagingService> logger, IConfiguration configuration)
        {
            _logger = logger;

            try
            {
                var credentialPath = configuration["Firebase:CredentialPath"];
                
                if (!string.IsNullOrEmpty(credentialPath) && File.Exists(credentialPath))
                {
                    _firebaseApp = FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credentialPath)
                    });
                    _logger.LogInformation("Firebase initialized successfully");
                }
                else
                {
                    _logger.LogWarning("Firebase credential file not found. Push notifications will be disabled.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase");
            }
        }

        public async Task<string?> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
        {
            if (_firebaseApp == null)
            {
                _logger.LogWarning("Firebase not initialized. Cannot send notification.");
                return null;
            }

            try
            {
                var message = new Message
                {
                    Token = deviceToken,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "chat_messages"
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation($"Successfully sent message: {response}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to token: {deviceToken}");
                return null;
            }
        }

        public async Task<BatchResponse?> SendMulticastNotificationAsync(
            List<string> deviceTokens, 
            string title, 
            string body, 
            Dictionary<string, string>? data = null)
        {
            if (_firebaseApp == null || deviceTokens == null || !deviceTokens.Any())
            {
                return null;
            }

            try
            {
                var message = new MulticastMessage
                {
                    Tokens = deviceTokens,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "chat_messages"
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                _logger.LogInformation($"Successfully sent {response.SuccessCount} messages out of {deviceTokens.Count}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send multicast notification");
                return null;
            }
        }
    }
}