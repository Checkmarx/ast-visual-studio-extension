using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.UI.FindingsWindow
{
    /// <summary>
    /// Base class for tree nodes in the Findings window
    /// </summary>
    public abstract class FindingsTreeNode : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a file node with vulnerability count badges
    /// </summary>
    public class FileNode : FindingsTreeNode
    {
        private string _fileName;
        private string _filePath;
        private ImageSource _fileIcon;
        private ObservableCollection<SeverityCount> _severityCounts;
        private ObservableCollection<VulnerabilityNode> _vulnerabilities;

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(nameof(FileName)); }
        }

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(nameof(FilePath)); }
        }

        public ImageSource FileIcon
        {
            get => _fileIcon;
            set { _fileIcon = value; OnPropertyChanged(nameof(FileIcon)); }
        }

        public ObservableCollection<SeverityCount> SeverityCounts
        {
            get => _severityCounts;
            set { _severityCounts = value; OnPropertyChanged(nameof(SeverityCounts)); }
        }

        public ObservableCollection<VulnerabilityNode> Vulnerabilities
        {
            get => _vulnerabilities;
            set { _vulnerabilities = value; OnPropertyChanged(nameof(Vulnerabilities)); }
        }

        public FileNode()
        {
            SeverityCounts = new ObservableCollection<SeverityCount>();
            Vulnerabilities = new ObservableCollection<VulnerabilityNode>();
        }
    }

    /// <summary>
    /// Represents a severity count badge (e.g., "🔴 2")
    /// </summary>
    public class SeverityCount : INotifyPropertyChanged
    {
        private string _severity;
        private int _count;
        private ImageSource _icon;

        public string Severity
        {
            get => _severity;
            set { _severity = value; OnPropertyChanged(nameof(Severity)); }
        }

        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(nameof(Count)); }
        }

        public ImageSource Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(nameof(Icon)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a vulnerability node with severity icon and details
    /// </summary>
    public class VulnerabilityNode : FindingsTreeNode
    {
        private string _severity;
        private ImageSource _severityIcon;
        private string _description;
        private string _packageName;
        private string _packageVersion;
        private int _line;
        private int _column;
        private string _filePath;
        private ScannerType _scanner;

        public string Severity
        {
            get => _severity;
            set { _severity = value; OnPropertyChanged(nameof(Severity)); }
        }

        public ImageSource SeverityIcon
        {
            get => _severityIcon;
            set { _severityIcon = value; OnPropertyChanged(nameof(SeverityIcon)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string PackageName
        {
            get => _packageName;
            set { _packageName = value; OnPropertyChanged(nameof(PackageName)); }
        }

        public string PackageVersion
        {
            get => _packageVersion;
            set { _packageVersion = value; OnPropertyChanged(nameof(PackageVersion)); }
        }

        public int Line
        {
            get => _line;
            set { _line = value; OnPropertyChanged(nameof(Line)); }
        }

        public int Column
        {
            get => _column;
            set { _column = value; OnPropertyChanged(nameof(Column)); }
        }

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(nameof(FilePath)); }
        }

        /// <summary>Scanner that produced this finding (OSS, ASCA, Secrets, etc.). Used for JetBrains-style primary text.</summary>
        public ScannerType Scanner
        {
            get => _scanner;
            set { _scanner = value; OnPropertyChanged(nameof(Scanner)); }
        }

        /// <summary>
        /// Full formatted display text: primary + " " + agent name + " [Ln N, Col M]" (used for copy, tooltips, message boxes).
        /// </summary>
        public string DisplayText
        {
            get => $"{PrimaryDisplayText} {CxAssistConstants.DisplayName} [Ln {Line}, Col {Column}]";
        }

        /// <summary>Primary text (bright), formatted by scanner like JetBrains IssueTreeRenderer: ASCA/IaC=title, OSS=severity-risk package: name@version, Secrets=severity-risk secret: title, Containers=severity-risk container image: title. Grouped-by-line rows show only the summary (e.g. "N OSS issues detected on this line").</summary>
        public string PrimaryDisplayText
        {
            get
            {
                string title = !string.IsNullOrEmpty(Description) ? Description : "";
                // Grouped-by-line summary rows: show only the message (e.g. "3 OSS issues detected on this line")
                if (title.Contains(" detected on this line") || title.Contains(" violations detected on this line"))
                    return title.TrimEnd();
                switch (Scanner)
                {
                    case ScannerType.ASCA:
                        return title + (string.IsNullOrEmpty(title) ? "" : " ");
                    case ScannerType.OSS:
                        {
                            // JetBrains uses detail.getTitle() + "@" + packageVersion; prefer title (e.g. "validator (CVE-...)") then PackageName
                            string name = !string.IsNullOrEmpty(title) ? title : (PackageName ?? "");
                            string version = !string.IsNullOrEmpty(PackageVersion) ? $"@{PackageVersion}" : "";
                            return $"{Severity}-risk package: {name}{version}";
                        }
                    case ScannerType.Secrets:
                        return $"{Severity}-risk secret: {title}";
                    case ScannerType.Containers:
                        return $"{Severity}-risk container image: {title}";
                    case ScannerType.IaC:
                        return title + (string.IsNullOrEmpty(title) ? "" : " ");
                    default:
                        return title;
                }
            }
        }

        /// <summary>Secondary text (darker grey): agent name + location e.g. "Checkmarx One Assist [Ln 14, Col 4]" for JetBrains-style UI.</summary>
        public string SecondaryDisplayText
        {
            get => $"{CxAssistConstants.DisplayName} [Ln {Line}, Col {Column}]";
        }
    }
}

