using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.LoadingModule.Shared.Data.ValueObjects;
using Modules.LoadingModule.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.LoadingModule.LoadingScreenModule.ViewsMediators
{
    /// <summary>
    /// The big bar. It draws whatever snapshot it is handed and remembers only which set that was;
    /// the smoothing towards the target is the one piece of motion it owns. The art behind it is
    /// the one thing not reset between openings: what the addressable load brought is the same for
    /// every set, so it stays on the pooled instance.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class LoadingScreenView : ScreenView
    {
        private const float SMOOTH_SPEED = 8f;

        [Tooltip("Off: the bar, the percentage and the step texts stay hidden, and the message line reads Loading Text.")]
        [SerializeField] private bool _showProgress = true;
        [Tooltip("With Show Progress off: the text under the art, by localisation key; the fallback when the key has no text yet.")]
        [SerializeField] private string _loadingKey = "loading.loading";
        [SerializeField] private string _loadingFallback = "Loading...";

        [SerializeField] private Image _background;
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _percent;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private TMP_Text _detail;

        [SerializeField] private GameObject _childGroup;
        [SerializeField] private Image _childFill;
        [SerializeField] private TMP_Text _childMessage;
        [SerializeField] private TMP_Text _childDetail;

        [SerializeField] private GameObject _failedGroup;
        [SerializeField] private TMP_Text _failedText;
        [SerializeField] private Button _retryButton;

        [Tooltip("Shows the build's version (Player Settings > Version) in a corner. Empty shows none.")]
        [SerializeField] private TMP_Text _version;

        private float _target;
        private float _shown;
        private float _childTarget;
        private float _childShown;

        public Action Retry;

        private Func<string, string> _localize;

        /// <summary>The function that turns a key into text in the language in use (ILocalizationService.Get).</summary>
        public void SetLocalizer(Func<string, string> localize)
        {
            _localize = localize;
            ShowLoadingText();
        }

        private void ShowLoadingText()
        {
            if (_showProgress) return;
            string text = _localize?.Invoke(_loadingKey);
            _message.text = string.IsNullOrEmpty(text) || text == _loadingKey ? _loadingFallback : text;
        }

        /// <summary>The set this screen is drawing, or null before the first Apply.</summary>
        public string ShowingSet { get; private set; }

        private void OnEnable() => _retryButton.onClick.AddListener(RetryClicked);

        private void OnDisable() => _retryButton.onClick.RemoveListener(RetryClicked);

        public void RetryClicked() => Retry?.Invoke();

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();

            ShowingSet = null;
            if (_version != null) _version.text = "v" + Application.version;
            _target = _shown = 0f;
            _childTarget = _childShown = 0f;
            SetFill(_fill, 0f);
            SetFill(_childFill, 0f);
            _percent.text = "0%";
            _message.text = string.Empty;
            _detail.text = string.Empty;
            _childGroup.SetActive(false);
            _failedGroup.SetActive(false);
            _fill.transform.parent.gameObject.SetActive(_showProgress);
            _percent.gameObject.SetActive(_showProgress);
            _detail.gameObject.SetActive(_showProgress);
            ShowLoadingText();
        }

        public void Apply(LoadingSetStatusRVO status)
        {
            ShowingSet = status.Set;
            _target = status.Progress;
            if (!_showProgress)
            {
                if (status.State == LoadingSetState.Running) _failedGroup.SetActive(false);
                return;
            }
            _message.text = status.Message ?? string.Empty;
            _detail.text = status.Detail ?? string.Empty;

            _childGroup.SetActive(status.HasChild);
            if (status.HasChild)
            {
                _childTarget = status.ChildProgress;
                _childMessage.text = status.ChildMessage ?? string.Empty;
                _childDetail.text = status.ChildDetail ?? string.Empty;
            }

            if (status.State == LoadingSetState.Running)
                _failedGroup.SetActive(false);
        }

        public void ShowFailed(string step)
        {
            _failedText.text = $"Loading stopped at {step}.";
            _failedGroup.SetActive(true);
        }

        public void HideFailed() => _failedGroup.SetActive(false);

        /// <summary>The addressable art, over the sprite the prefab bundles.</summary>
        public void ShowBackground(Sprite background) => _background.sprite = background;

        private void Update()
        {
            if (!_showProgress) return;
            _shown = Approach(_shown, _target);
            SetFill(_fill, _shown);
            _percent.text = $"{Mathf.RoundToInt(_shown * 100f)}%";

            if (!_childGroup.activeSelf) return;
            _childShown = Approach(_childShown, _childTarget);
            SetFill(_childFill, _childShown);
        }

        // The fill is a plain quad stretched to a share of the track - an Image with no sprite
        // draws its whole rect whatever its fill amount says, so the width is the progress.
        private static void SetFill(Image fill, float amount) =>
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);

        private static float Approach(float shown, float target)
        {
            if (Mathf.Abs(target - shown) < 0.002f) return target;
            return Mathf.Lerp(shown, target, SMOOTH_SPEED * Time.unscaledDeltaTime);
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
