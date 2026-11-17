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
        /// <summary>Whether this build may open the panel at all: never in a store build.</summary>
        bool Available { get; }
        /// <summary>Whether this device was unlocked with the PIN; it stays unlocked until the app's data is cleared.</summary>
        bool Unlocked { get; }
        /// <summary>Seconds the secret corner taps must fit in.</summary>
        float CornerSequenceSeconds { get; }
        /// <summary>How far a corner reaches into the screen, as a share of its shorter side.</summary>
        float CornerZone { get; }
        /// <summary>Unlocks this device when the PIN matches CD_DevTools.PinHash.</summary>
        bool TryUnlock(string pin);
        /// <summary>CD_DevTools.PanelEnabled: the panel opens without the PIN.</summary>
        bool EnabledInBuild { get; }
        /// <summary>Whether this opening of the panel may go ahead: always in a build with the panel enabled, else only right after the PIN was accepted.</summary>
        bool TakeOpening();
        /// <summary>
        /// Tells the developer on Telegram, through the DevAlert Cloud Code module, that a PIN was typed and
        /// whether it was right. Sent in the background and never waited on; not sent from the Editor.
        /// </summary>
        void ReportPinEntry(bool accepted);
        bool IsOn(string key);
        /// <summary>Sets and keeps the switch on this device; false when CD_DevTools does not list the key.</summary>
        bool Set(string key, bool on);
        /// <summary>The last errors and exceptions this run logged, newest last; readable on a store build too.</summary>
        IReadOnlyList<string> Errors();
        void ClearErrors();
    }
}
