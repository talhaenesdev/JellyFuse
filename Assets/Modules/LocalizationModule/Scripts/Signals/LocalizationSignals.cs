using FlowIoC.BaseModule.Signals;

namespace Modules.LocalizationModule.Signals
{
    /// <summary>
    /// The module's public surface besides ILocalizationService. A settings screen asks for another
    /// language through Incoming; every screen that shows text hears LanguageChanged and redraws.
    /// </summary>
    public class LocalizationSignals : ISignalHolder
    {
        public LocalizationSignalsIncoming Incoming = new();
        public LocalizationSignalsOutgoing Outgoing = new();
    }

    public class LocalizationSignalsIncoming
    {
        /// <summary>Switch the game to the language with this code, and keep the choice.</summary>
        public Signal<string> SetLanguage = new();
    }

    public class LocalizationSignalsOutgoing
    {
        /// <summary>The game's language is ready or has changed: the code of the one now in use.</summary>
        public Signal<string> LanguageChanged = new();
    }
}
