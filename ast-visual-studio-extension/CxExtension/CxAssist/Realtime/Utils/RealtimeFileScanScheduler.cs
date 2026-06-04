using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Per-file debounced scheduling (default 1000 ms), cancelling superseded requests.
    /// Mirrors the intent of JetBrains' DevAssistScanScheduler for rapid edits.
    /// </summary>
    public sealed class RealtimeFileScanScheduler : IDisposable
    {
        private const int DebounceMilliseconds = 1000;

        private readonly JoinableTaskFactory _joinableTaskFactory;
        private bool _disposed;
        private readonly ConcurrentDictionary<string, FileScheduleState> _states =
            new ConcurrentDictionary<string, FileScheduleState>(StringComparer.OrdinalIgnoreCase);

        private sealed class FileScheduleState
        {
            public long Version;
            public CancellationTokenSource Cancellation;
        }

        public RealtimeFileScanScheduler(JoinableTaskFactory joinableTaskFactory)
        {
            // Allow null for unit testing scenarios without VS context
            _joinableTaskFactory = joinableTaskFactory;
        }

        /// <summary>
        /// Schedules work to run after debounce. New calls for the same path cancel the previous pending run.
        /// </summary>
        public void Schedule(string filePath, Func<CancellationToken, Task> work)
        {
            if (_disposed || string.IsNullOrEmpty(filePath) || work == null)
                return;

            // Skip scheduling if no joinable task factory (unit test context without VS)
            if (_joinableTaskFactory == null)
                return;

            var key = NormalizePath(filePath);
            var state = _states.GetOrAdd(key, _ => new FileScheduleState());

            CancellationTokenSource newCts;
            long myVersion;
            lock (state)
            {
                state.Cancellation?.Cancel();
                state.Cancellation?.Dispose();
                state.Cancellation = new CancellationTokenSource();
                newCts = state.Cancellation;
                myVersion = ++state.Version;
            }

            var token = newCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceMilliseconds, token).ConfigureAwait(false);
                    if (token.IsCancellationRequested)
                        return;

                    lock (state)
                    {
                        if (state.Version != myVersion)
                            return;
                    }

                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    await work(token).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    // Expected when superseded or disposed.
                }
                catch (Exception ex)
                {
                    OutputPaneWriter.WriteError($"Realtime scan failed for {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }, CancellationToken.None);
        }

        /// <summary>
        /// Cancels any pending debounced work for the file (e.g. on document close).
        /// </summary>
        public void CancelPending(string filePath)
        {
            if (_disposed || string.IsNullOrEmpty(filePath))
                return;

            var key = NormalizePath(filePath);
            if (!_states.TryGetValue(key, out var state))
                return;

            lock (state)
            {
                state.Cancellation?.Cancel();
                state.Cancellation?.Dispose();
                state.Cancellation = null;
                state.Version++;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            foreach (var kv in _states)
            {
                var state = kv.Value;
                lock (state)
                {
                    state.Cancellation?.Cancel();
                    state.Cancellation?.Dispose();
                    state.Cancellation = null;
                    state.Version++;
                }
            }
            _states.Clear();
        }

        private static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path.Trim();
            }
        }
    }
}
