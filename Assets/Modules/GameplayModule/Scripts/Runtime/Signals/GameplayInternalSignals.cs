using FlowIoC.BaseModule.Signals;

namespace Modules.GameplayModule.Signals
{
    internal class GameplayInternalSignals : ISignalHolder
    {
        public Signal TimeExpired = new();
        public Signal Tick = new(hideCommandLog: true);
        /// <summary>Start listening for the live duel's messages.</summary>
        public Signal ListenLive = new();
        /// <summary>A live duel message: its type and JSON body.</summary>
        public Signal<string, string> LiveMessage = new();
        /// <summary>A Freeze Time ran out and the clock runs again.</summary>
        public Signal ClockThawed = new();
        /// <summary>A level was won: the coins it pays.</summary>
        public Signal<int> PayWin = new();
    }
}

