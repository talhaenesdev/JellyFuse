using System;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.DailyQuestsScreenModule.Entities
{
    /// <summary>One quest's row: its icon, what it asks, how far along it is, its reward, and a claim button once finished.</summary>
    public sealed class QuestRowWidget : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Sprite _normal;
        [SerializeField] private Sprite _claimable;
        [SerializeField] private Sprite _completed;
        [SerializeField] private Image _icon;
        [Tooltip("Icons in the order of QuestType: CompleteLevels, UseBooster, MergePieces.")]
        [SerializeField] private Sprite[] _icons;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _progress;
        [SerializeField] private TMP_Text _reward;
        [SerializeField] private Button _claimButton;
        [SerializeField] private TMP_Text _claimLabel;

        public Action Claimed;
        /// <summary>Where the reward is shown, for it to fly from when taken.</summary>
        public RectTransform RewardAnchor => (RectTransform)_reward.transform;

        private void OnEnable() => _claimButton.onClick.AddListener(OnClaimClicked);
        private void OnDisable() => _claimButton.onClick.RemoveListener(OnClaimClicked);

        public void Show(QuestSVO quest, Func<string, string, string> localize)
        {
            _background.sprite = quest.Claimed ? _completed : quest.IsClaimable ? _claimable : _normal;
            int type = (int)quest.Type;
            if (_icons != null && type < _icons.Length) _icon.sprite = _icons[type];
            string key = quest.Type switch
            {
                QuestType.CompleteLevels => "quests.complete_levels",
                QuestType.UseBooster => "quests.use_booster",
                _ => "quests.merge_pieces",
            };
            string fallback = quest.Type switch
            {
                QuestType.CompleteLevels => "Complete {0} levels",
                QuestType.UseBooster => "Use {0} boosters",
                _ => "Merge {0} jellies",
            };
            _description.text = string.Format(localize(key, fallback), quest.Target);
            float ratio = quest.Target > 0 ? Mathf.Clamp01((float)quest.Progress / quest.Target) : 0;
            _fill.anchorMax = new Vector2(ratio, _fill.anchorMax.y);
            _progress.text = $"{quest.Progress}/{quest.Target}";
            _reward.text = quest.Reward.Coins > 0 ? quest.Reward.Coins.ToString() : "+" + quest.Reward.BoosterCount;
            _claimButton.gameObject.SetActive(quest.IsClaimable);
            _claimLabel.text = localize("quests.claim", "Claim");
            _reward.gameObject.SetActive(!quest.Claimed);
        }

        private void OnClaimClicked() => Claimed?.Invoke();
    }
}
