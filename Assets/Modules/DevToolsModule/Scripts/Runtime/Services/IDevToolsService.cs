using System.Collections.Generic;
using Modules.DevToolsModule.Data.ValueObjects;

namespace Modules.DevToolsModule.Services
{
    /// <summary>
    /// Developer switches for trying a new system next to the old one. A Command that chooses between
    /// two behaviours asks IsOn with its key; the developer panel lists Toggles and flips them with Set.
    /// </summary>
    public interface IDevToolsService
    {
        IReadOnlyList<DevToggleCVO> Toggles { get; }
        /// <summary>
        /// Whether the developer panel may open: CD_DevTools.PanelEnabled, or this device was unlocked
        /// with the PIN. Off, every switch reads its DefaultOn.
        /// </summary>
        bool PanelEnabled { get; }
        /// <summary>Whether this device was unlocked with the PIN; it stays unlocked until the app's data is cleared.</summary>
        bool Unlocked { get; }
        /// <summary>Seconds the secret corner taps must fit in.</summary>
        float CornerSequenceSeconds { get; }
        /// <summary>How far a corner reaches into the screen, as a share of its shorter side.</summary>
        float CornerZone { get; }
        /// <summary>Unlocks this device when the PIN matches CD_DevTools.PinHash.</summary>
        bool TryUnlock(string pin);
        bool IsOn(string key);
        /// <summary>Sets and keeps the switch on this device; false when CD_DevTools does not list the key.</summary>
        bool Set(string key, bool on);
    }
}
