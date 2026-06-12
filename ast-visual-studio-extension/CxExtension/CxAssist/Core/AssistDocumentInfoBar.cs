using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Shows an <see cref="IVsInfoBarHost"/> strip on the active code document window (above the editor),
    /// not the IDE main-window info bar or status bar.
    /// </summary>
    internal static class AssistDocumentInfoBar
    {
        private sealed class InfoBarUiSink : IVsInfoBarUIEvents
        {
            private uint _cookie;

            public void Register(IVsInfoBarUIElement element)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                element?.Advise(this, out _cookie);
            }

            public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                try
                {
                    infoBarUIElement?.Unadvise(_cookie);
                }
                catch
                {
                }
            }

            public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            {
            }
        }

        /// <summary>
        /// Shows a warning info bar on the given document window frame. Invokes <paramref name="fallback"/>
        /// when the frame has no document info bar host or services are unavailable.
        /// </summary>
        public static void TryShowWarning(IVsWindowFrame documentFrame, string message, Action fallback)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (documentFrame == null || string.IsNullOrEmpty(message))
            {
                fallback?.Invoke();
                return;
            }

            try
            {
                if (ErrorHandler.Failed(documentFrame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out object hostObj)))
                {
                    fallback?.Invoke();
                    return;
                }

                var host = hostObj as IVsInfoBarHost;
                if (host == null)
                {
                    fallback?.Invoke();
                    return;
                }

                var sp = ServiceProvider.GlobalProvider;
                var factory = sp?.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                if (factory == null)
                {
                    fallback?.Invoke();
                    return;
                }

                var text = new InfoBarTextSpan(message);
                var model = new InfoBarModel(new[] { text }, KnownMonikers.StatusWarning, isCloseButtonVisible: true);
                IVsInfoBarUIElement element = factory.CreateInfoBar(model);

                var sink = new InfoBarUiSink();
                sink.Register(element);

                host.AddInfoBar(element);

                _ = DismissDocumentInfoBarAsync(host, element, delayMs: 20000);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[CxAssist] AssistDocumentInfoBar: " + ex.Message);
                fallback?.Invoke();
            }
        }

        private static async Task DismissDocumentInfoBarAsync(IVsInfoBarHost host, IVsInfoBarUIElement element, int delayMs)
        {
            await Task.Delay(delayMs).ConfigureAwait(false);
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                host?.RemoveInfoBar(element);
            }
            catch
            {
            }
        }
    }
}
