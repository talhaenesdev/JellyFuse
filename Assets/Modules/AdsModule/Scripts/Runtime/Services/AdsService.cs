using System;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Unity.Services.LevelPlay;

namespace Modules.AdsModule.Services
{
    public class AdsService : IAdsService
    {
        private LevelPlayRewardedAd _rewarded;
        private Action<bool> _onDone;
        private const int RewardWaitMilliseconds = 1500;
        private bool _earned;
        private bool _closed;
        private int _shows;
        private bool _started;

        public bool IsRewardedReady => _rewarded != null && _rewarded.IsAdReady();

        public void Initialize(string appKey, string rewardedAdUnitId)
        {
            if (_started || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(rewardedAdUnitId)) return;
            _started = true;
            LevelPlay.OnInitSuccess += _ => LoadRewarded(rewardedAdUnitId);
            LevelPlay.OnInitFailed += error => FlowLogger.LogWarning($"LevelPlay did not start: {error.ErrorMessage}");
            LevelPlay.Init(appKey);
        }

        public void ShowRewarded(Action<bool> onDone)
        {
            if (!IsRewardedReady)
            {
                onDone?.Invoke(false);
                return;
            }
            _onDone = onDone;
            _earned = false;
            _closed = false;
            _shows++;
            _rewarded.ShowAd();
        }

        private void LoadRewarded(string adUnitId)
        {
            _rewarded = new LevelPlayRewardedAd(adUnitId);
            _rewarded.OnAdLoadFailed += error => FlowLogger.LogWarning($"Rewarded ad not loaded: {error.ErrorMessage}");
            _rewarded.OnAdRewarded += (_, _) => OnRewarded();
            _rewarded.OnAdDisplayFailed += (_, error) =>
            {
                FlowLogger.LogWarning($"Rewarded ad not shown: {error.ErrorMessage}");
                Finish(false);
            };
            _rewarded.OnAdClosed += _ => OnClosed();
            _rewarded.LoadAd();
        }

        private void OnRewarded()
        {
            _earned = true;
            if (_closed) Finish(true);
        }

        // The reward may be told after the close, so a close without it waits a moment for it.
        private async void OnClosed()
        {
            _closed = true;
            if (_earned) { Finish(true); return; }
            int show = _shows;
            await Task.Delay(RewardWaitMilliseconds);
            if (show == _shows) Finish(_earned);
        }

        // The caller hears once whether the reward was earned, and the next ad loads.
        private void Finish(bool earned)
        {
            var onDone = _onDone;
            if (onDone == null) return;
            _onDone = null;
            _rewarded.LoadAd();
            onDone(earned);
        }
    }
}
