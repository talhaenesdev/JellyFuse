using FlowIoC.BaseModule.Signals;

namespace Modules.LocalizationModule.Signals
{
    /// <summary>
    /// What the module says to its own commands. None of these leave the module, so they sit apart
    /// from the public holder rather than widening what the module offers.
    /// </summary>
    internal class LocalizationInternalSignals : ISignalHolder
    {
        public Signal Initialize = new();
        public Signal<string> SetLanguage = new();
    }
}
