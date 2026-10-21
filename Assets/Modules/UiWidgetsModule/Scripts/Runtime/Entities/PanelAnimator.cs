using System.Collections;
using DG.Tweening;
using Modules.UiWidgetsModule.Shared.Data.UnityObjects;
using Modules.UiWidgetsModule.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Plays a panel's opening each time it is shown, with DOTween, as CD_PanelAnimation says: it pops, slides
    /// or fades in, and the dim behind it fades in with it. The dim is found by itself: an Image on the panel's
    /// parent, or a sibling named "Dim". Put it on a panel's root - on Pnl_Popup it reaches every popup made from
    /// it. It works with PopupFitter: the fitted size is what it grows to.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PanelAnimator : MonoBehaviour
    {
        [Tooltip("How it opens. Empty: it shows at once.")]
        [SerializeField] private CD_PanelAnimation _settings;
        [Tooltip("Optional: the dim behind the panel. Empty: an Image on the parent, or a sibling named Dim.")]
        [SerializeField] private Graphic _dim;
        [Tooltip("On: runs on real time, while the game is paused too.")]
        [SerializeField] private bool _ignoreTimeScale = true;

        private RectTransform _rect;
        private PopupFitter _fitter;
        private CanvasGroup _group;
        private Sequence _sequence;
        private Coroutine _queuedOpen;
        private Vector3 _scale;
        private Vector2 _position;
        private float _dimAlpha = -1;

        public CD_PanelAnimation Settings { get => _settings; set => _settings = value; }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _fitter = GetComponent<PopupFitter>();
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            if (_dim == null) _dim = FindDim();
            if (_dim != null) _dimAlpha = _dim.color.a;
        }
        private Graphic FindDim()
        {
            var parent = transform.parent;
            if (parent == null) return null;
            var image = parent.GetComponent<Image>();
            if (image != null) return image;
            foreach (Transform sibling in parent)
                if (sibling != transform && sibling.name.Contains("Dim") && sibling.TryGetComponent<Graphic>(out var graphic)) return graphic;
            return null;
        }

        // A pooled screen is enabled before ScreenManager has finished parenting and laying it out. PopupFitter
        // therefore reaches its final scale in LateUpdate. Start on the following frame so the tween grows to
        // that fitted scale instead of the smaller scale the prefab briefly had during its first opening.
        private void OnEnable()
        {
            if (_settings == null || _settings.Duration <= 0) { Open(); return; }
            _group.alpha = 0;
            _group.interactable = false;
            if (_dim != null && _dimAlpha > 0) SetDim(0);
            _queuedOpen = StartCoroutine(OpenAfterLayout());
        }

        private IEnumerator OpenAfterLayout()
        {
            yield return null;
            _queuedOpen = null;
            RestoreVisuals();
            Open();
        }

        private void OnDisable()
        {
            CancelQueuedOpen();
            Settle();
            RestoreVisuals();
        }

        /// <summary>Plays the opening from the start.</summary>
        public void Open()
        {
            CancelQueuedOpen();
            Settle();
            if (_settings == null || _settings.Duration <= 0) return;
            // ScreenManager may have reparented the pooled popup after PopupFitter.OnEnable. Fit against
            // its final parent now, before the tween captures the scale it must finish at.
            _fitter?.Fit();
            _scale = _rect.localScale; _position = _rect.anchoredPosition;
            var s = _settings;
            _sequence = DOTween.Sequence().SetUpdate(_ignoreTimeScale).SetTarget(this).SetDelay(s.Delay);
            if (s.BlockInputWhileOpening) _group.interactable = false;
            switch (s.Style)
            {
                case PanelOpenStyle.Pop:
                    _rect.localScale = _scale * s.StartScale;
                    _sequence.Insert(0, _rect.DOScale(_scale, s.Duration).SetEase(s.Ease));
                    break;
                case PanelOpenStyle.SlideUp:
                case PanelOpenStyle.SlideDown:
                {
                    var from = _position + Vector2.up * (s.Style == PanelOpenStyle.SlideUp ? -s.SlideDistance : s.SlideDistance);
                    _rect.anchoredPosition = from;
                    _sequence.Insert(0, DOTween.To(() => _rect.anchoredPosition, p => _rect.anchoredPosition = p, _position, s.Duration).SetEase(s.Ease));
                    break;
                }
            }
            if (s.FadeSeconds > 0 || s.Style == PanelOpenStyle.Fade)
            {
                _group.alpha = 0;
                float fade = s.Style == PanelOpenStyle.Fade ? s.Duration : Mathf.Min(s.FadeSeconds, s.Duration);
                _sequence.Insert(0, DOTween.To(() => _group.alpha, a => _group.alpha = a, 1, fade).SetEase(Ease.OutQuad));
            }
            if (_dim != null && s.DimSeconds > 0 && _dimAlpha > 0)
            {
                SetDim(0);
                _sequence.Insert(0, DOTween.To(() => _dim.color.a, a => SetDim(a), _dimAlpha, s.DimSeconds).SetEase(Ease.OutQuad));
            }
            _sequence.OnComplete(() => { _group.interactable = true; _sequence = null; });
        }

        /// <summary>Ends an opening wherever it is: the panel stands as it was made.</summary>
        private void Settle()
        {
            if (_sequence == null) return;
            if (_sequence.IsActive()) _sequence.Kill();
            _sequence = null;
            _rect.localScale = _scale; _rect.anchoredPosition = _position;
            _group.alpha = 1; _group.interactable = true;
            if (_dim != null && _dimAlpha >= 0) SetDim(_dimAlpha);
        }
        private void CancelQueuedOpen()
        {
            if (_queuedOpen == null) return;
            StopCoroutine(_queuedOpen);
            _queuedOpen = null;
        }
        private void RestoreVisuals()
        {
            if (_group == null) return;
            _group.alpha = 1;
            _group.interactable = true;
            if (_dim != null && _dimAlpha >= 0) SetDim(_dimAlpha);
        }
        private void SetDim(float alpha) { var c = _dim.color; c.a = alpha; _dim.color = c; }
        private void OnDestroy() { CancelQueuedOpen(); if (_sequence != null && _sequence.IsActive()) _sequence.Kill(); }
    }
}
