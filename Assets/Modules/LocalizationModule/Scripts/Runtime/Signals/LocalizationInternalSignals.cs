using FlowIoC.BaseModule.Signals;
using Modules.LocalizationModule.ViewsMediators;

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
        /// <summary>A localized text registered, or the language changed under it: fill it with its key's words. One per text, so kept out of the log.</summary>
        public Signal<LocalizedTextView> FillText = new(hideCommandLog: true);
    }
}
