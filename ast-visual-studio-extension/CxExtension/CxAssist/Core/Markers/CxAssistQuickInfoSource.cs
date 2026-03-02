using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    /// <summary>
    /// Static helper for building Quick Info content (ContainerElement, ClassifiedTextElement, ClassifiedTextRun).
    /// Used by <see cref="CxAssistAsyncQuickInfoSource"/> (IAsyncQuickInfoSource); legacy IQuickInfoSource was removed.
    /// </summary>
    internal static class CxAssistQuickInfoSource
    {
        internal const bool UseRichHover = true;

        /// <summary>
        /// Builds Quick Info content for all vulnerabilities on the line (JetBrains-style: grouped by scanner, engine-specific layout).
        /// Single vuln: one scanner block. Multiple same scanner: OSS/Containers show severity counts; ASCA/IAC show per-vuln rows. Multiple scanners: one section per scanner.
        /// </summary>
        internal static object BuildQuickInfoContentForLine(IReadOnlyList<Vulnerability> vulnerabilities)
        {
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return null;
            if (vulnerabilities.Count == 1)
                return BuildQuickInfoContent(vulnerabilities[0]);

            var elements = new List<object>();
            AddHeaderRow(elements);

            var byScanner = vulnerabilities
                .GroupBy(v => v.Scanner)
                .OrderBy(g => g.Key.ToString())
                .ToList();

            for (int i = 0; i < byScanner.Count; i++)
            {
                if (i > 0)
                {
                    var sep = CreateHorizontalSeparator();
                    if (sep != null) elements.Add(sep);
                }
                BuildContentForScannerGroup(byScanner[i].Key, byScanner[i].ToList(), elements);
            }

            var separator = CreateHorizontalSeparator();
            if (separator != null) elements.Add(separator);
            return new ContainerElement(ContainerElementStyle.Stacked, elements);
        }

        /// <summary>
        /// Builds content for a single vulnerability (JetBrains-style: one scanner block).
        /// </summary>
        internal static object BuildQuickInfoContent(Vulnerability v)
        {
            if (v == null) return null;
            var elements = new List<object>();
            AddHeaderRow(elements);
            BuildContentForScannerGroup(v.Scanner, new List<Vulnerability> { v }, elements);
            var separator = CreateHorizontalSeparator();
            if (separator != null) elements.Add(separator);
            return new ContainerElement(ContainerElementStyle.Stacked, elements);
        }

        /// <summary>
        /// Appends DevAssist header row to elements.
        /// </summary>
        private static void AddHeaderRow(List<object> elements)
        {
            var headerRow = CreateHeaderRow();
            if (headerRow != null)
                elements.Add(headerRow);
            else
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, CxAssistConstants.DisplayName, ClassifiedTextRunStyle.UseClassificationStyle | ClassifiedTextRunStyle.UseClassificationFont)
                ));
        }

        /// <summary>
        /// JetBrains-style: one block per scanner type. OSS/Containers = header + severity counts + remediation. Secrets = severity + title + "Secret finding". ASCA/IAC = per-vuln rows with remediation each.
        /// </summary>
        private static void BuildContentForScannerGroup(ScannerType scanner, List<Vulnerability> vulns, List<object> elements)
        {
            if (vulns == null || vulns.Count == 0) return;

            switch (scanner)
            {
                case ScannerType.OSS:
                    BuildOssDescription(vulns, elements);
                    break;
                case ScannerType.Containers:
                    BuildContainerDescription(vulns, elements);
                    break;
                case ScannerType.Secrets:
                    BuildSecretsDescription(vulns, elements);
                    break;
                case ScannerType.ASCA:
                    BuildAscaDescription(vulns, elements);
                    break;
                case ScannerType.IaC:
                    BuildIacDescription(vulns, elements);
                    break;
                default:
                    BuildDefaultDescription(vulns, elements);
                    break;
            }
        }

        /// <summary>OSS: package header (title@version + severity package) + severity count section + remediation (with Ignore all of this type).</summary>
        private static void BuildOssDescription(List<Vulnerability> vulns, List<object> elements)
        {
            var first = vulns[0];
            var title = string.IsNullOrEmpty(first.PackageName) ? (first.Title ?? first.Description ?? "") : first.PackageName;
            var version = first.PackageVersion ?? "";
            var severityLabel = first.Severity == SeverityLevel.Malicious ? "Malicious package" : (GetRichSeverityName(first.Severity) + " " + CxAssistConstants.SeverityPackageLabel);
            var displayTitle = string.IsNullOrEmpty(version) ? title : $"{title}@{version}";
            var packageTitleRow = CreateSeverityTitleRow(first.Severity, $"{displayTitle} - {severityLabel}", severityLabel);
            if (packageTitleRow != null) elements.Add(packageTitleRow);
            else
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, severityLabel, ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + title + (string.IsNullOrEmpty(version) ? "" : "@" + version), ClassifiedTextRunStyle.UseClassificationFont)
                ));
            BuildSeverityCountSection(vulns, elements);
            var linksRow = CreateActionLinksRow(first, includeIgnoreAllOfThisType: true);
            if (linksRow != null) elements.Add(linksRow);
            else AddDefaultActionLinks(first, elements, includeIgnoreAll: true);
        }

        /// <summary>Containers: image header (title@tag) + severity count section + remediation (with Ignore all of this type).</summary>
        private static void BuildContainerDescription(List<Vulnerability> vulns, List<object> elements)
        {
            var first = vulns[0];
            var title = first.Title ?? first.PackageName ?? first.Description ?? "Container image";
            var tag = first.PackageVersion ?? "";
            var headerText = string.IsNullOrEmpty(tag) ? title : $"{title}@{tag}";
            var row = CreateSeverityTitleRow(first.Severity, headerText, "Container");
            if (row != null) elements.Add(row);
            else
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, "Container", ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + headerText, ClassifiedTextRunStyle.UseClassificationFont)
                ));
            BuildSeverityCountSection(vulns, elements);
            var linksRow = CreateActionLinksRow(first, includeIgnoreAllOfThisType: true);
            if (linksRow != null) elements.Add(linksRow);
            else AddDefaultActionLinks(first, elements, includeIgnoreAll: true);
        }

        /// <summary>Secrets: severity icon + bold title + "Secret finding" + remediation.</summary>
        private static void BuildSecretsDescription(List<Vulnerability> vulns, List<object> elements)
        {
            var v = vulns[0];
            var title = v.Title ?? v.RuleName ?? v.Description ?? "";
            var severityTitleRow = CreateSeverityTitleRow(v.Severity, title, CxAssistConstants.SecretFindingLabel);
            if (severityTitleRow != null) elements.Add(severityTitleRow);
            else
            {
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, GetRichSeverityName(v.Severity), ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + title + " - " + CxAssistConstants.SecretFindingLabel, ClassifiedTextRunStyle.UseClassificationFont)
                ));
            }
            var linksRow = CreateActionLinksRow(v, includeIgnoreAllOfThisType: false);
            if (linksRow != null) elements.Add(linksRow);
            else AddDefaultActionLinks(v, elements, includeIgnoreAll: false);
        }

        /// <summary>ASCA: per-vuln row (severity + title + description + "SAST vulnerability") + remediation each.</summary>
        private static void BuildAscaDescription(List<Vulnerability> vulns, List<object> elements)
        {
            foreach (var v in vulns)
            {
                var title = v.Title ?? v.RuleName ?? v.Description ?? "";
                var desc = v.Description ?? "Vulnerability detected by ASCA.";
                var severityTitleRow = CreateSeverityTitleRow(v.Severity, title, GetRichSeverityName(v.Severity));
                if (severityTitleRow != null) elements.Add(severityTitleRow);
                else
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, GetRichSeverityName(v.Severity), ClassifiedTextRunStyle.UseClassificationFont),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + title, ClassifiedTextRunStyle.UseClassificationFont)
                    ));
                var descBlock = CreateDescriptionBlock(desc);
                if (descBlock != null) elements.Add(descBlock);
                else elements.Add(new ClassifiedTextElement(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, desc + " - " + CxAssistConstants.SastVulnerabilityLabel, ClassifiedTextRunStyle.UseClassificationFont)));
                var linksRow = CreateActionLinksRow(v, includeIgnoreAllOfThisType: false);
                if (linksRow != null) elements.Add(linksRow);
                else AddDefaultActionLinks(v, elements, includeIgnoreAll: false);
            }
        }

        /// <summary>IAC: per-vuln row (severity + title + actualValue + description + "IaC vulnerability") + remediation each.</summary>
        private static void BuildIacDescription(List<Vulnerability> vulns, List<object> elements)
        {
            foreach (var v in vulns)
            {
                var title = v.Title ?? v.RuleName ?? v.Description ?? "";
                var actualVal = v.ActualValue ?? "";
                var desc = v.Description ?? "IaC finding.";
                var severityTitleRow = CreateSeverityTitleRow(v.Severity, title, GetRichSeverityName(v.Severity));
                if (severityTitleRow != null) elements.Add(severityTitleRow);
                else
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, GetRichSeverityName(v.Severity), ClassifiedTextRunStyle.UseClassificationFont),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + title, ClassifiedTextRunStyle.UseClassificationFont)
                    ));
                var line2 = string.IsNullOrEmpty(actualVal) ? desc : $"{actualVal} - {desc}";
                var descBlock = CreateDescriptionBlock(line2 + " - " + CxAssistConstants.IacVulnerabilityLabel);
                if (descBlock != null) elements.Add(descBlock);
                else elements.Add(new ClassifiedTextElement(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, line2 + " - " + CxAssistConstants.IacVulnerabilityLabel, ClassifiedTextRunStyle.UseClassificationFont)));
                var linksRow = CreateActionLinksRow(v, includeIgnoreAllOfThisType: false);
                if (linksRow != null) elements.Add(linksRow);
                else AddDefaultActionLinks(v, elements, includeIgnoreAll: false);
            }
        }

        /// <summary>Default: severity + title + description + remediation.</summary>
        private static void BuildDefaultDescription(List<Vulnerability> vulns, List<object> elements)
        {
            var v = vulns[0];
            var title = v.Title ?? v.RuleName ?? v.Description ?? "";
            var description = v.Description ?? "Vulnerability detected by " + v.Scanner + ".";
            var severityTitleRow = CreateSeverityTitleRow(v.Severity, title, GetRichSeverityName(v.Severity));
            if (severityTitleRow != null) elements.Add(severityTitleRow);
            else
            {
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, GetRichSeverityName(v.Severity), ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + title, ClassifiedTextRunStyle.UseClassificationFont)
                ));
            }
            var descBlock = CreateDescriptionBlock(description);
            if (descBlock != null) elements.Add(descBlock);
            else elements.Add(new ClassifiedTextElement(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, description, ClassifiedTextRunStyle.UseClassificationFont)));
            var linksRow = CreateActionLinksRow(v, includeIgnoreAllOfThisType: false);
            if (linksRow != null) elements.Add(linksRow);
            else AddDefaultActionLinks(v, elements, includeIgnoreAll: false);
        }

        /// <summary>Severity count row: icon + count for each severity (JetBrains buildVulnerabilitySection).</summary>
        private static void BuildSeverityCountSection(List<Vulnerability> vulns, List<object> elements)
        {
            var counts = vulns.GroupBy(x => x.Severity).ToDictionary(g => g.Key, g => g.Count());
            var order = new[] { SeverityLevel.Malicious, SeverityLevel.Critical, SeverityLevel.High, SeverityLevel.Medium, SeverityLevel.Low, SeverityLevel.Info };
            try
            {
                var panel = ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var stack = new StackPanel { Orientation = Orientation.Horizontal };
                    foreach (var sev in order)
                        if (counts.TryGetValue(sev, out var c) && c > 0)
                        {
                            var icon = CreateSmallSeverityIcon(sev);
                            if (icon != null) stack.Children.Add(icon);
                            stack.Children.Add(new TextBlock { Text = c.ToString(), FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAD, 0xAD)), Margin = new Thickness(2, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
                        }
                    return (System.Windows.UIElement)stack;
                });
                if (panel != null && ((System.Windows.Controls.Panel)panel).Children.Count > 0)
                    elements.Add(panel);
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.BuildSeverityCountSection");
            }
        }

        private static System.Windows.UIElement CreateSmallSeverityIcon(SeverityLevel severity)
        {
            string theme = GetCurrentTheme();
            string fileName = GetSeverityIconFileName(severity);
            if (string.IsNullOrEmpty(fileName)) return null;
            var source = LoadIconFromAssembly(theme, fileName);
            if (source == null && theme != CxAssistConstants.ThemeDark) source = LoadIconFromAssembly(CxAssistConstants.ThemeDark, fileName);
            if (source == null) return null;
            return new Image { Source = source, Width = 14, Height = 14, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) };
        }

        private static void AddDefaultActionLinks(Vulnerability v, List<object> elements, bool includeIgnoreAll)
        {
            const string urlClassification = "url";
            var runs = new List<ClassifiedTextRun>
            {
                new ClassifiedTextRun(urlClassification, CxAssistConstants.FixWithCxOneAssist, () => RunFixWithAssist(v), CxAssistConstants.FixWithCxOneAssist, ClassifiedTextRunStyle.Underline),
                new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "  ", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun(urlClassification, CxAssistConstants.ViewDetails, () => RunViewDetails(v), CxAssistConstants.ViewDetails, ClassifiedTextRunStyle.Underline),
                new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "  ", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun(urlClassification, CxAssistConstants.IgnoreThis, () => RunIgnoreVulnerability(v), CxAssistConstants.IgnoreThis, ClassifiedTextRunStyle.Underline)
            };
            if (includeIgnoreAll)
            {
                runs.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "  ", ClassifiedTextRunStyle.UseClassificationFont));
                runs.Add(new ClassifiedTextRun(urlClassification, CxAssistConstants.IgnoreAllOfThisType, () => RunIgnoreAllOfThisType(v), CxAssistConstants.IgnoreAllOfThisType, ClassifiedTextRunStyle.Underline));
            }
            elements.Add(new ClassifiedTextElement(runs.ToArray()));
        }

        internal static void RunIgnoreAllOfThisType(Vulnerability v)
        {
            RunOnUiThread(() => MessageBox.Show($"Ignore all of this type:\n{v?.Title ?? v?.Description ?? "—"}\n(Scanner: {v?.Scanner})", CxAssistConstants.DisplayName));
        }

        internal static void RunFixWithAssist(Vulnerability v)
        {
            RunOnUiThread(() => MessageBox.Show($"Fix with {CxAssistConstants.DisplayName}:\n{v?.Title ?? v?.Description ?? "—"}", CxAssistConstants.DisplayName));
        }

        internal static void RunViewDetails(Vulnerability v)
        {
            var url = !string.IsNullOrEmpty(v?.LearnMoreUrl) ? v.LearnMoreUrl : v?.FixLink;
            if (!string.IsNullOrEmpty(url))
            {
                RunOnUiThread(() =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"CxAssist QuickInfo View Details: {ex.Message}");
                        MessageBox.Show($"Could not open link: {url}", CxAssistConstants.DisplayName);
                    }
                });
            }
            else
            {
                RunOnUiThread(() => MessageBox.Show($"View Details:\n{v?.Title ?? ""}\n{v?.Description ?? ""}\nSeverity: {v?.Severity}", CxAssistConstants.DisplayName));
            }
        }

        internal static void RunIgnoreVulnerability(Vulnerability v)
        {
            RunOnUiThread(() => MessageBox.Show($"Ignore vulnerability:\n{v?.Title ?? v?.Description ?? "—"}", CxAssistConstants.DisplayName));
        }

        internal static void RunOnUiThread(Action action)
        {
            if (action == null) return;
            try
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(action, DispatcherPriority.Send);
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.RunOnUiThread");
            }
        }

        /// <summary>
        /// Description text with extra line spacing between lines.
        /// </summary>
        private static System.Windows.UIElement CreateDescriptionBlock(string description)
        {
            if (string.IsNullOrEmpty(description)) return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return new TextBlock
                    {
                        Text = description,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 20,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 6)
                    };
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateDescriptionBlock");
                return null;
            }
        }

        /// <summary>
        /// Action links row: Fix with Checkmarx Assist, View Details, Ignore vulnerability; for OSS/Containers also "Ignore all of this type" (JetBrains-style).
        /// </summary>
        private static System.Windows.UIElement CreateActionLinksRow(Vulnerability v, bool includeIgnoreAllOfThisType = false)
        {
            if (v == null) return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var linkBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));
                    var panel = new StackPanel { Orientation = Orientation.Horizontal };

                    void AddLink(string text, Action clickAction)
                    {
                        var block = new TextBlock
                        {
                            Text = text,
                            Foreground = linkBrush,
                            Cursor = System.Windows.Input.Cursors.Hand,
                            Margin = new Thickness(0, 0, 12, 0)
                        };
                        block.MouseEnter += (s, _) => { block.TextDecorations = TextDecorations.Underline; };
                        block.MouseLeave += (s, _) => { block.TextDecorations = null; };
                        block.MouseLeftButtonDown += (s, _) => { RunOnUiThread(clickAction); };
                        panel.Children.Add(block);
                    }

                    AddLink(CxAssistConstants.FixWithCxOneAssist, () => RunFixWithAssist(v));
                    AddLink(CxAssistConstants.ViewDetails, () => RunViewDetails(v));
                    AddLink(CxAssistConstants.IgnoreThis, () => RunIgnoreVulnerability(v));
                    if (includeIgnoreAllOfThisType)
                        AddLink(CxAssistConstants.IgnoreAllOfThisType, () => RunIgnoreAllOfThisType(v));

                    return (System.Windows.UIElement)panel;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateActionLinksRow");
                return null;
            }
        }

        /// <summary>
        /// Creates a thin horizontal line (separator) to show after our Quick Info details.
        /// </summary>
        private static System.Windows.UIElement CreateHorizontalSeparator()
        {
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return new Border
                    {
                        Height = 1,
                        Margin = new Thickness(0, 6, 0, 0),
                        Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
                    };
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateHorizontalSeparator");
                return null;
            }
        }

        /// <summary>
        /// Returns current VS theme folder name for icon paths: "Dark" or "Light".
        /// Uses VSColorTheme so badge and severity icons follow IDE theme dynamically.
        /// </summary>
        private static string GetCurrentTheme()
        {
            try
            {
                var backgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                double brightness = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255.0;
                return brightness < 0.5 ? CxAssistConstants.ThemeDark : CxAssistConstants.ThemeLight;
            }
            catch
            {
                return CxAssistConstants.ThemeDark;
            }
        }

        /// <summary>
        /// Header row: badge + "Checkmarx One Assist" text (custom-popup style, no custom popup).
        /// </summary>
        private static System.Windows.UIElement CreateHeaderRow()
        {
            string theme = GetCurrentTheme();
            var source = LoadIconFromAssembly(theme, CxAssistConstants.BadgeIconFileName);
            if (source == null && theme != CxAssistConstants.ThemeDark)
                source = LoadIconFromAssembly(CxAssistConstants.ThemeDark, CxAssistConstants.BadgeIconFileName);
            if (source == null)
                return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var image = new Image
                    {
                        Source = source,
                        Width = 150,
                        Height = 32,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0)
                    };
                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 0, 0, 6)
                    };
                    panel.Children.Add(image);
                    return (System.Windows.UIElement)panel;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateHeaderRow");
                return null;
            }
        }

        /// <summary>
        /// Severity + title row: icon + finding title on one line (custom-popup style).
        /// </summary>
        private static System.Windows.UIElement CreateSeverityTitleRow(SeverityLevel severity, string title, string severityName)
        {
            string theme = GetCurrentTheme();
            string fileName = GetSeverityIconFileName(severity);
            ImageSource severitySource = null;
            if (!string.IsNullOrEmpty(fileName))
            {
                severitySource = LoadIconFromAssembly(theme, fileName);
                if (severitySource == null && theme != CxAssistConstants.ThemeDark)
                    severitySource = LoadIconFromAssembly(CxAssistConstants.ThemeDark, fileName);
            }
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                    if (severitySource != null)
                    {
                        var image = new Image
                        {
                            Source = severitySource,
                            Width = 16,
                            Height = 16,
                            Stretch = Stretch.Uniform,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 6, 0)
                        };
                        panel.Children.Add(image);
                    }
                    var text = new TextBlock
                    {
                        Text = string.IsNullOrEmpty(title) ? severityName : title,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    };
                    panel.Children.Add(text);
                    return (System.Windows.UIElement)panel;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateSeverityTitleRow");
                return null;
            }
        }

        /// <summary>
        /// Creates a WPF Image for the Checkmarx One Assist badge only (theme-based). Used when not using header row.
        /// </summary>
        private static System.Windows.UIElement CreateCxAssistBadgeImage()
        {
            string theme = GetCurrentTheme();
            var source = LoadIconFromAssembly(theme, CxAssistConstants.BadgeIconFileName);
            if (source == null && theme != CxAssistConstants.ThemeDark)
                source = LoadIconFromAssembly(CxAssistConstants.ThemeDark, CxAssistConstants.BadgeIconFileName);
            if (source == null)
                return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var image = new Image
                    {
                        Source = source,
                        Width = 150,
                        Height = 32,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    return (System.Windows.UIElement)image;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateCxAssistBadgeImage");
                return null;
            }
        }

        /// <summary>
        /// Creates a WPF Image for the severity icon (theme-based, dynamic by SeverityLevel).
        /// </summary>
        private static System.Windows.UIElement CreateSeverityImage(SeverityLevel severity)
        {
            string theme = GetCurrentTheme();
            string fileName = GetSeverityIconFileName(severity);
            if (string.IsNullOrEmpty(fileName))
                return null;
            var source = LoadIconFromAssembly(theme, fileName);
            if (source == null && theme != CxAssistConstants.ThemeDark)
                source = LoadIconFromAssembly(CxAssistConstants.ThemeDark, fileName);
            if (source == null)
                return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var image = new Image
                    {
                        Source = source,
                        Width = 16,
                        Height = 16,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    return (System.Windows.UIElement)image;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateSeverityImage");
                return null;
            }
        }

        private static string GetSeverityIconFileName(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Malicious: return "malicious.png";
                case SeverityLevel.Critical: return "critical.png";
                case SeverityLevel.High: return "high.png";
                case SeverityLevel.Medium: return "medium.png";
                case SeverityLevel.Low:
                case SeverityLevel.Info: return "low.png";
                default: return null;
            }
        }

        /// <summary>
        /// Loads a PNG from CxAssist Icons by theme (Dark/Light). Used for badge and severity icons.
        /// </summary>
        private static BitmapImage LoadIconFromAssembly(string theme, string fileName)
        {
            var packPath = $"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/CxAssist/Icons/{theme}/{fileName}";
            try
            {
                var streamInfo = System.Windows.Application.GetResourceStream(new Uri(packPath, UriKind.Absolute));
                if (streamInfo?.Stream != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        streamInfo.Stream.CopyTo(ms);
                        ms.Position = 0;
                        var img = new BitmapImage();
                        img.BeginInit();
                        img.StreamSource = ms;
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        img.Freeze();
                        return img;
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, $"QuickInfo.LoadIconFromAssembly (pack): {fileName}");
            }

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var resourceName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.Replace('\\', '/').EndsWith($"CxAssist/Icons/{theme}/{fileName}", StringComparison.OrdinalIgnoreCase)
                                      || n.Replace('\\', '.').EndsWith($"CxAssist.Icons.{theme}.{fileName}", StringComparison.OrdinalIgnoreCase));
                if (resourceName != null)
                {
                    using (var stream = asm.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            var img = new BitmapImage();
                            img.BeginInit();
                            img.StreamSource = stream;
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.EndInit();
                            img.Freeze();
                            return img;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, $"QuickInfo.LoadIconFromAssembly (manifest): {fileName}");
            }

            return null;
        }

        internal static string GetRichSeverityName(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Critical: return "Critical";
                case SeverityLevel.High: return "High";
                case SeverityLevel.Medium: return "Medium";
                case SeverityLevel.Low: return "Low";
                case SeverityLevel.Info: return "Info";
                case SeverityLevel.Malicious: return "Malicious";
                case SeverityLevel.Unknown: return "Unknown";
                case SeverityLevel.Ok: return "Ok";
                case SeverityLevel.Ignored: return "Ignored";
                default: return severity.ToString();
            }
        }
    }
}
