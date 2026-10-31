using DG.Tweening;
using Modules.UiWidgetsModule.Enums;
using UnityEngine;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Keeps an icon alive while it is on screen - a heart that beats, a coin that turns - with a rest
    /// between loops. Sits on the icon's Image; it draws nothing and decides nothing. Starts on enable
    /// and stops on disable, so a pooled screen opens with the icon at its own size every time.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class IconIdleAnimator : MonoBehaviour
    {
        [SerializeField] private IconIdleStyle _style = IconIdleStyle.HeartBeat;
        [Tooltip("Seconds of rest between two loops.")]
        [SerializeField, Min(0f)] private float _rest = 1.6f;
        [Tooltip("HeartBeat: how much bigger a beat gets. CoinFlip: unused.")]
        [SerializeField, Range(0f, .5f)] private float _strength = .14f;
        [Tooltip("Starts each icon at a random point of its rest, so icons on one screen do not move together.")]
        [SerializeField] private bool _randomStart = true;

        private Vector3 _restScale;
        private Vector2 _restPosition;
        private Quaternion _restRotation;
        private Sequence _loop;

        private void OnEnable() => Play();
        private void OnDisable() => Stop();

        private void Play()
        {
            _restScale = transform.localScale;
            var rect = (RectTransform)transform;
            _restPosition = rect.anchoredPosition;
            _restRotation = transform.localRotation;
            _loop = _style switch
            {
                IconIdleStyle.CoinFlip => CoinFlip(),
                IconIdleStyle.AvatarFloat => AvatarFloat(rect),
                _ => HeartBeat()
            };
            _loop.AppendInterval(_rest).SetLoops(-1).SetUpdate(true).SetTarget(transform);
            if (_randomStart) _loop.Goto(Random.value * _loop.Duration(false), true);
        }

        private void Stop()
        {
            _loop?.Kill();
            _loop = null;
            transform.localScale = _restScale;
            transform.localRotation = _restRotation;
            ((RectTransform)transform).anchoredPosition = _restPosition;
        }

        public void Configure(IconIdleStyle style, float rest = 0f, float strength = .08f, bool randomStart = true)
        {
            _style = style;
            _rest = Mathf.Max(0, rest);
            _strength = Mathf.Clamp(strength, 0, .5f);
            _randomStart = randomStart;
            if (!isActiveAndEnabled) return;
            Stop();
            Play();
        }

        private Sequence HeartBeat()
        {
            Vector3 big = _restScale * (1f + _strength);
            Vector3 small = _restScale * (1f + _strength * .55f);
            return DOTween.Sequence()
                .Append(ScaleTo(big, .11f, Ease.OutQuad))
                .Append(ScaleTo(_restScale, .12f, Ease.InQuad))
                .Append(ScaleTo(small, .1f, Ease.OutQuad))
                .Append(ScaleTo(_restScale, .22f, Ease.OutBack));
        }

        // A turn is the width running to nothing and back, twice - the far face shows mirrored,
        // which a round coin hides - with a small hop so it reads as a toss rather than a squash.
        private Sequence CoinFlip()
        {
            Vector3 edge = new(0f, _restScale.y, _restScale.z);
            Vector3 hop = _restScale * 1.08f;
            return DOTween.Sequence()
                .Append(ScaleTo(hop, .1f, Ease.OutQuad))
                .Append(ScaleTo(edge, .12f, Ease.InSine))
                .Append(ScaleTo(new Vector3(-_restScale.x, _restScale.y, _restScale.z), .12f, Ease.OutSine))
                .Append(ScaleTo(edge, .12f, Ease.InSine))
                .Append(ScaleTo(hop, .12f, Ease.OutSine))
                .Append(ScaleTo(_restScale, .18f, Ease.OutBack));
        }

        private Sequence AvatarFloat(RectTransform rect)
        {
            float amount = Mathf.Max(.02f, _strength);
            Vector3 inhale = new(_restScale.x * (1f + amount * .45f), _restScale.y * (1f + amount), _restScale.z);
            Vector3 exhale = new(_restScale.x * (1f + amount * .2f), _restScale.y * (1f - amount * .2f), _restScale.z);
            Vector2 high = _restPosition + Vector2.up * Mathf.Lerp(1.5f, 4f, amount / .5f);
            return DOTween.Sequence()
                .Append(AvatarPose(rect, inhale, high, 0, 1.4f, 1.15f, Ease.InOutSine))
                .Append(AvatarPose(rect, exhale, _restPosition, 1.4f, -1.1f, 1.15f, Ease.InOutSine))
                .Append(AvatarPose(rect, _restScale, _restPosition, -1.1f, 0, .65f, Ease.OutSine));
        }

        private Sequence AvatarPose(RectTransform rect, Vector3 scale, Vector2 position, float fromAngle, float toAngle, float seconds, Ease ease)
        {
            float angle = fromAngle;
            return DOTween.Sequence()
                .Append(ScaleTo(scale, seconds, ease))
                .Join(DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, position, seconds).SetEase(ease))
                .Join(DOTween.To(() => angle, value =>
                {
                    angle = value;
                    transform.localRotation = _restRotation * Quaternion.Euler(0, 0, value);
                }, toAngle, seconds).SetEase(ease));
        }

        // DOTween's core API only, so no DOTween module assembly is needed.
        private Tween ScaleTo(Vector3 to, float seconds, Ease ease)
        {
            var target = transform;
            return DOTween.To(() => target.localScale, s => target.localScale = s, to, seconds).SetEase(ease);
        }
    }
}
