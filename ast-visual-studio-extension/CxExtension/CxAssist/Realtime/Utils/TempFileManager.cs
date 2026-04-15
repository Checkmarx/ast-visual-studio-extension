using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Manages temporary files and directories with scanner-specific strategies.
    ///
    /// Design Patterns:
    /// - Factory: Creates scanner-specific temp structures
    /// - Strategy: Each scanner has its own temp file naming/organization strategy
    /// - Template Method: Common validation and hashing logic
    ///
    /// Each scanner type has unique requirements:
    /// - ASCA: Single file with sanitized filename
    /// - Secrets: Directory with hash-based naming for collision avoidance
    /// - IaC: Organized by hash under base directory
    /// - Containers: Organized by hash, special handling for Helm files
    /// - OSS: Hash-based directory with companion file support
    /// </summary>
    public static class TempFileManager
    {
        // Scanner-specific temp directory prefixes
        private const string ASCA_TEMP_PREFIX = "cx-asca-";
        private const string SECRETS_TEMP_PREFIX = "cx-secrets-";
        private const string IAC_TEMP_DIR = "Cx-iac-realtime-scanner";
        private const string CONTAINERS_TEMP_DIR = "Cx-container-realtime-scanner";
        private const string OSS_TEMP_DIR = "Cx-oss-realtime-scanner";

        // Configuration constants
        private const int MAX_FILENAME_LENGTH = 255;  // NTFS/Ext4 limit
        private const int HASH_SUFFIX_LENGTH = 8;     // Chars to use from content hash

        /// <summary>
        /// Creates temp file for ASCA with sanitized filename.
        ///
        /// Strategy:
        /// - Single temp file (not directory)
        /// - Filename sanitized: path separators removed, limited to 255 chars
        /// - Path validation prevents directory traversal attacks
        /// </summary>
        /// <param name="originalFileName">Original file name</param>
        /// <param name="content">File content to write</param>
        /// <returns>Absolute path to temp file</returns>
        public static string CreateAscaTempFile(string originalFileName, string content)
        {
            var tempBasePath = Path.GetTempPath();
            // Reserve space for prefix and ensure full path stays under MAX_PATH (260)
            // MAX_PATH = 260; subtract tempBasePath length and prefix length
            var maxFileNameLength = Math.Min(MAX_FILENAME_LENGTH, 260 - tempBasePath.Length - ASCA_TEMP_PREFIX.Length - 1);
            var sanitized = SanitizeFilename(originalFileName, maxFileNameLength);
            var tempFile = Path.Combine(tempBasePath, $"{ASCA_TEMP_PREFIX}{sanitized}");

            try
            {
                File.WriteAllText(tempFile, content, Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"ASCA: Created temp file: {tempFile}");
                return tempFile;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ASCA: Failed to create temp file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates unique temp directory for Secrets with collision-avoidant naming.
        ///
        /// Strategy:
        /// - Directory naming: {hash}_{UUID}_{timestamp}
        /// - hash: SHA-256 of content (collision detection)
        /// - UUID: Unique identifier (even if same content scanned multiple times)
        /// - timestamp: Unix milliseconds (chronological ordering)
        /// </summary>
        /// <param name="content">File content to hash</param>
        /// <returns>Absolute path to temp directory</returns>
        public static string CreateSecretsTempDir(string content)
        {
            var hash = GetContentHash(content);
            var uuid = Guid.NewGuid().ToString("N").Substring(0, 8);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var dirName = $"{SECRETS_TEMP_PREFIX}{hash}_{uuid}_{timestamp}";

            var tempDir = Path.Combine(Path.GetTempPath(), dirName);

            try
            {
                Directory.CreateDirectory(tempDir);
                System.Diagnostics.Debug.WriteLine($"Secrets: Created temp directory: {tempDir}");
                return tempDir;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Secrets: Failed to create temp directory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates temp directory for IaC with file hash organization.
        ///
        /// Strategy:
        /// - Base directory: Cx-iac-realtime-scanner/
        /// - Subdirectory: {fileHash}/ for organization
        /// - Prevents temp directory clutter
        /// </summary>
        /// <param name="fileHash">Hash of file content</param>
        /// <returns>Absolute path to temp directory</returns>
        public static string CreateIacTempDir(string fileHash)
        {
            if (string.IsNullOrEmpty(fileHash))
                fileHash = Guid.NewGuid().ToString("N").Substring(0, 8);

            var baseTempDir = Path.Combine(Path.GetTempPath(), IAC_TEMP_DIR);
            var tempDir = Path.Combine(baseTempDir, fileHash);

            try
            {
                Directory.CreateDirectory(tempDir);
                System.Diagnostics.Debug.WriteLine($"IaC: Created temp directory: {tempDir}");
                return tempDir;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IaC: Failed to create temp directory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates temp directory for Containers with optional Helm subfolder.
        ///
        /// Strategy:
        /// - Base directory: Cx-container-realtime-scanner/
        /// - Subdirectory: {fileHash}/
        /// - Optional: /helm/ subdirectory for Helm chart files
        /// </summary>
        /// <param name="fileHash">Hash of file content</param>
        /// <param name="isHelmFile">True if file is Helm chart (create /helm/ subfolder)</param>
        /// <returns>Absolute path to temp directory</returns>
        public static string CreateContainersTempDir(string fileHash, bool isHelmFile = false)
        {
            if (string.IsNullOrEmpty(fileHash))
                fileHash = Guid.NewGuid().ToString("N").Substring(0, 8);

            var baseTempDir = Path.Combine(Path.GetTempPath(), CONTAINERS_TEMP_DIR);
            var tempDir = Path.Combine(baseTempDir, fileHash);

            if (isHelmFile)
                tempDir = Path.Combine(tempDir, "helm");

            try
            {
                Directory.CreateDirectory(tempDir);
                System.Diagnostics.Debug.WriteLine($"Containers: Created temp directory: {tempDir}");
                return tempDir;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Containers: Failed to create temp directory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates temp directory for OSS with lock file support.
        ///
        /// Strategy:
        /// - Base directory: Cx-oss-realtime-scanner/
        /// - Subdirectory: {manifestHash}/ for organization
        /// - Caller will copy companion lock files here
        /// </summary>
        /// <param name="manifestHash">Hash of manifest file content</param>
        /// <returns>Absolute path to temp directory</returns>
        public static string CreateOssTempDir(string manifestHash)
        {
            if (string.IsNullOrEmpty(manifestHash))
                manifestHash = Guid.NewGuid().ToString("N").Substring(0, 8);

            var baseTempDir = Path.Combine(Path.GetTempPath(), OSS_TEMP_DIR);
            var tempDir = Path.Combine(baseTempDir, manifestHash);

            try
            {
                Directory.CreateDirectory(tempDir);
                System.Diagnostics.Debug.WriteLine($"OSS: Created temp directory: {tempDir}");
                return tempDir;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OSS: Failed to create temp directory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sanitizes filename to prevent directory traversal and invalid characters.
        ///
        /// Removes path separators and invalid characters from the filename stem only.
        /// Preserves the final file extension (e.g. Program.cs → stem sanitized + ".cs").
        /// Replacing every "." in the name was stripping extensions and broke ASCA CLI ("file must have an extension").
        ///
        /// Security: Blocks directory traversal in the stem; limits length.
        /// </summary>
        internal static string SanitizeFilename(string fileName, int maxLength)
        {
            if (string.IsNullOrEmpty(fileName))
                return "file.dat";

            var baseName = Path.GetFileName(fileName.Replace('\0', '_').Trim());
            if (string.IsNullOrEmpty(baseName))
                return "file.dat";

            var ext = Path.GetExtension(baseName);
            var stem = Path.GetFileNameWithoutExtension(baseName);

            // Dot-only names like ".editorconfig" — API leaves empty stem
            if (string.IsNullOrEmpty(stem))
                stem = "file";

            var sanitizedStem = stem
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_');

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                sanitizedStem = sanitizedStem.Replace(invalidChar, '_');

            sanitizedStem = sanitizedStem.Replace("..", "_");

            if (string.IsNullOrEmpty(ext))
                ext = ".dat";

            var result = sanitizedStem + ext;
            if (result.Length > maxLength)
            {
                int maxStem = maxLength - ext.Length;
                if (maxStem < 1)
                    maxStem = 1;
                if (sanitizedStem.Length > maxStem)
                    sanitizedStem = sanitizedStem.Substring(0, maxStem);
                result = sanitizedStem + ext;
            }

            return result;
        }

        /// <summary>
        /// Resolves <paramref name="candidate"/> to a canonical path, ensures a regular file exists (not a reparse point),
        /// and returns its <see cref="FileInfo"/>. Mitigates path traversal before any file read.
        /// </summary>
        internal static bool TryGetVerifiedRegularFileInfo(string candidate, out FileInfo fileInfo)
        {
            fileInfo = null;
            if (string.IsNullOrWhiteSpace(candidate) || candidate.IndexOf('\0') >= 0)
                return false;

            try
            {
                var normalized = Path.GetFullPath(candidate.Trim());
                if (!Path.IsPathRooted(normalized))
                    return false;

                var fi = new FileInfo(normalized);
                if (!fi.Exists)
                    return false;
                if ((fi.Attributes & FileAttributes.ReparsePoint) != 0)
                    return false;

                fileInfo = fi;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates path and file (same rules as <see cref="TryGetVerifiedRegularFileInfo"/>), enforces <paramref name="maxSizeBytes"/>,
        /// then reads UTF-8 text via <see cref="File.ReadAllText(string)"/> using <see cref="FileInfo.FullName"/> only.
        /// Centralizes the read so path traversal sinks are not duplicated in callers.
        /// </summary>
        internal static bool TryReadVerifiedExistingFileContent(
            string candidate,
            long maxSizeBytes,
            out string content,
            out string verifiedAbsolutePath)
        {
            content = null;
            verifiedAbsolutePath = null;

            if (!TryGetVerifiedRegularFileInfo(candidate, out var fi))
                return false;

            try
            {
                if (fi.Length > maxSizeBytes)
                    return false;

                verifiedAbsolutePath = fi.FullName;
                content = File.ReadAllText(fi.FullName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Computes SHA-256 hash of content for collision detection and organization.
        ///
        /// Falls back to simple hashCode if SHA-256 unavailable.
        /// Returns first N chars of hex for readability in filenames (default 8).
        /// </summary>
        /// <param name="content">Content to hash</param>
        /// <param name="length">Number of hex characters to return (default HASH_SUFFIX_LENGTH = 8)</param>
        internal static string GetContentHash(string content, int length = HASH_SUFFIX_LENGTH)
        {
            if (string.IsNullOrEmpty(content))
                return "empty";

            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                    var hexString = BitConverter.ToString(hash).Replace("-", "").ToLower();
                    return hexString.Substring(0, Math.Min(length, hexString.Length));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SHA-256 hashing failed: {ex.Message}. Using fallback.");
                // Fallback to simple hash code
                return content.GetHashCode().ToString("x8");
            }
        }

        /// <summary>
        /// Recursively deletes temp directory and all contents safely.
        ///
        /// Pattern: Walk directory tree in reverse order (deepest first)
        /// Ensures all files are deleted before parent directories.
        /// </summary>
        /// <param name="folderPath">Path to directory to delete</param>
        public static void DeleteTempDirectory(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            try
            {
                var dirInfo = new DirectoryInfo(folderPath);

                // Delete files first
                foreach (var file in dirInfo.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete file {file.FullName}: {ex.Message}");
                    }
                }

                // Delete subdirectories recursively
                foreach (var dir in dirInfo.GetDirectories())
                {
                    DeleteTempDirectory(dir.FullName);
                }

                // Delete the directory itself
                try
                {
                    dirInfo.Delete(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete directory {folderPath}: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine($"Temp directory deleted: {folderPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting temp directory {folderPath}: {ex.Message}");
            }
        }
    }
}
