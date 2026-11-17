using System;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.GameplayScreenModule.Controllers
{
    /// <summary>
    /// Downloads the board animal of the icon the player has - at launch, and again whenever a new one is saved - so
    /// the board has it on the device before play. Only the bundle is fetched; nothing is held in memory. An icon with
    /// no animal, an animal already on the device, or no connection leaves things as they are.
    /// </summary>
    // A first download over a slow connection can take a while; nothing waits on it.
    [LongRetain]
    internal sealed class PrefetchBoardAnimalCommand : Command
    {
        [Inject] private IAssetService _assets { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [SignalParam] private string _iconId { get; set; }

        /// <summary>Fetched, not listed, failed, threw - every way out resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            string address = null;
            try
            {
                var animals = _shared.GetScriptable<CD_BoardAnimals>();
                address = animals != null ? animals.AddressOf(_iconId) : null;
                if (address == null)
                {
                    Release();
                    return;
                }
                bool fetched = await _assets.DownloadDependenciesAsync(address);
                if (!fetched) FlowLogger.Log($"PrefetchBoardAnimalCommand - {address} was not downloaded, the board will try again when it opens.");
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"PrefetchBoardAnimalCommand - {address} could not be downloaded: {exception.Message}");
                Release();
            }
        }
    }
}
