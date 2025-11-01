using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using HeraldryPicker.Widgets;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeraldryPicker.Patches
{
    #region UI Setup

    /// <summary>
    /// Adds a heraldry tab to the crest selector and sets up the UI to toggle between the two panels.
    /// </summary>
    [HarmonyPatch(typeof(HeraldryCreatorPanel), "OnAddedToHierarchy")]
    public static class HeraldryCreatorPanel_OnAddedToHierarchy_HeraldryTab
    {
        [HarmonyPostfix]
        public static void Postfix(HeraldryCreatorPanel __instance)
        {
            var crestSelector = __instance.crestPicker.gameObject;
            if (crestSelector.transform.parent.Find("uixPrfPanl_companyHeraldrySelector") == null)
            {
                var heraldrySelector = Object.Instantiate(crestSelector.gameObject, crestSelector.transform.parent);
                heraldrySelector.name = "uixPrfPanl_companyHeraldrySelector";
                heraldrySelector.SetActive(false);

                var loadingNotif = heraldrySelector.transform.Find("Representation/loading-Notif")?.gameObject;
                if (loadingNotif != null)
                {
                    var messageText = loadingNotif.transform.Find("loadElementLayout/messageText")?.GetComponent<LocalizableText>();
                    messageText?.text = "LOADING HERALDRIES";
                }

                var pickerWidget = heraldrySelector.AddComponent<HeraldryPickerWidget>();
                pickerWidget.listParent = heraldrySelector.transform.Find("Representation/content-layout/crestsScrollview-list/Viewport/Content-crestItems") as RectTransform;
                pickerWidget.loadingNotification = loadingNotif;

                SetupTabs(crestSelector, heraldrySelector, pickerWidget, __instance);
            }
        }

        private static void SetupTabs(GameObject crestSelector, GameObject heraldrySelector, HeraldryPickerWidget pickerWidget, HeraldryCreatorPanel panelInstance)
        {
            var crestText = crestSelector.transform.Find("Representation/title-layout/crests_text")?.GetComponent<LocalizableText>();
            var heraldryText = heraldrySelector.transform.Find("Representation/title-layout/crests_text")?.GetComponent<LocalizableText>();

            if (crestText != null && heraldryText != null)
            {
                crestText.text = "Crests\n<size=50%>Heraldries</size>";
                heraldryText.text = "Heraldries\n<size=50%>Crests</size>";

                var crestTabButton = crestText.GetComponent<Button>() ?? crestText.gameObject.AddComponent<Button>();
                crestTabButton.onClick.RemoveAllListeners();
                crestTabButton.onClick.AddListener(() =>
                {
                    crestSelector.SetActive(false);
                    heraldrySelector.SetActive(true);

                    var spinner = heraldrySelector.GetComponentInChildren<UISpinner>(true);
                    if (spinner != null)
                    {
                        spinner.enabled = false;
                        spinner.enabled = true;
                    }

                    if (pickerWidget.listParent.childCount == 0)
                    {
                        pickerWidget.SetData(panelInstance.dataManager, panelInstance.activeDef?.Description?.Id ?? string.Empty, null);
                    }
                });

                var heraldryTabButton = heraldryText.GetComponent<Button>() ?? heraldryText.gameObject.AddComponent<Button>();
                heraldryTabButton.onClick.RemoveAllListeners();
                heraldryTabButton.onClick.AddListener(() =>
                {
                    crestSelector.SetActive(true);
                    heraldrySelector.SetActive(false);
                });
            }
        }
    }

    /// <summary>
    /// Activates and populates the faction filter dropdown in the heraldry selector.
    /// </summary>
    [HarmonyPatch(typeof(HeraldryCreatorPanel), "OnAddedToHierarchy")]
    public static class HeraldryCreatorPanel_OnAddedToHierarchy_FactionFilter
    {
        [HarmonyPrepare]
        public static bool Prepare() => Main.Settings.EnableHeraldryFiltering;

        [HarmonyPostfix, HarmonyPriority(Priority.Low)]
        public static void Postfix(HeraldryCreatorPanel __instance)
        {
            var heraldrySelector = __instance.transform.Find("Representation/uixPrfPanl_companyHeraldrySelector").gameObject;
            if (heraldrySelector == null) return;

            var heraldryPicker = heraldrySelector.GetComponent<HeraldryPickerWidget>();
            if (heraldrySelector == null || heraldryPicker == null) return;

            var squareGo = heraldrySelector.transform.Find("Representation/title-layout/square")?.gameObject;
            var dropdownGo = heraldrySelector.transform.Find("Representation/content-layout/filterDropdown-unused")?.gameObject;
            var fieldGo = dropdownGo?.transform.Find("uixPrfField_dropdown")?.gameObject;
            var dropdown = dropdownGo?.GetComponentInChildren<HBS_Dropdown>(true);
            if (squareGo == null || dropdownGo == null || fieldGo == null || dropdown == null) return;

            var squareButton = squareGo.GetComponent<Button>() ?? squareGo.AddComponent<Button>();
            squareButton.onClick.RemoveAllListeners();
            squareButton.onClick.AddListener(() => dropdownGo.SetActive(!dropdownGo.activeSelf));

            dropdownGo.SetActive(true);
            fieldGo.SetActive(true);

            PopulateDropdown(dropdown, heraldryPicker);
            var counterText = AddCounter(dropdownGo.transform, heraldrySelector.transform);
            heraldryPicker.counterText = counterText;
        }

        private static void PopulateDropdown(HBS_Dropdown dropdown, HeraldryPickerWidget heraldryPicker)
        {
            dropdown.ClearOptions();
            dropdown.onValueChanged.RemoveAllListeners();

            if (FactionGroupManager.IsCustomFilterActive)
            {
                var factions = heraldryPicker.GetFactionNames();
                dropdown.AddOptions(["All"]);
                dropdown.AddOptions(factions.OrderBy(f => f).ToList());
            }
            else
            {
                dropdown.AddOptions(["All", "Vanilla", "Modded"]);
            }

            dropdown.onValueChanged.AddListener(index =>
            {
                string selectedFilter = dropdown.options[index].text;
                heraldryPicker.FilterHeraldries(selectedFilter);
            });
        }

        private static LocalizableText AddCounter(Transform parent, Transform searchRoot)
        {
            if (parent != null)
            {
                var counterGo = new GameObject("heraldryCounter");
                counterGo.transform.SetParent(parent, false);
                var counterText = counterGo.AddComponent<LocalizableText>();

                var originalText = searchRoot.Find("Representation/title-layout/crests_text")?.GetComponent<LocalizableText>();
                if (originalText != null)
                {
                    counterText.font = originalText.font;
                    counterText.fontSize = 20;
                    counterText.alignment = TextAlignmentOptions.MidlineRight;
                }

                var rectTransform = counterGo.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(1, 0.5f);
                rectTransform.anchorMax = new Vector2(1, 0.5f);
                rectTransform.pivot = new Vector2(1, 0.5f);
                rectTransform.anchoredPosition = new Vector2(-20, 0);
                return counterText;
            }

            return null;
        }
    }

    #endregion

    #region State Management

    /// <summary>
    /// Resets the heraldry picker widget when the "Cancel" button is clicked.
    /// </summary>
    [HarmonyPatch(typeof(HeraldryCreatorPanel), "OnCancelClicked")]
    public static class HeraldryCreatorPanel_OnCancelClicked
    {
        [HarmonyPostfix]
        public static void Postfix(HeraldryCreatorPanel __instance)
        {
            var heraldryPickerWidget = __instance.GetComponentInChildren<HeraldryPickerWidget>(true);
            heraldryPickerWidget?.ResetSelection();
        }
    }

    /// <summary>
    /// Resets the heraldry picker widget when the "Reset" button is clicked.
    /// </summary>
    [HarmonyPatch(typeof(HeraldryCreatorPanel), "OnResetClicked")]
    public static class HeraldryCreatorPanel_OnResetClicked
    {
        [HarmonyPostfix]
        public static void Postfix(HeraldryCreatorPanel __instance)
        {
            var heraldryPickerWidget = __instance.GetComponentInChildren<HeraldryPickerWidget>(true);
            heraldryPickerWidget?.ResetSelection();
        }
    }

    #endregion

    #region Data Persistence Fixes

    /// <summary>
    /// Prevents overwriting a custom crest selection when refreshing the heraldry.
    /// </summary>
    [HarmonyPatch(typeof(HeraldryCreatorPanel), "RefreshHeraldry")]
    public static class HeraldryCreatorPanel_RefreshHeraldry
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            return new CodeMatcher(instructions, il)
                .Advance(6).CreateLabel(out var skipLabel)
                .Start().InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, typeof(HeraldryCreatorPanel).GetField("crestPicker", BindingFlags.Instance | BindingFlags.NonPublic)),
                    new CodeInstruction(OpCodes.Ldfld, typeof(HeraldryCrestPickerWidget).GetField("selectedCrest", BindingFlags.Instance | BindingFlags.NonPublic)),
                    new CodeInstruction(OpCodes.Brfalse, skipLabel))
                .InstructionEnumeration();
        }
    }

    /// <summary>
    /// Ensures the correct crest ID is saved when applying a crest from a heraldry.
    /// </summary>
    [HarmonyPatch(typeof(HeraldryCreatorPanel), "SaveHeraldry")]
    public static class HeraldryCreatorPanel_SaveHeraldry
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            return new CodeMatcher(instructions, il)
                .MatchForward(false,
                    new CodeMatch(i => i.opcode == OpCodes.Ldfld && i.operand is FieldInfo fi && fi.Name == "crestPicker"),
                    new CodeMatch(i => i.opcode == OpCodes.Callvirt && i.operand is MethodInfo mi && mi.Name == "get_selectedCrestId"))
                .SetOperandAndAdvance(typeof(HeraldryCreatorPanel).GetField("activeDef", BindingFlags.Instance | BindingFlags.NonPublic))
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldfld, typeof(HeraldryDef).GetField("textureLogoID", BindingFlags.Instance | BindingFlags.Public)))
                .InstructionEnumeration();
        }
    }

    #endregion
}