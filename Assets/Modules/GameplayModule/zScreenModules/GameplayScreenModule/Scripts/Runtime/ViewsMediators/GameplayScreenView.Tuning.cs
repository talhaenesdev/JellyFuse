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
        public const string PAGE_LOOK = "look";

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
            [PAGE_LOOK] = new[]
            {
                "Board.Wall.r", "Board.Wall.g", "Board.Wall.b",
                "Board.Door.r", "Board.Door.g", "Board.Door.b",
                "Board.Tile.r", "Board.Tile.g", "Board.Tile.b",
                "Board.TileAlt.r", "Board.TileAlt.g", "Board.TileAlt.b",
                "Board.GridCorner.r", "Board.GridCorner.g", "Board.GridCorner.b",
                "Look.GridCornerLightTint.r", "Look.GridCornerLightTint.g", "Look.GridCornerLightTint.b",
            },
        };
        /// <summary>The colours on the colour page, by field: the section each sits under and its name there.</summary>
        private static readonly Dictionary<string, (string Group, string Label)> COLOUR_NAMES = new()
        {
            ["Wall"] = ("Duvarlar", "Duvar"),
            ["Door"] = ("Kapılar", "Kapı"),
            ["Tile"] = ("Grid", "Karo A"),
            ["TileAlt"] = ("Grid", "Karo B"),
            ["GridCorner"] = ("Grid", "Köşe"),
            ["GridCornerLightTint"] = ("Grid", "Parlayan Köşe"),
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
                int channel = Channel(key);
                if (channel >= 0)
                {
                    // A colour is offered as three sliders, one a channel, each 0 to 255 in whole steps.
                    COLOUR_NAMES.TryGetValue(field.Name, out var named);
                    list.Add(new DevSliderVO
                    {
                        Key = key, Label = (named.Label ?? Nicify(field.Name)) + " " + "RGB"[channel], Group = named.Group,
                        Min = 0, Max = 255, Value = Read(target, field, key), Whole = true,
                    });
                    continue;
                }
                var range = field.GetCustomAttribute<RangeAttribute>();
                bool whole = field.FieldType == typeof(int);
                float value = Read(target, field, key);
                list.Add(new DevSliderVO { Key = key, Label = Nicify(field.Name), Min = range != null ? range.min : 0, Max = range != null ? range.max : Mathf.Max(1, value * 2), Value = value, Whole = whole });
            }
            return list;
        }

        public void SetTuned(string key, float value)
        {
            if (!Resolve(key, out var target, out var field)) return;
            Write(target, field, key, value);
            PlayerPrefs.SetFloat(PREFS + key, value);
            PlayerPrefs.Save();
            if (key.StartsWith("Hud.") || key.StartsWith("Look.") || key.StartsWith("Board.") || REBUILD.Contains(key)) RefreshPresentation();
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
                rebuild |= key.StartsWith("Hud.") || key.StartsWith("Look.") || key.StartsWith("Board.") || REBUILD.Contains(key);
            }
            foreach (var fresh in defaults.Values) Destroy(fresh);
            PlayerPrefs.Save();
            if (rebuild) RefreshPresentation();
        }

        /// <summary>The asset and the field a key names.</summary>
        private bool Resolve(string key, out object target, out FieldInfo field)
        {
            target = _presentation.Feel;
            string name = Channel(key) >= 0 ? key.Substring(0, key.Length - 2) : key;
            if (key.StartsWith("Hud.")) { target = _presentation.Hud; name = name.Substring(4); }
            else if (key.StartsWith("Look.")) { target = _presentation.Look; name = name.Substring(5); }
            else if (key.StartsWith("Panel.")) { target = _panelAnimation; name = name.Substring(6); }
            else if (key.StartsWith("Board.")) { target = _boardColours; name = name.Substring(6); }
            field = target != null ? target.GetType().GetField(name) : null;
            if (field == null) return false;
            return Channel(key) >= 0 ? field.FieldType == typeof(Color) : field.FieldType == typeof(float) || field.FieldType == typeof(int);
        }
        /// <summary>Which colour channel a key ending ".r", ".g" or ".b" names (0-2); -1 for any other key.</summary>
        private static int Channel(string key) =>
            key.Length > 2 && key[key.Length - 2] == '.' ? "rgb".IndexOf(key[key.Length - 1]) : -1;
        private static float Read(object target, FieldInfo field, string key)
        {
            int channel = Channel(key);
            if (channel >= 0) return Mathf.Round(((Color)field.GetValue(target))[channel] * 255);
            return field.FieldType == typeof(int) ? (int)field.GetValue(target) : (float)field.GetValue(target);
        }
        private static void Write(object target, FieldInfo field, string key, float value)
        {
            int channel = Channel(key);
            if (channel >= 0)
            {
                var colour = (Color)field.GetValue(target);
                colour[channel] = Mathf.Clamp(value, 0, 255) / 255f;
                field.SetValue(target, colour);
            }
            else if (field.FieldType == typeof(int)) field.SetValue(target, Mathf.RoundToInt(value));
            else field.SetValue(target, value);
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
                    Write(target, field, key, PlayerPrefs.GetFloat(PREFS + key));
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
