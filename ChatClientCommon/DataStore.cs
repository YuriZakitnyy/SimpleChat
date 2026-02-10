using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CharCommon;

namespace ChatClientCommon
{
    public class DataStore
    {
        const string MessagesFileName = "messages.dat";
        const string ConfigFileName = "config.dat";
        const string CommentsFileName = "comments.dat";
        private string messagesFilePath;
        private string configFilePath;
        private string commentsFilePath;

        public DataStore()
        {
            try
            {
                var path = CommonConstants.OutputDirectory;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                messagesFilePath = Path.Combine(path, MessagesFileName);
                configFilePath = Path.Combine(path, ConfigFileName);
                commentsFilePath = Path.Combine(path, CommentsFileName);
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
        }

        public List<ChatMessage> LoadMessages()
        {
            Logger.Debug(this, "Loading messages");
            try
            {
                if (!File.Exists(messagesFilePath))
                {
                    return new List<ChatMessage>();
                }
                var lines = File.ReadAllLines(messagesFilePath);
                return lines.Select(line =>
                {
                    var jsonBytes = Convert.FromBase64String(line);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var msg = JsonSerializer.Deserialize<ChatMessage>(jsonBytes, options);
                    return msg;
                }).Where(m => m != null).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
            return new List<ChatMessage>();
        }

        public void AddMessage(ChatMessage message)
        {
            Logger.Debug(this, "Add message {0}", message.Id);
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var bytes = JsonSerializer.Serialize(message, options);
                var msg = Encoding.UTF8.GetBytes(bytes);
                var text = Convert.ToBase64String(msg);
                File.AppendAllLines(messagesFilePath, new string[] { text });
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
        }

        public ChatSettings LoadSettings()
        {
            ChatSettings result = null;
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    result = JsonSerializer.Deserialize<ChatSettings>(json, options);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
            return result ?? new ChatSettings
            {
                BackendUrl = ChatSettings.DefaultAddresses.First(),
                UserName = null
            };
        }

        public void SaveSettings(ChatSettings settings)
        {
            try
            {
                Logger.Debug(this, "Saving settings");
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(configFilePath, json);

            }
            catch(Exception ex)
            {
                Logger.Error(this, ex);
            }
        }

        public string LoadComments()
        {
            try
            {
                Logger.Debug(this, "Loading comments");
                if (File.Exists(commentsFilePath))
                {
                    return File.ReadAllText(commentsFilePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
            return string.Empty;
        }

        public void SaveComments(string data)
        {
            try
            {
                Logger.Debug(this, "Saving comments");
                File.WriteAllText(commentsFilePath, data);
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
        }
    }
}
