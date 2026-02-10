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
        public override string GetTitle(int toolWindowId) => "Notes";
        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            Version vsVersion = await VS.Shell.GetVsVersionAsync();
            Logger.Messg(this, "Creating tool window: {0}", vsVersion);
            return new MyToolWindowControl(vsVersion);
        }

        [Guid("03030460-e1a2-49ab-a4c5-b7b9cfc2a4df")]
        public class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}