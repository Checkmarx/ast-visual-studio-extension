using System;
using CxWrapperClass = ast_visual_studio_extension.CxCLI.CxWrapper;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base
{
    /// <summary>
    /// Generic base class for thread-safe lazy singleton scanner services.
    ///
    /// Design Pattern: Generic Singleton with Double-Checked Locking
    ///
    /// Each closed generic type (SingletonScannerBase&lt;AscaService&gt;, etc.) has its own
    /// independent static _instance slot, guaranteed by the CLR's generic type system.
    /// This eliminates 5× identical DCL boilerplate across all scanner services.
    /// </summary>
    public abstract class SingletonScannerBase<T> : BaseRealtimeScannerService
        where T : SingletonScannerBase<T>
    {
        private static volatile T _instance;
        private static readonly object _lock = new object();

        protected SingletonScannerBase(CxWrapperClass cxWrapper) : base(cxWrapper)
        {
        }

        /// <summary>
        /// Gets or creates the singleton instance using the provided factory.
        /// Thread-safe double-checked locking pattern.
        /// </summary>
        /// <param name="factory">Factory function that creates the instance</param>
        /// <returns>Singleton instance (either existing or newly created)</returns>
        protected static T GetOrCreate(Func<T> factory)
        {
            if (_instance != null) return _instance;
            lock (_lock)
            {
                if (_instance == null)
                    _instance = factory();
            }
            return _instance;
        }

        /// <summary>
        /// Resets the singleton instance to null.
        /// Must be called from subclass's UnregisterAsync override after awaiting base.UnregisterAsync().
        /// Thread-safe.
        /// </summary>
        protected static void ResetInstance()
        {
            lock (_lock) { _instance = null; }
        }
    }
}
