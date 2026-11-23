using System;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.PetHomeModule.PetHomeScreenModule.Signals;
using Modules.ProfileModule.Shared.Data.UnityObjects;

namespace Modules.PetHomeModule.PetHomeScreenModule.Controllers
{
    /// <summary>
    /// The room's animal is the one the player's profile icon shows, fetched the same way the board fetches it - the
    /// asset service keeps it once it came. Not listed, no connection or too slow, and the room waits without it.
    /// </summary>
    [LongRetain(20)]
    internal sealed class LoadPetHomeAnimalCommand : Command
    {
        [Inject] private IAssetService _assets { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private PetHomeScreenInternalSignals _internalSignals { get; set; }

        public override async void Execute()
        {
            Retain();
            var signals = _internalSignals;
            try
            {
                var animals = _shared.GetScriptable<CD_BoardAnimals>();
                var profile = _shared.GetScriptable<SD_PlayerProfile>();
                string address = animals != null && profile != null ? animals.AddressOf(profile.SelectedIconId) : null;
                if (address == null)
                {
                    signals.AnimalLoaded.Dispatch(null);
                    Release();
                    return;
                }
                var load = _assets.LoadAssetAsync<CD_BoardAnimal>(address);
                bool inTime = await Task.WhenAny(load, Task.Delay(TimeSpan.FromSeconds(animals.LoadTimeoutSeconds))) == load;
                var animal = inTime && !load.IsFaulted ? load.Result : null;
                if (animal == null) FlowLogger.Log($"LoadPetHomeAnimalCommand - {address} did not come.");
                signals.AnimalLoaded.Dispatch(animal);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"LoadPetHomeAnimalCommand - the animal could not be fetched: {exception.Message}");
                signals.AnimalLoaded.Dispatch(null);
                Release();
            }
        }
    }
}
