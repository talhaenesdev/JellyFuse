using FlowIoC.BaseModule.Signals;

namespace Modules.GameplayModule.PauseScreenModule.Signals
{
    public class PauseScreenSignals : ISignalHolder
    {
        public PauseScreenSignalsIncoming Incoming = new();
        public PauseScreenSignalsOutgoing Outgoing = new();
    }

    public class PauseScreenSignalsIncoming
    {
        public Signal OpenPauseScreen = new();
    }

    public class PauseScreenSignalsOutgoing
    {
        /// <summary>The panel is opening: the game behind it holds still.</summary>
        public Signal Paused = new();
        /// <summary>Resume or the close button: the panel has closed itself and play goes on.</summary>
        public Signal Resumed = new();
        /// <summary>Quit: the panel has closed itself and the player leaves the level.</summary>
        public Signal QuitClicked = new();
    }
}
