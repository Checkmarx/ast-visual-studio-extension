using System.Collections.Generic;
using System.Threading.Tasks;
using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.Enums;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    public class StateManager
    {
        private readonly CxWrapper _cxWrapper;

        public StateManager(CxWrapper cxWrapper)
        {
            _cxWrapper = cxWrapper;
        }

        public async Task<Dictionary<MenuItem, State>> GetStatesAsync()
        {
            var states = await _cxWrapper.TriageGetStatesAsync();
            var stateFilters = new Dictionary<MenuItem, State>();

            foreach (var state in states)
            {
                var menuItem = new MenuItem { Header = state.Getname };
                stateFilters.Add(menuItem, state);
            }

            return stateFilters;
        }
    }
}
