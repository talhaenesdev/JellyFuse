using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// An Off / On switch: a track with Off on its left and On on its right, and a knob that slides
    /// over the one in force - the Off picture on the left, the On picture on the right. A tap, or a
    /// swipe towards the other side, asks for a change through Requested; the switch moves only when
    /// told the new state through SetOn, so whoever owns the setting decides.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SlideSwitch : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("The part that slides: half the track's width, anchored to its left half.")]
        [SerializeField] private RectTransform _knob;
        [SerializeField] private Image _knobImage;
        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Sprite _offSprite;
        [Tooltip("The words under the knob's two places.")]
        [SerializeField] private TMP_Text _offLabel;
        [SerializeField] private TMP_Text _onLabel;
        [Tooltip("The word the knob sits on.")]
        [SerializeField] private Color _activeText = Color.white;
        [Tooltip("The word on the other side.")]
        [SerializeField] private Color _inactiveText = new Color(1, 1, 1, .45f);
        [Tooltip("Seconds the knob takes to slide across.")]
        [SerializeField, Min(0)] private float _slideTime = .15f;
        [Tooltip("How far a swipe must travel, in units, to count as one.")]
        [SerializeField, Min(0)] private float _swipeDistance = 12;

        /// <summary>The player asked for the other state: true for On.</summary>
        public event Action<bool> Requested;
        public bool IsOn { get; private set; }

        private bool _shown;
        private bool _dragging;
        private float _dragTravel;
        private Coroutine _slide;
        private RectTransform Track => (RectTransform)transform;

        /// <summary>Shows a state: sliding there when it has shown one already, at once the first time.</summary>
        public void SetOn(bool on, bool animate = true)
        {
            IsOn = on;
            if (_slide != null) { StopCoroutine(_slide); _slide = null; }
            if (animate && _shown && isActiveAndEnabled && _slideTime > 0) _slide = StartCoroutine(Slide(on));
            else Place(on ? 1 : 0);
            _shown = true;
        }

        private void OnEnable() => Place(IsOn ? 1 : 0);
        private void OnDisable() { _slide = null; _dragging = false; }

        private IEnumerator Slide(bool on)
        {
            float from = _knob.anchorMin.x * 2, to = on ? 1 : 0;
            for (float t = 0; t < _slideTime; t += Time.unscaledDeltaTime)
            {
                Place(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / _slideTime)));
                yield return null;
            }
            Place(to);
            _slide = null;
        }

        /// <summary>The knob at a share of its way from Off (0) to On (1); its picture turns at the middle.</summary>
        private void Place(float k)
        {
            if (_knob == null) return;
            _knob.anchorMin = new Vector2(k * .5f, 0);
            _knob.anchorMax = new Vector2(k * .5f + .5f, 1);
            _knob.offsetMin = _knob.offsetMax = Vector2.zero;
            bool on = k >= .5f;
            if (_knobImage != null) _knobImage.sprite = on ? _onSprite : _offSprite;
            if (_offLabel != null) _offLabel.color = on ? _inactiveText : _activeText;
            if (_onLabel != null) _onLabel.color = on ? _activeText : _inactiveText;
        }

        // ---------- input: a tap asks for the other state, a swipe for the side it heads to ----------

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_dragging) return;
            Requested?.Invoke(!IsOn);
        }
        public void OnBeginDrag(PointerEventData eventData) { _dragging = true; _dragTravel = 0; }
        public void OnDrag(PointerEventData eventData) => _dragTravel += eventData.delta.x / Mathf.Max(.01f, Track.lossyScale.x);
        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            if (Mathf.Abs(_dragTravel) < _swipeDistance) return;
            bool want = _dragTravel > 0;
            if (want != IsOn) Requested?.Invoke(want);
        }
    }
}
