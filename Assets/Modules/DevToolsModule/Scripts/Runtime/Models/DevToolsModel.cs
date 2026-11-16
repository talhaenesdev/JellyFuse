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
    /// under "DevTools." and its key; a switch never flipped here has its DefaultOn. A device unlocked
    /// with the PIN keeps the switches it set, until its data is cleared; the panel itself asks for the PIN
    /// every time it opens.
    /// </summary>
    public sealed class DevToolsModel : IConstructable
    {
        private const string PREFS_PREFIX = "DevTools.";
        private const string UNLOCKED_PREF = PREFS_PREFIX + "Unlocked";

        [Inject(nameof(DevToolsServiceContext))] private GameObject _root { get; set; }

        private readonly List<DevToggleCVO> _toggles = new();
        private bool _configEnabled = true;
        private string _pinHash = string.Empty;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }
        public IReadOnlyList<DevToggleCVO> Toggles => _toggles;
        /// <summary>Whether the panel may open: enabled in the config, or this device was unlocked. Off, every switch is its DefaultOn.</summary>
        public bool PanelEnabled => _configEnabled || Unlocked;
        /// <summary>Whether this device was unlocked with the PIN.</summary>
        public bool Unlocked { get; private set; }
        /// <summary>CD_DevTools.PanelEnabled: the panel opens without the PIN (a development build).</summary>
        public bool EnabledInBuild => _configEnabled;
        /// <summary>The PIN was just accepted: the one opening it lets through. Kept in memory only.</summary>
        private bool _pinPass;
        public float CornerSequenceSeconds { get; private set; } = 3f;
        public float CornerZone { get; private set; } = .18f;

        public void PostConstruct()
        {
            _toggles.Clear();
            Unlocked = PlayerPrefs.GetInt(UNLOCKED_PREF, 0) == 1;
            var config = _root.GetComponent<RootAdapter>().GetScriptable<CD_DevTools>();
            if (config == null) return;
            _configEnabled = config.PanelEnabled;
            _pinHash = config.PinHash ?? string.Empty;
            CornerSequenceSeconds = config.CornerSequenceSeconds;
            CornerZone = config.CornerZone;
            foreach (var toggle in config.Toggles)
                if (toggle != null && !string.IsNullOrWhiteSpace(toggle.Key)) _toggles.Add(toggle);
        }

        public void Deconstruct() => _toggles.Clear();

        /// <summary>Unlocks this device when the PIN matches the configured hash. No hash configured, nothing unlocks.</summary>
        public bool TryUnlock(string pin)
        {
            if (string.IsNullOrEmpty(_pinHash) || CD_DevTools.HashPin(pin) != _pinHash) return false;
            Unlocked = true;
            _pinPass = true;
            PlayerPrefs.SetInt(UNLOCKED_PREF, 1);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>
        /// Whether this opening of the panel may go ahead: a build with the panel enabled always may; otherwise
        /// only the opening right after the PIN was accepted, so every opening asks for the PIN again.
        /// </summary>
        public bool TakeOpening()
        {
            if (_configEnabled) return true;
            bool pass = _pinPass;
            _pinPass = false;
            return pass;
        }

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
