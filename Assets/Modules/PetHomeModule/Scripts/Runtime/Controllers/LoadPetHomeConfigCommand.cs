using System;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.PetHomeModule.Models;
using Modules.PetHomeModule.Shared.Data.UnityObjects;

namespace Modules.PetHomeModule.Controllers
{
    /// <summary>
    /// The first time the home opens, its config - and with it every picture of the shop and the rooms - is loaded
    /// from the pet home's own bundle, which ships inside the app, so it comes offline too. Kept after that: the
    /// screen's pooled instance goes on showing those pictures. Nothing came, and the home does not open.
    /// </summary>
    internal sealed class LoadPetHomeConfigCommand : Command
    {
        private const string ADDRESS = "CD_PetHome";

        [Inject] private IPetHomeModel _model { get; set; }
        [Inject] private IAssetService _assets { get; set; }

        /// <summary>Already loaded, loaded, nothing came, threw - every way out resolves the retain.</summary>
        public override async void Execute()
        {
            if (_model.HasConfig) return;
            Retain();
            var model = _model;
            try
            {
                var config = await _assets.LoadAssetAsync<CD_PetHome>(ADDRESS);
                if (config == null)
                {
                    FlowLogger.LogError("LoadPetHomeConfigCommand - CD_PetHome is not in the pet home's bundle.");
                    Stop();
                    return;
                }
                model.UseConfig(config);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"LoadPetHomeConfigCommand threw while loading CD_PetHome: {exception}");
                Stop();
            }
        }
    }
}
