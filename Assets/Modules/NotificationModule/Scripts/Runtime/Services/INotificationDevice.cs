using System;
namespace Modules.NotificationModule.Services
{
    /// <summary>The platform's notification centre: Android or iOS, nothing in the Editor.</summary>
    internal interface INotificationDevice
    {
        void RequestPermission();
        void Schedule(int id, string title, string text, DateTime at);
        void Cancel(int id);
        void CancelAll();
    }
}
