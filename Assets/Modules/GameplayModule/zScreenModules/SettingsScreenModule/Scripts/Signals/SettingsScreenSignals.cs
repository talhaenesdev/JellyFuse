using FlowIoC.BaseModule.Signals;

namespace Modules.GameplayModule.SettingsScreenModule.Signals
{
    public class SettingsScreenSignals : ISignalHolder
    {
        public SettingsScreenSignalsIncoming Incoming = new();
        public SettingsScreenSignalsOutgoing Outgoing = new();
    }

    public class SettingsScreenSignalsIncoming
    {
        public Signal OpenSettingsScreen = new();
        /// <summary>The game's language changed: the code of the new one. The screen marks it and redraws its texts.</summary>
        public Signal<string> ApplyLanguage = new();
    }

    public class SettingsScreenSignalsOutgoing
    {
        /// <summary>The player picked a language: its code.</summary>
        public Signal<string> LanguagePicked = new();
        /// <summary>The player closed the screen.</summary>
        public Signal Closed = new();
    }
}
