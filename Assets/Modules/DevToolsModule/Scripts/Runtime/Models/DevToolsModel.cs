using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DevToolsModule.Data.UnityObjects;
using Modules.DevToolsModule.Data.ValueObjects;
using Modules.DevToolsModule.RootsContexts;
using UnityEngine;

namespace Modules.DevToolsModule.Models
{
    /// <summary>
    /// The switches CD_DevTools lists and their values on this device. A value is kept in PlayerPrefs
    /// under "DevTools." and its key; a switch never flipped here has its DefaultOn.
    /// </summary>
    public sealed class DevToolsModel : IConstructable
    {
        private const string PREFS_PREFIX = "DevTools.";

        [Inject(nameof(DevToolsServiceContext))] private GameObject _root { get; set; }

        private readonly List<DevToggleCVO> _toggles = new();

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }
        public IReadOnlyList<DevToggleCVO> Toggles => _toggles;
        /// <summary>Whether the panel may open; off, every switch is its DefaultOn.</summary>
        public bool PanelEnabled { get; private set; } = true;

        public void PostConstruct()
        {
            _toggles.Clear();
            var config = _root.GetComponent<RootAdapter>().GetScriptable<CD_DevTools>();
            if (config == null) return;
            PanelEnabled = config.PanelEnabled;
            foreach (var toggle in config.Toggles)
                if (toggle != null && !string.IsNullOrWhiteSpace(toggle.Key)) _toggles.Add(toggle);
        }

        public void Deconstruct() => _toggles.Clear();

        /// <summary>The switch's value; false for a key the config does not list.</summary>
        public bool IsOn(string key)
        {
            var toggle = Find(key);
            if (toggle == null) return false;
            if (!PanelEnabled) return toggle.DefaultOn;
            return PlayerPrefs.GetInt(PREFS_PREFIX + key, toggle.DefaultOn ? 1 : 0) == 1;
        }

        /// <summary>Sets and keeps a listed switch; a key the config does not list is ignored.</summary>
        public bool Set(string key, bool on)
        {
            if (Find(key) == null || !PanelEnabled) return false;
            PlayerPrefs.SetInt(PREFS_PREFIX + key, on ? 1 : 0);
            PlayerPrefs.Save();
            return true;
        }

        private DevToggleCVO Find(string key)
        {
            foreach (var toggle in _toggles) if (toggle.Key == key) return toggle;
            return null;
        }
    }
}
