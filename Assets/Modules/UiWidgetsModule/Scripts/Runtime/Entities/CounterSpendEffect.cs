using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DG.Tweening;
using Modules.UiWidgetsModule.Data.UnityObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Put on a counter's text - coins, gems: when the number in it goes down, it counts down to the new one
    /// instead of jumping, and copies of the counter's icon drop away one by one, as CD_CounterSpend says. A
    /// number going up is left alone. It needs no code: it watches the text, so whatever writes the count keeps
    /// writing it as before.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CounterSpendEffect : MonoBehaviour
    {
        [Tooltip("How the spend is shown. Empty: the number just changes.")]
        [SerializeField] private CD_CounterSpend _settings;
        [Tooltip("The icon beside the count; its copies are what drop.")]
        [SerializeField] private Image _icon;
        [Tooltip("On: runs on real time, while the game is paused too.")]
        [SerializeField] private bool _ignoreTimeScale = true;

        private static readonly Regex NUMBER = new(@"-?\d+");
        private TMP_Text _text;
        private long _shown;
        private bool _known, _writing, _counting;
        private string _target, _written;
        private Tween _count, _punch;
        private Vector3 _scale = Vector3.one;
        private readonly List<Image> _pool = new();

        public CD_CounterSpend Settings { get => _settings; set => _settings = value; }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _scale = _text.rectTransform.localScale;
            _known = Number(_text.text, out _shown);
        }
        private void OnEnable() => TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            Finish();
            foreach (var icon in _pool) if (icon != null) { icon.DOKill(); icon.rectTransform.DOKill(); icon.gameObject.SetActive(false); }
        }

        private void OnTextChanged(Object changed)
        {
            // While it counts, the texts it writes are its own, and TextMeshPro reports them frames later.
            if (changed != _text || _writing || _counting || _text.text == _written) return;
            string now = _text.text;
            if (!Number(now, out long value)) { _known = false; return; }
            long from = _shown;
            bool spent = _known && value < from && _settings != null && isActiveAndEnabled;
            _known = true;
            if (!spent) { StopCount(); _shown = value; return; }
            Spend(from, value, now);
        }

        private void Spend(long from, long to, string template)
        {
            StopCount();
            _target = template; _counting = true;
            // Back to the number it had, then down to the new one.
            Write(template, from);
            _count = DOTween.To(() => (float)from, v => { _shown = (long)Mathf.Round(v); Write(template, _shown); }, to, Mathf.Max(.01f, _settings.CountSeconds))
                .SetEase(_settings.CountEase).SetUpdate(_ignoreTimeScale)
                .OnComplete(Finish);
            if (_settings.CountPunch > 0)
            {
                _text.rectTransform.localScale = _scale;
                _punch = _text.rectTransform.DOPunchScale(_scale * _settings.CountPunch, Mathf.Max(.1f, _settings.CountSeconds), 5, .5f).SetUpdate(_ignoreTimeScale);
            }
            // TMP raises TEXT_CHANGED_EVENT while its graphic can be inside Canvas rebuild. Creating or
            // changing Images there is rejected by Unity, so do the visual work on the next frame.
            StartCoroutine(DropIconsNextFrame(from - to));
        }

        private IEnumerator DropIconsNextFrame(long spent)
        {
            yield return null;
            if (isActiveAndEnabled) DropIcons(spent);
        }

        private void DropIcons(long spent)
        {
            if (_icon == null || _icon.sprite == null) return;
            int count = (int)Mathf.Clamp(spent / Mathf.Max(1, _settings.AmountPerIcon), 1, _settings.MaxIcons);
            var parent = (RectTransform)_icon.transform.parent;
            var start = _icon.rectTransform.anchoredPosition;
            for (int i = 0; i < count; i++)
            {
                var drop = Take(parent);
                drop.sprite = _icon.sprite; drop.preserveAspect = _icon.preserveAspect;
                var rect = drop.rectTransform;
                rect.anchorMin = _icon.rectTransform.anchorMin; rect.anchorMax = _icon.rectTransform.anchorMax; rect.pivot = _icon.rectTransform.pivot;
                rect.sizeDelta = _icon.rectTransform.sizeDelta;
                rect.anchoredPosition = start;
                rect.localScale = Vector3.one * _settings.StartScale;
                rect.localRotation = Quaternion.identity;
                rect.SetAsLastSibling();
                drop.color = new Color(1, 1, 1, 0);
                float delay = i * _settings.Stagger, time = _settings.DropSeconds;
                float side = Random.Range(-1f, 1f) * _settings.DropSideways;
                var end = start + new Vector2(side, -_settings.DropDistance);
                // A small hop, then a fall that speeds up, drifting sideways, turning, shrinking and fading.
                var path = DOTween.Sequence().SetUpdate(_ignoreTimeScale).SetTarget(rect).SetDelay(delay);
                path.AppendCallback(() => drop.color = Color.white);
                path.Append(DOTween.To(() => 0f, k =>
                {
                    float hop = Mathf.Sin(Mathf.Clamp01(k * 3) * Mathf.PI) * _settings.Hop * (k < 1f / 3 ? 1 : 0);
                    var p = Vector2.Lerp(start, end, k * k);
                    p.x = Mathf.Lerp(start.x, end.x, k);
                    rect.anchoredPosition = p + Vector2.up * hop;
                    rect.localScale = Vector3.one * Mathf.Lerp(_settings.StartScale, _settings.EndScale, k);
                    rect.localRotation = Quaternion.Euler(0, 0, Mathf.Sign(side) * -_settings.Spin * k);
                    drop.color = new Color(1, 1, 1, k < .6f ? 1 : 1 - (k - .6f) / .4f);
                }, 1, time).SetEase(Ease.Linear));
                path.OnComplete(() => drop.gameObject.SetActive(false));
            }
        }
        private Image Take(RectTransform parent)
        {
            foreach (var icon in _pool) if (icon != null && !icon.gameObject.activeSelf) { icon.rectTransform.SetParent(parent, false); icon.gameObject.SetActive(true); return icon; }
            var made = new GameObject("Img_SpentIcon", typeof(RectTransform)).AddComponent<Image>();
            made.gameObject.layer = gameObject.layer;
            made.rectTransform.SetParent(parent, false);
            made.raycastTarget = false;
            _pool.Add(made);
            return made;
        }

        private void StopCount()
        {
            if (_count != null && _count.IsActive()) _count.Kill();
            if (_punch != null && _punch.IsActive()) _punch.Kill();
            _count = _punch = null;
            if (_text != null) _text.rectTransform.localScale = _scale;
            _counting = false;
        }
        /// <summary>Ends a count wherever it is: the text shows what it was heading for.</summary>
        private void Finish()
        {
            bool counting = _counting;
            string target = _target;
            StopCount();
            if (!counting || target == null || _text == null || !Number(target, out long value)) return;
            Write(target, value);
            _shown = value;
        }

        private static bool Number(string text, out long value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text)) return false;
            var match = NUMBER.Match(text);
            return match.Success && long.TryParse(match.Value, out value);
        }
        private void Write(string template, long value)
        {
            _writing = true;
            _text.text = _written = NUMBER.Replace(template, value.ToString(), 1);
            _writing = false;
        }
        private void OnDestroy() => StopCount();
    }
}
