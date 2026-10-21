using System.Collections.Generic;
using System.Reflection;
using Modules.DevToolsModule.Data.ValueObjects;
using UnityEngine;
using CD_Feel = Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects.CD_JellyFeel;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The values the developer panel's tuning pages may drag: the jelly feel (CD_JellyFeel) on the
    /// animation page, the map's placement (CD_GameplayHud) and frame (CD_JellyBoardLook) on the map
    /// page. A change shows at once; the ones that shape the pictures or the layout redraw them.
    /// In the Editor the asset keeps the change after Play. On a device the change is kept in
    /// PlayerPrefs and put back over the assets every time the game starts - a stopgap while the game
    /// is being tuned on phones, to be removed with the tuning pages.
    /// </summary>
    public partial class GameplayScreenView
    {
        /// <summary>The tuning page names the developer panel asks for.</summary>
        public const string PAGE_ANIMATION = "animation";

        /// <summary>
        /// The fields offered, page by page, in the order the page lists them. A key is the field's
        /// name on CD_JellyFeel, or "Hud." / "Look." and its name on CD_GameplayHud / CD_JellyBoardLook.
        /// </summary>
        private static readonly Dictionary<string, string[]> TUNABLE = new()
        {
            [PAGE_ANIMATION] = new[]
            {
                nameof(CD_Feel.SlideSpeed), nameof(CD_Feel.DragFollow),
                nameof(CD_Feel.BaseMotion), nameof(CD_Feel.TopLag), nameof(CD_Feel.TopMaxOffset), nameof(CD_Feel.TopSquash),
                nameof(CD_Feel.TopStiffness), nameof(CD_Feel.TopDamping), nameof(CD_Feel.RigBend), nameof(CD_Feel.RigVerticalStretch), nameof(CD_Feel.RigCurve),
                nameof(CD_Feel.RigBulge), nameof(CD_Feel.RigTopDip), nameof(CD_Feel.BaseOverlap),
                nameof(CD_Feel.FuseBitsPerCell), nameof(CD_Feel.FuseBitSpeed), nameof(CD_Feel.FuseBitSize), nameof(CD_Feel.FuseBitLife),
                nameof(CD_Feel.FuseFlash), nameof(CD_Feel.FuseFlashTime), nameof(CD_Feel.FuseRingSize), nameof(CD_Feel.FuseRingTime), nameof(CD_Feel.FuseRingAlpha),
                nameof(CD_Feel.FuseShake), nameof(CD_Feel.FuseShakeTime),
                "Panel.Duration", "Panel.StartScale", "Panel.SlideDistance", "Panel.FadeSeconds", "Panel.DimSeconds", "Panel.Delay",
                "Look.FuseGlowBloomStrength", "Look.FuseGlowBloomSize", "Look.FuseGlowCornerScale", "Look.FuseGlowSeconds", "Look.FuseGlowBlinks",
            },
        };
        /// <summary>Fields that change how a picture is cut or meshed, or where the map sits: redrawn after them.</summary>
        private static readonly HashSet<string> REBUILD = new()
        {
            nameof(CD_Feel.BaseOverlap), nameof(CD_Feel.BaseShare), nameof(CD_Feel.RigCurve), nameof(CD_Feel.RigBulge), nameof(CD_Feel.RigTopDip),
        };

        public List<DevSliderVO> TuningSliders(string page)
        {
            var list = new List<DevSliderVO>();
            if (!TUNABLE.TryGetValue(page, out var keys)) return list;
            foreach (var key in keys)
            {
                if (!Resolve(key, out var target, out var field)) continue;
                var range = field.GetCustomAttribute<RangeAttribute>();
                bool whole = field.FieldType == typeof(int);
                float value = whole ? (int)field.GetValue(target) : (float)field.GetValue(target);
                list.Add(new DevSliderVO { Key = key, Label = Nicify(field.Name), Min = range != null ? range.min : 0, Max = range != null ? range.max : Mathf.Max(1, value * 2), Value = value, Whole = whole });
            }
            return list;
        }

        public void SetTuned(string key, float value)
        {
            if (!Resolve(key, out var target, out var field)) return;
            Write(target, field, value);
            PlayerPrefs.SetFloat(PREFS + key, value);
            PlayerPrefs.Save();
            if (key.StartsWith("Hud.") || key.StartsWith("Look.") || REBUILD.Contains(key)) RefreshPresentation();
        }

        /// <summary>
        /// Every value on a page back to its default - the value a new asset of its type starts with,
        /// as written in code - and whatever this device kept for it forgotten.
        /// </summary>
        public void ResetTuning(string page)
        {
            if (!TUNABLE.TryGetValue(page, out var keys)) return;
            var defaults = new Dictionary<System.Type, ScriptableObject>();
            bool rebuild = false;
            foreach (var key in keys)
            {
                if (!Resolve(key, out var target, out var field)) continue;
                var type = target.GetType();
                if (!defaults.TryGetValue(type, out var fresh)) defaults[type] = fresh = ScriptableObject.CreateInstance(type);
                field.SetValue(target, field.GetValue(fresh));
                PlayerPrefs.DeleteKey(PREFS + key);
                rebuild |= key.StartsWith("Hud.") || key.StartsWith("Look.") || REBUILD.Contains(key);
            }
            foreach (var fresh in defaults.Values) Destroy(fresh);
            PlayerPrefs.Save();
            if (rebuild) RefreshPresentation();
        }

        /// <summary>The asset and the field a key names.</summary>
        private bool Resolve(string key, out object target, out FieldInfo field)
        {
            target = _presentation.Feel;
            string name = key;
            if (key.StartsWith("Hud.")) { target = _presentation.Hud; name = key.Substring(4); }
            else if (key.StartsWith("Look.")) { target = _presentation.Look; name = key.Substring(5); }
            else if (key.StartsWith("Panel.")) { target = _panelAnimation; name = key.Substring(6); }
            field = target != null ? target.GetType().GetField(name) : null;
            return field != null && (field.FieldType == typeof(float) || field.FieldType == typeof(int));
        }
        private static void Write(object target, FieldInfo field, float value)
        {
            if (field.FieldType == typeof(int)) field.SetValue(target, Mathf.RoundToInt(value)); else field.SetValue(target, value);
        }

        private const string PREFS = "devtools.feel.";
        private bool _savedTuningApplied;

        /// <summary>On a device, the values tuned on it before are put back once per run. The Editor keeps them in the assets themselves.</summary>
        private void ApplySavedFeel()
        {
#if !UNITY_EDITOR
            if (_savedTuningApplied) return;
            _savedTuningApplied = true;
            foreach (var keys in TUNABLE.Values)
                foreach (var key in keys)
                {
                    if (!PlayerPrefs.HasKey(PREFS + key) || !Resolve(key, out var target, out var field)) continue;
                    Write(target, field, PlayerPrefs.GetFloat(PREFS + key));
                }
#endif
        }

        private static string Nicify(string name)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) sb.Append(' ');
                sb.Append(name[i]);
            }
            return sb.ToString();
        }
    }
}
