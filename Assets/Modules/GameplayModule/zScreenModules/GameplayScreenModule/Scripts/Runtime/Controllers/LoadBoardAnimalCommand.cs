using System;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.ProfileModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.GameplayScreenModule.Controllers
{
    /// <summary>
    /// The board's animal is the one the player's profile icon shows. Its frames are fetched from the content server
    /// - only that one animal, once; the asset service keeps it after - and announced. An icon with no animal, no
    /// connection or a slow server announces none, and the board keeps the cat it ships with.
    /// </summary>
    // A first download over a slow connection can take a while; the board plays meanwhile.
    [LongRetain(20)]
    internal sealed class LoadBoardAnimalCommand : Command
    {
        [Inject] private IAssetService _assets { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplayScreenInternalSignals _internalSignals { get; set; }

        /// <summary>Loaded, not listed, timed out, threw - every way out resolves the retain.</summary>
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
                    signals.BoardAnimalLoaded.Dispatch(null);
                    Release();
                    return;
                }
                var load = _assets.LoadAssetAsync<CD_BoardAnimal>(address);
                bool inTime = await Task.WhenAny(load, Task.Delay(TimeSpan.FromSeconds(animals.LoadTimeoutSeconds))) == load;
                var animal = inTime && !load.IsFaulted ? load.Result : null;
                if (animal == null) FlowLogger.Log($"LoadBoardAnimalCommand - {address} did not come, the board keeps its own cat.");
                signals.BoardAnimalLoaded.Dispatch(animal);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"LoadBoardAnimalCommand - the animal could not be fetched, the board keeps its own cat: {exception.Message}");
                signals.BoardAnimalLoaded.Dispatch(null);
                Release();
            }
        }
    }
}
