using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.GameplayScreenModule.Signals
{
    /// <summary>What the screen says to its own commands.</summary>
    internal class GameplayScreenInternalSignals : ISignalHolder
    {
        /// <summary>A jelly started melting into its door: the seconds the melt takes.</summary>
        public Signal<float> JellyMelting = new(hideCommandLog: true);
        /// <summary>The animal the player's profile brings to the board; null keeps the board's own cat.</summary>
        public Signal<CD_BoardAnimal> BoardAnimalLoaded = new();
    }
}
