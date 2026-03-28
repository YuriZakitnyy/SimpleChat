using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using ChatCommon;
using ChatClientCommon.UI;

namespace ChatClientCommon
{
    public class ChatMessageModel : BindingItem
    {
        private ChatMessage _message;
        private string _userName;
        private string _messageText;
        private DateTime _time;
        private BitmapImage _image;
        private bool _isSent;
        private bool _isReceived;
        private bool _isLink;
        private static readonly Regex UrlRegex = new Regex(
            @"(https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public ChatMessage Message => _message;

        public string UserName
        {
            get { return _userName; }
            set { SetField(ref _userName, value, nameof(UserName)); }
        }

        public string MessageText
        {
            get { return _messageText; }
            set
            {
                if (SetField(ref _messageText, value, nameof(MessageText)))
                {
                    CallPropertyChanged(nameof(IsLink));
                }
            }
        }

        public DateTime Time
        {
            get { return _time; }
            set { SetField(ref _time, value, nameof(Time)); }
        }

        public BitmapImage Image
        {
            get { return _image; }
            set
            {
                if (SetField(ref _image, value, nameof(Image)))
                {
                    CallPropertyChanged(nameof(IsImage));
                }
            }
        }

        public bool IsImage
        {
            get
            {
                return Message.ContentType == ChatMessageContentType.Image;
            }
        }

        public bool IsEmoji
        {
            get
            {
                return Message.ContentType == ChatMessageContentType.Emogi;
            }
        }

        public bool IsText
        {
            get
            {
                return Message.ContentType == ChatMessageContentType.Text && !IsLink;
            }
        }

        public bool IsFile
        {
            get
            {
                return Message.ContentType == ChatMessageContentType.File;
            }
        }

        public bool IsLink
        {
            get
            {
                return _isLink;
            }
        }

        public string FileName
        {
            get
            {
                return Message.ContentName;
            }
        }

        public bool IsSent
        {
            get { return _isSent; }
            set { SetField(ref _isSent, value, nameof(IsSent)); }
        }

        public bool IsReceived
        {
            get { return _isReceived; }
            set { SetField(ref _isReceived, value, nameof(IsReceived)); }
        }

        public RelayCommand ClickCommand { get; set; }
        
        public ChatMessageModel(ChatMessage message, bool isReceived)
        {
            _message = message;
            IsReceived = isReceived;
            IsSent = !isReceived;
            ClickCommand = new RelayCommand((s) => Click(s), "", true);
            Load(message);
        }

        private void Load(ChatMessage message)
        {
            UserName = message.UserFrom;
            Time = message.Timestamp.ToLocalTime();
            if (message.ContentType == ChatMessageContentType.Text)
            {
                MessageText = message.Message;
                _isLink = !string.IsNullOrEmpty(MessageText) && UrlRegex.IsMatch(MessageText);
            }

            if (message.ContentType == ChatMessageContentType.Image ||
                message.ContentType == ChatMessageContentType.Emogi)
            {
                try
                {
                    var bytes = ByteStringConverter.FromZ85String(message.Message);
                    using (var stream = new MemoryStream(bytes))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                        Image = image;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(this, ex);
                }
            }
        }

        private void Click(object s)
        {
            try
            {
                // If it's a link, open it in the browser
                if (IsLink)
                {
                    var match = UrlRegex.Match(MessageText);
                    if (match.Success)
                    {
                        Process.Start(new ProcessStartInfo(match.Value) { UseShellExecute = true });
                        return;
                    }
                }

                // Otherwise, save the file/image
                var path = CommonConstants.OutputDirectory;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var fileName = _message.Id + "_";
                fileName += _message.ContentType == ChatMessageContentType.File ? _message.ContentName : ".png";
                fileName = Path.Combine(path, fileName);

                var bytes = ByteStringConverter.FromZ85String(_message.Message);
                File.WriteAllBytes(fileName, bytes);
                Process.Start("explorer.exe", $"/select,\"{fileName}\"");
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
        }
    }
}
