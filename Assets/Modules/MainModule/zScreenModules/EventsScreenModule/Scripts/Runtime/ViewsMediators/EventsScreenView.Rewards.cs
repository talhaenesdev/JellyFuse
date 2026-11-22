using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.EventsScreenModule.ViewsMediators
{
    /// <summary>
    /// A card's reward as icons with counts - coins, boosters, lives - in its reward pill, instead of a sentence that
    /// would not fit. The row is made once per card from the pill's own text, so it wears the card's font.
    /// </summary>
    public partial class EventsScreenView
    {
        [Header("Reward icons")]
        [SerializeField] private Sprite _rewardCoin;
        [SerializeField] private Sprite _rewardRocket;
        [SerializeField] private Sprite _rewardFreeze;
        [SerializeField] private Sprite _rewardUfo;
        [SerializeField] private Sprite _rewardLife;

        private readonly Dictionary<Transform, RectTransform> _rewardRows = new();
        private const float PILL_WIDTH = 190f;
        private const float COUNT_SIZE = 18f;
        private const float ICON_SIZE = 26f;

        /// <summary>Every card's reward pill the same width, wide enough for three icons or a chest's name.</summary>
        private static void WidenPill(Transform pill)
        {
            var rect = (RectTransform)pill;
            // A pill stretched between its neighbours already fits the card; only a fixed one is widened.
            if (Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x) && rect.sizeDelta.x < PILL_WIDTH) rect.sizeDelta = new Vector2(PILL_WIDTH, rect.sizeDelta.y);
        }

        private void ShowRewardIcons(Transform card, int coins, int rockets, int freezes, int ufos, int lives)
        {
            var pill = card.Find("Pnl_Reward");
            var label = pill != null ? pill.Find("Txt_Reward")?.GetComponent<TMP_Text>() : null;
            if (pill == null || label == null) return;
            WidenPill(pill);
            if (!_rewardRows.TryGetValue(card, out var row) || row == null)
            {
                row = new GameObject("Row_Reward", typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(pill, false);
                row.anchorMin = Vector2.zero; row.anchorMax = Vector2.one;
                row.offsetMin = new Vector2(10, 4); row.offsetMax = new Vector2(-10, -4);
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 4;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                _rewardRows[card] = row;
            }
            label.gameObject.SetActive(false);
            for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);
            Add(row, label, _rewardCoin, coins);
            Add(row, label, _rewardRocket, rockets);
            Add(row, label, _rewardFreeze, freezes);
            Add(row, label, _rewardUfo, ufos);
            Add(row, label, _rewardLife, lives);
        }

        /// <summary>The pill shows its text again - a card whose reward is a name, not a list.</summary>
        private void ShowRewardText(Transform card, string text)
        {
            if (_rewardRows.TryGetValue(card, out var row) && row != null) row.gameObject.SetActive(false);
            var label = card.Find("Pnl_Reward/Txt_Reward")?.GetComponent<TMP_Text>();
            if (label == null) return;
            WidenPill(label.transform.parent);
            label.gameObject.SetActive(true);
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12;
            label.fontSizeMax = COUNT_SIZE;
            label.text = text;
        }

        private static void Add(RectTransform row, TMP_Text style, Sprite icon, int count)
        {
            if (count <= 0) return;
            row.gameObject.SetActive(true);
            float size = ICON_SIZE;
            if (icon != null)
            {
                var image = new GameObject("Img_Reward", typeof(RectTransform)).AddComponent<Image>();
                image.rectTransform.SetParent(row, false);
                image.sprite = icon; image.preserveAspect = true; image.raycastTarget = false;
                var fit = image.gameObject.AddComponent<LayoutElement>();
                fit.preferredWidth = fit.preferredHeight = size;
            }
            var text = Instantiate(style, row);
            text.name = "Txt_Count";
            text.gameObject.SetActive(true);
            text.enableAutoSizing = false;
            text.enableWordWrapping = false;
            text.fontSize = COUNT_SIZE;
            var textFit = text.gameObject.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
            textFit.minWidth = 0;
            text.text = count.ToString();
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }
}
