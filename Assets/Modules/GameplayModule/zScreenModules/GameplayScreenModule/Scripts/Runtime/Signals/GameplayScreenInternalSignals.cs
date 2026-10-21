using FlowIoC.BaseModule.Signals;

namespace Modules.GameplayModule.GameplayScreenModule.Signals
{
    /// <summary>What the screen says to its own commands.</summary>
    internal class GameplayScreenInternalSignals : ISignalHolder
    {
        /// <summary>A jelly started melting into its door: the seconds the melt takes.</summary>
        public Signal<float> JellyMelting = new(hideCommandLog: true);
    }
}
