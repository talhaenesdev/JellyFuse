using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// A streak of light that sweeps across a button now and then, clipped to the button's rectangle. Builds its own
    /// mask and streak under this object the first time it is enabled; draws nothing else and takes no taps.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ShineSweep : MonoBehaviour
    {
        [SerializeField] private Sprite _streak;
        [SerializeField] private Color _color = new(1, 1, 1, .55f);
        [Tooltip("Seconds of rest between two sweeps.")]
        [SerializeField, Min(.2f)] private float _rest = 3f;
        [SerializeField, Min(.1f)] private float _sweepSeconds = .55f;
        [Tooltip("Inset of the clip from the button's edges, so the sweep stays inside rounded corners.")]
        [SerializeField] private Vector2 _inset = new(14, 10);

        private RectTransform _shine;
        private Sequence _loop;

        private void OnEnable()
        {
            if (_streak == null) return;
            if (_shine == null) Build();
            _shine.gameObject.SetActive(true);
            var area = ((RectTransform)_shine.parent).rect;
            float from = area.xMin - _shine.rect.width, to = area.xMax + _shine.rect.width;
            _shine.anchoredPosition = new Vector2(from, 0);
            _loop = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .AppendInterval(Random.Range(.5f, _rest))
                .Append(DOTween.To(() => _shine.anchoredPosition.x, x => _shine.anchoredPosition = new Vector2(x, 0), to, _sweepSeconds).SetEase(Ease.InOutSine))
                .AppendCallback(() => _shine.anchoredPosition = new Vector2(from, 0))
                .AppendInterval(_rest)
                .SetLoops(-1);
        }

        private void OnDisable()
        {
            _loop?.Kill();
            _loop = null;
            if (_shine != null) _shine.gameObject.SetActive(false);
        }

        private void Build()
        {
            var clip = new GameObject("Fx_ShineClip", typeof(RectTransform), typeof(RectMask2D), typeof(LayoutElement));
            clip.layer = gameObject.layer;
            clip.GetComponent<LayoutElement>().ignoreLayout = true;
            var clipRect = (RectTransform)clip.transform;
            clipRect.SetParent(transform, false);
            clipRect.SetSiblingIndex(0);
            clipRect.anchorMin = Vector2.zero; clipRect.anchorMax = Vector2.one;
            clipRect.offsetMin = _inset; clipRect.offsetMax = -_inset;

            var shine = new GameObject("Fx_Shine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shine.layer = gameObject.layer;
            _shine = (RectTransform)shine.transform;
            _shine.SetParent(clipRect, false);
            _shine.anchorMin = _shine.anchorMax = new Vector2(.5f, .5f);
            var height = clipRect.rect.height;
            _shine.sizeDelta = new Vector2(height * 1.1f, height * 1.6f);
            _shine.localEulerAngles = new Vector3(0, 0, 70);
            var image = shine.GetComponent<Image>();
            image.sprite = _streak;
            image.color = _color;
            image.raycastTarget = false;
        }
    }
}
