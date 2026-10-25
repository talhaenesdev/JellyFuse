using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DevToolsModule.DevToolsScreenModule.Models;
using Modules.DevToolsModule.DevToolsScreenModule.Signals;
using Modules.DevToolsModule.Services;
using UnityEngine;

namespace Modules.DevToolsModule.DevToolsScreenModule.Controllers
{
    /// <summary>
    /// A press that begins this frame in a screen corner moves the secret gesture on; the fourth corner
    /// in order, inside the time the config allows, asks for the PIN. Read from the pointer, so taps on
    /// buttons count too and no screen has to know about it.
    /// </summary>
    internal sealed class DetectSecretCornerCommand : Command
    {
        [Inject] private SecretGestureModel _model { get; set; }
        [Inject] private IDevToolsService _devTools { get; set; }
        [InjectSignal] private DevToolsScreenInternalSignals _signals { get; set; }

        public override void Execute()
        {
            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            int corner = Corner(pointer.position.ReadValue(), _devTools.CornerZone);
            if (_model.Tap(corner, Time.unscaledTime, _devTools.CornerSequenceSeconds)) _signals.SecretEntered.Dispatch();
        }

        // 0 top-left, 1 top-right, 2 bottom-right, 3 bottom-left; -1 anywhere else.
        private static int Corner(Vector2 position, float zone)
        {
            float reach = Mathf.Min(Screen.width, Screen.height) * zone;
            bool left = position.x <= reach, right = position.x >= Screen.width - reach;
            bool bottom = position.y <= reach, top = position.y >= Screen.height - reach;
            if (top && left) return 0;
            if (top && right) return 1;
            if (bottom && right) return 2;
            if (bottom && left) return 3;
            return -1;
        }
    }
}
