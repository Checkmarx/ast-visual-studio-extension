using System;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests.Helpers
{
    /// <summary>
    /// Initializes <see cref="ThreadHelper.JoinableTaskFactory"/> for unit tests that run outside
    /// a live VS instance. Without this, any production code that calls
    /// <c>ThreadHelper.JoinableTaskFactory.RunAsync(...)</c> as fire-and-forget (telemetry, UI
    /// updates) throws a NullReferenceException because the backing context is never set.
    /// </summary>
    internal static class VsThreadingTestHelper
    {
        private static bool _initialized;
        private static readonly object _lock = new object();

        /// <summary>
        /// Ensures <see cref="ThreadHelper.JoinableTaskFactory"/> is non-null.
        /// Safe to call multiple times; initialization only runs once per process.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;

                // If already initialized (e.g., running inside VS), do nothing.
                try
                {
                    if (ThreadHelper.JoinableTaskFactory != null)
                    {
                        _initialized = true;
                        return;
                    }
                }
                catch { /* JoinableTaskFactory getter may throw when context is null */ }

                // Set the backing context cache via reflection so the static property returns a
                // real JoinableTaskFactory and fire-and-forget RunAsync calls don't NPE.
#pragma warning disable VSSDK005  // Intentional: unit tests have no VS JoinableTaskContext; creating one for test isolation is safe here.
                var jtc = new JoinableTaskContext();
#pragma warning restore VSSDK005
                var field = typeof(ThreadHelper).GetField(
                    "_joinableTaskContextCache",
                    BindingFlags.Static | BindingFlags.NonPublic);
                field?.SetValue(null, jtc);

                _initialized = true;
            }
        }
    }
}
