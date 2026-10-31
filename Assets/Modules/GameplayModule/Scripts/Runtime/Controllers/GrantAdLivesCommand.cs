using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Gives CD_Lives' lives per watched ad, up to the most the player can hold.</summary>
    internal class GrantAdLivesCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILivesModel _lives { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Lives>();
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            if (config == null || progress == null || config.LivesPerAd <= 0)
            {
                Retain();
                Stop();
                return;
            }
            _lives.Add(progress, config, config.LivesPerAd, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
    }
}
