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
                    PrettifyName(h.Description.Name),
                    h.Description.Id.Replace("heraldrydef_", ""),
                    vanillaHeraldries.Contains(h.Description.Id.Replace("heraldrydef_", "")) ? "Vanilla" : "",
                    h.primaryMechColorID,
                    h.secondaryMechColorID,
                    h.tertiaryMechColorID
                )).OrderBy(h => h.Name, new NaturalStringComparer()).ThenBy(h => h.Id, new NaturalStringComparer()).ToList();

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
        /// Converts a PascalCase string to a more human-readable "Pascal Case" string. Excludes some prefixes from spacing.
        /// </summary>
        private static string PrettifyName(string name) => Regex.Replace(name ?? string.Empty, @"(\p{Ll})(\p{Lu})", "$1 $2")
            .Replace("Com Star", "ComStar").Replace("Mac ", "Mac").Replace("Mc ", "Mc").Replace("Mech Warrior", "MechWarrior");

        /// <summary>
        /// Generates the full content for the CSV file line-by-line, including the header.
        /// </summary>
        private static IEnumerable<string> CreateCsvLines(IEnumerable<HeraldryExportData> records)
        {
            static string Sanitize(string name) => name == null ? string.Empty : name.Contains(',') || name.Contains('"') || name.Contains('\n') ? $"\"{name.Replace("\"", "\"\"")}\"" : name;

            yield return "Name,Id,Group,PrimaryColor,SecondaryColor,TertiaryColor";
            foreach (var record in records) yield return $"{Sanitize(record.Name)},{record.Id},{record.Group},{record.PrimaryMechColorID},{record.SecondaryMechColorID},{record.TertiaryMechColorID}";
        }

        internal readonly struct HeraldryExportData
        {
            public string Name { get; }
            public string Id { get; }
            public string Group { get; }
            public string PrimaryMechColorID { get; }
            public string SecondaryMechColorID { get; }
            public string TertiaryMechColorID { get; }
            public HeraldryExportData(string name, string id, string group, string primary, string secondary, string tertiary) =>
                (Name, Id, Group, PrimaryMechColorID, SecondaryMechColorID, TertiaryMechColorID) = (name, id, group, primary, secondary, tertiary);
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
