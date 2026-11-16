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

        [Header("Quit confirmation")]
        [Tooltip("The pause card. Quit swaps it for the confirmation card; staying swaps it back.")]
        [SerializeField] private GameObject _pauseDialog;
        [Tooltip("Asks before leaving: leaving a level still in play costs a life.")]
        [SerializeField] private GameObject _quitDialog;
        [SerializeField] private TMP_Text _quitTitle;
        [SerializeField] private TMP_Text _quitBody;
        [SerializeField] private TMP_Text _quitStayLabel;
        [SerializeField] private TMP_Text _quitConfirmLabel;
        [SerializeField] private Button _quitStayButton;
        [SerializeField] private Button _quitCloseButton;
        [SerializeField] private Button _quitConfirmButton;

        /// <summary>Resume, or the close button: both carry on playing.</summary>
        public Action ResumeClicked;
        public Action QuitClicked;
        public Action<SettingsOption> OptionClicked;

        private Func<string, string> _localize;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnResumeClicked);
            _resumeButton.onClick.AddListener(OnResumeClicked);
            _quitButton.onClick.AddListener(ShowQuitConfirm);
            _quitStayButton.onClick.AddListener(ShowPause);
            _quitCloseButton.onClick.AddListener(ShowPause);
            _quitConfirmButton.onClick.AddListener(OnQuitClicked);
            _soundsSwitch.Requested += OnSoundsClicked;
            _musicSwitch.Requested += OnMusicClicked;
            _hapticsSwitch.Requested += OnHapticsClicked;
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnResumeClicked);
            _resumeButton.onClick.RemoveListener(OnResumeClicked);
            _quitButton.onClick.RemoveListener(ShowQuitConfirm);
            _quitStayButton.onClick.RemoveListener(ShowPause);
            _quitCloseButton.onClick.RemoveListener(ShowPause);
            _quitConfirmButton.onClick.RemoveListener(OnQuitClicked);
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
            _quitTitle.text = L("pause.quitconfirm.title", "Leave Level?");
            _quitBody.text = L("pause.quitconfirm.body", "If you leave now, you will lose a life.");
            _quitStayLabel.text = L("pause.quitconfirm.stay", "Keep Playing");
            _quitConfirmLabel.text = L("pause.quit", "Quit");
        }

        /// <summary>The pooled screen opens on the pause card, whatever the last opening left showing.</summary>
        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            _quitDialog.SetActive(false);
            _pauseDialog.SetActive(true);
        }

        // Swapping the two cards decides nothing; leaving is still the confirm button's Quit.
        private void ShowQuitConfirm() { _pauseDialog.SetActive(false); _quitDialog.SetActive(true); }
        private void ShowPause() { _quitDialog.SetActive(false); _pauseDialog.SetActive(true); }

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
        protected override void PlayHideAnimation()
        {
            // The panel shrinks and fades out first; the screen is hidden once it has.
            var panel = GetComponentInChildren<PanelAnimator>();
            if (panel != null) panel.Close(() => HideCompleted?.Invoke(this));
            else HideCompleted?.Invoke(this);
        }
    }
}
