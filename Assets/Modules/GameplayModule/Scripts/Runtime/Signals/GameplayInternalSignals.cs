using FlowIoC.BaseModule.Signals;

namespace Modules.GameplayModule.Signals
{
    internal class GameplayInternalSignals : ISignalHolder
    {
        public Signal TimeExpired = new();
        public Signal Tick = new(hideCommandLog: true);
        /// <summary>A level was won: the coins it pays.</summary>
        public Signal<int> PayWin = new();
    }
}

