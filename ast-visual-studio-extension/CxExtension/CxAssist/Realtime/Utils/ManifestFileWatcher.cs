using ast_visual_studio_extension.CxExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Monitors manifest files in the project/solution directory for changes.
    /// Detects when dependency files are created, modified, or deleted.
    /// Manifest patterns: package.json, pom.xml, requirements.txt, go.mod, Gemfile, composer.json,
    /// Cargo.toml, Pipfile, pubspec.yaml, mix.exs, *.csproj, *.tfvars, dockerfile, etc.
    /// </summary>
    public class ManifestFileWatcher : IDisposable
    {
        private readonly string _solutionDirectory;
        private FileSystemWatcher _watcher;
        private readonly List<string> _ignoredPatterns = new List<string>
        {
            "node_modules", "dist", "bin", "obj", ".git", ".vs", ".vscode", "packages",
            "out", "target", "__pycache__", ".pytest_cache"
        };

        private static readonly HashSet<string> ManifestFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "package.json", "yarn.lock", "package-lock.json", "npm-shrinkwrap.json",
            "pom.xml", "pom.xml.lock",
            "requirements.txt", "requirements.lock",
            "go.mod", "go.sum",
            "packages.config",
            "Gemfile", "Gemfile.lock",
            "build.gradle",
            "composer.json", "composer.lock",
            "Cargo.toml", "Cargo.lock",
            "pubspec.yaml", "pubspec.lock",
            "Pipfile", "Pipfile.lock",
            "mix.exs", "mix.lock",
            ".checkmarxignore", ".checkmarxIgnoredTempList"
        };

        private static readonly HashSet<string> ManifestExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".csproj", ".vbproj", ".fsproj", ".tfvars", ".tf"
        };

        private static readonly HashSet<string> DockerFilePatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "dockerfile", "docker-compose.yml", "docker-compose.yaml"
        };

        /// <summary>
        /// Callback when a manifest file is detected as changed.
        /// Parameters: file path, change type (Created, Modified, Deleted, Renamed)
        /// </summary>
        public event Action<string, WatcherChangeTypes> ManifestFileChanged;

        public ManifestFileWatcher(string solutionDirectory)
        {
            _solutionDirectory = solutionDirectory ?? throw new ArgumentNullException(nameof(solutionDirectory));
        }

        /// <summary>
        /// Starts monitoring the solution directory for manifest file changes.
        /// </summary>
        public void Start()
        {
            if (!Directory.Exists(_solutionDirectory))
            {
                OutputPaneWriter.WriteWarning($"ManifestFileWatcher: Solution directory does not exist: {_solutionDirectory}");
                return;
            }

            if (_watcher != null)
                return; // Already running

            try
            {
                _watcher = new FileSystemWatcher(_solutionDirectory)
                {
                    Filter = "*.*",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = true
                };

                _watcher.Created += OnFileCreated;
                _watcher.Changed += OnFileModified;
                _watcher.Deleted += OnFileDeleted;
                _watcher.Renamed += OnFileRenamed;
                _watcher.Error += OnWatcherError;

                _watcher.EnableRaisingEvents = true;
                OutputPaneWriter.WriteLine($"ManifestFileWatcher: Started monitoring manifest files in: {_solutionDirectory}");
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"ManifestFileWatcher: Failed to start: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops monitoring the solution directory.
        /// </summary>
        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                OutputPaneWriter.WriteLine("ManifestFileWatcher: Stopped monitoring");
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (IsManifestFile(e.FullPath))
            {
                OutputPaneWriter.WriteDebug($"ManifestFileWatcher: Manifest file created: {e.Name}");
                ManifestFileChanged?.Invoke(e.FullPath, WatcherChangeTypes.Created);
            }
        }

        private void OnFileModified(object sender, FileSystemEventArgs e)
        {
            if (IsManifestFile(e.FullPath))
            {
                OutputPaneWriter.WriteDebug($"ManifestFileWatcher: Manifest file modified: {e.Name}");
                ManifestFileChanged?.Invoke(e.FullPath, WatcherChangeTypes.Changed);
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (IsManifestFile(e.FullPath))
            {
                OutputPaneWriter.WriteDebug($"ManifestFileWatcher: Manifest file deleted: {e.Name}");
                ManifestFileChanged?.Invoke(e.FullPath, WatcherChangeTypes.Deleted);
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            bool oldIsManifest = IsManifestFile(e.OldFullPath);
            bool newIsManifest = IsManifestFile(e.FullPath);

            if (oldIsManifest || newIsManifest)
            {
                OutputPaneWriter.WriteDebug($"ManifestFileWatcher: Manifest file renamed: {e.OldName} -> {e.Name}");
                if (oldIsManifest)
                    ManifestFileChanged?.Invoke(e.OldFullPath, WatcherChangeTypes.Deleted);
                if (newIsManifest)
                    ManifestFileChanged?.Invoke(e.FullPath, WatcherChangeTypes.Created);
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            Exception ex = e.GetException();
            if (ex != null)
                OutputPaneWriter.WriteError($"ManifestFileWatcher: {ex.Message}");
        }

        /// <summary>
        /// Determines if a file path represents a manifest file that should trigger re-scans.
        /// Checks: filename against manifest names, extensions, and docker file patterns.
        /// Filters: ignores files in excluded directories (node_modules, dist, bin, etc.).
        /// </summary>
        private bool IsManifestFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            // Skip files in ignored directories
            if (IsInIgnoredDirectory(filePath))
                return false;

            string fileName = Path.GetFileName(filePath);
            string extension = Path.GetExtension(filePath);

            // Check exact filename match
            if (ManifestFileNames.Contains(fileName))
                return true;

            // Check extension match
            if (ManifestExtensions.Contains(extension))
                return true;

            // Check docker file patterns (dockerfile, dockerfile.*, docker-compose.*)
            if (DockerFilePatterns.Contains(fileName))
                return true;

            if (fileName.StartsWith("dockerfile", StringComparison.OrdinalIgnoreCase))
                return true;

            if (fileName.StartsWith("docker-compose", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a file path is in an ignored directory.
        /// </summary>
        private bool IsInIgnoredDirectory(string filePath)
        {
            return _ignoredPatterns.Any(pattern =>
                filePath.IndexOf($"{Path.DirectorySeparatorChar}{pattern}{Path.DirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                filePath.IndexOf($"{Path.DirectorySeparatorChar}{pattern}{Path.AltDirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                filePath.Contains($"{Path.DirectorySeparatorChar}{pattern}{Path.DirectorySeparatorChar}") ||
                filePath.Contains($"{Path.AltDirectorySeparatorChar}{pattern}{Path.AltDirectorySeparatorChar}")
            );
        }

        public void Dispose()
        {
            Stop();
            _watcher?.Dispose();
            _watcher = null;
        }
    }
}
