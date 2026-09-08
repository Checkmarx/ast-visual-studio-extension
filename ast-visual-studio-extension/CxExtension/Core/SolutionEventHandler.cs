using ast_visual_studio_extension.CxExtension.CxAssist.Realtime;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.Core
{
    /// <summary>
    /// Handles solution lifecycle events (open, close) to manage realtime scanner initialization.
    /// Implements IVsSolutionEvents to auto-register scanners when solution opens,
    /// and cleanup when solution closes.
    /// </summary>
    internal sealed class SolutionEventHandler : IVsSolutionEvents
    {
        private readonly AsyncPackage _package;
        private readonly IVsSolution _solution;
        private uint _solutionEventsCookie = VSConstants.VSCOOKIE_NIL;

        public SolutionEventHandler(AsyncPackage package, IVsSolution solution)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _solution = solution ?? throw new ArgumentNullException(nameof(solution));
        }

        /// <summary>
        /// Registers this handler with the solution service to receive solution events.
        /// </summary>
        public void Register()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_solutionEventsCookie == VSConstants.VSCOOKIE_NIL)
            {
                _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
                Debug.WriteLine("SolutionEventHandler: Registered solution events");
            }
        }

        /// <summary>
        /// Unregisters this handler from the solution service.
        /// </summary>
        public void Unregister()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_solutionEventsCookie != VSConstants.VSCOOKIE_NIL)
            {
                _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                _solutionEventsCookie = VSConstants.VSCOOKIE_NIL;
                Debug.WriteLine("SolutionEventHandler: Unregistered solution events");
            }
        }

        #region IVsSolutionEvents Implementation

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called after a solution is opened.
        /// Triggers automatic registration of realtime scanners.
        /// </summary>
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Debug.WriteLine("SolutionEventHandler: Solution opened, registering realtime scanners");
                _ = RegisterRealtimeScannersAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SolutionEventHandler: Error on solution open: {ex.Message}");
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called before a solution is closed.
        /// Triggers cleanup and unregistration of realtime scanners.
        /// </summary>
        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Debug.WriteLine("SolutionEventHandler: Solution closing, unregistering realtime scanners");
                _ = UnregisterRealtimeScannersAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SolutionEventHandler: Error on solution close: {ex.Message}");
            }
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        #endregion

        /// <summary>
        /// Registers realtime scanners via <see cref="RealtimeScannerHost"/> (no tool window required).
        /// </summary>
        private async Task RegisterRealtimeScannersAsync()
        {
            try
            {
                await RealtimeScannerHost.RegisterFromPackageAsync(_package, typeof(SolutionEventHandler));
                Debug.WriteLine("SolutionEventHandler: Realtime scanners registration attempted");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SolutionEventHandler: Failed to register realtime scanners: {ex.Message}");
            }
        }

        /// <summary>
        /// Unregisters realtime scanners and cleans up resources.
        /// </summary>
        private async Task UnregisterRealtimeScannersAsync()
        {
            try
            {
                await RealtimeScannerHost.UnregisterAsync();
                Debug.WriteLine("SolutionEventHandler: Realtime scanners unregistered");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SolutionEventHandler: Failed to unregister realtime scanners: {ex.Message}");
            }
        }
    }
}
