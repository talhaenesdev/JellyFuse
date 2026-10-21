using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Data.UnityObjects;
using Modules.HapticModule.Data.ValueObjects;
using Modules.HapticModule.Enums;
using Modules.HapticModule.RootsContexts;
using UnityEngine;
namespace Modules.HapticModule.Models
{
    public sealed class HapticModel : IConstructable
    {
        public const string PREFERENCE_KEY = "Haptic.Enabled";
        [Inject(nameof(HapticServiceContext))] private GameObject _root { get; set; }
        private readonly Dictionary<HapticPreset, double> _lastPlayed = new();
        public CD_Haptic Config { get; private set; }
        public bool Enabled { get; private set; }
        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }
        public void PostConstruct()
        {
            var config = _root.GetComponent<RootAdapter>().GetScriptable<CD_Haptic>();
            Load(config, PlayerPrefs.GetInt(PREFERENCE_KEY, config != null && config.DefaultEnabled ? 1 : 0) == 1);
        }
        public void Load(CD_Haptic config, bool enabled) { Config = config; Enabled = enabled; _lastPlayed.Clear(); }
        public void SetEnabled(bool enabled) { Enabled = enabled; _lastPlayed.Clear(); }
        /// <summary>The config entry that says how the effect feels; null when the config does not list it.</summary>
        public HapticPresetCVO EntryFor(HapticPreset effect) => Config?.Presets.Find(p => p != null && p.Preset == effect);
        public bool TryAccept(HapticPreset preset, double now, out HapticPreset effect)
        {
            effect = preset;
            if (!Enabled || Config == null || double.IsNaN(now) || double.IsInfinity(now)) return false;
            var entry = Config.Presets.Find(p => p != null && p.Preset == preset);
            if (entry == null || !entry.Enabled || !Enum.IsDefined(typeof(HapticPreset), entry.Effect)) return false;
            float interval = entry.MinimumInterval;
            if (float.IsNaN(interval) || float.IsInfinity(interval)) return false;
            if (_lastPlayed.TryGetValue(preset, out double last) && now - last < Math.Max(0, interval)) return false;
            _lastPlayed[preset] = now; effect = entry.Effect; return true;
        }
    }
}
