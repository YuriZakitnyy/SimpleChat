using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using CharCommon;
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

        public ChatMessage Message => _message;

        public string UserName
        {
            get { return _userName; }
            set { SetField(ref _userName, value, nameof(UserName)); }
        }

        public string MessageText
        {
            get { return _messageText; }
            set { SetField(ref _messageText, value, nameof(MessageText)); }
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
                return Image != null;
            }
        }

        public bool IsText
        {
            get
            {
                return Message.ContentType == ChatMessageContentType.Text;
            }
        }

        public bool IsFile
        {
            get
            {
                return Message.ContentType == ChatMessageContentType.File;
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
            Time = message.Timestamp;
            if (message.ContentType == ChatMessageContentType.Text)
            {
                MessageText = message.Message;
            }
            if (message.ContentType == ChatMessageContentType.Image)
            {
                try
                {
                    var bytes = ByteStringConverter.FromZ85String(message.Message);
                    using (var stream = new System.IO.MemoryStream(bytes))
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
