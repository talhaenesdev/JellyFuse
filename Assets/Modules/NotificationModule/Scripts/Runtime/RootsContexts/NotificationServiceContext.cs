using FlowIoC.BaseModule.Contexts;
using Modules.NotificationModule.Controllers;
using Modules.NotificationModule.Models;
using Modules.NotificationModule.Services;
using Modules.NotificationModule.Signals;

namespace Modules.NotificationModule.RootsContexts
{
    public class NotificationServiceContext : Context
    {
        private NotificationInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinder.Bind<NotificationInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<NotificationModel>();
            InjectionBinder.Bind<INotificationDevice, NotificationDevice>();
            InjectionBinderCrossContext.Bind<INotificationService, NotificationService>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.RequestPermission).ToSequence<RequestNotificationPermissionCommand>();
            CommandBinder.Bind(_signals.SetEnabled).ToSequence<SetNotificationsEnabledCommand>();
            CommandBinder.Bind(_signals.Schedule).ToSequence<ScheduleNotificationCommand>();
            CommandBinder.Bind(_signals.Cancel).ToSequence<CancelNotificationCommand>();
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
            // Asked as the game opens; a platform that already has the answer asks nothing.
            _signals.RequestPermission.Dispatch();
        }
    }
}
