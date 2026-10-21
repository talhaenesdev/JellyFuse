using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.PauseScreenModule.Signals
{
    /// <summary>
    /// What the module says to its own commands. None of these leave the module, so they sit apart
    /// from the public holder in Scripts/Signals rather than widening what the module offers.
    /// </summary>
    internal class PauseScreenInternalSignals : ISignalHolder
    {
        public Signal<SettingsOption> ToggleOption = new();
        public Signal<SettingsOption, bool> OptionChanged = new();
    }
}
