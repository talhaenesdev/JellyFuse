using System;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The rewarded ads the result panels offer: the win's coins again beside Continue, more time beside Try Again.
    /// A button whose offer is not open now stays in place, dimmed, so the panel keeps the shape it was authored in.
    /// </summary>
    public partial class GameplayScreenView
    {
        [Header("Rewarded ads")]
        [Tooltip("The win panel's ad button: the win's coins again.")]
        [SerializeField] private Button _doubleRewardButton;
        [SerializeField] private TMP_Text _doubleRewardLabel;
        [Tooltip("The lose panel's ad button: more time on a level the clock ran out on.")]
        [SerializeField] private Button _extraTimeButton;
        [SerializeField] private TMP_Text _extraTimeLabel;
        [Tooltip("How see-through an ad button is while its offer is not open.")]
        [SerializeField, Range(0, 1)] private float _adDisabledAlpha = .45f;

        public event Action DoubleRewardClicked;
        public event Action ExtraTimeClicked;
        /// <summary>A booster with no use left was pressed while today's free booster is offered for an ad.</summary>
        public event Action<BoosterType> BoosterAdClicked;

        private AdOffersRVO _adOffers = AdOffersRVO.None;

        private void WireAds(bool on)
        {
            if (_doubleRewardButton != null)
            {
                if (on) _doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
                else _doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
            }
            if (_extraTimeButton != null)
            {
                if (on) _extraTimeButton.onClick.AddListener(OnExtraTimeClicked);
                else _extraTimeButton.onClick.RemoveListener(OnExtraTimeClicked);
            }
        }
        private void OnDoubleRewardClicked() { if (_adOffers.DoubleReward) DoubleRewardClicked?.Invoke(); }
        private void OnExtraTimeClicked() { if (_adOffers.ExtraTime) ExtraTimeClicked?.Invoke(); }

        /// <summary>The ads the result panels offer now.</summary>
        public void ApplyAdOffers(AdOffersRVO offers)
        {
            _adOffers = offers ?? AdOffersRVO.None;
            DrawAdButtons();
        }

        private void DrawAdButtons()
        {
            DrawAdButton(_doubleRewardButton, _doubleRewardLabel, _adOffers.DoubleReward,
                LF(_adOffers.DoubleRewardKey ?? "gameplay.ad_double_reward", "Ad  x{0}", Math.Max(2, _adOffers.Multiplier)));
            DrawAdButton(_extraTimeButton, _extraTimeLabel, _adOffers.ExtraTime,
                LF(_adOffers.ExtraTimeKey ?? "gameplay.ad_extra_time", "Ad  +{0}s", _adOffers.ExtraSeconds > 0 ? _adOffers.ExtraSeconds : 30));
        }
        private void DrawAdButton(Button button, TMP_Text label, bool offered, string text)
        {
            if (button == null) return;
            if (label != null) label.text = text;
            button.interactable = offered;
            var group = button.GetComponent<CanvasGroup>();
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = offered ? 1 : _adDisabledAlpha;
        }
    }
}
