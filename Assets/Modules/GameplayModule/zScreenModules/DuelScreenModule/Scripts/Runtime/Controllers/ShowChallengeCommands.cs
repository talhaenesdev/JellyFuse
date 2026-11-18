using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.LocalizationModule.Services;

namespace Modules.GameplayModule.DuelScreenModule.Controllers
{
    /// <summary>A challenge waits for this player: the duel screen, open or opened now, shows who and where.</summary>
    internal class ShowChallengeInviteCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [SignalParam] private ChallengeInviteRVO _invite { get; set; }

        public override async void Execute()
        {
            Retain();
            var invite = _invite;
            try
            {
                var screen = await DuelScreenOpenFunction.Get(_screenService, _shared, _localization);
                if (screen == null) { FlowLogger.LogError("ShowChallengeInviteCommand - the duel screen did not open."); Stop(); return; }
                screen.ShowChallenge(invite);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"ShowChallengeInviteCommand threw while showing a challenge: {exception}");
                Stop();
            }
        }
    }

    /// <summary>A challenge this player sent was answered: the duel screen shows how it went along the top.</summary>
    internal class ShowChallengeOutcomeCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILocalizationService _localization { get; set; }
        [SignalParam] private ChallengeOutcomeRVO _outcome { get; set; }

        public override async void Execute()
        {
            Retain();
            var outcome = _outcome;
            try
            {
                var screen = await DuelScreenOpenFunction.Get(_screenService, _shared, _localization);
                if (screen == null) { FlowLogger.LogError("ShowChallengeOutcomeCommand - the duel screen did not open."); Stop(); return; }
                screen.ShowOutcome(outcome);
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"ShowChallengeOutcomeCommand threw while showing an outcome: {exception}");
                Stop();
            }
        }
    }
}
