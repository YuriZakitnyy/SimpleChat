using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ChatClientCommon
{
    public static class RichTextBoxHelper
    {
        private static readonly Regex UrlRegex = new Regex(
            @"(https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly DependencyProperty DocumentXamlProperty =
            DependencyProperty.RegisterAttached(
                "DocumentXaml",
                typeof(string),
                typeof(RichTextBoxHelper),
                new PropertyMetadata(null, OnDocumentXamlChanged));

        public static string GetDocumentXaml(DependencyObject obj)
        {
            return (string)obj.GetValue(DocumentXamlProperty);
        }

        public static void SetDocumentXaml(DependencyObject obj, string value)
        {
            obj.SetValue(DocumentXamlProperty, value);
        }

        private static void OnDocumentXamlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox richTextBox && e.NewValue is string text)
            {
                richTextBox.Document.Blocks.Clear();
                
                if (string.IsNullOrEmpty(text))
                    return;

                var paragraph = new Paragraph();
                var lastIndex = 0;

                // Find all URLs in the text
                foreach (Match match in UrlRegex.Matches(text))
                {
                    // Add text before the URL
                    if (match.Index > lastIndex)
                    {
                        var textBeforeUrl = text.Substring(lastIndex, match.Index - lastIndex);
                        paragraph.Inlines.Add(new Run(textBeforeUrl));
                    }

                    // Add the URL as a Hyperlink
                    var hyperlink = new Hyperlink(new Run(match.Value))
                    {
                        NavigateUri = new Uri(match.Value),
                        ToolTip = match.Value
                    };

                    hyperlink.RequestNavigate += (sender, args) =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = args.Uri.AbsoluteUri,
                                UseShellExecute = true
                            });
                            args.Handled = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    paragraph.Inlines.Add(hyperlink);
                    lastIndex = match.Index + match.Length;
                }

                // Add remaining text after the last URL
                if (lastIndex < text.Length)
                {
                    var textAfterUrl = text.Substring(lastIndex);
                    paragraph.Inlines.Add(new Run(textAfterUrl));
                }

                richTextBox.Document.Blocks.Add(paragraph);
            }
        }
    }
}