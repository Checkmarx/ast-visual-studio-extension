using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace ast_visual_studio_extension.CxExtension.DevAssist.UI.FindingsWindow
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

        /// <summary>
        /// Formatted display text (e.g., "High-risk package: helm.sh/helm/v3@v3.18.2 Checkmarx One Assist [Ln 38, Col 1]")
        /// </summary>
        public string DisplayText
        {
            get
            {
                if (!string.IsNullOrEmpty(PackageName))
                {
                    return $"{Severity}-risk package: {PackageName}@{PackageVersion} Checkmarx One Assist [Ln {Line}, Col {Column}]";
                }
                return $"{Description} Checkmarx One Assist [Ln {Line}, Col {Column}]";
            }
        }
    }
}

