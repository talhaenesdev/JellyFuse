using System;
using FlowIoC.ConsoleModule;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using Unity.Notifications;
#endif
namespace Modules.NotificationModule.Services
{
    /// <summary>
    /// Unity Mobile Notifications on a device. In the Editor nothing is shown; each call is logged instead.
    /// </summary>
    internal sealed class NotificationDevice : INotificationDevice
    {
        private const string CHANNEL = "jelly_fuse_default";
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private bool _ready;
        private void Ready()
        {
            if (_ready) return;
            _ready = true;
            var args = NotificationCenterArgs.Default;
            args.AndroidChannelId = CHANNEL;
            args.AndroidChannelName = "Game";
            args.AndroidChannelDescription = "Lives and reminders";
            NotificationCenter.Initialize(args);
        }
        public void RequestPermission() { Ready(); NotificationCenter.RequestPermission(); }
        public void Schedule(int id, string title, string text, DateTime at)
        {
            Ready();
            NotificationCenter.CancelScheduledNotification(id);
            NotificationCenter.ScheduleNotification(new Notification { Identifier = id, Title = title, Text = text },
                new NotificationDateTimeSchedule(at));
        }
        public void Cancel(int id) { Ready(); NotificationCenter.CancelScheduledNotification(id); }
        public void CancelAll() { Ready(); NotificationCenter.CancelAllScheduledNotifications(); }
#else
        public void RequestPermission() => FlowLogger.Log("Notification permission asked (not on a device)");
        public void Schedule(int id, string title, string text, DateTime at) => FlowLogger.Log($"Notification {id} at {at:g}: {title} - {text}");
        public void Cancel(int id) => FlowLogger.Log($"Notification {id} cancelled");
        public void CancelAll() => FlowLogger.Log("Every notification cancelled");
#endif
    }
}
