using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.DevToolsModule.DevToolsScreenModule.Controllers;
using Modules.DevToolsModule.DevToolsScreenModule.Signals;
using Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators;

namespace Modules.DevToolsModule.DevToolsScreenModule.RootsContexts
{
    public class DevToolsScreenContext : ScreenSubContext<DevToolsScreenView, DevToolsScreenMediator>
    {
        // Layer 2: over the gameplay screen (layer 1), which stays open underneath.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("DevToolsScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private DevToolsScreenSignals _signals;
        private DevToolsScreenInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<DevToolsScreenSignals>();
            _internalSignals = InjectionBinder.Bind<DevToolsScreenInternalSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.OpenDevToolsScreen).ToSequence<OpenDevToolsScreenCommand>();
            CommandBinder.Bind(_internalSignals.SetToggle).ToSequence<SetDevToggleCommand>();
        }
    }
}
