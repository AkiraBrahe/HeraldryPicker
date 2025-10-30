using BattleTech.Data;
using BattleTech.UI;
using UnityEngine.Events;

namespace HeraldryPicker.Patches
{
    /// <summary>
    /// Fixes an issue where the crest picker widget could not start its coroutine when not visible.
    /// </summary>
    [HarmonyPatch(typeof(HeraldryCrestPickerWidget), "SetData")]
    public static class HeraldryCrestPickerWidget_SetData
    {
        [HarmonyPrefix]
        public static bool Prefix(HeraldryCrestPickerWidget __instance, DataManager dataManager, string selectedCrestId, UnityAction crestSelectedCB)
        {
            __instance.dataManager = dataManager;
            __instance.crestSelectedCB = crestSelectedCB;
            __instance.initialSelectedCrestID = selectedCrestId;

            var panel = __instance.GetComponentInParent<HeraldryCreatorPanel>();
            if (panel != null)
            {
                panel.StartCoroutine(__instance.InitCrests());
            }
            else
            {
                __instance.StartCoroutine(__instance.InitCrests());
            }

            return false;
        }
    }
}