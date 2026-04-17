using System;
using System.Collections.Concurrent;
using System.IO;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Ensures <see cref="Oss.OssService.ScanAllManifestsInSolutionAsync"/> runs only when needed:
    /// once per solution directory per IDE session after a successful sweep — not on every realtime
    /// orchestrator re-init (e.g. toggling ASCA or other non-OSS scanners).
    /// JetBrains runs the sweep from <c>OssScannerCommand.initializeScanner</c> when the OSS engine starts;
    /// that corresponds to OSS being registered, not every global settings sync.
    /// </summary>
    internal static class OssManifestSweepPolicy
    {
        private static readonly ConcurrentDictionary<string, byte> CompletedSweeps =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        public static bool ShouldScheduleFullManifestSweep(string solutionRoot)
        {
            if (string.IsNullOrEmpty(solutionRoot))
                return false;

            var key = NormalizeRoot(solutionRoot);
            return !CompletedSweeps.ContainsKey(key);
        }

        public static void MarkSweepCompleted(string solutionRoot)
        {
            if (string.IsNullOrEmpty(solutionRoot))
                return;
            CompletedSweeps.TryAdd(NormalizeRoot(solutionRoot), 0);
        }

        /// <summary>
        /// Clears per-solution sweep bookkeeping (e.g. after logout). Otherwise a re-login in the same IDE session
        /// skips <see cref="Oss.OssService.ScanAllManifestsInSolutionAsync"/> because the policy still thinks the sweep ran.
        /// </summary>
        internal static void ClearSession()
        {
            CompletedSweeps.Clear();
        }

        private static string NormalizeRoot(string solutionRoot)
        {
            try
            {
                return Path.GetFullPath(solutionRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return solutionRoot.Trim();
            }
        }
    }
}
