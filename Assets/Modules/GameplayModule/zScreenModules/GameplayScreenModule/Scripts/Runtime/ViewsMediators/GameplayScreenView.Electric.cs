using System.Collections;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// A combo charging a door: the board crackles the jellies and sends them in, and a badge with the combo pops
    /// in the board's top corner.
    /// </summary>
    public partial class GameplayScreenView
    {
        private Image _strikeBadge;
        private TMP_Text _strikeText;
        private Coroutine _strikeShow;

        /// <summary>The combo window from the game's config, so the combo words count the way the game does.</summary>
        public void SetComboWindow(float seconds) { if (_spriteBoard != null && seconds > 0) _spriteBoard.ComboWindow = seconds; }

        public void ApplyComboStrike(ComboStrikeRVO strike)
        {
            if (strike == null) return;
            var fx = _presentation != null ? _presentation.Electric : null;
            _spriteBoard.Charge(strike.PieceIds, strike.Door, strike.Seconds, fx);
            if (fx != null && fx.Badge != null) ShowStrikeBadge(strike.Combo, fx.Badge);
        }

        private void ShowStrikeBadge(int combo, Sprite badge)
        {
            if (!isActiveAndEnabled) return;
            if (_strikeBadge == null)
            {
                _strikeBadge = new GameObject("Img_ComboStrike", typeof(RectTransform)).AddComponent<Image>();
                _strikeBadge.rectTransform.SetParent(_board, false);
                _strikeBadge.raycastTarget = false;
                _strikeBadge.preserveAspect = true;
                var rect = _strikeBadge.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
                rect.anchoredPosition = new Vector2(0, -12);
                rect.sizeDelta = new Vector2(170, 170);
                if (_message != null)
                {
                    _strikeText = Instantiate(_message, rect);
                    _strikeText.name = "Txt_ComboStrike";
                    _strikeText.raycastTarget = false;
                    _strikeText.enableWordWrapping = false;
                    _strikeText.alignment = TextAlignmentOptions.Center;
                    _strikeText.color = Color.white;
                    _strikeText.fontSize = 64;
                    var text = _strikeText.rectTransform;
                    text.anchorMin = Vector2.zero; text.anchorMax = Vector2.one;
                    text.offsetMin = new Vector2(0, 0); text.offsetMax = new Vector2(0, -36);
                }
            }
            _strikeBadge.sprite = badge;
            if (_strikeText != null) { _strikeText.text = "x" + combo; _strikeText.gameObject.SetActive(true); }
            _strikeBadge.transform.SetAsLastSibling();
            if (_strikeShow != null) StopCoroutine(_strikeShow);
            _strikeShow = StartCoroutine(PopStrikeBadge());
        }

        private IEnumerator PopStrikeBadge()
        {
            _strikeBadge.gameObject.SetActive(true);
            const float life = 1.6f;
            for (float t = 0; t < life; t += Time.unscaledDeltaTime)
            {
                float k = t / life;
                float pop = k < .12f ? Mathf.Lerp(.3f, 1.25f, k / .12f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((k - .12f) / .15f));
                _strikeBadge.rectTransform.localScale = Vector3.one * pop;
                _strikeBadge.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 18) * 6 * (1 - k));
                float alpha = k < .75f ? 1 : 1 - (k - .75f) / .25f;
                _strikeBadge.color = new Color(1, 1, 1, alpha);
                if (_strikeText != null) _strikeText.alpha = alpha;
                yield return null;
            }
            _strikeBadge.gameObject.SetActive(false);
            _strikeShow = null;
        }
    }
}
