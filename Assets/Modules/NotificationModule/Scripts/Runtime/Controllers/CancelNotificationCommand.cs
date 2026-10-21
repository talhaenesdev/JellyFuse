using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.NotificationModule.Data.ValueObjects;
using Modules.NotificationModule.Models;
using Modules.NotificationModule.Services;
namespace Modules.NotificationModule.Controllers
{
    /// <summary>Takes back what a key had scheduled.</summary>
    internal sealed class CancelNotificationCommand : Command
    {
        [Inject] private NotificationModel _model { get; set; }
        [Inject] private INotificationDevice _device { get; set; }
        [SignalParam] private string _key { get; set; }
        public override void Execute() { if (!string.IsNullOrEmpty(_key)) _device.Cancel(_model.IdOf(_key)); }
    }
}
