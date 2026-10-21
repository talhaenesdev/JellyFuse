using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.Shared.Enums;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.PauseScreenModule.ViewsMediators
{
    /// <summary>
    /// The pause panel over the game: a title, the sounds, music and haptics switches, Resume and Quit.
    /// The panel is the shared popup (UiWidgets Pnl_Popup), which fits itself to the screen.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public sealed class PauseScreenView : ScreenView
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _soundsLabel;
        [SerializeField] private TMP_Text _musicLabel;
        [SerializeField] private TMP_Text _hapticsLabel;
        [SerializeField] private TMP_Text _resumeLabel;
        [SerializeField] private TMP_Text _quitLabel;

        [Header("Controls")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _quitButton;
        [Tooltip("Off / On switches: a tap or a swipe asks for a change, and each slides when the setting has changed.")]
        [SerializeField] private SlideSwitch _soundsSwitch;
        [SerializeField] private SlideSwitch _musicSwitch;
        [SerializeField] private SlideSwitch _hapticsSwitch;

        /// <summary>Resume, or the close button: both carry on playing.</summary>
        public Action ResumeClicked;
        public Action QuitClicked;
        public Action<SettingsOption> OptionClicked;

        private Func<string, string> _localize;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnResumeClicked);
            _resumeButton.onClick.AddListener(OnResumeClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
            _soundsSwitch.Requested += OnSoundsClicked;
            _musicSwitch.Requested += OnMusicClicked;
            _hapticsSwitch.Requested += OnHapticsClicked;
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnResumeClicked);
            _resumeButton.onClick.RemoveListener(OnResumeClicked);
            _quitButton.onClick.RemoveListener(OnQuitClicked);
            _soundsSwitch.Requested -= OnSoundsClicked;
            _musicSwitch.Requested -= OnMusicClicked;
            _hapticsSwitch.Requested -= OnHapticsClicked;
        }

        public void SetLocalizer(Func<string, string> localize)
        {
            _localize = localize;
            RefreshTexts();
        }

        public void ShowOption(SettingsOption option, bool enabled)
        {
            SlideSwitch toggle = option switch
            {
                SettingsOption.Sounds => _soundsSwitch,
                SettingsOption.Music => _musicSwitch,
                SettingsOption.Haptics => _hapticsSwitch,
                _ => null,
            };

            if (toggle != null) toggle.SetOn(enabled);
        }

        private void RefreshTexts()
        {
            _title.text = L("pause.title", "Pause");
            _soundsLabel.text = L("settings.sounds", "Sounds") + ":";
            _musicLabel.text = L("settings.music", "Music") + ":";
            _hapticsLabel.text = L("settings.haptics", "Haptics") + ":";
            _resumeLabel.text = L("pause.resume", "Resume");
            _quitLabel.text = L("pause.quit", "Quit");
        }

        private string L(string key, string fallback)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        private void OnResumeClicked() => ResumeClicked?.Invoke();
        private void OnQuitClicked() => QuitClicked?.Invoke();
        // The switch says which state it was asked for; the setting toggles, so only the request counts.
        private void OnSoundsClicked(bool on) => OptionClicked?.Invoke(SettingsOption.Sounds);
        private void OnMusicClicked(bool on) => OptionClicked?.Invoke(SettingsOption.Music);
        private void OnHapticsClicked(bool on) => OptionClicked?.Invoke(SettingsOption.Haptics);

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
