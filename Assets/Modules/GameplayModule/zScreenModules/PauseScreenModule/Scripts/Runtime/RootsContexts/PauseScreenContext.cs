using FlowIoC.BaseModule.Controller.Commands;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.PauseScreenModule.Controllers;
using Modules.GameplayModule.PauseScreenModule.Models;
using Modules.GameplayModule.PauseScreenModule.Signals;
using Modules.GameplayModule.PauseScreenModule.ViewsMediators;

namespace Modules.GameplayModule.PauseScreenModule.RootsContexts
{
    public class PauseScreenContext : ScreenSubContext<PauseScreenView, PauseScreenMediator>
    {
        // Layer 2: over the gameplay screen (layer 1), which stays open underneath.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("PauseScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private PauseScreenSignals _signals;
        private PauseScreenInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<PauseScreenSignals>();
            _internalSignals = InjectionBinder.Bind<PauseScreenInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<IPauseSettingsModel, PauseSettingsModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.OpenPauseScreen)
                .ToSequence<SignalDispatchCommand>(_signals.Outgoing.Paused)
                .ToSequence<OpenPauseScreenCommand>();
            CommandBinder.Bind(_internalSignals.ToggleOption).ToSequence<TogglePauseOptionCommand>();
        }
    }
}
