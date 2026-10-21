using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.Shared.Enums;
using Modules.LocalizationModule.Shared.Data.ValueObjects;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.SettingsScreenModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public sealed class SettingsScreenView : ScreenView
    {
        [Header("Layout")]
        [Tooltip("The whole screen's content, authored DesignWidth wide. It is scaled to the safe area's width and stretched to its height, so the screen fills the device.")]
        [SerializeField] private RectTransform _dialog;
        [Tooltip("The width the content is authored at, in units.")]
        [SerializeField, Min(1)] private float _designWidth = 460;
        [Tooltip("Space kept at the left and right of the safe area, in screen units.")]
        [SerializeField, Min(0)] private float _horizontalMargin;
        [Tooltip("Space kept at the top and bottom of the safe area, in screen units.")]
        [SerializeField, Min(0)] private float _verticalMargin;

        [Header("Header")]
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _closeButton;

        [Header("Rows")]
        [SerializeField] private TMP_Text _notificationsLabel;
        [SerializeField] private TMP_Text _soundsLabel;
        [SerializeField] private TMP_Text _musicLabel;
        [SerializeField] private TMP_Text _hapticsLabel;
        [Tooltip("Off / On switches: a tap or a swipe asks for a change, and each slides when the setting has changed.")]
        [SerializeField] private SlideSwitch _notificationsSwitch;
        [SerializeField] private SlideSwitch _soundsSwitch;
        [SerializeField] private SlideSwitch _musicSwitch;
        [SerializeField] private SlideSwitch _hapticsSwitch;

        public Action<string> LanguagePicked;
        public Action CloseClicked;
        public Action<SettingsOption> OptionClicked;

        private Func<string, string> _localize;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
            _notificationsSwitch.Requested += OnNotificationsClicked;
            _soundsSwitch.Requested += OnSoundsClicked;
            _musicSwitch.Requested += OnMusicClicked;
            _hapticsSwitch.Requested += OnHapticsClicked;
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _notificationsSwitch.Requested -= OnNotificationsClicked;
            _soundsSwitch.Requested -= OnSoundsClicked;
            _musicSwitch.Requested -= OnMusicClicked;
            _hapticsSwitch.Requested -= OnHapticsClicked;
        }

        public void ShowLanguages(IReadOnlyList<LanguageCVO> languages, string current, Func<string, string> localize)
        {
            _localize = localize;
            RefreshTexts();
        }

        public void ShowCurrent(string code) => RefreshTexts();

        public void ShowOption(SettingsOption option, bool enabled)
        {
            SlideSwitch toggle = option switch
            {
                SettingsOption.Notifications => _notificationsSwitch,
                SettingsOption.Sounds => _soundsSwitch,
                SettingsOption.Music => _musicSwitch,
                SettingsOption.Haptics => _hapticsSwitch,
                _ => null,
            };

            if (toggle != null) toggle.SetOn(enabled);
        }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            FitDialogToSafeArea();
        }

        public override void AfterScreenActivation()
        {
            base.AfterScreenActivation();
            FitDialogToSafeArea();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_dialog != null) FitDialogToSafeArea();
        }

        /// <summary>
        /// Full screen: the content is scaled so its authored width fills the safe area's width, and
        /// made as tall as the safe area at that scale. What is anchored to its top stays at the top,
        /// what is anchored to its bottom at the bottom, and what stretches fills the rest.
        /// </summary>
        private void FitDialogToSafeArea()
        {
            var root = (RectTransform)transform;
            if (root.rect.width <= 0 || root.rect.height <= 0 || Screen.width <= 0 || Screen.height <= 0) return;

            Rect safe = Screen.safeArea;
            float width = safe.width / Screen.width * root.rect.width - _horizontalMargin * 2;
            float height = safe.height / Screen.height * root.rect.height - _verticalMargin * 2;
            float scale = Mathf.Max(.1f, width / _designWidth);
            _dialog.anchorMin = _dialog.anchorMax = _dialog.pivot = new Vector2(.5f, .5f);
            _dialog.localScale = Vector3.one * scale;
            _dialog.sizeDelta = new Vector2(_designWidth, height / scale);

            Vector2 safeCenter = safe.center;
            Vector2 screenCenter = new(Screen.width * .5f, Screen.height * .5f);
            _dialog.anchoredPosition = new Vector2(
                (safeCenter.x - screenCenter.x) / Screen.width * root.rect.width,
                (safeCenter.y - screenCenter.y) / Screen.height * root.rect.height);
        }

        private void RefreshTexts()
        {
            _title.text = L("settings.title", "Settings");
            _notificationsLabel.text = L("settings.notifications", "Notifications") + ":";
            _soundsLabel.text = L("settings.sounds", "Sounds") + ":";
            _musicLabel.text = L("settings.music", "Music") + ":";
            _hapticsLabel.text = L("settings.haptics", "Haptics") + ":";
        }

        private string L(string key, string fallback)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        private void OnCloseClicked() => CloseClicked?.Invoke();
        // The switch says which state it was asked for; the setting toggles, so only the request counts.
        private void OnNotificationsClicked(bool on) => OptionClicked?.Invoke(SettingsOption.Notifications);
        private void OnSoundsClicked(bool on) => OptionClicked?.Invoke(SettingsOption.Sounds);
        private void OnMusicClicked(bool on) => OptionClicked?.Invoke(SettingsOption.Music);
        private void OnHapticsClicked(bool on) => OptionClicked?.Invoke(SettingsOption.Haptics);

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
