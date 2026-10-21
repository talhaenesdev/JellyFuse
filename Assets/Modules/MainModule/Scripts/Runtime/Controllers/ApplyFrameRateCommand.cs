using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DevToolsModule.Constants;
using Modules.DevToolsModule.Services;
using Modules.MainModule.Models;
using UnityEngine;

namespace Modules.MainModule.Controllers
{
    /// <summary>
    /// Sets the frame rate: CD_FrameRate's, or as fast as the device goes while the developer switch
    /// "Uncapped frame rate" is on. Runs at boot and again whenever that switch changes.
    /// </summary>
    internal sealed class ApplyFrameRateCommand : Command
    {
        [Inject] private FrameRateModel _model { get; set; }
        [Inject] private IDevToolsService _devTools { get; set; }

        public override void Execute()
        {
            bool uncapped = _devTools != null && _devTools.IsOn(DevToolKeys.UNCAPPED_FPS);
            // vSync off: on mobile it is ignored anyway, and in the Editor it would override the target.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = uncapped ? -1 : _model.TargetFrameRate;
        }
    }
}
