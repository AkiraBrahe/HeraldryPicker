using BattleTech.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HeraldryPicker.Patches
{
    /// <summary>
    /// Fixes an issue where the color picker widget would not properly refresh its color options.
    /// </summary>
    [HarmonyPatch(typeof(HeraldryColorPickerWidget), "SetData")]
    public static class HeraldryColorPickerWidget_SetData
    {
        private static readonly Dictionary<HeraldryColorPickerWidget, Coroutine> runningCoroutines = [];

        [HarmonyPrefix]
        public static bool Prefix(HeraldryColorPickerWidget __instance, string selectedPrimaryColorId, string selectedSecondaryColorId, string selectedAccentColorId, UnityAction OnColorChanged)
        {
            if (runningCoroutines.TryGetValue(__instance, out var coroutine) && coroutine != null)
            {
                __instance.StopCoroutine(coroutine);
                runningCoroutines.Remove(__instance);
            }

            __instance.selectedPrimaryColorId = selectedPrimaryColorId;
            __instance.selectedSecondaryColorId = selectedSecondaryColorId;
            __instance.selectedAccentColorId = selectedAccentColorId;
            __instance.OnColorChangedCB = OnColorChanged;

            var newCoroutine = __instance.StartCoroutine(ResetColorOptions(__instance, __instance.LoadColorOptions));
            runningCoroutines[__instance] = newCoroutine;

            return false;
        }

        // Custom coroutine that matches the original logic
        private static IEnumerator ResetColorOptions(HeraldryColorPickerWidget widget, UnityAction onComplete = null)
        {
            var primary = widget.primaryColorPicker;
            var secondary = widget.secondaryColorPicker;
            var accent = widget.accentColorPicker;

            primary.onValueChanged = (UnityAction)System.Delegate.Remove(primary.onValueChanged, new UnityAction(widget.RefreshSelectedColors));
            secondary.onValueChanged = (UnityAction)System.Delegate.Remove(secondary.onValueChanged, new UnityAction(widget.RefreshSelectedColors));
            accent.onValueChanged = (UnityAction)System.Delegate.Remove(accent.onValueChanged, new UnityAction(widget.RefreshSelectedColors));
            primary.ClearOptions();
            secondary.ClearOptions();
            accent.ClearOptions();
            widget.selectedPrimaryColor = HeraldryColorPickerWidget.primarySwatches[0];
            widget.selectedSecondaryColor = HeraldryColorPickerWidget.secondarySwatches[0];
            widget.selectedAccentColor = HeraldryColorPickerWidget.accentSwatches[0];
            yield return new WaitForSeconds(0.5f);
            onComplete?.Invoke();

            runningCoroutines.Remove(widget);
        }
    }
}