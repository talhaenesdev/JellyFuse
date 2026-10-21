using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.GameplayScreenModule.Controllers;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.GameplayModule.GameplayScreenModule.ViewsMediators;

namespace Modules.GameplayModule.GameplayScreenModule.RootsContexts
{
    public class GameplayScreenContext : ScreenSubContext<GameplayScreenView, GameplayScreenMediator>
    {
        private GameplayScreenSignals _signals;

        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 1,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("GameplayScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private GameplayScreenInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<GameplayScreenSignals>();
            _internalSignals = InjectionBinder.Bind<GameplayScreenInternalSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.OpenGameplayScreen).ToSequence<OpenGameplayScreenCommand>();
            CommandBinder.Bind(_internalSignals.JellyMelting).ToSequence<PlayMeltHapticCommand>();
        }
    }
}