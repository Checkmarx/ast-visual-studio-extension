using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
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
            BitmapImage bitmapImage = new BitmapImage(new Uri(Path.Combine(Environment.CurrentDirectory, CxConstants.FOLDER_CX_EXTENSION, CxConstants.FOLDER_RESOURCES, CxConstants.ICON_CX_LOGO_INITIAL_PANEL)));
            severityIcon.Source = bitmapImage;

            InitialPanelIcon.Source = bitmapImage;

            // Subscribe OnApply event in checkmarx settings window
            CxPreferencesUI.GetInstance().OnApplySettingsEvent += CheckToolWindowPanel;
        }

        /// <summary>
        /// Check if panel should be redraw after applying new checkmarx settings
        /// </summary>
        private void CheckToolWindowPanel()
        {
            if (CxUtils.AreCxCredentialsDefined(package))
            {
                CxPreferencesUI.GetInstance().OnApplySettingsEvent -= CheckToolWindowPanel;
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
