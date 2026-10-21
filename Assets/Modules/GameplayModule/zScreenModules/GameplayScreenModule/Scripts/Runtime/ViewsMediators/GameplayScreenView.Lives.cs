using System;
using System.Collections;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using TMPro;
using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The lives in the top bar: how many the player holds, and the time to the next one. The time
    /// is counted down here between two announcements; at zero the screen asks the game to bring
    /// the lives up to date. With no life to start a level, the retry panel says when one comes back.
    /// </summary>
    public partial class GameplayScreenView
    {
        [Header("Lives")]
        [SerializeField] private TMP_Text _livesCount;
        [SerializeField] private TMP_Text _livesTimer;

        /// <summary>The time to the next life ran out: the lives should be brought up to date.</summary>
        public event Action LivesRefreshRequested;

        private LivesStateRVO _livesState;
        private float _lifeSeconds;
        private bool _lifeRefreshAsked, _outOfLives;
        private Coroutine _lifeCountdown;

        public void ApplyLives(LivesStateRVO state)
        {
            _livesState = state;
            _lifeSeconds = state.SecondsToNext;
            _lifeRefreshAsked = false;
            if (_livesCount != null) _livesCount.text = $"{state.Lives}/{state.MaxLives}";
            DrawLifeTime();
            if (_lifeCountdown == null && isActiveAndEnabled) _lifeCountdown = StartCoroutine(CountLifeTime());
        }

        /// <summary>A level could not start for want of a life: the retry panel says when the next comes back.</summary>
        public void ApplyOutOfLives(float seconds)
        {
            _lifeSeconds = seconds;
            HidePanels();
            StopPresentation();
            _outOfLives = true;
            DrawLifeTime();
            ShowDim(_retryPanel);
        }

        private IEnumerator CountLifeTime()
        {
            var wait = new WaitForSecondsRealtime(1);
            while (true)
            {
                yield return wait;
                if (_livesState == null || _lifeSeconds <= 0) continue;
                _lifeSeconds = Mathf.Max(0, _lifeSeconds - 1);
                DrawLifeTime();
                if (_lifeSeconds <= 0 && !_lifeRefreshAsked)
                {
                    _lifeRefreshAsked = true;
                    LivesRefreshRequested?.Invoke();
                }
            }
        }

        private void DrawLifeTime()
        {
            string time = LifeTime(_lifeSeconds);
            if (_livesTimer != null)
                _livesTimer.text = _livesState != null && _livesState.Lives >= _livesState.MaxLives ? "" : time;
            if (_outOfLives && _retryBody != null) _retryBody.text = LF("gameplay.out_of_lives", "No lives left!\nNext life in {0}", time);
        }
        private static string LifeTime(float seconds)
        {
            int whole = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return whole >= 3600 ? $"{whole / 3600}:{whole / 60 % 60:00}:{whole % 60:00}" : $"{whole / 60:00}:{whole % 60:00}";
        }

        /// <summary>The retry panel goes back to its own text once it is closed.</summary>
        private void EndOutOfLives()
        {
            if (!_outOfLives) return;
            _outOfLives = false;
            if (_retryBody != null) _retryBody.text = L("gameplay.lose_life", "You will lose 1 life!");
        }
        private void StopLives()
        {
            if (_lifeCountdown != null) { StopCoroutine(_lifeCountdown); _lifeCountdown = null; }
            EndOutOfLives();
        }
    }
}
