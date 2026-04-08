using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Interfaces
{
    /// <summary>
    /// Interface for all realtime scanner services (ASCA, Secrets, IaC, Containers, OSS).
    /// Defines the contract for initializing, unregistering, and file-type filtering.
    /// </summary>
    public interface IRealtimeScannerService
    {
        /// <summary>
        /// Determines if this scanner should process the given file path.
        /// Each scanner implements its own file type filtering logic.
        /// </summary>
        bool ShouldScanFile(string filePath);

        /// <summary>
        /// Initializes the scanner: registers for text change events and performs any setup.
        /// Called when the scanner is enabled via settings.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Unregisters the scanner: removes event subscriptions and clears UI markers.
        /// Called when the scanner is disabled via settings or on extension shutdown.
        /// </summary>
        Task UnregisterAsync();

        /// <summary>
        /// Scans a file on disk (startup sweep, manifest watcher) without requiring an open document.
        /// </summary>
        Task ScanExternalFileAsync(string filePath);

        /// <summary>
        /// Instantly scans the given file (e.g., active document).
        /// Called when user logs in or scanners are re-enabled.
        /// </summary>
        Task InstantScanAsync(string filePath);

        /// <summary>
        /// Triggers an instant scan of the currently active document.
        /// Called when scanner is enabled or after user logs in.
        /// </summary>
        Task TriggerCurrentDocumentScanAsync();

        /// <summary>
        /// Rescans all manifest files in the solution directory.
        /// Called when user logs in, after re-authentication, or when scanners are re-enabled.
        /// </summary>
        Task RescanManifestFilesAsync(string solutionRoot);

        /// <summary>
        /// Cancels all pending debounced scans.
        /// Called when scanner is disabled in settings.
        /// </summary>
        void CancelPendingScans();
    }
}
