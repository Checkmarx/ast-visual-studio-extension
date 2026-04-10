using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences.Configuration;
using ast_visual_studio_extension.CxWrapper.Exceptions;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace ast_visual_studio_extension.CxPreferences
{
    public partial class CxPreferencesUI : UserControl
    {
        internal CxPreferencesModule cxPreferencesModule;

        public delegate void EventHandler();
        public event EventHandler OnApplySettingsEvent = delegate { };

        private static CxPreferencesUI Instance;
        private static ASCAService _ascaService;
        private static bool _isAuthenticated;
        internal static event Action<bool> AuthStateChanged;
        private static int _restoreAuthInProgress;
        private bool _isValidationInProgress;
        private bool _isInitializing;
        private bool _hasLoaded;
        private TreeNode _cachedAssistNode;
        private int _cachedAssistNodeIndex = -1;

        // Theme-aware colors
        private static readonly Color SuccessColor = Color.FromArgb(0, 120, 50);
        private static readonly Color ErrorColorLight = Color.FromArgb(160, 0, 0);
        private static readonly Color ErrorColorDark = Color.FromArgb(200, 60, 60);

        private System.Threading.CancellationTokenSource _messageDismissCts;

        private CxPreferencesUI()
        {
            InitializeComponent();
        }

        public static CxPreferencesUI GetInstance()
        {
            if (Instance == null)
                Instance = new CxPreferencesUI();

            return Instance;
        }

        internal static bool IsAuthenticated()
        {
            return _isAuthenticated;
        }

        private static void SetAuthState(bool isAuthenticated)
        {
            if (_isAuthenticated == isAuthenticated)
                return;

            _isAuthenticated = isAuthenticated;

            try
            {
                AuthStateChanged?.Invoke(_isAuthenticated);
            }
            catch
            {
                // Keep auth flow resilient even if listeners fail.
            }
        }

        public void ThrowEventOnApply()
        {
            OnApplySettingsEvent();
        }

        #region Initialization

        public void Initialize(CxPreferencesModule preferencesModule)
        {
            _isInitializing = true;

            cxPreferencesModule = preferencesModule;
            tbAdditionalParameters.Text = cxPreferencesModule.AdditionalParameters;

            string savedApiKey = cxPreferencesModule.ApiKey ?? string.Empty;

            // Sync UI from persisted settings without treating this as user input.
            tbApiKey.Text = savedApiKey;

            if (string.IsNullOrWhiteSpace(savedApiKey))
                ResetAuthState();

            if (string.IsNullOrWhiteSpace(savedApiKey))
                cxPreferencesModule.RestoreAuthenticatedSession = false;

            _isValidationInProgress = false;

            // Restore auth on load only when user explicitly authenticated before
            // and did not logout afterwards.
            if (!_isAuthenticated
                && cxPreferencesModule.RestoreAuthenticatedSession
                && !string.IsNullOrWhiteSpace(savedApiKey))
            {
                _ = ValidateApiKeyAsync(showErrorOnFailure: false);
            }

            UpdateAuthControlsState();
            _isInitializing = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!_hasLoaded)
            {
                _hasLoaded = true;
            }

            // Always restore correct visual state on (re)open
            UpdateAuthControlsState();
            if (_isAuthenticated)
                SetValidationMessage(CxConstants.AUTH_VALIDATE_SUCCESS, isSuccess: true, autoDismiss: false);
        }

        #endregion

        #region Event Handlers

        private void OnApiKeyChange(object sender, EventArgs e)
        {
            if (_isInitializing) return;

            // Ignore programmatic changes when field is read-only
            if (tbApiKey.ReadOnly) return;

            cxPreferencesModule.ApiKey = LogForgingSanitizer.StripLineTermination(tbApiKey.Text.Trim());
            cxPreferencesModule.RestoreAuthenticatedSession = false;
            ResetAuthState();
            UpdateAuthControlsState();
        }

        private void OnAdditionalParametersChange(object sender, EventArgs e)
        {
            cxPreferencesModule.AdditionalParameters = LogForgingSanitizer.StripLineTermination(tbAdditionalParameters.Text);
        }

        private async void OnValidateConnection(object sender, EventArgs e)
        {
            if (_isValidationInProgress || string.IsNullOrWhiteSpace(tbApiKey.Text))
                return;

            SetValidationMessage(CxConstants.AUTH_VALIDATE_IN_PROGRESS, isSuccess: true);
            await ValidateApiKeyAsync(showErrorOnFailure: true);
        }

        private async void OnValidateConnection(object sender, EventArgs e)
        {
            if (_isValidationInProgress || string.IsNullOrWhiteSpace(tbApiKey.Text))
                return;

            SetValidationMessage(CxConstants.AUTH_VALIDATE_IN_PROGRESS, isSuccess: true);
            await ValidateApiKeyAsync(showErrorOnFailure: true);
        }

        private void OnLogout(object sender, EventArgs e)
        {
            var choice = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (choice != DialogResult.Yes)
                return;

            try
            {
                var mcpInstallService = new McpInstallService();
                mcpInstallService.Uninstall(out _);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MCP cleanup on logout failed: {ex.Message}");
            }

            // JetBrains parity: do not persist "all scanners off" — toggles stay as last user choice in storage.
            // Runtime scanning stops via auth → RealtimeScannerHost.UnregisterAsync (GlobalScannerController.syncAll / stopAll).
            cxPreferencesModule.RestoreAuthenticatedSession = false;
            ResetAuthState();

            try
            {
                CxOneAssistSettingsModule oneAssistModule = GetOneAssistSettingsModule();
                if (oneAssistModule != null)
                {
                    oneAssistModule.ContainersTool = "docker";
                    oneAssistModule.PersistSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to persist Assist settings on logout: {ex.Message}");
            }
            SetValidationMessage(CxConstants.AUTH_LOGOUT_SUCCESS, isSuccess: true);
            UpdateAuthControlsState();
        }

        private void HelpPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://checkmarx.com/resource/documents/en/34965-68738-checkmarx-one-visual-studio-extension--plugin-.html");
        }

        private void AdditionalParametersHelPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://checkmarx.com/resource/documents/en/34965-68626-global-flags.html");
        }

        private void GoToAssistLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!_isAuthenticated)
            {
                SetValidationMessage("Please authenticate first to access Checkmarx One Assist.", isSuccess: false);
                return;
            }

            cxPreferencesModule?.GetOwnerPackage()?.ShowOptionPage(typeof(CxOneAssistSettingsModule));
        }

        #endregion

        #region Authentication

        private async Task ValidateApiKeyAsync(bool showErrorOnFailure)
        {
            if (_isValidationInProgress || string.IsNullOrWhiteSpace(tbApiKey.Text))
                return;

            _isValidationInProgress = true;
            UpdateAuthControlsState();

            try
            {
                await Task.Run(() =>
                {
                    var cxWrapper = new CxCLI.CxWrapper(GetCxConfig(), GetType());
                    cxWrapper.AuthValidate();
                });

                SetAuthState(true);
                if (cxPreferencesModule != null)
                    cxPreferencesModule.RestoreAuthenticatedSession = true;
                // autoDismiss: false for background validation — user sees connected state when Settings opens
                SetValidationMessage(CxConstants.AUTH_VALIDATE_SUCCESS, isSuccess: true, autoDismiss: !showErrorOnFailure);

                // Do not block auth UI state updates on MCP checks/auto-install.
                _ = CompleteAuthenticationSetupAsync(GetCxConfig(), showWelcomeDialog: showErrorOnFailure);
            }
            catch (CxException ex) when (showErrorOnFailure)
            {
                SetAuthState(false);
                if (cxPreferencesModule != null)
                    cxPreferencesModule.RestoreAuthenticatedSession = false;
                SetValidationMessage(string.Format(CxConstants.AUTH_VALIDATE_FAIL_TEMPLATE, SanitizeErrorMessage(ex.Message)), isSuccess: false);
            }
            catch (Exception ex) when (showErrorOnFailure)
            {
                SetAuthState(false);
                if (cxPreferencesModule != null)
                    cxPreferencesModule.RestoreAuthenticatedSession = false;
                SetValidationMessage(CxConstants.AUTH_VALIDATE_ERROR, isSuccess: false);
                System.Diagnostics.Debug.WriteLine($"Authentication error: {LogForgingSanitizer.StripLineTermination(ex.Message)}");
            }
            catch
            {
                SetAuthState(false);
                if (cxPreferencesModule != null)
                    cxPreferencesModule.RestoreAuthenticatedSession = false;
            }
            finally
            {
                _isValidationInProgress = false;
                UpdateAuthControlsState();
            }
        }

        private void ResetAuthState()
        {
            SetAuthState(false);
            ClearValidationMessage();
        }

        /// <summary>
        /// Sanitizes input from UI controls to prevent log injection.
        /// </summary>
        private static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove control characters and newlines that could be used for log injection
            return System.Text.RegularExpressions.Regex.Replace(input, @"[\r\n\t\x00-\x1F]", "");
        }

        private CxConfig GetCxConfig() => new CxConfig
        {
            ApiKey = SanitizeInput(tbApiKey.Text),
            AdditionalParameters = SanitizeInput(tbAdditionalParameters.Text),
        };

        internal static CxConfig GetConfigSnapshot()
        {
            var ui = GetInstance();
            if (ui.cxPreferencesModule != null)
                return ui.cxPreferencesModule.GetCxConfig();

            return new CxConfig
            {
                ApiKey = SanitizeInput(ui.tbApiKey.Text),
                AdditionalParameters = SanitizeInput(ui.tbAdditionalParameters.Text),
            };
        }

        internal static async Task TryRestoreAuthenticatedSessionAsync(AsyncPackage package)
        {
            if (package == null)
                return;

            if (Interlocked.Exchange(ref _restoreAuthInProgress, 1) == 1)
                return;

            CxPreferencesModule preferencesModule = null;

            try
            {
                preferencesModule = package.GetDialogPage(typeof(CxPreferencesModule)) as CxPreferencesModule;
                if (preferencesModule == null)
                {
                    SetAuthState(false);
                    return;
                }

                if (!preferencesModule.RestoreAuthenticatedSession || string.IsNullOrWhiteSpace(preferencesModule.ApiKey))
                {
                    SetAuthState(false);
                    return;
                }

                var config = preferencesModule.GetCxConfig();
                await Task.Run(() =>
                {
                    var cxWrapper = new CxCLI.CxWrapper(config, typeof(CxPreferencesUI));
                    cxWrapper.AuthValidate();
                });

                SetAuthState(true);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var oneAssistRestore = package.GetDialogPage(typeof(CxOneAssistSettingsModule)) as CxOneAssistSettingsModule;
                bool previousMcpEnabled = oneAssistRestore?.McpEnabled ?? false;
                bool mcpStatusPreviouslyChecked = oneAssistRestore?.McpStatusChecked ?? false;

                bool hadAssist = await ApplyAssistTenantLicenseAndMcpFlagsAsync(package, config, typeof(CxPreferencesUI));
                if (hadAssist && oneAssistRestore != null)
                {
                    var installService = new McpInstallService();
                    ApplyJetBrainsStyleRealtimeScannerPolicy(oneAssistRestore, previousMcpEnabled, mcpStatusPreviouslyChecked);
                    if (oneAssistRestore.McpEnabled)
                        await installService.InstallSilentlyAsync(config, typeof(CxPreferencesUI));
                    oneAssistRestore.PersistSettings();
                    CxOneAssistSettingsUI.GetInstance()?.RefreshCheckboxesFromModule();
                }
            }
            catch
            {
                SetAuthState(false);
                if (preferencesModule != null)
                    preferencesModule.RestoreAuthenticatedSession = false;
            }
            finally
            {
                Interlocked.Exchange(ref _restoreAuthInProgress, 0);
            }
        }

        /// <summary>
        /// Tenant license flags + MCP enabled (tenant check). Does not install MCP or persist.
        /// </summary>
        /// <returns>false if Assist settings page is missing (MCP silent-install only path).</returns>
        private static async Task<bool> ApplyAssistTenantLicenseAndMcpFlagsAsync(AsyncPackage package, CxConfig config, Type ownerType)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var oneAssistModule = package.GetDialogPage(typeof(CxOneAssistSettingsModule)) as CxOneAssistSettingsModule;
            if (oneAssistModule == null)
            {
                var silentInstallService = new McpInstallService();
                await silentInstallService.InstallSilentlyAsync(config, ownerType);
                return false;
            }

            List<TenantSetting> tenantSettings = await Task.Run(() =>
            {
                var w = new CxCLI.CxWrapper(config, ownerType);
                return w.TenantSettings();
            });
            AssistEntitlementSync.ApplyFromSettings(tenantSettings, oneAssistModule);

            var installService = new McpInstallService();
            bool mcpEnabled = await installService.IsTenantMcpEnabledAsync(config, ownerType);

            oneAssistModule.McpEnabled = mcpEnabled;
            oneAssistModule.McpStatusChecked = true;

            return true;
        }

        /// <summary>
        /// JetBrains <c>GlobalSettingsComponent.completeAuthenticationSetup</c>: only adjust realtime toggles on
        /// first MCP resolution or when tenant MCP flag changes — not on every re-auth (persisted toggles stay correct).
        /// </summary>
        private static void ApplyJetBrainsStyleRealtimeScannerPolicy(
            CxOneAssistSettingsModule module,
            bool previousMcpEnabled,
            bool mcpStatusPreviouslyChecked)
        {
            if (module == null)
                return;

            bool mcpStatusChanged = mcpStatusPreviouslyChecked && (previousMcpEnabled != module.McpEnabled);

            if (!mcpStatusPreviouslyChecked)
            {
                if (module.McpEnabled)
                    module.AutoEnableRealtimeScanners();
                else
                    module.DisableAllRealtimeScannersWhenMcpUnavailable();
            }
            else if (mcpStatusChanged)
            {
                if (module.McpEnabled)
                    module.AutoEnableRealtimeScanners();
                else
                    module.DisableAllRealtimeScannersWhenMcpUnavailable();
            }
        }

        private async Task CompleteAuthenticationSetupAsync(CxConfig config, bool showWelcomeDialog)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.ApiKey))
                return;

            try
            {
                var package = cxPreferencesModule?.GetOwnerPackage() as AsyncPackage;
                if (package == null)
                    return;

                var oneAssistModule = package.GetDialogPage(typeof(CxOneAssistSettingsModule)) as CxOneAssistSettingsModule;
                bool previousMcpEnabled = oneAssistModule?.McpEnabled ?? false;
                bool mcpStatusPreviouslyChecked = oneAssistModule?.McpStatusChecked ?? false;

                bool hadAssist = await ApplyAssistTenantLicenseAndMcpFlagsAsync(package, config, GetType());
                if (!hadAssist)
                    return;

                oneAssistModule = package.GetDialogPage(typeof(CxOneAssistSettingsModule)) as CxOneAssistSettingsModule;
                if (oneAssistModule == null)
                    return;

                var installService = new McpInstallService();

                ApplyJetBrainsStyleRealtimeScannerPolicy(oneAssistModule, previousMcpEnabled, mcpStatusPreviouslyChecked);

                if (oneAssistModule.McpEnabled)
                    await installService.InstallSilentlyAsync(config, GetType());

                // JetBrains calls apply() after MCP update every login — Persist drives RealtimeAssistSettingsChanged / Resync.
                oneAssistModule.PersistSettings();

                if (showWelcomeDialog)
                    ShowOneAssistWelcomeDialog(oneAssistModule, oneAssistModule.McpEnabled);

                CxOneAssistSettingsUI.GetInstance()?.RefreshCheckboxesFromModule();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MCP auto-install skipped: {ex.Message}");
            }
        }

        private CxOneAssistSettingsModule GetOneAssistSettingsModule()
        {
            try
            {
                var package = cxPreferencesModule?.GetOwnerPackage();
                if (package == null)
                    return null;

                return package.GetDialogPage(typeof(CxOneAssistSettingsModule)) as CxOneAssistSettingsModule;
            }
            catch
            {
                return null;
            }
        }

        private void ShowOneAssistWelcomeDialog(CxOneAssistSettingsModule module, bool mcpEnabled)
        {
            try
            {
                using (var welcomeDialog = new CxOneAssistWelcomeDialog(module, mcpEnabled))
                {
                    welcomeDialog.ShowDialog(FindForm());
                    module.WelcomeShown = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show welcome dialog: {ex.Message}");
            }
        }

        #endregion

        #region UI Helpers

        private void UpdateAuthControlsState()
        {
            bool hasApiKey = !string.IsNullOrWhiteSpace(tbApiKey.Text);

            // Use ReadOnly instead of Enabled=false to avoid the washed-out disabled appearance
            tbApiKey.ReadOnly = _isAuthenticated;
            tbApiKey.BackColor = _isAuthenticated
                ? System.Drawing.SystemColors.Control
                : System.Drawing.SystemColors.Window;

            button1.Enabled = hasApiKey && !_isAuthenticated && !_isValidationInProgress;
            btnLogout.Enabled = _isAuthenticated && !_isValidationInProgress;
            // Temporary: hide assist navigation link from the Authentication page.
            goToAssistLink.Visible = false;

            UpdateAssistNodeVisibility();
        }

        private void UpdateAssistNodeVisibility()
        {
            try
            {
                Control optionsHost = TopLevelControl ?? FindForm() ?? Parent;
                if (optionsHost == null)
                    return;

                TreeView optionsTree = FindControl<TreeView>(optionsHost);
                if (optionsTree == null)
                    return;

                const string parentNodeName = "Checkmarx One";
                const string assistNodeName = "Checkmarx One Assist";

                TreeNode parentNode = FindNodeByText(optionsTree.Nodes, parentNodeName);
                if (parentNode == null)
                    return;

                TreeNode assistNode = FindNodeByText(parentNode.Nodes, assistNodeName);

                if (!_isAuthenticated)
                {
                    if (assistNode != null)
                    {
                        _cachedAssistNode = assistNode;
                        _cachedAssistNodeIndex = assistNode.Index;
                        parentNode.Nodes.Remove(assistNode);
                    }
                    return;
                }
                // On session restore (showWelcomeDialog=false): trust registry values, don't touch scanners.

                if (mcpEnabled)
                    await installService.InstallSilentlyAsync(config, GetType());

                if (assistNode == null && _cachedAssistNode != null)
                {
                    int insertIndex = _cachedAssistNodeIndex >= 0 && _cachedAssistNodeIndex <= parentNode.Nodes.Count
                        ? _cachedAssistNodeIndex
                        : parentNode.Nodes.Count;

                    parentNode.Nodes.Insert(insertIndex, _cachedAssistNode);
                }
            }
            catch
            {
                // Keep settings page stable even if options tree internals change between VS versions.
            }
        }

        private static T FindControl<T>(Control root) where T : Control
        {
            if (root == null)
                return null;

            if (root is T match)
                return match;

            foreach (Control child in root.Controls)
            {
                T nested = FindControl<T>(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static TreeNode FindNodeByText(TreeNodeCollection nodes, string text)
        {
            foreach (TreeNode node in nodes)
            {
                if (NodeTextEquals(node.Text, text))
                    return node;

                TreeNode nested = FindNodeByText(node.Nodes, text);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static bool NodeTextEquals(string left, string right)
        {
            string normalizedLeft = (left ?? string.Empty).Replace("&", string.Empty).Trim();
            string normalizedRight = (right ?? string.Empty).Replace("&", string.Empty).Trim();

            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }

        private void SetValidationMessage(string message, bool isSuccess, bool autoDismiss = true)
        {
            lblValidationResult.Text = message;
            lblValidationResult.ForeColor = ResolveMessageColor(isSuccess);

            if (autoDismiss)
                _ = AutoDismissMessageAsync();
        }

        private async Task AutoDismissMessageAsync()
        {
            // Cancel any previously scheduled dismiss
            _messageDismissCts?.Cancel();
            _messageDismissCts = new System.Threading.CancellationTokenSource();
            var token = _messageDismissCts.Token;

            try
            {
                await Task.Delay(10000, token);

                if (!token.IsCancellationRequested)
                    ClearValidationMessage();
            }
            catch (TaskCanceledException)
            {
                // A new message replaced this one — do nothing
            }
        }

        private void ClearValidationMessage()
        {
            lblValidationResult.Text = string.Empty;
        }

        private static Color ResolveMessageColor(bool isSuccess)
        {
            try
            {
                if (isSuccess) return SuccessColor;
                return ResourceKeyToColorConverter.IsDarkTheme ? ErrorColorDark : ErrorColorLight;
            }
            catch
            {
                return isSuccess ? Color.Green : Color.Red;
            }
        }

        private static string SanitizeErrorMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return CxConstants.AUTH_VALIDATE_CLEAR_FAIL_REASON;

            string lower = message.ToLower();
            bool isTokenError = lower.Contains("token") &&
                (lower.Contains("malformed") || lower.Contains("decoding") ||
                 lower.Contains("invalid") || lower.Contains("segment") || lower.Contains("decode"));

            if (isTokenError)
                return CxConstants.AUTH_VALIDATE_CLEAR_FAIL_REASON;

            string firstLine = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

            return string.IsNullOrWhiteSpace(firstLine) || firstLine.Length > 100
                ? CxConstants.AUTH_VALIDATE_CLEAR_FAIL_REASON
                : firstLine;
        }

        #endregion
    }
}
