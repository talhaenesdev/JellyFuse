using Modules.AdsModule.Services;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Services;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Services;
using Modules.GameplayModule.Signals;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.Controllers;
using Modules.GameplayModule.Enums;
using Modules.GameplayModule.Models;
using Modules.LocalSaveModule.Services;

namespace Modules.GameplayModule.RootsContexts
{

    public class GameplaySystemContext : Context
    {
		private GameplaySignals _signals;
        private GameplayInternalSignals _internalSignals;
        private IJellyPuzzleModel _model;
        /// <summary>The progress asset as it is filed on LocalSaveServiceRoot.</summary>
        private const string PROGRESS = "SD_JellyProgress";

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<GameplaySignals>();
            _internalSignals = InjectionBinder.Bind<GameplayInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            _model = InjectionBinder.Bind<IJellyPuzzleModel, JellyPuzzleModel>();
            InjectionBinder.Bind<PuzzleFeedbackModel>();
            InjectionBinder.Bind<ILivesModel, LivesModel>();
            InjectionBinder.Bind<IJellyTutorialModel, JellyTutorialModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Incoming.Start)
                .ToSequence<RefreshLivesCommand>()
                .ToSequence<RequireLifeCommand>(WaitingLevelStart.Replay)
                .ToSequence<StartPuzzleCommand>()
                .ToSequence<RecordLevelStartCommand>()
                .ToSequence<StartTutorialCommand>()
                .ToSequence<IAudioService.Commands.PlayMusic>(MusicTrack.Gameplay)
                .ToSequence<StartPuzzleClockCommand>()
                .ToSequence<StorePuzzleProgressCommand>()
                .ToSequence<GrantBoostersCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceBoostersCommand>()
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>();
            CommandBinder.Bind(_signals.Incoming.Drag)
                .ToSequence<CapturePuzzleFeedbackCommand>()
                .ToSequence<DragJellyCommand>()
                .ToSequence<StartClockOnMoveCommand>()
                .ToSequence<StartPuzzleClockCommand>()
                .ToSequence<AdvanceTutorialCommand>()
                .ToSequence<CheckWinPaymentCommand>()
                .ToSequence<PlayPuzzleFeedbackCommand>()
                .ToSequence<PlayPuzzleSoundCommand>()
                .ToSequence<RecordPuzzleResultCommand>()
                .ToSequence<RequestReviewCommand>();
            CommandBinder.Bind(_signals.Incoming.Undo)
                .ToSequence<UndoPuzzleCommand>()
                .ToSequence<IAudioService.Commands.Play>(AudioCue.Undo)
                .ToSequence<StartPuzzleClockCommand>();
            CommandBinder.Bind(_signals.Incoming.Stop).ToSequence<StopPuzzleClockCommand>();
            // The gameplay screen closed: back to the menu. A level still being played costs a life.
            CommandBinder.Bind(_signals.Incoming.Quit)
                .ToSequence<ForgetWaitingLevelCommand>()
                .ToSequence<LoseLifeOnQuitCommand>()
                .ToSequence<StopPuzzleClockCommand>()
                .ToSequence<LeaveLevelCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>();
            CommandBinder.Bind(_signals.Incoming.Resume)
                .ToSequence<ResumePuzzleClockCommand>()
                .ToSequence<StartPuzzleClockCommand>();
            CommandBinder.Bind(_signals.Incoming.NextLevel)
                .ToSequence<RefreshLivesCommand>()
                .ToSequence<RequireLifeCommand>(WaitingLevelStart.Next)
                .ToSequence<NextPuzzleLevelCommand>()
                .ToSequence<StartPuzzleCommand>()
                .ToSequence<RecordLevelStartCommand>()
                .ToSequence<StartTutorialCommand>()
                .ToSequence<IAudioService.Commands.PlayMusic>(MusicTrack.Gameplay)
                .ToSequence<StartPuzzleClockCommand>()
                .ToSequence<StorePuzzleProgressCommand>()
                .ToSequence<GrantBoostersCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceBoostersCommand>()
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>();
            CommandBinder.Bind(_signals.Incoming.SkipLevel)
                .ToSequence<SkipPuzzleLevelCommand>()
                .ToSequence<StorePuzzleProgressCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceCurrentLevelCommand>()
                .ToSequence<RequireLevelInPlayCommand>()
                .ToSequence<StartPuzzleCommand>()
                .ToSequence<RecordLevelStartCommand>()
                .ToSequence<StartTutorialCommand>()
                .ToSequence<IAudioService.Commands.PlayMusic>(MusicTrack.Gameplay)
                .ToSequence<StartPuzzleClockCommand>()
                .ToSequence<GrantBoostersCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceBoostersCommand>()
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>();
#if UNITY_EDITOR
            CommandBinder.Bind(_signals.Incoming.ForceResult)
                .ToSequence<CapturePuzzleFeedbackCommand>()
                .ToSequence<ForcePuzzleResultCommand>()
                .ToSequence<CheckWinPaymentCommand>()
                .ToSequence<PlayPuzzleFeedbackCommand>()
                .ToSequence<PlayPuzzleSoundCommand>()
                .ToSequence<RecordPuzzleResultCommand>()
                .ToSequence<RequestReviewCommand>()
                .ToSequence<LoseLifeCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>()
                .ToSequence<AnnounceAdOffersCommand>();
#endif
            CommandBinder.Bind(_signals.Incoming.GrantShopProduct)
                .ToSequence<GrantShopProductCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceBoostersCommand>()
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<AnnounceShopSoldOutCommand>();
            CommandBinder.Bind(_signals.Incoming.RequestShopSoldOut).ToSequence<AnnounceShopSoldOutCommand>();
            CommandBinder.Bind(_signals.Incoming.ResetBoosters)
                .ToSequence<ResetBoostersCommand>()
                .ToSequence<GrantBoostersCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceBoostersCommand>();
            CommandBinder.Bind(_signals.Incoming.UseBooster)
                .ToSequence<CheckBoosterCommand>()
                .ToSequence<ApplyBoosterCommand>()
                .ToSequence<PlayBoosterSoundCommand>()
                .ToSequence<PayBoosterCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceBoostersCommand>()
                .ToSequence<CheckWinPaymentCommand>();
            CommandBinder.Bind(_internalSignals.TimeExpired)
                .ToSequence<IHapticService.Commands.Play>(HapticPreset.Failure)
                .ToSequence<IAudioService.Commands.Play>(AudioCue.Lose)
                .ToSequence<RecordTimeOutCommand>()
                .ToSequence<LoseLifeCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>()
                .ToSequence<AnnounceBoostersCommand>()
                .ToSequence<AnnounceAdOffersCommand>();
            CommandBinder.Bind(_signals.Incoming.RefreshLives)
                .ToSequence<RefreshLivesCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>();
            CommandBinder.Bind(_signals.Incoming.OpenLivesOffer)
                .ToSequence<RefreshLivesCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>()
                .ToSequence<OpenLivesOfferCommand>();
            CommandBinder.Bind(_signals.Incoming.RefreshLivesOffer)
                .ToSequence<RefreshLivesCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>()
                .ToSequence<AnnounceLivesOfferCommand>();
            CommandBinder.Bind(_signals.Incoming.BuyLives)
                .ToSequence<RefreshLivesCommand>()
                .ToSequence<BuyLivesCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>()
                .ToSequence<AnnounceLivesOfferCommand>()
                // Bought for a level that waited for a life: that level starts.
                .ToSequence<StartWaitingLevelCommand>();
            CommandBinder.Bind(_signals.Incoming.WatchAdForLives)
                .ToSequence<RefreshLivesCommand>()
                .ToSequence<IAdsService.Commands.ShowRewarded>()
                .ToSequence<GrantAdLivesCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>()
                .ToSequence<AnnounceLivesOfferCommand>()
                // Watched for a level that waited for a life: that level starts.
                .ToSequence<StartWaitingLevelCommand>();
            CommandBinder.Bind(_signals.Incoming.WatchAdForDoubleReward)
                .ToSequence<CheckDoubleRewardCommand>()
                .ToSequence<IAdsService.Commands.ShowRewarded>()
                .ToSequence<DoubleWinRewardCommand>();
            CommandBinder.Bind(_signals.Incoming.WatchAdForTime)
                .ToSequence<CheckExtraTimeCommand>()
                .ToSequence<IAdsService.Commands.ShowRewarded>()
                .ToSequence<ExtendTimeCommand>()
                .ToSequence<StartPuzzleClockCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>()
                .ToSequence<AnnounceBoostersCommand>()
                .ToSequence<AnnounceAdOffersCommand>();
            CommandBinder.Bind(_signals.Incoming.WatchAdForBooster)
                .ToSequence<CheckBoosterAdCommand>()
                .ToSequence<IAdsService.Commands.ShowRewarded>()
                .ToSequence<GrantBoosterAdCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceBoostersCommand>();
            CommandBinder.Bind(_signals.Incoming.ResetLives)
                .ToSequence<ResetLivesCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceLivesCommand>()
                .ToSequence<ScheduleNotificationsCommand>();
            CommandBinder.Bind(_internalSignals.Tick).ToSequence<TickPuzzleCommand>();
            // Freeze Time could not be used while the clock was frozen; now it can again.
            CommandBinder.Bind(_internalSignals.ClockThawed).ToSequence<AnnounceBoostersCommand>();
            CommandBinder.Bind(_internalSignals.PayWin)
                .ToSequence<AddCoinsCommand>()
                .ToSequence<IAudioService.Commands.Play>(AudioCue.Coin)
                .ToSequence<ILocalSaveService.Commands.Save>(PROGRESS)
                .ToSequence<AnnounceBoostersCommand>()
                .ToSequence<AnnounceAdOffersCommand>();
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
        }
        public override void DestroyContext()
        {
            if (_model != null) _model.IsTicking = false;
            base.DestroyContext();
        }
    }
}
