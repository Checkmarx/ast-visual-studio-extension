using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Resolves <see cref="ITextBuffer"/> for a file that is open in the editor but may not yet be in the glyph tagger cache.
    /// </summary>
    internal static class EditorBufferResolver
    {
        /// <summary>
        /// Call from the UI thread. Returns null if the document is not open in the RDT or adapters cannot produce a buffer.
        /// </summary>
        public static ITextBuffer TryGetTextBufferForMoniker(string documentPath)
        {
            if (string.IsNullOrWhiteSpace(documentPath))
                return null;

            string moniker;
            try
            {
                moniker = Path.GetFullPath(documentPath);
            }
            catch
            {
                moniker = documentPath;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            var rdt = Package.GetGlobalService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if (rdt == null)
                return null;

            IVsHierarchy hierarchy = null;
            uint itemId = 0;
            IntPtr punkDocData = IntPtr.Zero;
            uint cookie = 0;

            try
            {
                int hr = rdt.FindAndLockDocument(
                    (uint)_VSRDTFLAGS.RDT_ReadLock,
                    moniker,
                    out hierarchy,
                    out itemId,
                    out punkDocData,
                    out cookie);

                if (ErrorHandler.Failed(hr) || punkDocData == IntPtr.Zero)
                    return null;

                try
                {
                    object docObj = Marshal.GetObjectForIUnknown(punkDocData);
                    var vsTextBuffer = docObj as IVsTextBuffer;
                    if (vsTextBuffer == null)
                        return null;

                    var mef = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
                    var adapters = mef?.GetService<IVsEditorAdaptersFactoryService>();
                    return adapters?.GetDocumentBuffer(vsTextBuffer);
                }
                finally
                {
                    Marshal.Release(punkDocData);
                }
            }
            finally
            {
                if (cookie != 0)
                {
                    try
                    {
                        rdt.UnlockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, cookie);
                    }
                    catch
                    {
                        // Unlock is best-effort when tearing down RDT state.
                    }
                }
            }
        }
    }
}
