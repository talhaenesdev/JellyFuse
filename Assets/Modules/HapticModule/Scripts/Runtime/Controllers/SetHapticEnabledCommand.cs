using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Models;
using Modules.HapticModule.Services;
using UnityEngine;
namespace Modules.HapticModule.Controllers
{
    internal sealed class SetHapticEnabledCommand : Command
    {
        [Inject] private HapticModel _model { get; set; }
        [Inject] private IHapticDevice _device { get; set; }
        [SignalParam] private bool _enabled { get; set; }
        public override void Execute()
        {
            if (_model.Enabled == _enabled) return;
            _model.SetEnabled(_enabled);
            _device.Cancel();
            PlayerPrefs.SetInt(HapticModel.PREFERENCE_KEY, _enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
