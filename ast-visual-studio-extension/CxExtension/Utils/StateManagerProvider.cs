using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxWrapper.Models;
using log4net.Repository.Hierarchy;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    public static class StateManagerProvider
    {
        private static StateManager _stateManager;

        public static void Initialize(CxCLI.CxWrapper cxWrapper)
        {
            if (_stateManager == null)
            {
                _stateManager = new StateManager(cxWrapper);
                _ = _stateManager.InitializeStatesAsync(); 
            }
        }

        public static StateManager GetStateManager()
        {
            return _stateManager;
        }
    }
}
