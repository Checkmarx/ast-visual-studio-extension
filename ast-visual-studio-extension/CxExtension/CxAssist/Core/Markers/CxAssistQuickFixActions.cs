using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Prompts;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    /// <summary>
    /// Quick Fix action: "Fix with Checkmarx One Assist" (same behavior as hover popup link).
    /// </summary>
    internal sealed class FixWithCxOneAssistSuggestedAction : ISuggestedAction
    {
        private readonly Vulnerability _vulnerability;

        public FixWithCxOneAssistSuggestedAction(Vulnerability vulnerability)
        {
            _vulnerability = vulnerability ?? throw new ArgumentNullException(nameof(vulnerability));

        }

        public string DisplayText => "Fix with Checkmarx One Assist";

        public string IconAutomationText => null;

        public ImageMoniker IconMoniker => default(ImageMoniker);

        public string InputGestureText => null;

        public bool HasPreview => false;

        public bool HasActionSets => false;

        public void Dispose()
        {
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (_vulnerability == null) return;
            var v = _vulnerability;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    string prompt = CxOneAssistFixPrompts.BuildForVulnerability(v);
                    if (!string.IsNullOrEmpty(prompt))
                        CopilotIntegration.SendPromptToCopilot(prompt, "Fix prompt copied. Paste into GitHub Copilot Chat to get remediation steps.");
                    else
                        MessageBox.Show($"Fix with Checkmarx One Assist\nVulnerability: {v.Title}\nID: {v.Id}", CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "FixWithCxOneAssistSuggestedAction.Invoke");
                }
            }));
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

    /// <summary>
    /// Quick Fix action: "View details" (same behavior as hover popup link).
    /// </summary>
    internal sealed class ViewDetailsSuggestedAction : ISuggestedAction
    {
        private readonly Vulnerability _vulnerability;

        public ViewDetailsSuggestedAction(Vulnerability vulnerability)
        {
            _vulnerability = vulnerability ?? throw new ArgumentNullException(nameof(vulnerability));
        }

        public string DisplayText => "View details";

        public string IconAutomationText => null;

        public ImageMoniker IconMoniker => default(ImageMoniker);

        public string InputGestureText => null;

        public bool HasPreview => false;

        public bool HasActionSets => false;

        public void Dispose()
        {
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (_vulnerability == null) return;
            var v = _vulnerability;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    string prompt = ViewDetailsPrompts.BuildForVulnerability(v);
                    if (!string.IsNullOrEmpty(prompt))
                        CopilotIntegration.SendPromptToCopilot(prompt, "View details prompt copied. Paste into GitHub Copilot Chat to get an explanation.");
                    else
                        MessageBox.Show($"{v.Title}\n\n{v.Description}\n\nScanner: {v.Scanner} | Severity: {v.Severity}", CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "ViewDetailsSuggestedAction.Invoke");
                }
            }));
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

    /// <summary>
    /// Quick Fix action: "Ignore this vulnerability" (same as hover popup / context menu).
    /// </summary>
    internal sealed class IgnoreThisVulnerabilitySuggestedAction : ISuggestedAction
    {
        private readonly Vulnerability _vulnerability;

        public IgnoreThisVulnerabilitySuggestedAction(Vulnerability vulnerability)
        {
            _vulnerability = vulnerability ?? throw new ArgumentNullException(nameof(vulnerability));
        }

        public string DisplayText => CxAssistConstants.GetIgnoreThisLabel(_vulnerability.Scanner);

        public string IconAutomationText => null;

        public ImageMoniker IconMoniker => default(ImageMoniker);

        public string InputGestureText => null;

        public bool HasPreview => false;

        public bool HasActionSets => false;

        public void Dispose() { }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<SuggestedActionSet>>(null);

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
            => Task.FromResult<object>(null);

        public void Invoke(CancellationToken cancellationToken)
        {
            if (_vulnerability == null) return;
            var v = _vulnerability;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    string label = CxAssistConstants.GetIgnoreThisLabel(v.Scanner);
                    var result = MessageBox.Show(
                        $"{label}?\n{v.Title ?? v.Description ?? v.Id}",
                        CxAssistConstants.DisplayName,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                        MessageBox.Show(CxAssistConstants.GetIgnoreThisSuccessMessage(v.Scanner), CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreThisVulnerabilitySuggestedAction.Invoke");
                }
            }));
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

    /// <summary>
    /// Quick Fix action: "Ignore all of this type" (same as hover popup / context menu).
    /// </summary>
    internal sealed class IgnoreAllOfThisTypeSuggestedAction : ISuggestedAction
    {
        private readonly Vulnerability _vulnerability;

        public IgnoreAllOfThisTypeSuggestedAction(Vulnerability vulnerability)
        {
            _vulnerability = vulnerability ?? throw new ArgumentNullException(nameof(vulnerability));
        }

        public string DisplayText => CxAssistConstants.GetIgnoreAllLabel(_vulnerability.Scanner);

        public string IconAutomationText => null;

        public ImageMoniker IconMoniker => default(ImageMoniker);

        public string InputGestureText => null;

        public bool HasPreview => false;

        public bool HasActionSets => false;

        public void Dispose() { }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<SuggestedActionSet>>(null);

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
            => Task.FromResult<object>(null);

        public void Invoke(CancellationToken cancellationToken)
        {
            if (_vulnerability == null) return;
            var v = _vulnerability;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    string label = CxAssistConstants.GetIgnoreAllLabel(v.Scanner);
                    var result = MessageBox.Show(
                        $"{label}?\n{v.Description}",
                        CxAssistConstants.DisplayName,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        MessageBox.Show(CxAssistConstants.GetIgnoreAllSuccessMessage(v.Scanner), CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreAllOfThisTypeSuggestedAction.Invoke");
                }
            }));
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
