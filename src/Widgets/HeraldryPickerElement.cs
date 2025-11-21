using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HeraldryPicker.Widgets
{
    /// <summary>
    /// UI element for a single heraldry entry.
    /// </summary>
    public class HeraldryPickerElement : MonoBehaviour
    {
        public HeraldryDef heraldryDef;
        private HBSDOTweenToggle heraldryBtn;
        private UnityAction<HeraldryDef, bool> onSelected;

        public void SetData(HeraldryDef def, UnityAction<HeraldryDef, bool> onSelected)
        {
            this.heraldryDef = def;
            this.onSelected = onSelected;

            var contents = transform.Find("Representation/contents");
            if (contents != null)
            {
                var crestImage = contents.Find("crest_image")?.GetComponent<Image>();
                if (crestImage != null && def.TextureLogo != null)
                {
                    crestImage.sprite = Sprite.Create(def.TextureLogo, new Rect(0, 0, def.TextureLogo.width, def.TextureLogo.height), new Vector2(0.5f, 0.5f));
                }
            }

            var tooltip = gameObject.GetComponent<HBSTooltip>() ?? gameObject.AddComponent<HBSTooltip>();
            string title = def.Description.Name != null ? Regex.Replace(def.Description.Name, @"(\p{Ll})(\p{Lu})", "$1 $2") : "Unknown";
            string details = def.Description.Id != null ? $"Id: {def.Description.Id.Replace("heraldrydef_", "")}" : "";
            var descriptionDef = new BaseDescriptionDef(def.Description.Id, title, details, null);
            tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descriptionDef));

            var button = gameObject.GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        public void OnClick()
        {
            bool assignCrest = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            onSelected?.Invoke(heraldryDef, assignCrest);
        }

        public void SetSelected(bool isSelected)
        {
            if (heraldryBtn == null) heraldryBtn = GetComponentInChildren<HBSDOTweenToggle>();
            heraldryBtn?.SetState(isSelected ? ButtonState.Selected : ButtonState.Enabled, false);
        }
    }
}