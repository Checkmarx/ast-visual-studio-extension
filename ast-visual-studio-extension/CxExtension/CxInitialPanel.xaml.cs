using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using Microsoft.VisualStudio.Shell;
using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ast_visual_studio_extension.CxExtension
{
    public partial class CxInitialPanel : UserControl
    {
        private readonly AsyncPackage package;

        public CxInitialPanel(AsyncPackage package)
        {
            InitializeComponent();

            this.package = package;

            Image severityIcon = new Image();
            BitmapImage bitmapImage = new BitmapImage(new Uri(CxConstants.RESOURCES_BASE_DIR + CxConstants.ICON_CX_LOGO_INITIAL_PANEL, UriKind.RelativeOrAbsolute));
            severityIcon.Source = bitmapImage;

            InitialPanelIcon.Source = bitmapImage;

            CxPreferencesUI.AuthStateChanged += OnAuthStateChanged;
            CheckToolWindowPanel();
        }

        private void OnAuthStateChanged(bool _)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(CheckToolWindowPanel);
                return;
            }

            CheckToolWindowPanel();
        }

        /// <summary>
        /// Check if panel should be redraw after applying new checkmarx settings
        /// </summary>
        private void CheckToolWindowPanel()
        {
            if (CxPreferencesUI.IsAuthenticated())
            {
                CxPreferencesUI.AuthStateChanged -= OnAuthStateChanged;
                CxToolbar.redrawExtension = true;
                Content = new CxWindowControl(package);
            }
        }

        /// <summary>
        /// On click Open Settings button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickOpenSettings(object sender, System.Windows.RoutedEventArgs e)
        {
            package.ShowOptionPage(typeof(CxPreferencesModule));
        }
    }
}
