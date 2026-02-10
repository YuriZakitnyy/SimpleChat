using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ChatClientCommon;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        private ChatModel _model;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _model = new ChatModel();
            _model.Loaded += () =>
            {
                if (MessagesListBox.Items.Count > 0)
                {
                    MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
                }
            };
            _model.MessageArrived += () =>
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                FlashWindow(helper.Handle, true);
            };
            DataContext = _model;
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
    }
}
