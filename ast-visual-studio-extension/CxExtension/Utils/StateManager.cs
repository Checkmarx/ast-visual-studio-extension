﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ast_visual_studio_extension.CxWrapper.Models;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    public class StateManager
    {
        public  CxCLI.CxWrapper _cxWrapper;
        public List<State> _allStates = new List<State>();
        public HashSet<string> enabledCustemStates = new HashSet<string>();
        public StateManager(CxCLI.CxWrapper cxWrapper)
        {
            _cxWrapper = cxWrapper;
        }

        public async Task InitializeStatesAsync(Action callback)
        {
            _allStates = await GetStatesAsync();
            callback?.Invoke();
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
                    System.Diagnostics.Debug.WriteLine($"Custom states fetch failed : {ex.Message}");
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
