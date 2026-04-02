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
            var sanitized = SanitizeFilename(originalFileName, MAX_FILENAME_LENGTH);
            var tempFile = Path.Combine(Path.GetTempPath(), $"{ASCA_TEMP_PREFIX}{sanitized}");

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
        /// Removes:
        /// - Path separators (\ /)
        /// - Dots (to prevent relative path references)
        /// - Limits length to max filename length
        ///
        /// Security: Prevents attacks like "../../../etc/passwd"
        /// </summary>
        private static string SanitizeFilename(string fileName, int maxLength)
        {
            if (string.IsNullOrEmpty(fileName))
                return "file";

            // Remove path separators and dots
            var sanitized = fileName
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_')
                .Replace(".", "_");

            // Remove invalid filename characters
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            // Limit length
            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            return sanitized;
        }

        /// <summary>
        /// Computes SHA-256 hash of content for collision detection and organization.
        ///
        /// Falls back to simple hashCode if SHA-256 unavailable.
        /// Returns first 8 chars of hex for readability in filenames.
        /// </summary>
        private static string GetContentHash(string content)
        {
            if (string.IsNullOrEmpty(content))
                return "empty";

            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                    var hexString = Convert.ToHexString(hash);
                    return hexString.Substring(0, Math.Min(HASH_SUFFIX_LENGTH, hexString.Length));
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
