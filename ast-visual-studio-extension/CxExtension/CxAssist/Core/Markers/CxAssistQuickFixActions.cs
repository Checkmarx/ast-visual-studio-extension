using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
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

        public ImageMoniker IconMoniker => default;

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
                    MessageBox.Show(
                        $"Fix with Checkmarx One Assist\nVulnerability: {v.Title}\nID: {v.Id}",
                        "CxAssist",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
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

        public ImageMoniker IconMoniker => default;

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
                    MessageBox.Show(
                        $"{v.Title}\n\n{v.Description}\n\nScanner: {v.Scanner} | Severity: {v.Severity}",
                        "View details",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
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
}
