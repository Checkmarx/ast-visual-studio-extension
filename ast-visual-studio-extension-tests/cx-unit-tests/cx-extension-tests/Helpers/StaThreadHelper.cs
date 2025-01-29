using System;
using System.Threading;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public static class StaThreadHelper
    {
        public static void RunInStaThread(Action action)
        {
            var staThread = new Thread(() =>
            {
                action();
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }
    }
}
