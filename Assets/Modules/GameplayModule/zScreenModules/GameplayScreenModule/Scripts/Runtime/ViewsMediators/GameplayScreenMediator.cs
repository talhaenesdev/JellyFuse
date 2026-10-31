using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Extensions;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    public class GameplayScreenMediator : IMediator
    {
        [Inject] private GameplayScreenView _view { get; set; }
        [InjectSignal] private GameplayScreenSignals _signals { get; set; }
        [InjectSignal] private GameplayScreenInternalSignals _internalSignals { get; set; }
        private bool _subscribed;
        public void OnRegister()
        {
            _view.ShowCompleted += OnShown;
            _view.HideCompleted += OnHidden;
        }
        public void OnRemove()
        {
            Unsubscribe();
            _view.ShowCompleted -= OnShown;
            _view.HideCompleted -= OnHidden;
        }
        private void OnShown(IScreenBody screen)
        {
            Unsubscribe();
            _subscribed = true;
            _view.Dragged += OnDrag;
            _view.UndoClicked += OnUndo;
            _view.RestartClicked += OnRestart;
            _view.NextLevelClicked += OnNextLevel;
            _view.SettingsClicked += OnSettings;
            _view.QuitClicked += OnQuitClicked;
            _view.JellyMelting += OnJellyMelting;
            _view.BoosterChosen += OnBoosterChosen;
            _view.DoubleRewardClicked += OnDoubleRewardClicked;
            _view.ExtraTimeClicked += OnExtraTimeClicked;
            _view.BoosterAdClicked += OnBoosterAdClicked;
            _signals.Incoming.ApplyAdOffers.AddListener(_view.ApplyAdOffers);
            _signals.Incoming.SendTuningSliders.AddListener(OnSendTuningSliders);
            _signals.Incoming.SetTuned.AddListener(_view.SetTuned);
            _signals.Incoming.ResetTuning.AddListener(OnResetTuning);
            _signals.Incoming.ShowTopBar.AddListener(_view.ShowTopBar);
            _signals.Incoming.ShowBottomBar.AddListener(_view.ShowBottomBar);
            _signals.Incoming.ApplyTutorial.AddListener(_view.ApplyTutorial);
            _signals.Incoming.ShowInfo.AddListener(_view.ShowInfo);
            _view.LivesRefreshRequested += OnLivesRefreshRequested;
            _signals.Incoming.ApplyLives.AddListener(_view.ApplyLives);
            _signals.Incoming.ApplyOutOfLives.AddListener(_view.ApplyOutOfLives);
            _signals.Incoming.ApplyBoosters.AddListener(_view.ApplyBoosters);
            _signals.Incoming.ApplyBoosterUsed.AddListener(_view.ApplyBoosterUsed);
            _signals.Incoming.ApplyClockFrozen.AddListener(_view.ApplyClockFrozen);
            _signals.Incoming.ApplyCoins.AddListener(_view.ApplyCoins);
            _signals.Incoming.ApplyReward.AddListener(_view.ApplyReward);
            _signals.Incoming.ApplyLanguage.AddListener(_view.ApplyLanguage);
#if UNITY_EDITOR
            _view.SkipLevelPressed += OnSkipLevel;
            _view.ForceResultPressed += OnForceResult;
#endif
            _signals.Incoming.ApplyLevel.AddListener(_view.ApplyLevel);
            _signals.Incoming.ApplyBoard.AddListener(_view.ApplyBoard);
            _signals.Incoming.ApplyTime.AddListener(_view.ApplyTime);
            _signals.Incoming.Quit.AddListener(OnQuit);
            _signals.Outgoing.Start.Dispatch();
        }
        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            _view.Dragged -= OnDrag;
            _view.UndoClicked -= OnUndo;
            _view.RestartClicked -= OnRestart;
            _view.NextLevelClicked -= OnNextLevel;
            _view.SettingsClicked -= OnSettings;
            _view.QuitClicked -= OnQuitClicked;
            _view.JellyMelting -= OnJellyMelting;
            _view.BoosterChosen -= OnBoosterChosen;
            _view.DoubleRewardClicked -= OnDoubleRewardClicked;
            _view.ExtraTimeClicked -= OnExtraTimeClicked;
            _view.BoosterAdClicked -= OnBoosterAdClicked;
            _signals.Incoming.ApplyAdOffers.RemoveListener(_view.ApplyAdOffers);
            _signals.Incoming.SendTuningSliders.RemoveListener(OnSendTuningSliders);
            _signals.Incoming.SetTuned.RemoveListener(_view.SetTuned);
            _signals.Incoming.ResetTuning.RemoveListener(OnResetTuning);
            _signals.Incoming.ShowTopBar.RemoveListener(_view.ShowTopBar);
            _signals.Incoming.ShowBottomBar.RemoveListener(_view.ShowBottomBar);
            _signals.Incoming.ApplyTutorial.RemoveListener(_view.ApplyTutorial);
            _signals.Incoming.ShowInfo.RemoveListener(_view.ShowInfo);
            _view.LivesRefreshRequested -= OnLivesRefreshRequested;
            _signals.Incoming.ApplyLives.RemoveListener(_view.ApplyLives);
            _signals.Incoming.ApplyOutOfLives.RemoveListener(_view.ApplyOutOfLives);
            _signals.Incoming.ApplyBoosters.RemoveListener(_view.ApplyBoosters);
            _signals.Incoming.ApplyBoosterUsed.RemoveListener(_view.ApplyBoosterUsed);
            _signals.Incoming.ApplyClockFrozen.RemoveListener(_view.ApplyClockFrozen);
            _signals.Incoming.ApplyCoins.RemoveListener(_view.ApplyCoins);
            _signals.Incoming.ApplyReward.RemoveListener(_view.ApplyReward);
            _signals.Incoming.ApplyLanguage.RemoveListener(_view.ApplyLanguage);
#if UNITY_EDITOR
            _view.SkipLevelPressed -= OnSkipLevel;
            _view.ForceResultPressed -= OnForceResult;
#endif
            _signals.Incoming.ApplyLevel.RemoveListener(_view.ApplyLevel);
            _signals.Incoming.ApplyBoard.RemoveListener(_view.ApplyBoard);
            _signals.Incoming.ApplyTime.RemoveListener(_view.ApplyTime);
            _signals.Incoming.Quit.RemoveListener(OnQuit);
        }
        private void OnHidden(IScreenBody screen)
        {
            Unsubscribe();
            _view.StopPresentation();
            _signals.Outgoing.Closed.Dispatch();
        }
        private void OnDrag(Vector2Int cell, Vector2Int targetCell, bool released)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.Drag.Dispatch(cell, targetCell, released);
        }
        private void OnUndo()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.Undo.Dispatch();
        }
#if UNITY_EDITOR
        private void OnSkipLevel(int direction)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.SkipLevel.Dispatch(direction);
        }
        private void OnForceResult(PuzzleStatus status)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.ForceResult.Dispatch(status);
        }
#endif
        // Not guarded: the lives are brought up to date whatever state the screen is in.
        private void OnLivesRefreshRequested() => _signals.Outgoing.LivesRefreshRequested.Dispatch();
        // Not guarded: the developer panel sits over the screen while it asks.
        // The page shows the defaults straight away.
        private void OnResetTuning(string page)
        {
            _view.ResetTuning(page);
            OnSendTuningSliders(page);
        }
        private void OnSendTuningSliders(string page) => _signals.Outgoing.TuningSliders.Dispatch(page, _view.TuningSliders(page));
        private void OnBoosterChosen(BoosterType type, int target)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.UseBooster.Dispatch(type, target);
        }
        private void OnDoubleRewardClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.DoubleRewardClicked.Dispatch();
        }
        private void OnExtraTimeClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.ExtraTimeClicked.Dispatch();
        }
        private void OnBoosterAdClicked(BoosterType type)
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.BoosterAdClicked.Dispatch(type);
        }
        private void OnSettings()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.PauseClicked.Dispatch();
        }
        // Not guarded: the melt is shown whatever state the screen is in, and the haptic follows it.
        private void OnJellyMelting(float seconds) => _internalSignals.JellyMelting.Dispatch(seconds);
        private void OnQuitClicked()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) OnQuit();
        }
        // Back to the main menu: closing the screen stops the level (Closed), and Home opens the menu.
        private void OnQuit()
        {
            _view.Hide();
            _signals.Outgoing.HomeClicked.Dispatch();
        }
        private void OnNextLevel()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.NextLevel.Dispatch();
        }
        private void OnRestart()
        {
            if (_view.Data.HasState(ScreenState.AvailableToSendSignal)) _signals.Outgoing.Start.Dispatch();
        }
    }
}
