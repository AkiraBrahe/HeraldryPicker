using BattleTech.Rendering.MechCustomization;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using UnityEngine;
using UnityEngine.UI;

namespace HeraldryPicker.Patches
{
    /// <summary>
    /// Shows the selected color name in the header of the color selectors.
    /// </summary>
    [HarmonyPatch(typeof(HorizontalScrollSelectorColor), "SetColor")]
    public static class HorizontalScrollSelectorColor_SetColor_HeaderName
    {
        [HarmonyPostfix]
        public static void Postfix(HorizontalScrollSelectorColor __instance, int index, ColorSwatch option)
        {
            if (index != 3) return;

            var mainColorHeader = __instance.transform.Find("mainColorHeader");
            if (mainColorHeader != null)
            {
                var headerText = mainColorHeader.Find("text").GetComponent<LocalizableText>();
                headerText?.text = $"{headerText.text.Split(':')[0]}: {option.name}";
            }
        }
    }

    /// <summary>
    /// Shows the color name on each swatch of the color selectors.
    /// </summary>
    [HarmonyPatch(typeof(HorizontalScrollSelectorColor), "SetColor")]
    public static class HorizontalScrollSelectorColor_SetColor_SwatchNames
    {
        [HarmonyPrepare]
        public static bool Prepare() => Main.Settings.ShowColorNameOnEachSwatch;

        [HarmonyPostfix]
        public static void Postfix(HorizontalScrollSelectorColor __instance, int index, ColorSwatch option)
        {
            var colorSwatch = __instance.colorTrackers[index].gameObject;
            var textComponent = colorSwatch?.GetComponentInChildren<Text>();

            if (textComponent == null)
            {
                GameObject textWidget = new("ColorID");
                textWidget.transform.SetParent(colorSwatch.transform, false);

                textComponent = textWidget.AddComponent<Text>();
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textComponent.fontStyle = FontStyle.Bold;
                textComponent.fontSize = 10;
                textComponent.alignment = TextAnchor.MiddleCenter;
                textComponent.verticalOverflow = VerticalWrapMode.Overflow;

                var outline = textComponent.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1f, -1f);

                var rectTransform = textComponent.GetComponent<RectTransform>();
                var parentRect = colorSwatch.transform.parent.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero; rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero; rectTransform.offsetMax = Vector2.zero;
                rectTransform.sizeDelta = parentRect.sizeDelta - new Vector2(5f, 5f);
            }

            string displayText;
            if (option.name.StartsWith("Bright"))
            {
                string[] parts = option.name.Split('_');
                string colorName = parts[0].Replace("Bright", "").Substring(0, 2);
                displayText = colorName + parts[1];
            }
            else
            {
                displayText = option.name.Substring(option.name.IndexOf('_') + 1);
            }

            textComponent.text = displayText;
        }
    }
}