using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxWrapper.Models;
using log4net.Repository.Hierarchy;
using System.Web.UI.WebControls;
using System.Diagnostics;


namespace ast_visual_studio_extension.CxExtension.Utils
{
    public class StateManager
    {
        public  CxCLI.CxWrapper _cxWrapper;
        public List<State> _allStates = new List<State>();

        public StateManager(CxCLI.CxWrapper cxWrapper)
        {
            _cxWrapper = cxWrapper;
        }

        public async Task InitializeStatesAsync()
        {
            
            if (_allStates.Count == 0) 
            {
                _allStates = await GetStatesAsync();
            }
        }
        public async Task<List<State>> GetStatesAsync()
        {
            string errorMessage = string.Empty;

            List<State> states = await Task.Run(() =>
            {
                try
                {
                    var statesList = _cxWrapper.TriageGetStates(false);
                    return statesList;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return null;
                }
            }).ConfigureAwait(false);


            return states ?? new List<State>();

        }




        public List<State> GetAllStates()
        {
            return _allStates;
        }

    }
}
