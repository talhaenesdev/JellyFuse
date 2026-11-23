using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.PetHomeModule.PetHomeScreenModule.Enums;

namespace Modules.PetHomeModule.PetHomeScreenModule.Signals
{
    /// <summary>What the screen module says to its own commands and mediator.</summary>
    internal class PetHomeScreenInternalSignals : ISignalHolder
    {
        /// <summary>The player's animal arrived from the content server - or null, and the room stays empty of it.</summary>
        public Signal<CD_BoardAnimal> AnimalLoaded = new();
        /// <summary>A moment to hear and feel: a bounce, a meal, a ball brought back.</summary>
        public Signal<PetHomeFeel> Feel = new();
    }
}
