using HBS.Logging;
using HeraldryPicker.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HeraldryPicker
{
    public class Main
    {
        internal static Harmony harmony;
        internal static string ModDir { get; private set; }
        internal static ILog Log { get; private set; }
        internal static ModSettings Settings { get; private set; }

        public class ModSettings
        {
            public bool ShowColorNameOnEachSwatch { get; set; } = true;
            public bool EnableHeraldryFiltering { get; set; } = true;
            public string HeraldryFilterGroup { get; set; } = "NotSet";
            public bool GroupHeraldryByFaction { get; set; } = false;
        }

        public static void Init(string directory, string settingsJSON)
        {
            ModDir = directory;
            Log = Logger.GetLogger("HeraldryPicker", LogLevel.Debug);

            try
            {
                Settings = JsonConvert.DeserializeObject<ModSettings>(settingsJSON) ?? new ModSettings();
                harmony = new Harmony("com.github.AkiraBrahe.HeraldryPicker");
                LoadFilterGroup();
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                Log.LogException(ex);
            }
        }

        private class HeraldryGroupInfo
        {
            public string Id { get; set; }
            public string Group { get; set; }
        }

        /// <summary>
        /// Loads the heraldry filter groups from the specified JSON file.
        /// </summary>
        public static void LoadFilterGroup()
        {
            if (!Settings.EnableHeraldryFiltering)
            {
                Log.Log("Faction filtering is disabled.");
                return;
            }

            if (string.IsNullOrEmpty(Settings.HeraldryFilterGroup) || Settings.HeraldryFilterGroup.Equals("NotSet", StringComparison.OrdinalIgnoreCase) || Settings.HeraldryFilterGroup.Equals("Vanilla", StringComparison.OrdinalIgnoreCase))
            {
                bool isVanilla = Settings.HeraldryFilterGroup.Equals("Vanilla", StringComparison.OrdinalIgnoreCase);
                Log.Log(isVanilla ? "Vanilla filter selected." : "No filter group selected. Falling back to 'vanilla/modded' filtering.");
                FactionGroupManager.SetGroups(HeraldryExporter.vanillaHeraldries.ToDictionary(id => $"heraldrydef_{id}", id => "Vanilla", StringComparer.OrdinalIgnoreCase));
                return;
            }

            string filterFilePath = Path.Combine(ModDir, "FilterGroups", Settings.HeraldryFilterGroup) + ".csv";
            if (!File.Exists(filterFilePath))
            {
                Log.LogError($"Filter group file '{Settings.HeraldryFilterGroup}' not found. Falling back to 'vanilla/modded' filtering.");
                FactionGroupManager.SetGroups(HeraldryExporter.vanillaHeraldries.ToDictionary(id => $"heraldrydef_{id}", id => "Vanilla", StringComparer.OrdinalIgnoreCase));
                return;
            }

            try
            {
                var heraldryList = new List<HeraldryGroupInfo>();
                string[] csvLines = File.ReadAllLines(filterFilePath);

                foreach (string line in csvLines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        string id = $"heraldrydef_{parts[1].Trim().Trim('"')}";
                        string group = parts.Length > 2 ? parts[2].Trim().Trim('"') : string.Empty;
                        heraldryList.Add(new HeraldryGroupInfo { Id = id, Group = group });
                    }
                }

                if (heraldryList.Any())
                {
                    Log.Log($"Custom filter group '{Settings.HeraldryFilterGroup}' loaded.");
                    var groupMappings = heraldryList.ToDictionary(h => h.Id, h => h.Group, StringComparer.OrdinalIgnoreCase);
                    FactionGroupManager.SetGroups(groupMappings);
                }
            }
            catch (Exception ex)
            {
                Log.LogException($"Failed to load or parse filter group '{Settings.HeraldryFilterGroup}'. Disabling filtering.", ex);
                Settings.EnableHeraldryFiltering = false;
            }
        }
    }

    /// <summary>
    /// Manages the mapping of heraldry IDs to custom faction groups.
    /// </summary>
    public static class FactionGroupManager
    {
        private static Dictionary<string, string> FactionGroups = new(StringComparer.OrdinalIgnoreCase);
        public static bool IsCustomFilterActive => FactionGroups.Values.Distinct().Count() > 1;

        private const string DefaultGroup = "Other";

        public static void SetGroups(Dictionary<string, string> groupMappings)
        {
            FactionGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in groupMappings)
            {
                if (string.IsNullOrEmpty(kvp.Key)) continue;
                FactionGroups[kvp.Key] = !string.IsNullOrEmpty(kvp.Value) ? kvp.Value : DefaultGroup;
            }
        }

        public static string GetGroupForHeraldry(string heraldryId) => FactionGroups.TryGetValue(heraldryId, out string group) ? group : DefaultGroup;
    }
}