using System;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.LoginRewardsScreenModule.Entities
{
    /// <summary>One day's tile of the login reward: the day, its reward, and whether it is taken, today's or still to come.</summary>
    public sealed class LoginDayWidget : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Sprite _claimed;
        [SerializeField] private Sprite _today;
        [SerializeField] private Sprite _locked;
        [Tooltip("The day-seven chest, shut until its day is taken; empty on the other days.")]
        [SerializeField] private Image _chest;
        [SerializeField] private Sprite _chestClosed;
        [SerializeField] private Sprite _chestOpened;
        [SerializeField] private TMP_Text _day;
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private Sprite _coinIcon;
        [Tooltip("Booster icons in the order of BoosterType: Rocket, FreezeTime, Ufo.")]
        [SerializeField] private Sprite[] _boosterIcons;
        [SerializeField] private TMP_Text _reward;

        public void Show(int day, LoginDayRVO entry, Func<string, string, string> localize)
        {
            _background.sprite = entry.State switch
            {
                LoginDayState.Claimed => _claimed,
                LoginDayState.Today => _today,
                _ => _locked,
            };
            _day.text = string.Format(localize("login.day", "Day {0}"), day);
            var reward = entry.Reward;
            bool coins = reward.Coins > 0;
            int booster = (int)reward.Booster;
            if (_rewardIcon != null)
                _rewardIcon.sprite = coins || _boosterIcons == null || booster >= _boosterIcons.Length ? _coinIcon : _boosterIcons[booster];
            string text = coins ? reward.Coins.ToString() : "";
            if (reward.BoosterCount > 0) text += (coins ? " +" : "x") + reward.BoosterCount;
            _reward.text = text;
            if (_chest != null) _chest.sprite = entry.State == LoginDayState.Claimed ? _chestOpened : _chestClosed;
        }
    }
}
