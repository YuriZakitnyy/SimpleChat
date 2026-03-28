using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ChatCommon;

namespace ChatServer.Services
{
    public class MessageStore
    {
        private readonly string _storageDirectory;
        private readonly ILogger<MessageStore> _logger;
        private readonly object _fileLock = new();
        private readonly List<ChatMessage> _messageCache = new();
        private const int MaxCacheSize = 10000;
        private const int MaxChunkSize = 50;

        public MessageStore(ILogger<MessageStore> logger, IConfiguration configuration)
        {
            _logger = logger;
            _storageDirectory = configuration["MessageStorage:Directory"] ?? "/var/data/messages";

            // Ensure directory exists
            if (!Directory.Exists(_storageDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_storageDirectory);
                    _logger.LogInformation($"Created message storage directory: {_storageDirectory}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create message storage directory: {_storageDirectory}");
                }
            }

            LoadMessages();
        }

        public void AddMessage(ChatMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.Id))
                return;

            lock (_messageCache)
            {
                if (_messageCache.Count >= MaxCacheSize)
                {
                    _messageCache.RemoveAt(0);
                }
                _messageCache.Add(message);
            }
            SaveMessage(message);
        }

        public IEnumerable<ChatMessage> ListMessages(DateTime date, string user)
        {
            ChatMessage[] snapshot;
            if (date.Kind == DateTimeKind.Local)
            {
                date = date.ToUniversalTime();
            }   
            lock (_messageCache)
            {
                snapshot = _messageCache
                    .Where(m => m.Timestamp >= date)
                    .ToArray();
            }

            return snapshot;
        }

        public ChatMessagesChunk ListMessagesNext(string? lastId, string user)
        {
            string? next = null;
            var snapshot = new List<ChatMessage>();
            bool add = false;
            DateTime minDate = DateTime.UtcNow.AddDays(-30);
            lock (_messageCache)
            {
                foreach (var message in _messageCache)
                {
                    if ((lastId != null && message.Id == lastId) ||
                        (lastId == null && message.Timestamp >= minDate))
                    {
                        add = true;
                    }
                    if (add)
                    {
                        if (MaxChunkSize == snapshot.Count)
                        {
                            next = message.Id;
                            break;
                        }
                        snapshot.Add(message);
                    }
                }
            }
            return new ChatMessagesChunk { Messages = snapshot, Next = next };
        }

        public int GetMessageCount()
        {
            return _messageCache.Count;
        }

        private void SaveMessage(ChatMessage message)
        {
            lock (_fileLock)
            {
                try
                {
                    var fileName = $"messages_{DateTime.UtcNow:yyyy-MM-dd}.json";
                    var messageFilePath = Path.Combine(_storageDirectory, fileName);

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNameCaseInsensitive = true
                    };

                    var json = JsonSerializer.Serialize(message, options);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    var text = Convert.ToBase64String(bytes);
                    File.AppendAllLines(messageFilePath, new string[] { text });

                    _logger.LogDebug($"Saved message {message.Id} to {messageFilePath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to save message {message.Id}");
                }
            }
        }

        private void LoadMessages()
        {
            lock (_fileLock)
            {
                try
                {
                    if (!Directory.Exists(_storageDirectory))
                        return;

                    var files = Directory.GetFiles(_storageDirectory, "*.json")
                        .Where(f => !f.EndsWith(".tmp"))
                        .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                        .ToList();

                    int loadedCount = 0;
                    foreach (var file in files)
                    {
                        try
                        {
                            var lines = File.ReadAllLines(file);
                            var messages = lines.Select(line =>
                            {
                                var jsonBytes = Convert.FromBase64String(line);
                                var options = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                };

                                var msg = JsonSerializer.Deserialize<ChatMessage>(jsonBytes, options);
                                return msg;
                            }).Where(m => m != null).ToList();

                            foreach (var message in messages)
                            {
                                if (message != null && !string.IsNullOrEmpty(message.Id))
                                {
                                    _messageCache.Add(message);
                                    if (_messageCache.Count >= MaxCacheSize)
                                    {
                                        _messageCache.RemoveAt(0);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to load message from {file}");
                        }
                    }

                    _logger.LogInformation($"Loaded {loadedCount} messages from {files.Count} files");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load messages");
                }
            }
        }

        public void DeleteOldFiles(int daysToKeep = 90)
        {
            lock (_fileLock)
            {
                try
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                    var files = Directory.GetFiles(_storageDirectory, "*.json")
                        .Where(f => !f.EndsWith(".tmp"))
                        .ToList();

                    int deletedCount = 0;
                    foreach (var file in files)
                    {
                        try
                        {
                            // Try to load message to check timestamp
                            var json = File.ReadAllText(file);
                            var message = JsonSerializer.Deserialize<ChatMessage>(json);
                            
                            if (message != null && message.Timestamp < cutoffDate)
                            {
                                File.Delete(file);
                                deletedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to process file {file} for cleanup");
                        }
                    }

                    if (deletedCount > 0)
                    {
                        _logger.LogInformation($"Deleted {deletedCount} old message files");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete old files");
                }
            }
        }
    }
}