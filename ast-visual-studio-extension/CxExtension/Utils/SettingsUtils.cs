using ast_visual_studio_extension.CxExtension.Enums;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    public class SettingsUtils
    {
        public static readonly string severityCollection = "Checkmarx/Filter/Severity";
        public static readonly string dependencyFiltersCollection = "Checkmarx/Filter/DependencyFilters";
        public static readonly string stateCollection = "Checkmarx/Filter/State";
        public static readonly string groupByCollection = "Checkmarx/GroupBy";
        public static readonly string toolbarCollection = "Checkmarx/Toolbar";
        public static readonly string projectIdProperty = "projectId";
        public static readonly string branchProperty = "branch";
        public static readonly string scanIdProperty = "scanId";
        public static readonly string createdScanIdProperty = "createdScanId";

        public static readonly Dictionary<object, bool> severityDefaultValues = new Dictionary<object, bool>
        {
            { Severity.CRITICAL, true},
            { Severity.HIGH, true },
            { Severity.MEDIUM, true },
            { Severity.LOW, false },
            { Severity.INFO, false },
        };

        public static readonly Dictionary<object, bool> dependencyFilterDefaultValues = new Dictionary<object, bool>
{
    { DependencyFilter.IsDevOrTestDependency, false },
};
        public static readonly Dictionary<object, bool> stateDefaultValues = new Dictionary<object, bool>
        {
            { SystemState.CONFIRMED, true },
            { SystemState.TO_VERIFY, true },
            { SystemState.URGENT, true },
            { SystemState.NOT_EXPLOITABLE, false},
            { SystemState.PROPOSED_NOT_EXPLOITABLE, false},
            { SystemState.IGNORED, true },
            { SystemState.NOT_IGNORED, true },
        };
        public static readonly Dictionary<object, bool> groupByDefaultValues = new Dictionary<object, bool>
        {
            { GroupBy.ENGINE, false },
            { GroupBy.FILE, false },
            { GroupBy.SEVERITY, true },
            { GroupBy.STATE, false },
            { GroupBy.QUERY_NAME, true },
        };

        public static void Store(AsyncPackage package, string collection, object property, Dictionary<object, bool> defaults)
        {
            WritableSettingsStore userSettingsStore = new ShellSettingsManager(package).GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!userSettingsStore.CollectionExists(collection))
            {
                userSettingsStore.CreateCollection(collection);
            }
            bool enabled = userSettingsStore.GetBoolean(collection, property.ToString(), defaults[property]);
            enabled = !enabled;
            userSettingsStore.SetBoolean(collection, property.ToString(), enabled);
        }

        public static void StoreToolbarValue(AsyncPackage package, string collection, object property, string value)
        {
            WritableSettingsStore userSettingsStore = new ShellSettingsManager(package).GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!userSettingsStore.CollectionExists(collection))
            {
                userSettingsStore.CreateCollection(collection);
            }

            userSettingsStore.SetString(collection, property.ToString(), value);
        }

        public static string GetToolbarValue(AsyncPackage package, string property)
        {
            var readOnlyStore = new ShellSettingsManager(package).GetReadOnlySettingsStore(SettingsScope.UserSettings);

            return readOnlyStore.GetString(toolbarCollection, property, string.Empty);
        }

        public static HashSet<Severity> EnabledSeverities(AsyncPackage package)
        {
            var severities = new HashSet<Severity>();
            var readOnlyStore = new ShellSettingsManager(package).GetReadOnlySettingsStore(SettingsScope.UserSettings);
            foreach (Severity severity in Enum.GetValues(typeof(Severity)))
            {
                if (readOnlyStore.GetBoolean(severityCollection, severity.ToString(), severityDefaultValues[severity]))
                {
                    severities.Add(severity);
                }
            }
            return severities;
        }

        public static HashSet<SystemState> EnabledStates(AsyncPackage package)
        {
            var states = new HashSet<SystemState>();
            var readOnlyStore = new ShellSettingsManager(package).GetReadOnlySettingsStore(SettingsScope.UserSettings);
            foreach (SystemState state in Enum.GetValues(typeof(SystemState)))
            {
                if (readOnlyStore.GetBoolean(stateCollection, state.ToString(), stateDefaultValues[state]))
                {
                    states.Add(state);
                }
            }
            return states;
        }

        public static List<GroupBy> EnabledGroupByOptions(AsyncPackage package)
        {
            var groupByOptions = new List<GroupBy>();
            var readOnlyStore = new ShellSettingsManager(package).GetReadOnlySettingsStore(SettingsScope.UserSettings);
            foreach (GroupBy groupBy in Enum.GetValues(typeof(GroupBy)))
            {
                if (readOnlyStore.GetBoolean(groupByCollection, groupBy.ToString(), groupByDefaultValues[groupBy]))
                {
                    groupByOptions.Add(groupBy);
                }
            }
            return groupByOptions;
        }
    }
}
