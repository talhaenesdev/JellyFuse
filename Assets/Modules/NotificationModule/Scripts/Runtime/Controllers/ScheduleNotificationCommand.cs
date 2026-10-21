using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.NotificationModule.Data.ValueObjects;
using Modules.NotificationModule.Models;
using Modules.NotificationModule.Services;
namespace Modules.NotificationModule.Controllers
{
    /// <summary>Shows a notification at its time, in place of the one its key had.</summary>
    internal sealed class ScheduleNotificationCommand : Command
    {
        [Inject] private NotificationModel _model { get; set; }
        [Inject] private INotificationDevice _device { get; set; }
        [SignalParam] private NotificationRequestVO _request { get; set; }
        public override void Execute()
        {
            if (_request == null || string.IsNullOrEmpty(_request.Key)) return;
            int id = _model.IdOf(_request.Key);
            // Off, or a time already gone: nothing is shown, and what that key had scheduled goes too.
            if (!_model.Enabled || _request.At <= DateTime.Now) { _device.Cancel(id); return; }
            _device.Schedule(id, _request.Title, _request.Text, _request.At);
        }
    }
}
