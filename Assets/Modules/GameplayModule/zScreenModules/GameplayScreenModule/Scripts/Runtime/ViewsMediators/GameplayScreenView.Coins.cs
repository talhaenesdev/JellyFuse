using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The coin count top left. A win's coins are not shown at once: when the win panel opens they fly
    /// from its big coin to the coin icon, and the count goes up one by one as they land. Leaving the
    /// panel before they are done shows the full count straight away.
    /// </summary>
    public partial class GameplayScreenView
    {
        [Header("Coin flight")]
        [Tooltip("The big coin on the win panel the coins fly from.")]
        [SerializeField] private RectTransform _winCoinIcon;
        [Tooltip("The coin icon by the count, top left, they fly to.")]
        [SerializeField] private RectTransform _coinIcon;

        private int _shownCoins = -1, _targetCoins;
        /// <summary>Coins a win paid that are not on the count yet: they arrive with the flight.</summary>
        private int _pendingReward;
        private Coroutine _coinFlight;
        private readonly List<Image> _flyingCoins = new();

        public void ApplyCoins(int coins)
        {
            _targetCoins = coins;
            // While a win's coins are on their way, the count waits for them.
            if (_pendingReward > 0 || _coinFlight != null) return;
            ShowCoins(coins);
        }

        public void ApplyReward(int coins)
        {
            // More coins for a win already on show - an ad paid it again - add to what the panel reads, and fly at once.
            bool onShow = _winPanel != null && _winPanel.parent.gameObject.activeSelf;
            if (_winReward != null)
                _winReward.text = (onShow && int.TryParse(_winReward.text, out var was) ? was + coins : coins).ToString();
            if (coins <= 0) return;
            // The count stays where it was until the win panel shows the coins flying in.
            if (_shownCoins < 0 && _coins != null && int.TryParse(_coins.text, out var shown)) _shownCoins = shown;
            _pendingReward += coins;
            if (onShow) StartCoinFlight();
        }

        private void ShowCoins(int coins)
        {
            _shownCoins = coins;
            if (_coins != null) _coins.text = coins.ToString();
        }

        /// <summary>The win panel is showing: the pending coins fly to the count.</summary>
        private void StartCoinFlight()
        {
            if (_pendingReward <= 0 || _coinFlight != null) return;
            if (_winCoinIcon == null || _coinIcon == null) { FinishCoinFlight(); return; }
            _coinFlight = StartCoroutine(FlyCoins(_pendingReward));
        }

        private IEnumerator FlyCoins(int reward)
        {
            var hud = _presentation.Hud;
            int from = Mathf.Max(0, _targetCoins - reward);
            ShowCoins(from);
            int flying = Mathf.Clamp(reward, 1, hud.CoinFlyCount);
            var sprite = _winCoinIcon.GetComponent<Image>() != null ? _winCoinIcon.GetComponent<Image>().sprite : null;
            var root = (RectTransform)transform;
            Vector2 start = Local(root, _winCoinIcon), end = Local(root, _coinIcon);
            // As big as the coin icon it flies to, times Coin Fly Scale - read off its height, since the
            // icon's idle turn runs its width through nothing.
            var size = _coinIcon.rect.size * (_coinIcon.lossyScale.y / root.lossyScale.y) * hud.CoinFlyScale;
            float arc = Vector2.Distance(start, end) * hud.CoinFlyArc;
            while (_flyingCoins.Count < flying)
            {
                var made = new GameObject("Img_FlyingCoin", typeof(RectTransform)).AddComponent<Image>();
                made.rectTransform.SetParent(transform, false);
                made.rectTransform.anchorMin = made.rectTransform.anchorMax = new Vector2(.5f, .5f);
                made.raycastTarget = false; made.preserveAspect = true; made.enabled = false;
                _flyingCoins.Add(made);
            }
            for (int i = 0; i < flying; i++)
            {
                var coin = _flyingCoins[i];
                coin.sprite = sprite;
                coin.rectTransform.sizeDelta = size;
                coin.rectTransform.anchoredPosition = start;
                coin.rectTransform.SetAsLastSibling();
            }
            float total = hud.CoinFlySeconds + hud.CoinFlyStagger * (flying - 1);
            int landed = 0;
            for (float t = 0; t < total; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < flying; i++)
                {
                    float k = Mathf.Clamp01((t - i * hud.CoinFlyStagger) / hud.CoinFlySeconds);
                    var coin = _flyingCoins[i];
                    coin.enabled = k > 0 && k < 1;
                    // An arc toward the count, easing in as it nears it.
                    float e = k * k * (3 - 2 * k);
                    var p = Vector2.Lerp(start, end, e) + Vector2.up * Mathf.Sin(k * Mathf.PI) * arc;
                    coin.rectTransform.anchoredPosition = p;
                    coin.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, .8f, e);
                }
                // The count goes up one at a time, in step with the coins landing.
                int arrived = Mathf.Clamp(Mathf.FloorToInt(reward * Mathf.Clamp01((t - hud.CoinFlySeconds * .5f) / Mathf.Max(.01f, total - hud.CoinFlySeconds * .5f))), 0, reward);
                if (arrived != landed) { landed = arrived; ShowCoins(from + landed); }
                yield return null;
            }
            _coinFlight = null;
            FinishCoinFlight();
        }

        /// <summary>Ends the flight wherever it is: the count shows every coin, and the flying ones go.</summary>
        private void FinishCoinFlight()
        {
            if (_coinFlight != null) { StopCoroutine(_coinFlight); _coinFlight = null; }
            foreach (var coin in _flyingCoins) coin.enabled = false;
            _pendingReward = 0;
            ShowCoins(_targetCoins);
        }

        private static Vector2 Local(RectTransform space, RectTransform target) =>
            space.InverseTransformPoint(target.TransformPoint(target.rect.center));
    }
}
