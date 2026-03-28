using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ChatClientCommon;
using ChatClientCommon.UI;

namespace ChatClient
{
    public class ChatModel : ChatModelBase
    {
        private EmojiInfo _selectedEmoji;
        public ObservableCollection<EmojiInfo> Emojis { get; } = new ObservableCollection<EmojiInfo>();
        public RelayCommand SendEmojiCommand { get; set; }

        public EmojiInfo SelectedEmoji
        {
            get => _selectedEmoji;
            set
            {
                SetField(ref _selectedEmoji, value, nameof(SelectedEmoji));
            }
        }

        public ChatModel()
        {
            SendEmojiCommand = new RelayCommand(s => SendEmoji(), "React", true);
            SendEmojiCommand.Enabled = false;
            UserName ??= "I";
            InitializeEmojis();
        }

        public override void RunOnMainThread(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => action()));
        }

        protected override void SetCanSend(bool canSend)
        {
            base.SetCanSend(canSend);
            SendEmojiCommand.Enabled = SendCommand.Enabled;
        }

        private void SendEmoji()
        {
            if (SelectedEmoji != null && SendCommand.Enabled)
            {
                var encoded = EmojiRenderer.RenderEmoji(SelectedEmoji?.Emoji, 32);
                SendImage(encoded, true);
            }
        }

        private void InitializeEmojis()
        {
            try
            {
                // Smileys & Emotion
                AddEmoji("🙄", "Face with Rolling Eyes");
                AddEmoji("😋", "Face Savoring Food");
                AddEmoji("🙃", "Upside-Down Face");
                AddEmoji("😁", "Beaming Face");
                AddEmoji("🤣", "Rolling on the Floor Laughing");
                AddEmoji("😀", "Grinning Face");
                AddEmoji("😃", "Grinning Face with Big Eyes");
                AddEmoji("😄", "Grinning Face with Smiling Eyes");
                AddEmoji("😆", "Grinning Squinting Face");
                AddEmoji("😅", "Grinning Face with Sweat");
                AddEmoji("😂", "Face with Tears of Joy");
                AddEmoji("🙂", "Slightly Smiling Face");
                AddEmoji("😉", "Winking Face");
                AddEmoji("😊", "Smiling Face with Smiling Eyes");

                // More emotions
                AddEmoji("😛", "Face with Tongue");
                AddEmoji("😜", "Winking Face with Tongue");
                AddEmoji("🤪", "Zany Face");
                AddEmoji("😝", "Squinting Face with Tongue");
                AddEmoji("🤑", "Money-Mouth Face");
                AddEmoji("🤗", "Hugging Face");
                AddEmoji("🤭", "Face with Hand Over Mouth");
                AddEmoji("🤫", "Shushing Face");
                AddEmoji("🤔", "Thinking Face");

                // Neutral & Skeptical
                AddEmoji("🤐", "Zipper-Mouth Face");
                AddEmoji("🤨", "Face with Raised Eyebrow");
                AddEmoji("😐", "Neutral Face");
                AddEmoji("😑", "Expressionless Face");
                AddEmoji("😶", "Face Without Mouth");
                AddEmoji("😏", "Smirking Face");
                AddEmoji("😒", "Unamused Face");
                AddEmoji("😬", "Grimacing Face");
                AddEmoji("🤥", "Lying Face");

                // Sleepy & Unwell
                AddEmoji("😌", "Relieved Face");
                AddEmoji("😔", "Pensive Face");
                AddEmoji("😪", "Sleepy Face");
                AddEmoji("🤤", "Drooling Face");
                AddEmoji("😴", "Sleeping Face");

                // Gestures
                AddEmoji("👍", "Thumbs Up");
                AddEmoji("👎", "Thumbs Down");
                AddEmoji("👌", "OK Hand");
                AddEmoji("✌️", "Victory Hand");
                AddEmoji("🤞", "Crossed Fingers");
                AddEmoji("🤟", "Love-You Gesture");
                AddEmoji("🤘", "Sign of the Horns");
                AddEmoji("👏", "Clapping Hands");
                AddEmoji("🙌", "Raising Hands");
                AddEmoji("👐", "Open Hands");
                AddEmoji("🤲", "Palms Up Together");
                AddEmoji("🙏", "Folded Hands");
                AddEmoji("✊", "Raised Fist");
                AddEmoji("👊", "Oncoming Fist");
                AddEmoji("🤛", "Left-Facing Fist");
                AddEmoji("🤜", "Right-Facing Fist");

                // Symbols & Objects
                AddEmoji("🎉", "Party Popper");
                AddEmoji("🎊", "Confetti Ball");
                AddEmoji("🎈", "Balloon");
                AddEmoji("🎁", "Wrapped Gift");
                AddEmoji("🏆", "Trophy");
                AddEmoji("🥇", "1st Place Medal");
                AddEmoji("🔥", "Fire");
                AddEmoji("⭐", "Star");
                AddEmoji("✨", "Sparkles");
                AddEmoji("💯", "Hundred Points");
                AddEmoji("💪", "Flexed Biceps");
                AddEmoji("🚀", "Rocket");
                AddEmoji("⚡", "High Voltage");
                AddEmoji("💡", "Light Bulb");
                AddEmoji("🎯", "Direct Hit");
                AddEmoji("✅", "Check Mark Button");
                AddEmoji("❌", "Cross Mark");
                AddEmoji("⚠️", "Warning");

                SelectedEmoji = Emojis.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
        }

        private void AddEmoji(string emoji, string name)
        {
            try
            {
                var info = EmojiRenderer.CreateEmojiInfo(emoji, name);
                if (info != null)
                {
                    Emojis.Add(info);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
        }
    }
}
