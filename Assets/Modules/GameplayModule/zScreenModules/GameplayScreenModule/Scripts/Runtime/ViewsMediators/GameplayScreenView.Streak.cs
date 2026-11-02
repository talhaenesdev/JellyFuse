using Modules.GameplayModule.Shared.Data.ValueObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The win streak in the top bar: a flame that grows with the streak's tier and the wins in a row.
    /// Hidden while there is no streak.
    /// </summary>
    public partial class GameplayScreenView
    {
        [Header("Win streak")]
        [SerializeField] private GameObject _streak;
        [SerializeField] private Image _streakFlame;
        [Tooltip("The flame for each tier, lowest first; a streak below the first tier shows the first.")]
        [SerializeField] private Sprite[] _streakFlames;
        [SerializeField] private TMP_Text _streakCount;

        public void ApplyEvents(EventsRVO events)
        {
            if (_streak == null) return;
            _streak.SetActive(events.WinStreak > 0);
            if (events.WinStreak <= 0) return;
            if (_streakFlames != null && _streakFlames.Length > 0)
                _streakFlame.sprite = _streakFlames[Mathf.Clamp(events.WinStreakTier - 1, 0, _streakFlames.Length - 1)];
            if (_streakCount != null) _streakCount.text = events.WinStreak.ToString();
        }
    }
}
