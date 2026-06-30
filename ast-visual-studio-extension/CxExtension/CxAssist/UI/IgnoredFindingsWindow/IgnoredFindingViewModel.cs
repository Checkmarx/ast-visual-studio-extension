using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore;

namespace ast_visual_studio_extension.CxExtension.CxAssist.UI.IgnoredFindingsWindow
{
    /// <summary>A single file reference row shown inside a finding row.</summary>
    public sealed class FileReferenceViewModel
    {
        public string AbsolutePath { get; }
        public string DisplayText { get; }
        public int? Line { get; }

        public FileReferenceViewModel(string relativePath, int? line, string solutionRoot)
        {
            Line = line;
            DisplayText = Path.GetFileName(relativePath ?? string.Empty);

            if (!string.IsNullOrEmpty(solutionRoot) && !string.IsNullOrEmpty(relativePath))
                AbsolutePath = Path.Combine(solutionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }

    /// <summary>
    /// Row view-model for the Ignored Findings tab. Wraps an <see cref="IgnoreEntry"/> with display-friendly
    /// properties, clickable file references, and expand/collapse for entries with multiple files.
    /// </summary>
    public sealed class IgnoredFindingViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isExpanded;
        private ImageSource _severityIcon;
        private ImageSource _scannerChipIcon;

        public IgnoreEntry Entry { get; }
        public string Key { get; }

        public IgnoredFindingViewModel(IgnoreEntry entry, string key)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            Key = key;

            string root = IgnoreFileManager.SolutionRoot;
            _allFileRefs = (entry.Files ?? Enumerable.Empty<IgnoreEntry.FileReference>())
                .Where(f => f.Active)
                .Select(f => new FileReferenceViewModel(f.Path, f.Line, root))
                .ToList();
        }

        private readonly List<FileReferenceViewModel> _allFileRefs;

        /// <summary>Scanner type (used by scanner filter).</summary>
        public ScannerType Scanner => Entry.Type;

        public string Title
        {
            get
            {
                string baseName;
                switch (Entry.Type)
                {
                    case ScannerType.OSS:
                        string mgr = Entry.PackageManager ?? "pkg";
                        string ver = !string.IsNullOrEmpty(Entry.PackageVersion) ? "@" + Entry.PackageVersion : string.Empty;
                        baseName = $"{mgr}@{Entry.PackageName}{ver}";
                        break;
                    case ScannerType.Containers:
                        baseName = $"{Entry.ImageName}:{Entry.ImageTag}";
                        break;
                    default:
                        baseName = Entry.Title ?? string.Empty;
                        break;
                }

                string severity = Entry.Severity ?? string.Empty;
                return !string.IsNullOrEmpty(severity) ? $"{severity} {baseName}" : baseName;
            }
        }

        public string SeverityText => Entry.Severity ?? string.Empty;
        public string ScannerLabel => Entry.Type.ToString();

        public string ScannerChipLabel
        {
            get
            {
                switch (Entry.Type)
                {
                    case ScannerType.OSS:        return "SCA";
                    case ScannerType.Containers: return "CONTAINERS";
                    case ScannerType.IaC:        return "IAC";
                    case ScannerType.ASCA:       return "SAST";
                    case ScannerType.Secrets:    return "SECRETS";
                    default: return Entry.Type.ToString().ToUpperInvariant();
                }
            }
        }

        public ImageSource FileIcon { get; set; }

        /// <summary>Pre-built card icon (scanner-type × severity SVG) — exact match to JetBrains ignored_card icons.</summary>
        public ImageSource CardIcon { get; set; }

        /// <summary>Revive button SVG icon (revive.svg).</summary>
        public ImageSource ReviveIcon { get; set; }

        // ── File references ────────────────────────────────────────────────────────
        /// <summary>All active file references for this entry.</summary>
        public IReadOnlyList<FileReferenceViewModel> AllFileRefs => _allFileRefs;

        /// <summary>Visible refs: first one when collapsed, all when expanded.</summary>
        public IEnumerable<FileReferenceViewModel> VisibleFileRefs =>
            _isExpanded || _allFileRefs.Count <= 1 ? _allFileRefs : _allFileRefs.Take(1);

        public bool HasHiddenFiles => !_isExpanded && _allFileRefs.Count > 1;
        public bool CanCollapse   => _isExpanded  && _allFileRefs.Count > 1;

        public string ExpandLabel => $"and {_allFileRefs.Count - 1} more files";

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(VisibleFileRefs));
                OnPropertyChanged(nameof(HasHiddenFiles));
                OnPropertyChanged(nameof(CanCollapse));
            }
        }

        // ── Date ──────────────────────────────────────────────────────────────────
        public string DateAddedDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Entry.DateAdded)) return string.Empty;
                if (DateTime.TryParse(Entry.DateAdded, out var dt))
                {
                    long days = (long)(DateTime.UtcNow.Date - dt.ToUniversalTime().Date).TotalDays;
                    if (days == 0) return "Today";
                    if (days < 2) return "1 day ago";
                    if (days < 7) return $"{days} days ago";
                    if (days < 30) { int w = (int)(days / 7); return w == 1 ? "1 week ago" : $"{w} weeks ago"; }
                    if (days < 365) { int m = (int)(days / 30); return m == 1 ? "1 month ago" : $"{m} months ago"; }
                    int y = (int)(days / 365); return y == 1 ? "1 year ago" : $"{y} years ago";
                }
                return Entry.DateAdded;
            }
        }

        public string DescriptionDisplay
        {
            get
            {
                if (Entry.Type == ScannerType.OSS && Entry.Severity == "Malicious")
                {
                    string packageId = !string.IsNullOrEmpty(Entry.PackageVersion)
                        ? $"{Entry.PackageName}@{Entry.PackageVersion}"
                        : Entry.PackageName;
                    return $"Malicious package: {packageId}";
                }
                return Entry.Description ?? string.Empty;
            }
        }

        public DateTime SortableDateAdded
        {
            get
            {
                if (!string.IsNullOrEmpty(Entry.DateAdded) && DateTime.TryParse(Entry.DateAdded, out var dt))
                    return dt;
                return DateTime.MinValue;
            }
        }

        // ── Icons ─────────────────────────────────────────────────────────────────
        public ImageSource SeverityIcon
        {
            get => _severityIcon;
            set { _severityIcon = value; OnPropertyChanged(nameof(SeverityIcon)); }
        }

        public ImageSource ScannerChipIcon
        {
            get => _scannerChipIcon;
            set { _scannerChipIcon = value; OnPropertyChanged(nameof(ScannerChipIcon)); }
        }

        // ── Selection ─────────────────────────────────────────────────────────────
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Sort options for the Ignored Findings tab.</summary>
    public enum IgnoredFindingsSortMode
    {
        SeverityHighToLow = 0,
        SeverityLowToHigh = 1,
        DateAddedNewestFirst = 2,
        DateAddedOldestFirst = 3
    }
}
