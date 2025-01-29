using FlaUI.Core.AutomationElements;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UITests
{
    public static class TestUtils
    {
        public static AutomationElement GetElementByAutomationIdWithNotNullCheck(AutomationElement parentElement, string automationId, string errorMessage)
        {
            var element = parentElement.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            Assert.IsNotNull(element, errorMessage);
            return element;
        }
        public static AutomationElement GetElementByNameWithNotNullCheck(AutomationElement parentElement, string name, string errorMessage)
        {
            var element = parentElement.FindFirstDescendant(cf => cf.ByName(name));
            Assert.IsNotNull(element, errorMessage);
            return element;
        }
    }
}
