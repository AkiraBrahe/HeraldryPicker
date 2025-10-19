using BattleTech;
using BattleTech.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HeraldryPicker.Utils
{
    internal class HeraldryExporter
    {
        private readonly struct HeraldryExportData
        {
            public string Name { get; }
            public string Id { get; }
            public string Group { get; }
            public HeraldryExportData(string name, string id, string group) => (Name, Id, Group) = (name, id, group);
        }

        private static bool exported = false;

        /// <summary>
        /// Exports all heraldries present in the game to an external CSV file.
        /// </summary>
        public static void ExportHeraldries(DataManager dataManager)
        {
            if (exported) return;

            try
            {
                string filterGroupDir = Path.Combine(Main.ModDir, "FilterGroups");
                Directory.CreateDirectory(filterGroupDir);
                string exportPath = Path.Combine(filterGroupDir, "AllHeraldries.csv");

                var allHeraldryDefs = new List<HeraldryDef>();
                foreach (var kvp in dataManager.Heraldries)
                {
                    var def = kvp.Value;
                    if (def != null)
                    {
                        allHeraldryDefs.Add(def);
                    }
                }

                var heraldryList = allHeraldryDefs.Select(h => new HeraldryExportData(
                    PrettifyName(h.Description.Name).Replace(",", ""),
                    h.Description.Id.Replace("heraldrydef_", ""),
                    vanillaHeraldries.Contains(h.Description.Id.Replace("heraldrydef_", "")) ? "Vanilla" : ""
                )).OrderBy(h => h.Name).ThenBy(h => h.Id, new NaturalStringComparer()).ToList();

                File.WriteAllLines(exportPath, CreateCsvLines(heraldryList));
                Main.Log.LogDebug($"Successfully exported {heraldryList.Count()} heraldries to {exportPath}");
            }
            catch (Exception ex)
            {
                Main.Log.LogException("Failed to export heraldries.", ex);
            }
            finally
            {
                exported = true;
            }
        }

        /// <summary>
        /// Converts a PascalCase string to a more human-readable "Pascal Case" string.
        /// </summary>
        private static string PrettifyName(string name) => Regex.Replace(name ?? string.Empty, @"(\p{Ll})(\p{Lu})", "$1 $2");

        /// <summary>
        /// Generates the full content for the CSV file line-by-line, including the header.
        /// </summary>
        private static IEnumerable<string> CreateCsvLines(IEnumerable<HeraldryExportData> records)
        {
            yield return "Name,Id,Group";

            foreach (var record in records)
            {
                string name = record.Name;
                string id = record.Id;
                string group = record.Group;
                yield return $"{name},{id},{group}";
            }
        }

        internal static readonly HashSet<string> vanillaHeraldries =
        [
            "BaumannGroup", "Betrayers", "BlackCalderaDefense", "BlackWidowCompany", "BountyHunterAssociates", "canopus", "career_freelancer", "career_gladiator", "career_mercenary", "career_merchantguard",
            "career_pirate", "career_soldier", "davion", "default", "directorate", "DuchyOfAndurien", "EmeraldDawn", "enemy", "GrayDeathLegion", "hostileMercs",
            "KellHounds", "kurita", "liao", "locals", "MajestyMetals", "marik", "MasonsMarauders", "nautilus", "none", "PaladinProtectionLLC",
            "pirate01", "pirate02", "pirate03", "pirate04", "pirate05", "pirates", "player", "ProfHorvat", "random01", "random02",
            "random03", "random04", "random05", "RazorbackMercs", "RedHareRegiment", "restoration", "SecuritySolutionsInc", "SianTriumphant", "sldf", "sldf_OD",
            "SteelBeast", "steiner", "taurian", "Template", "yangBigScore_stolen"
        ];
    }
}
