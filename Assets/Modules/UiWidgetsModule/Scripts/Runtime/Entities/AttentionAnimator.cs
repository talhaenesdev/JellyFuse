using DG.Tweening;
using UnityEngine;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Asks for a look now and then: the object hops and wiggles, rests, and does it again - while a notification dot
    /// is shown, or always when none is given. Moves position and rotation only, so a press animation that scales the
    /// same object is left alone. Draws nothing and decides nothing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class AttentionAnimator : MonoBehaviour
    {
        [Tooltip("Only while this is shown - a notification dot. Empty: always.")]
        [SerializeField] private GameObject _onlyWhile;
        [Tooltip("Seconds of rest between two hops.")]
        [SerializeField, Min(.2f)] private float _rest = 2.2f;
        [Tooltip("How high a hop goes, in units.")]
        [SerializeField] private float _hop = 14;
        [Tooltip("How far a wiggle turns, in degrees.")]
        [SerializeField] private float _wiggle = 9;

        private Sequence _loop;
        private Vector2 _restPosition;
        private Quaternion _restRotation;

        private void OnEnable()
        {
            _restPosition = ((RectTransform)transform).anchoredPosition;
            _restRotation = transform.localRotation;
        }

        private void OnDisable() => Stop();

        private void Update()
        {
            bool wanted = _onlyWhile == null || _onlyWhile.activeInHierarchy;
            if (wanted && _loop == null) Play();
            else if (!wanted && _loop != null) Stop();
        }

        private void Play()
        {
            var rect = (RectTransform)transform;
            float hopTime = .32f;
            _loop = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .AppendInterval(Random.Range(0, _rest))
                .Append(DOTween.To(() => rect.anchoredPosition, p => rect.anchoredPosition = p, _restPosition + Vector2.up * _hop, hopTime * .5f).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => rect.anchoredPosition, p => rect.anchoredPosition = p, _restPosition, hopTime * .5f).SetEase(Ease.InQuad))
                .Append(rect.DOLocalRotate(new Vector3(0, 0, _wiggle), .08f))
                .Append(rect.DOLocalRotate(new Vector3(0, 0, -_wiggle), .14f))
                .Append(rect.DOLocalRotate(new Vector3(0, 0, _wiggle * .5f), .1f))
                .Append(rect.DOLocalRotate(Vector3.zero, .08f))
                .AppendInterval(_rest)
                .SetLoops(-1);
        }

        private void Stop()
        {
            _loop?.Kill();
            _loop = null;
            ((RectTransform)transform).anchoredPosition = _restPosition;
            transform.localRotation = _restRotation;
        }
    }
}
