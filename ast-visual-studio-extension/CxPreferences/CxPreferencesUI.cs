using ast_visual_studio_extension.CxExtension.Services;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxWrapper.Exceptions;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.PlatformUI;

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
        private bool _isValidationInProgress;
        private bool _hasLoaded;

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

        public void ThrowEventOnApply()
        {
            OnApplySettingsEvent();
        }

        #region Initialization

        public void Initialize(CxPreferencesModule preferencesModule)
        {
            cxPreferencesModule = preferencesModule;
            tbAdditionalParameters.Text = cxPreferencesModule.AdditionalParameters;

            string savedApiKey = cxPreferencesModule.ApiKey ?? string.Empty;

            // Reset auth only if the API key changed or was cleared
            if (tbApiKey.Text != savedApiKey || string.IsNullOrWhiteSpace(savedApiKey))
            {
                tbApiKey.Text = savedApiKey;
                ResetAuthState();
            }

            _isValidationInProgress = false;

            if (!string.IsNullOrWhiteSpace(savedApiKey) && !_isAuthenticated)
                _ = ValidateApiKeyAsync(showErrorOnFailure: false);

            UpdateAuthControlsState();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!_hasLoaded)
            {
                _hasLoaded = true;
                ResetAuthState();
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
            // Ignore programmatic changes when field is read-only (e.g. during logout clear)
            if (tbApiKey.ReadOnly) return;

            cxPreferencesModule.ApiKey = tbApiKey.Text.Trim();
            ResetAuthState();
            UpdateAuthControlsState();
        }

        private void OnAdditionalParametersChange(object sender, EventArgs e)
        {
            cxPreferencesModule.AdditionalParameters = tbAdditionalParameters.Text;
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

            tbApiKey.Text = string.Empty;
            ResetAuthState();
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

                _isAuthenticated = true;
                // autoDismiss: false for background validation — user sees connected state when Settings opens
                SetValidationMessage(CxConstants.AUTH_VALIDATE_SUCCESS, isSuccess: true, autoDismiss: !showErrorOnFailure);
            }
            catch (CxException ex) when (showErrorOnFailure)
            {
                _isAuthenticated = false;
                SetValidationMessage(string.Format(CxConstants.AUTH_VALIDATE_FAIL_TEMPLATE, SanitizeErrorMessage(ex.Message)), isSuccess: false);
            }
            catch (Exception ex) when (showErrorOnFailure)
            {
                _isAuthenticated = false;
                SetValidationMessage(CxConstants.AUTH_VALIDATE_ERROR, isSuccess: false);
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
            }
            catch
            {
                _isAuthenticated = false;
            }
            finally
            {
                _isValidationInProgress = false;
                UpdateAuthControlsState();
            }
        }

        private void ResetAuthState()
        {
            _isAuthenticated = false;
            ClearValidationMessage();
        }

        private CxConfig GetCxConfig() => new CxConfig
        {
            ApiKey = tbApiKey.Text,
            AdditionalParameters = tbAdditionalParameters.Text,
        };

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
