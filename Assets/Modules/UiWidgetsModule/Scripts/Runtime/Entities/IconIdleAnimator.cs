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
        private Sequence _loop;

        private void OnEnable()
        {
            _restScale = transform.localScale;
            _loop = _style == IconIdleStyle.CoinFlip ? CoinFlip() : HeartBeat();
            _loop.AppendInterval(_rest).SetLoops(-1).SetUpdate(true).SetTarget(transform);
            if (_randomStart) _loop.Goto(Random.value * _loop.Duration(false), true);
        }

        private void OnDisable()
        {
            _loop?.Kill();
            _loop = null;
            transform.localScale = _restScale;
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

        // DOTween's core API only, so no DOTween module assembly is needed.
        private Tween ScaleTo(Vector3 to, float seconds, Ease ease)
        {
            var target = transform;
            return DOTween.To(() => target.localScale, s => target.localScale = s, to, seconds).SetEase(ease);
        }
    }
}
