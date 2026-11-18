using Modules.GameplayModule.DuelScreenModule.Signals;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.DuelScreenModule.Controllers;
using Modules.GameplayModule.DuelScreenModule.ViewsMediators;

namespace Modules.GameplayModule.DuelScreenModule.RootsContexts
{
    public class DuelScreenContext : ScreenSubContext<DuelScreenView, DuelScreenMediator>
    {
        // Layer 3: the race bar moves every frame, and that layer has a canvas of its own.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 3,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("DuelScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private DuelScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<DuelScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.OpenSearch).ToSequence<OpenDuelSearchCommand>();
            CommandBinder.Bind(_signals.Incoming.StartRace).ToSequence<StartDuelRaceScreenCommand>();
            CommandBinder.Bind(_signals.Incoming.ShowChallenge).ToSequence<ShowChallengeInviteCommand>();
            CommandBinder.Bind(_signals.Incoming.ShowOutcome).ToSequence<ShowChallengeOutcomeCommand>();
        }
    }
}
