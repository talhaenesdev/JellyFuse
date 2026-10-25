using DG.Tweening;
using Modules.UiWidgetsModule.Data.UnityObjects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Shrinks a button while it is held and springs it back when let go, from a CD_ButtonPress
    /// preset. Sits on the button beside its Button; it draws nothing and decides nothing, and the
    /// button's click works as before. Always returns to the scale the button had before the press.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ButtonPressAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Empty uses the defaults: 0.92, a quick shrink and a springy return.")]
        [SerializeField] private CD_ButtonPress _preset;

        private Selectable _selectable;
        private Vector3 _restScale;
        private bool _pressed;
        private Tween _tween;

        private void Awake() => _selectable = GetComponent<Selectable>();

        private void OnDisable()
        {
            // A screen hidden mid-press - or mid-spring back, which is every button that closes its
            // own screen: the click lands on the release - comes back from the pool at its own size.
            bool moving = _pressed || (_tween != null && _tween.IsActive());
            _tween?.Kill();
            _tween = null;
            if (moving) transform.localScale = _restScale;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            bool animateDisabled = _preset != null && _preset.AnimateWhenDisabled;
            if (_selectable != null && !_selectable.IsInteractable() && !animateDisabled) return;
            // Only a button at rest says what its size is; one still springing back is mid-way.
            bool atRest = !_pressed && (_tween == null || !_tween.IsActive());
            if (atRest) _restScale = transform.localScale;
            _pressed = true;
            Animate(_restScale * (_preset != null ? _preset.PressedScale : .92f), _preset != null ? _preset.PressDuration : .07f, _preset != null ? _preset.PressEase : Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed) return;
            _pressed = false;
            Animate(_restScale, _preset != null ? _preset.ReleaseDuration : .22f, _preset != null ? _preset.ReleaseEase : Ease.OutBack);
        }

        // DOTween's core API only, so no DOTween module assembly is needed.
        private void Animate(Vector3 to, float seconds, Ease ease)
        {
            _tween?.Kill();
            var target = transform;
            _tween = DOTween.To(() => target.localScale, s => target.localScale = s, to, seconds).SetEase(ease).SetUpdate(true).SetTarget(target);
        }
    }
}
