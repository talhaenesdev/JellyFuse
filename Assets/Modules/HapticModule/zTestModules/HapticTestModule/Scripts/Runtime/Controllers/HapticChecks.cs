#if UNITY_EDITOR
using System;
using Modules.HapticModule.Data.UnityObjects;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Models;
using UnityEngine;
namespace Modules.HapticModule.HapticTestModule.Controllers
{
    public sealed class HapticChecks
    {
        public string Run()
        {
            int count = 0;
            void Check(bool ok, string name) { if (!ok) throw new Exception("Haptic check: " + name); count++; }
            var config = ScriptableObject.CreateInstance<CD_Haptic>();
            try
            {
                var model = new HapticModel(); model.Load(config, true);
                Check(model.TryAccept(HapticPreset.Selection, 0, out _), "First selection accepted");
                Check(!model.TryAccept(HapticPreset.Selection, .01, out _), "Repeated selection throttled");
                Check(model.TryAccept(HapticPreset.Selection, .1, out _), "Selection allowed after interval");
                Check(model.TryAccept(HapticPreset.Success, .11, out _), "Success not suppressed by recent selection");
                Check(!model.TryAccept(HapticPreset.Success, .12, out _), "Repeated success throttled");
                model.SetEnabled(false);
                Check(!model.TryAccept(HapticPreset.Heavy, 1, out _), "Disabled blocks feedback");
                model.SetEnabled(true);
                Check(model.TryAccept(HapticPreset.Selection, 1, out _), "Enable resets cooldown");
                config.Presets[0].Enabled = false;
                Check(!model.TryAccept(HapticPreset.Selection, 2, out _), "Preset can be disabled");
                config.Presets[1].Effect = HapticPreset.Heavy;
                Check(model.TryAccept(HapticPreset.Light, 2, out var effect) && effect == HapticPreset.Heavy, "Preset remapping");
                Check(!model.TryAccept((HapticPreset)99, 3, out _), "Unknown preset ignored");
                Check(!model.TryAccept(HapticPreset.Medium, double.NaN, out _), "Invalid clock ignored");
                model.Load(null, true);
                Check(!model.TryAccept(HapticPreset.Selection, 4, out _), "Missing config does not vibrate");
                return "Haptic: " + count + " checks passed.";
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }
    }
}
#endif
