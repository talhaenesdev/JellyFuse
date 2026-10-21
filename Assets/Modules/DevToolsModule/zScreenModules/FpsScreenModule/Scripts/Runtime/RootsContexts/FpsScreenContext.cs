using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.DevToolsModule.FpsScreenModule.Controllers;
using Modules.DevToolsModule.FpsScreenModule.Signals;
using Modules.DevToolsModule.FpsScreenModule.ViewsMediators;

namespace Modules.DevToolsModule.FpsScreenModule.RootsContexts
{
    public class FpsScreenContext : ScreenSubContext<FpsScreenView, FpsScreenMediator>
    {
        // Layer 8: over every game screen and popup, under the loading screen (9).
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 8,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("FpsScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private FpsScreenSignals _signals;
        private FpsScreenInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<FpsScreenSignals>();
            _internalSignals = InjectionBinder.Bind<FpsScreenInternalSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_internalSignals.Refresh).ToSequence<RefreshFpsOverlayCommand>();
            CommandBinder.Bind(_signals.Incoming.SetVisible).ToSequence<SetFpsOverlayVisibleCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            _internalSignals.Refresh.Dispatch();
        }
    }
}
