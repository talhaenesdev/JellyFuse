using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.LoginRewardsScreenModule.Entities;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.LoginRewardsScreenModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public class LoginRewardsScreenView : ScreenView
    {
        [Tooltip("The popup, authored at its own size and scaled to fit the screen.")]
        [SerializeField] private RectTransform _content;
        [SerializeField, Range(.3f, 1)] private float _widthShare = .9f;
        [SerializeField, Range(.3f, 1)] private float _heightShare = .82f;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backdropButton;
        [Tooltip("The day tiles in order, the seventh being the chest.")]
        [SerializeField] private LoginDayWidget[] _days;
        [SerializeField] private Button _claimButton;
        [SerializeField] private TMP_Text _claimLabel;
        [Tooltip("Turns behind today's tile while its reward is waiting.")]
        [SerializeField] private RectTransform _rays;

        public Action CloseClicked;
        public Action ClaimClicked;

        private Func<string, string> _localize;
        private RectTransform _raysTarget;

        private void OnRectTransformDimensionsChange() => FitLayout();

        private void FitLayout()
        {
            if (_content == null) return;
            var rect = ((RectTransform)transform).rect;
            var size = _content.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            _content.localScale = Vector3.one * Mathf.Max(.01f, Mathf.Min(rect.width * _widthShare / size.x, rect.height * _heightShare / size.y));
        }

        private void OnEnable()
        {
            FitLayout();
            _closeButton.onClick.AddListener(OnCloseClicked);
            _claimButton.onClick.AddListener(OnClaimClicked);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _claimButton.onClick.RemoveListener(OnClaimClicked);
            if (_backdropButton != null) _backdropButton.onClick.RemoveListener(OnCloseClicked);
        }

        public void SetLocalizer(Func<string, string> localize) => _localize = localize;

        public void ShowEvents(EventsRVO events)
        {
            _title.text = L("login.title", "Daily Rewards");
            _raysTarget = null;
            for (int i = 0; i < _days.Length; i++)
            {
                bool used = i < events.LoginDays.Count;
                _days[i].gameObject.SetActive(used);
                if (!used) continue;
                _days[i].Show(i + 1, events.LoginDays[i], L);
                if (events.LoginDays[i].State == Shared.Enums.LoginDayState.Today) _raysTarget = (RectTransform)_days[i].transform;
            }
            _claimButton.interactable = events.LoginClaimable;
            _claimLabel.text = events.LoginClaimable ? L("login.claim", "Claim") : L("login.come_back", "Come back tomorrow");
            if (_rays != null)
            {
                _rays.gameObject.SetActive(_raysTarget != null);
                if (_raysTarget != null) _rays.position = _raysTarget.position;
            }
        }

        private void Update()
        {
            if (_rays != null && _rays.gameObject.activeSelf) _rays.Rotate(0, 0, -30 * Time.unscaledDeltaTime);
        }

        private string L(string key, string fallback)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        private void OnCloseClicked() => CloseClicked?.Invoke();
        private void OnClaimClicked() => ClaimClicked?.Invoke();

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
