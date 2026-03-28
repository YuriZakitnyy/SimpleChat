using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ChatClientCommon;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace ToolWindow
{
    public class MyToolWindow : BaseToolWindow<MyToolWindow>
    {
        private const string DefaultTitle = "Notes";
        private const string UnreadTitle = "Notes -";
        private MyToolWindowControl _control;

        public override string GetTitle(int toolWindowId) => DefaultTitle;
        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            Version vsVersion = await VS.Shell.GetVsVersionAsync();
            Logger.Messg(this, "Creating tool window: {0}", vsVersion);
            _control = new MyToolWindowControl(vsVersion);
            _control.UnreadChanged += OnUnreadChanged;
            return _control;
        }

        private void OnUnreadChanged(bool hasUnread)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var pane = Pane.Instance;
                if (pane != null)
                {
                    pane.Caption = hasUnread ? UnreadTitle : DefaultTitle;
                }
            });
        }

        [Guid("03030460-e1a2-49ab-a4c5-b7b9cfc2a4ff")]
        public class Pane : ToolWindowPane
        {
            public static Pane Instance;
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}