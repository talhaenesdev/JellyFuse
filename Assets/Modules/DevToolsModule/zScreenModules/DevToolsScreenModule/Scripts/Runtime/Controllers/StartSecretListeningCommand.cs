using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using Modules.DevToolsModule.DevToolsScreenModule.Models;
using Modules.DevToolsModule.DevToolsScreenModule.Signals;

namespace Modules.DevToolsModule.DevToolsScreenModule.Controllers
{
    /// <summary>
    /// Puts the secret corner gesture on the frame: while it listens, every frame dispatches SecretTick.
    /// Hooks once, however many times it is asked.
    /// </summary>
    internal sealed class StartSecretListeningCommand : Command
    {
        [Inject] private SecretGestureModel _model { get; set; }
        [Inject] private IUpdateProvider _updateProvider { get; set; }
        [InjectSignal] private DevToolsScreenInternalSignals _signals { get; set; }

        public override void Execute()
        {
            _model.IsListening = true;
            if (_model.IsHooked) return;
            _model.IsHooked = true;
            var model = _model;
            var provider = _updateProvider;
            var tick = _signals.SecretTick;
            Action onFrame = null;
            onFrame = () =>
            {
                if (!model.IsListening)
                {
                    provider.RemoveUpdate(onFrame);
                    model.IsHooked = false;
                    return;
                }
                tick.Dispatch();
            };
            provider.AddUpdate(onFrame);
        }
    }
}
