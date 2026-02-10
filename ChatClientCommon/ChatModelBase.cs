using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CharCommon;
using ChatClientCommon.UI;

namespace ChatClientCommon
{
    public abstract class ChatModelBase : BindingItem
    {
        private HubClient _hubClient;
        private string _backendUrl;
        private string _userName;
        private bool _connecing;
        private bool _connected;
        private string _error;
        private string _messageText;
        private string _contentName;
        private ChatMessageContentType _messageContentType;
        private string _messageId;

        protected DataStore _store;

        public Action Loaded { get; set; }
        public Action MessageArrived { get; set; }

        public ObservableCollection<ChatMessageModel> Messages { get; } = new ObservableCollection<ChatMessageModel>();
        public ObservableCollection<string> Addresses { get; } = new ObservableCollection<string>();

        public string BackendUrl
        {
            get { return _backendUrl; }
            set { SetField(ref _backendUrl, value, nameof(BackendUrl)); }
        }

        public string UserName
        {
            get { return _userName; }
            set { SetField(ref _userName, value, nameof(UserName)); }
        }

        public bool Connecting
        {
            get { return _connecing; }
            set { SetField(ref _connecing, value, nameof(Connecting)); }
        }

        public bool Connected
        {
            get { return _connected; }
            set { SetField(ref _connected, value, nameof(Connected)); }
        }

        public string Error
        {
            get { return _error; }
            set { SetField(ref _error, value, nameof(Error)); }
        }

        public string MessageText
        {
            get { return _messageText; }
            set { SetField(ref _messageText, value, nameof(MessageText)); }
        }

        public string ContentName
        {
            get { return _contentName; }
            set { SetField(ref _contentName, value, nameof(ContentName)); }
        }

        public string MessageId
        {
            get { return _messageId; }
            set { SetField(ref _messageId, value, nameof(MessageId)); }
        }

        public ChatMessageContentType MessageContentType
        {
            get { return _messageContentType; }
            set { SetField(ref _messageContentType, value, nameof(MessageContentType)); }
        }

        public RelayCommand ConnectCommand { get; set; }
        public RelayCommand DisconnectCommand { get; set; }
        public RelayCommand SendCommand { get; set; }
        public ChatSettings Settings { get; set; }

        public ChatModelBase()
        {
            try
            {
                _hubClient = new HubClient();
                _store = new DataStore();
                Settings = _store.LoadSettings();
                BackendUrl = Settings.BackendUrl;
                UserName = Settings.UserName;

                ConnectCommand = new RelayCommand(s => ConnectAsync(), "Connect", true);
                DisconnectCommand = new RelayCommand(s => DisconnectAsync(), "Disconnect", true);
                SendCommand = new RelayCommand(s => SendAsync(), "Send", true);
                ConnectCommand.Enabled = true;
                DisconnectCommand.Enabled = false;
                SendCommand.Enabled = false;
                ChatSettings.DefaultAddresses.ForEach(addr => Addresses.Add(addr));
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        public async void SendFiles(string[] files)
        {
            if (SendCommand.Enabled)
            {
                try
                {
                    foreach (var file in files)
                    {
                        var bytes = File.ReadAllBytes(file);
                        if (bytes.Length < 3 * CommonConstants.MaxMessageBytes / 4)
                        {
                            await _hubClient.SendMessageAsync(new ChatMessage
                            {
                                ContentType = ChatMessageContentType.File,
                                Message = ByteStringConverter.ToZ85String(bytes),
                                Id = Guid.NewGuid().ToString(),
                                UserFrom = UserName,
                                ContentName = Path.GetFileName(file)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(this, ex);
                    Error = ex.Message;
                }
            }
        }

        public async void SendImage(string encoded)
        {
            if (SendCommand.Enabled)
            {
                try
                {
                    if (encoded.Length < 3 * CommonConstants.MaxMessageBytes / 4)
                    {
                        await _hubClient.SendMessageAsync(new ChatMessage
                        {
                            ContentType = ChatMessageContentType.Image,
                            Message = encoded,
                            Id = Guid.NewGuid().ToString(),
                            UserFrom = UserName,
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(this, ex);
                    Error = ex.Message;
                }
            }
        }

        private async void ConnectAsync()
        {
            _store.SaveSettings(new ChatSettings
            {
                BackendUrl = BackendUrl,
                UserName = UserName
            });

            _hubClient.ReceivedMessage = (message) =>
            {
                RunOnMainThread(() =>
                {
                    try
                    {
                        _store.AddMessage(message);
                        var model = CreateMessageModel(message);
                        Messages.Add(model);
                        Loaded();
                        if (model.IsReceived)
                        {
                            MessageArrived();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(this, ex);
                    }
                });
            };

            _hubClient.Reconnecting = () =>
            {
                RunOnMainThread(() =>
                {
                    Logger.Messg(this, "Reconnecting...");
                    Connected = false;
                    Connecting = true;
                    ConnectCommand.Enabled = false;
                    DisconnectCommand.Enabled = false;
                    SendCommand.Enabled = false;
                });
            };

            _hubClient.Reconnected = () =>
            {
                RunOnMainThread(async () =>
                {
                    Logger.Messg(this, "Reconnected...");
                    Connected = true;
                    Connecting = false;
                    ConnectCommand.Enabled = false;
                    DisconnectCommand.Enabled = true;
                    SendCommand.Enabled = true;
                    await LoadMessages(false);
                });
            };

            _hubClient.ConnectionClosed = () =>
            {
                RunOnMainThread(() =>
                {
                    Logger.Messg(this, "Disconnected");
                    Connected = false;
                    Connecting = false;
                    ConnectCommand.Enabled = true;
                    DisconnectCommand.Enabled = false;
                    SendCommand.Enabled = false;
                });
            };
            
            try
            {
                await _hubClient.ConnectAsync(BackendUrl);
                ConnectCommand.Enabled = false;
                DisconnectCommand.Enabled = true;
                SendCommand.Enabled = true;

                await LoadMessages(true);
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
                Error = ex.Message;
            }
        }

        private async void DisconnectAsync()
        {
            try
            {
                await _hubClient.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
                Error = ex.Message;
            }
            Connected = false;
            Connecting = false;
            ConnectCommand.Enabled = true;
            DisconnectCommand.Enabled = false;
            SendCommand.Enabled = true;
        }

        private async void SendAsync()
        {
            if (SendCommand.Enabled)
            {
                try
                {
                    await _hubClient.SendMessageAsync(new ChatMessage
                    {
                        ContentType = MessageContentType,
                        Message = MessageText,
                        Id = MessageId ?? Guid.NewGuid().ToString(),
                        UserFrom = UserName,
                        ContentName = ContentName
                    });
                    MessageId = Guid.NewGuid().ToString();
                    MessageText = string.Empty;
                    MessageContentType = ChatMessageContentType.Text;
                    ContentName = string.Empty;
                }
                catch (Exception ex)
                {
                    Logger.Error(this, ex);
                    Error = ex.Message;
                }
            }
        }

        private async Task LoadMessages(bool withStored)
        {
            try
            {
                if (withStored)
                {
                    var stored = _store.LoadMessages();
                    AddMessages(stored);
                }
                var last = Messages.OrderBy(it => it.Time).LastOrDefault();
                var newMesasges = await _hubClient.ListMessagesAsync(last == null ? DateTime.UtcNow.AddYears(-1) : last.Time, UserName);
                AddMessages(newMesasges, true);
                Loaded();
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
                Error = ex.Message;
            }
        }

        private void AddMessages(IEnumerable<ChatMessage> messages, bool store = false)
        {
            foreach (var message in messages)
            {
                if (!Messages.Any(it => it.Message.Id == message.Id))
                {
                    if (store)
                    {
                        _store.AddMessage(message);
                    }
                    Messages.Add(CreateMessageModel(message));
                }
            }
        }

        protected virtual ChatMessageModel CreateMessageModel(ChatMessage message)
        {
            return new ChatMessageModel(message, message.UserFrom != UserName);
        }

        public abstract void RunOnMainThread(Action act);
    }
}
