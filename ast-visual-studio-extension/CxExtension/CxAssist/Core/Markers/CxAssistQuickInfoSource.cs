using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        /// Builds Quick Info content for all vulnerabilities on the line (reference-style: grouped by scanner, engine-specific layout).
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
        /// Builds content for a single vulnerability (reference-style: one scanner block).
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
        /// reference-style: one block per scanner type. OSS/Containers = header + severity counts + remediation. Secrets = severity + title + "Secret finding". ASCA/IAC = per-vuln rows with remediation each.
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

        /// <summary>OSS: package header (title@version + highest severity + "Severity Package", reference-style) + severity count badges (e.g. H 1, M 1) + remediation (with Ignore all of this type).</summary>
        private static void BuildOssDescription(List<Vulnerability> vulns, List<object> elements)
        {
            var first = vulns[0];
            var title = string.IsNullOrEmpty(first.PackageName) ? (first.Title ?? first.Description ?? "") : first.PackageName;
            var version = first.PackageVersion ?? "";
            // Use highest severity among all vulns for header (e.g. validator with 1 High + 1 Medium → "High Severity Package")
            var headerSeverity = GetHighestSeverity(vulns);
            var severityLabel = headerSeverity == SeverityLevel.Malicious ? "Malicious package" : (CxAssistConstants.GetRichSeverityName(headerSeverity) + " " + CxAssistConstants.SeverityPackageLabel);
            var displayTitle = string.IsNullOrEmpty(version) ? title : $"{title}@{version}";
            // reference: package row uses neutral package/cube icon (not severity icon); Malicious keeps severity icon; severity label greyed out
            var packageTitleRow = headerSeverity == SeverityLevel.Malicious
                ? CreateSeverityTitleRow(headerSeverity, $"{displayTitle} - {severityLabel}", severityLabel)
                : CreateOssPackageTitleRow(displayTitle, severityLabel);
            if (packageTitleRow != null) elements.Add(packageTitleRow);
            else
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, severityLabel, ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + title + (string.IsNullOrEmpty(version) ? "" : "@" + version), ClassifiedTextRunStyle.UseClassificationFont)
                ));
            // Reference plugin: count row only for Critical/High/Medium/Low; do not show count for Malicious-only
            if (vulns.Any(v => v.Severity != SeverityLevel.Malicious))
                BuildSeverityCountSection(vulns, elements);
            var linksRow = CreateActionLinksRow(first, includeIgnoreAllOfThisType: true);
            if (linksRow != null) elements.Add(linksRow);
            else AddDefaultActionLinks(first, elements, includeIgnoreAll: true);
        }

        /// <summary>Containers: container icon + "imageName:tag - Critical Severity Image" (JetBrains-style); fallback to severity icon if no container icon.</summary>
        private static void BuildContainerDescription(List<Vulnerability> vulns, List<object> elements)
        {
            var first = vulns[0];
            var title = first.Title ?? first.PackageName ?? first.Description ?? "Container image";
            var tag = first.PackageVersion ?? "";
            var headerText = string.IsNullOrEmpty(tag) ? title : $"{title}@{tag}";
            var headerSeverity = GetHighestSeverity(vulns);
            var severityLabel = headerSeverity == SeverityLevel.Malicious
                ? "Malicious image"
                : (CxAssistConstants.GetRichSeverityName(headerSeverity) + " " + CxAssistConstants.SeverityImageLabel);
            // Prefer container icon (neutral) + text, like OSS package row; fallback to severity icon if no container icon
            var row = CreateContainerTitleRow(headerText, severityLabel);
            if (row == null)
            {
                var displayTitle = $"{headerText} - {severityLabel}";
                row = CreateSeverityTitleRow(headerSeverity, displayTitle, severityLabel);
            }
            if (row != null) elements.Add(row);
            else
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, severityLabel, ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + headerText, ClassifiedTextRunStyle.UseClassificationFont)
                ));
            if (vulns.Any(v => v.Severity != SeverityLevel.Malicious))
                BuildSeverityCountSection(vulns, elements);
            var linksRow = CreateActionLinksRow(first, includeIgnoreAllOfThisType: true);
            if (linksRow != null) elements.Add(linksRow);
            else AddDefaultActionLinks(first, elements, includeIgnoreAll: true);
        }

        /// <summary>Returns the highest severity present in the list (for OSS package header: e.g. High when vulns are High + Medium).</summary>
        private static SeverityLevel GetHighestSeverity(List<Vulnerability> vulns)
        {
            if (vulns == null || vulns.Count == 0) return SeverityLevel.Unknown;
            var order = new[] { SeverityLevel.Malicious, SeverityLevel.Critical, SeverityLevel.High, SeverityLevel.Medium, SeverityLevel.Low, SeverityLevel.Info, SeverityLevel.Unknown, SeverityLevel.Ok, SeverityLevel.Ignored };
            var set = vulns.Select(x => x.Severity).ToHashSet();
            return order.FirstOrDefault(s => set.Contains(s));
        }

        /// <summary>Secrets: severity icon + bold title (Title-Case) + grey " - Secret finding" + three actions (reference-style).</summary>
        private static void BuildSecretsDescription(List<Vulnerability> vulns, List<object> elements)
        {
            var v = vulns[0];
            var rawTitle = v.Title ?? v.RuleName ?? v.Description ?? "";
            var displayTitle = CxAssistConstants.FormatSecretTitle(rawTitle);
            var secretRow = CreateSecretFindingTitleRow(v.Severity, displayTitle);
            if (secretRow != null) elements.Add(secretRow);
            else
            {
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, CxAssistConstants.GetRichSeverityName(v.Severity), ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + displayTitle + " - " + CxAssistConstants.SecretFindingLabel, ClassifiedTextRunStyle.UseClassificationFont)
                ));
            }
            var linksRow = CreateActionLinksRow(v, includeIgnoreAllOfThisType: false);
            if (linksRow != null) elements.Add(linksRow);
            else AddDefaultActionLinks(v, elements, includeIgnoreAll: false);
        }

        /// <summary>ASCA: reference-style — summary line when multiple; per-vuln row (icon + bold title - description - grey "SAST vulnerability"); separators between entries.</summary>
        private static void BuildAscaDescription(List<Vulnerability> vulns, List<object> elements)
        {
            if (vulns == null || vulns.Count == 0) return;

            if (vulns.Count > 1)
            {
                var summaryRow = CreateMultipleIssuesSummaryRow(vulns.Count, CxAssistConstants.MultipleAscaViolationsOnLine);
                if (summaryRow != null) elements.Add(summaryRow);
            }

            for (int i = 0; i < vulns.Count; i++)
            {
                var v = vulns[i];
                var title = v.Title ?? v.RuleName ?? v.Description ?? "";
                var desc = v.Description ?? "Vulnerability detected by ASCA.";
                var ascaRow = CreateAscaTitleRow(v.Severity, title, desc);
                if (ascaRow != null) elements.Add(ascaRow);
                else
                {
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, CxAssistConstants.GetRichSeverityName(v.Severity), ClassifiedTextRunStyle.UseClassificationFont),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + title + " - " + desc + " - " + CxAssistConstants.SastVulnerabilityLabel, ClassifiedTextRunStyle.UseClassificationFont)
                    ));
                }
                var linksRow = CreateActionLinksRow(v, includeIgnoreAllOfThisType: false);
                if (linksRow != null) elements.Add(linksRow);
                else AddDefaultActionLinks(v, elements, includeIgnoreAll: false);
                if (i < vulns.Count - 1)
                {
                    var sep = CreateHorizontalSeparator();
                    if (sep != null) elements.Add(sep);
                }
            }
        }

        /// <summary>IaC: reference-style — summary line when multiple; per-vuln row (icon + bold title - actualValue description - grey "IaC vulnerability"); separators between entries.</summary>
        private static void BuildIacDescription(List<Vulnerability> vulns, List<object> elements)
        {
            if (vulns == null || vulns.Count == 0) return;

            // Summary line for multiple issues (reference: "4 IAC issues detected on this line Checkmarx One Assist")
            if (vulns.Count > 1)
            {
                var summaryRow = CreateMultipleIssuesSummaryRow(vulns.Count, CxAssistConstants.MultipleIacIssuesOnLine);
                if (summaryRow != null) elements.Add(summaryRow);
            }

            for (int i = 0; i < vulns.Count; i++)
            {
                var v = vulns[i];
                var title = v.Title ?? v.RuleName ?? v.Description ?? "";
                var actualVal = v.ActualValue ?? "";
                var desc = v.Description ?? "IaC finding.";
                var iacRow = CreateIacTitleRow(v.Severity, title, actualVal, desc);
                if (iacRow != null) elements.Add(iacRow);
                else
                {
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, CxAssistConstants.GetRichSeverityName(v.Severity), ClassifiedTextRunStyle.UseClassificationFont),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, " " + title + (string.IsNullOrEmpty(actualVal) ? "" : " - " + actualVal) + " " + desc + " " + CxAssistConstants.IacVulnerabilityLabel, ClassifiedTextRunStyle.UseClassificationFont)
                    ));
                }
                var linksRow = CreateActionLinksRow(v, includeIgnoreAllOfThisType: false);
                if (linksRow != null) elements.Add(linksRow);
                else AddDefaultActionLinks(v, elements, includeIgnoreAll: false);
                // Separator between entries (reference: thin grey line between each finding)
                if (i < vulns.Count - 1)
                {
                    var sep = CreateHorizontalSeparator();
                    if (sep != null) elements.Add(sep);
                }
            }
        }

        /// <summary>Default: severity + title + description + remediation.</summary>
        private static void BuildDefaultDescription(List<Vulnerability> vulns, List<object> elements)
        {
            var v = vulns[0];
            var title = v.Title ?? v.RuleName ?? v.Description ?? "";
            var description = v.Description ?? "Vulnerability detected by " + v.Scanner + ".";
            var severityTitleRow = CreateSeverityTitleRow(v.Severity, title, CxAssistConstants.GetRichSeverityName(v.Severity));
            if (severityTitleRow != null) elements.Add(severityTitleRow);
            else
            {
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, CxAssistConstants.GetRichSeverityName(v.Severity), ClassifiedTextRunStyle.UseClassificationFont),
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

        /// <summary>Severity count row: icon + bold count for each severity. Never show count for Malicious package (reference plugin has no Malicious count icon).</summary>
        private static void BuildSeverityCountSection(List<Vulnerability> vulns, List<object> elements)
        {
            if (vulns == null || vulns.Count == 0) return;
            // Do not show count row when all findings are Malicious
            if (vulns.All(v => v.Severity == SeverityLevel.Malicious)) return;

            var counts = vulns.GroupBy(x => x.Severity).ToDictionary(g => g.Key, g => g.Count());
            // Only Critical, High, Medium, Low, Info—never Malicious
            var order = new[] { SeverityLevel.Critical, SeverityLevel.High, SeverityLevel.Medium, SeverityLevel.Low, SeverityLevel.Info };

            try
            {
                var panel = ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var stack = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    foreach (var sev in order)
                        if (counts.TryGetValue(sev, out var c) && c > 0)
                        {
                            var icon = CreateSmallSeverityIcon(sev);
                            if (icon != null) stack.Children.Add(icon);
                            stack.Children.Add(new TextBlock
                            {
                                Text = c.ToString(),
                                FontSize = 10,
                                FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAD, 0xAD)),
                                Margin = new Thickness(2, 0, 8, 0),
                                VerticalAlignment = VerticalAlignment.Center
                            });
                        }
                    if (stack.Children.Count == 0) return null;
                    var border = new Border
                    {
                        Child = stack,
                        MinHeight = 18,
                        Height = 18,
                        Margin = new Thickness(0, 2, 0, 4),
                        VerticalAlignment = VerticalAlignment.Top,
                        Padding = new Thickness(0)
                    };
                    return (System.Windows.UIElement)border;
                });
                if (panel != null)
                    elements.Add(panel);
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.BuildSeverityCountSection");
            }
        }

        private static System.Windows.UIElement CreateSmallSeverityIcon(SeverityLevel severity)
        {
            var source = AssistIconLoader.LoadSeverityIcon(severity);
            if (source == null) return null;
            return new Image { Source = source, Width = 14, Height = 14, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) };
        }

        private static void AddDefaultActionLinks(Vulnerability v, List<object> elements, bool includeIgnoreAll)
        {
            const string urlClassification = "url";
            string ignoreThisLabel = CxAssistConstants.GetIgnoreThisLabel(v.Scanner);
            var runs = new List<ClassifiedTextRun>
            {
                new ClassifiedTextRun(urlClassification, CxAssistConstants.FixWithCxOneAssist, () => RunFixWithAssist(v), CxAssistConstants.FixWithCxOneAssist, ClassifiedTextRunStyle.Underline),
                new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "  ", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun(urlClassification, CxAssistConstants.ViewDetails, () => RunViewDetails(v), CxAssistConstants.ViewDetails, ClassifiedTextRunStyle.Underline),
                new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "  ", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun(urlClassification, ignoreThisLabel, () => RunIgnoreVulnerability(v), ignoreThisLabel, ClassifiedTextRunStyle.Underline)
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
        /// Action links row: Fix with Checkmarx Assist, View Details, Ignore vulnerability; for OSS/Containers also "Ignore all of this type" (reference-style).
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
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };

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
                    AddLink(CxAssistConstants.GetIgnoreThisLabel(v.Scanner), () => RunIgnoreVulnerability(v));
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
        /// Summary row when multiple issues on same line (reference: "4 IAC issues detected on this line Checkmarx One Assist" with suffix grey).
        /// </summary>
        private static System.Windows.UIElement CreateMultipleIssuesSummaryRow(int count, string suffix)
        {
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var brightBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                    var greyBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0xAD, 0xAD));
                    var text = new TextBlock
                    {
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 18,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                        Margin = new Thickness(0, 0, 0, 6)
                    };
                    text.Inlines.Add(new Run(count + suffix) { Foreground = brightBrush });
                    text.Inlines.Add(new Run(" " + CxAssistConstants.DisplayName) { Foreground = greyBrush });
                    return (System.Windows.UIElement)text;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateMultipleIssuesSummaryRow");
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
        /// Header row: badge + "Checkmarx One Assist" text (custom-popup style, no custom popup).
        /// </summary>
        private static System.Windows.UIElement CreateHeaderRow()
        {
            var source = AssistIconLoader.LoadBadgeIcon();
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
        /// Container image title row: neutral container icon + image:tag (bold) + " - " + severity label (greyed), JetBrains-style.
        /// </summary>
        private static System.Windows.UIElement CreateContainerTitleRow(string displayTitle, string severityLabel)
        {
            var containerSource = AssistIconLoader.LoadContainerIcon();
            if (containerSource == null) return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var image = new Image
                    {
                        Source = containerSource,
                        Width = 16,
                        Height = 16,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    Grid.SetColumn(image, 0);
                    grid.Children.Add(image);
                    const double fontSize = 12;
                    var brightBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                    var greyBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0xAD, 0xAD));
                    var text = new TextBlock
                    {
                        FontSize = fontSize,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 18,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                    };
                    text.Inlines.Add(new Run(displayTitle) { FontWeight = FontWeights.SemiBold, Foreground = brightBrush });
                    text.Inlines.Add(new Run(" - ") { Foreground = greyBrush });
                    text.Inlines.Add(new Run(severityLabel) { Foreground = greyBrush });
                    Grid.SetColumn(text, 1);
                    grid.Children.Add(text);
                    return (System.Windows.UIElement)grid;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateContainerTitleRow");
                return null;
            }
        }

        /// <summary>
        /// OSS package title row: neutral package/cube icon + package name (bold) + " - " + severity label (greyed, reference 11px).
        /// </summary>
        private static System.Windows.UIElement CreateOssPackageTitleRow(string displayTitle, string severityLabel)
        {
            var packageSource = AssistIconLoader.LoadPackageIcon();
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    if (packageSource != null)
                    {
                        var image = new Image
                        {
                            Source = packageSource,
                            Width = 24,
                            Height = 24,
                            Stretch = Stretch.Uniform,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 6, 0)
                        };
                        Grid.SetColumn(image, 0);
                        grid.Children.Add(image);
                    }
                    const double packageTitleFontSize = 12;
                    var brightBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                    var greyBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0xAD, 0xAD));
                    var text = new TextBlock
                    {
                        FontSize = packageTitleFontSize,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 18,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                    };
                    text.Inlines.Add(new Run(displayTitle) { FontWeight = FontWeights.SemiBold, Foreground = brightBrush });
                    text.Inlines.Add(new Run(" - ") { Foreground = greyBrush });
                    text.Inlines.Add(new Run(severityLabel) { Foreground = greyBrush });
                    Grid.SetColumn(text, 1);
                    grid.Children.Add(text);
                    return (System.Windows.UIElement)grid;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateOssPackageTitleRow");
                return null;
            }
        }

        /// <summary>
        /// Severity + title row: icon + finding title on one line (custom-popup style).
        /// </summary>
        private static System.Windows.UIElement CreateSeverityTitleRow(SeverityLevel severity, string title, string severityName)
        {
            var severitySource = AssistIconLoader.LoadSeverityIcon(severity);
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
                        Grid.SetColumn(image, 0);
                        grid.Children.Add(image);
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
                    Grid.SetColumn(text, 1);
                    grid.Children.Add(text);
                    return (System.Windows.UIElement)grid;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateSeverityTitleRow");
                return null;
            }
        }

        /// <summary>
        /// Secret finding row: severity icon + bold title + grey " - Secret finding" (reference-style).
        /// </summary>
        private static System.Windows.UIElement CreateSecretFindingTitleRow(SeverityLevel severity, string displayTitle)
        {
            var severitySource = AssistIconLoader.LoadSeverityIcon(severity);
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
                        Grid.SetColumn(image, 0);
                        grid.Children.Add(image);
                    }
                    var brightBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                    var greyBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0xAD, 0xAD));
                    var text = new TextBlock
                    {
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 18,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                    };
                    text.Inlines.Add(new Run(displayTitle ?? "") { FontWeight = FontWeights.SemiBold, Foreground = brightBrush });
                    text.Inlines.Add(new Run(" - ") { Foreground = greyBrush });
                    text.Inlines.Add(new Run(CxAssistConstants.SecretFindingLabel) { Foreground = greyBrush });
                    Grid.SetColumn(text, 1);
                    grid.Children.Add(text);
                    return (System.Windows.UIElement)grid;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateSecretFindingTitleRow");
                return null;
            }
        }

        /// <summary>
        /// ASCA row: severity icon + bold title - description - grey "SAST vulnerability" (reference-style, single line block).
        /// </summary>
        private static System.Windows.UIElement CreateAscaTitleRow(SeverityLevel severity, string title, string description)
        {
            var severitySource = AssistIconLoader.LoadSeverityIcon(severity);
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
                        Grid.SetColumn(image, 0);
                        grid.Children.Add(image);
                    }
                    var brightBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                    var greyBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0xAD, 0xAD));
                    var text = new TextBlock
                    {
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 18,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                    };
                    text.Inlines.Add(new Run(title ?? "") { FontWeight = FontWeights.SemiBold, Foreground = brightBrush });
                    text.Inlines.Add(new Run(" - ") { Foreground = brightBrush });
                    text.Inlines.Add(new Run(description ?? "") { Foreground = brightBrush });
                    text.Inlines.Add(new Run(" - ") { Foreground = greyBrush });
                    text.Inlines.Add(new Run(CxAssistConstants.SastVulnerabilityLabel) { Foreground = greyBrush });
                    Grid.SetColumn(text, 1);
                    grid.Children.Add(text);
                    return (System.Windows.UIElement)grid;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateAscaTitleRow");
                return null;
            }
        }

        /// <summary>
        /// IaC row: severity icon + bold title - actualValue description - grey "IaC vulnerability" (reference-style, single block like JetBrains).
        /// </summary>
        private static System.Windows.UIElement CreateIacTitleRow(SeverityLevel severity, string title, string actualValue, string description)
        {
            var severitySource = AssistIconLoader.LoadSeverityIcon(severity);
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
                        Grid.SetColumn(image, 0);
                        grid.Children.Add(image);
                    }
                    var brightBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                    var greyBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0xAD, 0xAD));
                    var text = new TextBlock
                    {
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 18,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                    };
                    text.Inlines.Add(new Run(title ?? "") { FontWeight = FontWeights.SemiBold, Foreground = brightBrush });
                    text.Inlines.Add(new Run(" - ") { Foreground = brightBrush });
                    if (!string.IsNullOrEmpty(actualValue))
                    {
                        text.Inlines.Add(new Run(actualValue + " ") { Foreground = brightBrush });
                    }
                    text.Inlines.Add(new Run(description ?? "") { Foreground = brightBrush });
                    text.Inlines.Add(new Run(" " + CxAssistConstants.IacVulnerabilityLabel) { Foreground = greyBrush });
                    Grid.SetColumn(text, 1);
                    grid.Children.Add(text);
                    return (System.Windows.UIElement)grid;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateIacTitleRow");
                return null;
            }
        }

        /// <summary>
        /// Creates a WPF Image for the Checkmarx One Assist badge only (theme-based). Used when not using header row.
        /// </summary>
        private static System.Windows.UIElement CreateCxAssistBadgeImage()
        {
            var source = AssistIconLoader.LoadBadgeIcon();
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
            var source = AssistIconLoader.LoadSeverityIcon(severity);
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

    }
}
