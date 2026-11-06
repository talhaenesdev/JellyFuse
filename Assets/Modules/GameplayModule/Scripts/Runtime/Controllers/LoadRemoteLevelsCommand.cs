using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.LoadingModule.Services;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Fetches the level list published to the content server since this build (the addressable "JellyLevels",
    /// in the Remote_Levels group) and plays its levels in place of the ones built in. Offline, slow or with
    /// nothing published, the step is skipped and the built-in list plays: the boot never waits on the server
    /// for more than <see cref="TIMEOUT_SECONDS"/>.
    /// </summary>
    internal sealed class LoadRemoteLevelsCommand : Command
    {
        private const string STEP = "Levels";
        private const string ADDRESS = "JellyLevels";
        private const float TIMEOUT_SECONDS = 6f;

        [Inject] private IAssetService _assets { get; set; }
        [Inject] private ILoadingService _loading { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        /// <summary>Loaded, skipped, timed out, threw - every way out resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            var step = _loading.Report(STEP);
            step.Start();
            try
            {
                var filed = _shared.GetScriptable<CD_JellyLevels>();
                var load = _assets.LoadAssetAsync<CD_JellyLevels>(ADDRESS);
                bool inTime = await Task.WhenAny(load, Task.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS))) == load;
                var remote = inTime && !load.IsFaulted ? load.Result : null;
                if (filed == null || remote == null || remote.Levels.Count == 0)
                {
                    FlowLogger.Log(inTime ? "LoadRemoteLevelsCommand - no published level list, the built-in levels play." : "LoadRemoteLevelsCommand - the content server did not answer in time, the built-in levels play.");
                    step.Skip();
                    Release();
                    return;
                }
                // Only the list is taken: the order, the levels and the coin default. StartLevel stays the build's.
                filed.Levels = new List<CD_JellyLevel>(remote.Levels);
                filed.DefaultCoinReward = remote.DefaultCoinReward;
                FlowLogger.Log($"LoadRemoteLevelsCommand - {remote.Levels.Count} levels from the content server.");
                step.Complete();
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"LoadRemoteLevelsCommand - the level list could not be fetched, the built-in levels play: {exception.Message}");
                step.Skip();
                Release();
            }
        }
    }
}
