using System;
using System.Windows;
using ChatClientCommon;

namespace ChatClient
{
    public class ChatModel : ChatModelBase
    {
        public ChatModel()
        {
            UserName ??= "I";
        }

        public override void RunOnMainThread(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => action()));
        }
    }
}
