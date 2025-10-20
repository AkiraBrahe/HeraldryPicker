using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using HeraldryPicker.Widgets;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeraldryPicker.Patches
{
    #region Heraldry Tab

    /// <summary>
    /// Adds a Heraldry tab to the Crest selector in the Customize Company menu
    /// and sets up the UI to toggle between the Crest and Heraldry selectors.
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

    #endregion

    #region Faction Filter Dropdown

    /// <summary>
    /// Activates and populates the faction filter dropdown in the Heraldry selector.
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
}