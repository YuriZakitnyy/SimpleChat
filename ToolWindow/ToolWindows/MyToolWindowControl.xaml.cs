using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ChatClientCommon;

namespace ToolWindow
{
    public partial class MyToolWindowControl : UserControl
    {
        private TooChatModel _model;
        private DispatcherTimer _timer;
        private DateTime _lastAction = DateTime.UtcNow;
        public event Action<bool> UnreadChanged;
        public MyToolWindowControl(Version vsVersion)
        {
            Logger.Messg(this, "Creating");
            try
            {
                InitializeComponent();
                Loaded += MyToolWindowControl_Loaded;
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
        }

        private void MyToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_model == null)
                {
                    Logger.Messg(this, "Loaded");
                    _model = new TooChatModel();
                    _model.MessagesTextBox = MessagesTextBox;
                    _model.Loaded = () =>
                    {
                        if (MessagesListBox.Items.Count > 0)
                        {
                            MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
                        }
                    };
                    _model.MessageArrived += () =>
                    {
                        _model.RunOnMainThread(() =>
                        {
                            _model.Unread = true;
                        });
                    };
                    _model.PropertyChanged += (s, args) =>
                    {
                        if (args.PropertyName == nameof(_model.Unread))
                        {
                            UnreadChanged?.Invoke(_model.Unread);
                        }
                    };
                    DataContext = _model;
                }
                else
                {
                    Logger.Messg(this, "Already was loaded");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
            if (_timer == null)
            {
                try
                {
                    _timer = new DispatcherTimer();
                    _timer.Interval = TimeSpan.FromSeconds(2);
                    _timer.Tick += (s, e) =>
                    {
                        if ((_model.DisplayMode != DisplayMode.Comments) && ((DateTime.UtcNow - _lastAction).TotalSeconds >= 15))
                        {
                            Logger.Messg(this, "Self hide");
                            _model.DisplayMode = DisplayMode.Comments;
                        }
                    };
                    _timer.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error(this, ex);
                }
            }
        }

        private void MessageTextBox_PasteCanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsText() || Clipboard.ContainsImage() || Clipboard.ContainsFileDropList();
            e.Handled = true;
        }

        private void MessageTextBox_PasteExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                e.Handled = false;
                if (Clipboard.ContainsImage())
                {
                    var encoded = ClipboardImageConverter.GetPngStringFromClipboard();
                    if (!string.IsNullOrEmpty(encoded))
                    {
                        e.Handled = true;
                        _model.RunOnMainThread(() => _model.SendImage(encoded));
                    }
                }
                if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList().Cast<string>().ToArray();
                    if (files.Length > 0)
                    {
                        e.Handled = true;
                        _model.RunOnMainThread(() => _model.SendFiles(files));
                    }
                }
                if (Clipboard.ContainsText())
                {
                    var textBox = sender as TextBox;
                    string pastedText = Clipboard.GetText();
                    var oldStart = textBox.SelectionStart;
                    textBox.SelectedText = pastedText;
                    textBox.SelectionStart = oldStart + pastedText.Length;
                    textBox.SelectionLength = 0;
                }
            }
            catch (Exception ex)
            {
                // Optional: log or show error
                MessageBox.Show($"Paste failed: {ex.Message}", "Paste error", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
            }
        }

        private void FilePathTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Check if the dragged data contains file paths
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Display the "Copy" cursor
                e.Effects = DragDropEffects.Copy;
                // Mark the event as handled to prevent the TextBox's default behavior
                e.Handled = true;
            }
            else
            {
                // Display the "None" (forbidden) cursor
                e.Effects = DragDropEffects.None;
            }
        }

        private void FilePathTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Retrieve the array of file paths
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length > 0)
                {
                    _model.SendFiles(files);
                    e.Handled = true;
                }
            }
        }

        private void MyToolWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _lastAction = DateTime.UtcNow;
        }

        private void MyToolWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            _lastAction = DateTime.UtcNow;
        }

        private void MyToolWindow_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void MyToolWindow_LostFocus(object sender, RoutedEventArgs e)
        {
        }
    }
}