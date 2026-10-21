using System;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.NotificationModule.Data.ValueObjects;
using Modules.NotificationModule.Models;
using Modules.NotificationModule.Signals;
namespace Modules.NotificationModule.Services
{
    public sealed class NotificationService : INotificationService
    {
        [Inject] private NotificationModel _model { get; set; }
        [InjectSignal] private NotificationInternalSignals _signals { get; set; }
        public bool Enabled => _model.Enabled;
        public void SetEnabled(bool enabled) => _signals.SetEnabled.Dispatch(enabled);
        public void RequestPermission() => _signals.RequestPermission.Dispatch();
        public void Schedule(string key, string title, string text, DateTime at) =>
            _signals.Schedule.Dispatch(new NotificationRequestVO { Key = key, Title = title, Text = text, At = at });
        public void Cancel(string key) => _signals.Cancel.Dispatch(key);
    }
}
