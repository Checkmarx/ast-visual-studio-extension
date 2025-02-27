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
        public Dictionary<MenuItem, State> _allStates = new Dictionary<MenuItem, State>();

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
        public async Task <Dictionary<MenuItem, State>> GetStatesAsync()
        {
            string errorMessage = string.Empty;

            List<State> states = await Task.Run(() =>
            {
                try
                {
                    var x = _cxWrapper.TriageGetStates(false);
                    return x;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return null;
                }
            }).ConfigureAwait(false);


          
            

            var stateFilters = new Dictionary<MenuItem, State>();

            foreach (var state in states)
            {
                var menuItem = new MenuItem { Text = state.name };
                stateFilters.Add(menuItem, state);
            }

            return stateFilters;
        }


        public Dictionary<MenuItem, State> GetAllStates()
        {
            return _allStates;

        }

    }
}
