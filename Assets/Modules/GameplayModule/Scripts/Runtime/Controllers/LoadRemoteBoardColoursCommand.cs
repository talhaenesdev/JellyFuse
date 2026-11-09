using System;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Fetches the board colours published to the content server (the addressable "BoardColours", in the
    /// Remote_Levels group) and paints the board with them in place of the built-in ones. Offline, slow or with
    /// nothing published, the built-in colours stay: the boot never waits more than <see cref="TIMEOUT_SECONDS"/>.
    /// </summary>
    internal sealed class LoadRemoteBoardColoursCommand : Command
    {
        private const string ADDRESS = "BoardColours";
        private const float TIMEOUT_SECONDS = 4f;

        [Inject] private IAssetService _assets { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        /// <summary>Loaded, skipped, timed out, threw - every way out resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                var filed = _shared.GetScriptable<CD_BoardColours>();
                var load = _assets.LoadAssetAsync<CD_BoardColours>(ADDRESS);
                bool inTime = await Task.WhenAny(load, Task.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS))) == load;
                var remote = inTime && !load.IsFaulted ? load.Result : null;
                if (filed == null || remote == null || ReferenceEquals(filed, remote))
                {
                    FlowLogger.Log(inTime ? "LoadRemoteBoardColoursCommand - the built-in board colours stay." : "LoadRemoteBoardColoursCommand - the content server did not answer in time, the built-in board colours stay.");
                    Release();
                    return;
                }
                filed.CopyFrom(remote);
                FlowLogger.Log("LoadRemoteBoardColoursCommand - board colours from the content server.");
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"LoadRemoteBoardColoursCommand - the board colours could not be fetched, the built-in ones stay: {exception.Message}");
                Release();
            }
        }
    }
}
