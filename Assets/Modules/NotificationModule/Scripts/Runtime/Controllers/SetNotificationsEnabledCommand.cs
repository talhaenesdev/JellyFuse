using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.NotificationModule.Data.ValueObjects;
using Modules.NotificationModule.Models;
using Modules.NotificationModule.Services;
namespace Modules.NotificationModule.Controllers
{
    /// <summary>The player turned notifications on or off.</summary>
    internal sealed class SetNotificationsEnabledCommand : Command
    {
        [Inject] private NotificationModel _model { get; set; }
        [Inject] private INotificationDevice _device { get; set; }
        [SignalParam] private bool _enabled { get; set; }
        public override void Execute()
        {
            if (_model.Enabled == _enabled) return;
            _model.SetEnabled(_enabled);
            // Turned off: nothing already scheduled is shown. Turned on: the game schedules again the next time it does.
            if (!_enabled) _device.CancelAll();
            else _device.RequestPermission();
        }
    }
}
