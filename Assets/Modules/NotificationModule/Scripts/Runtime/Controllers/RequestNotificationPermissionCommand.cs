using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.NotificationModule.Data.ValueObjects;
using Modules.NotificationModule.Models;
using Modules.NotificationModule.Services;
namespace Modules.NotificationModule.Controllers
{
    /// <summary>Asks the player whether the game may show notifications.</summary>
    internal sealed class RequestNotificationPermissionCommand : Command
    {
        [Inject] private INotificationDevice _device { get; set; }
        public override void Execute() => _device.RequestPermission();
    }
}
