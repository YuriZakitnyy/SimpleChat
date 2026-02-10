using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CharCommon;
using ChatClientCommon;
using ChatClientCommon.UI;

namespace ToolWindow
{
    public enum DisplayMode
    {
        Text,
        Comments,
        Messages
    }

    public class TooChatModel : ChatModelBase
    {
        private string _comments;
        private DisplayMode _displayMode = DisplayMode.Comments;

        public DisplayMode DisplayMode
        {
            get => _displayMode;
            set { SetField(ref _displayMode, value, nameof(DisplayMode), nameof(ShowComments), nameof(ShowText), nameof(ShowMessages)); }
        }

        public bool ShowComments => _displayMode == DisplayMode.Comments;
        public bool ShowText => _displayMode == DisplayMode.Text;
        public bool ShowMessages => _displayMode == DisplayMode.Messages;

        public string Comments
        {
            get => _comments;
            set { SetField(ref _comments, value, nameof(Comments)); }
        }

        public RelayCommand SaveComments { get; set; }
        public RelayCommand LoadComments { get; set; }
        public RelayCommand SwitchMode { get; set; }
        public RelayCommand ShowMessagesCommand { get; set; }

        public TextBox MessagesTextBox { get; set; }

        public TooChatModel()
        {
            Logger.Messg(this, "Creating");
            try
            {
                UserName ??= "Y";
                DoLoadComments();
                SaveComments = new RelayCommand(s => DoSaveComments(), "Save", true);
                LoadComments = new RelayCommand(s => DoLoadComments(), "Load", true);
                SwitchMode = new RelayCommand(s => DoSwitchMode(), "Switch", true);
                ShowMessagesCommand = new RelayCommand(s => DoShowMessages(), "Messages", true);
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
                Error = ex.Message;
            }
        }

        private void DoSaveComments()
        {
            Logger.Messg(this, "Save comments");
            _store.SaveComments(Comments);
        }

        private void DoLoadComments()
        {
            Logger.Messg(this, "Load comments");
            Comments = _store.LoadComments();
        }

        private void DoSwitchMode()
        {
            switch (_displayMode)
            {
                case DisplayMode.Text:
                    DisplayMode = DisplayMode.Comments;
                    break;
                case DisplayMode.Comments:
                    DisplayMode = DisplayMode.Text;
                    break;
                case DisplayMode.Messages:
                    DisplayMode = DisplayMode.Comments;
                    break;
                default:
                    break;
            }
        }

        private void DoShowMessages()
        {
            DisplayMode = DisplayMode.Messages;
        }

        public override void RunOnMainThread(Action act)
        {
            Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _dispatcher.BeginInvoke(DispatcherPriority.Normal, act);
        }

        protected override ChatMessageModel CreateMessageModel(ChatMessage message)
        {
            var result = base.CreateMessageModel(message);
            switch (message.ContentType)
            {
                case ChatMessageContentType.File:
                    MessagesTextBox.AppendText("\r\n" + message.ContentName);
                    break;
                case ChatMessageContentType.Image:
                    MessagesTextBox.AppendText("\r\nimg");
                    break;
                case ChatMessageContentType.Text:
                    MessagesTextBox.AppendText("\r\n" + message.Message);
                    break;
            }
            return result;
        }
    }
}
