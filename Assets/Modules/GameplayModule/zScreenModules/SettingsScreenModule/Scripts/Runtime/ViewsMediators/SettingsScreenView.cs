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
        [Tooltip("When set, Design Width and the margins come from its config, and the objects it lists take its places.")]
        [SerializeField] private ScreenLayout _layout;

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

        [Header("Language")]
        [SerializeField] private TMP_Text _languageLabel;
        [Tooltip("Shows the language in use, in its own name; a tap moves on to the next one offered.")]
        [SerializeField] private Button _languageButton;
        [SerializeField] private TMP_Text _languageValue;

        [Header("Privacy")]
        [SerializeField] private TMP_Text _privacyLabel;
        [Tooltip("Asks the consent question again, so the player can change the answer.")]
        [SerializeField] private Button _privacyButton;
        [SerializeField] private TMP_Text _privacyValue;

        [Header("Promo code")]
        [SerializeField] private TMP_Text _promoLabel;
        [SerializeField] private Button _promoButton;
        [SerializeField] private TMP_Text _promoValue;
        [Tooltip("The panel the code is typed into, over the settings.")]
        [SerializeField] private GameObject _promoPanel;
        [SerializeField] private TMP_Text _promoTitle;
        [SerializeField] private TMP_InputField _promoInput;
        [SerializeField] private Button _promoSubmit;
        [SerializeField] private TMP_Text _promoSubmitLabel;
        [SerializeField] private Button _promoClose;
        [SerializeField] private TMP_Text _promoMessage;
        [SerializeField] private Color _promoGood = new(.2f, .65f, .25f, 1f);
        [SerializeField] private Color _promoBad = new(.85f, .25f, .3f, 1f);

        public Action<string> LanguagePicked;
        public Action CloseClicked;
        public Action PrivacyClicked;
        public Action<string> PromoSubmitted;
        public Action<SettingsOption> OptionClicked;

        private Func<string, string> _localize;
        private IReadOnlyList<LanguageCVO> _languages;
        private string _language;

        private void OnEnable()
        {
            if (_layout != null) _layout.Changed += FitDialogToSafeArea;
            _closeButton.onClick.AddListener(OnCloseClicked);
            _notificationsSwitch.Requested += OnNotificationsClicked;
            _soundsSwitch.Requested += OnSoundsClicked;
            _musicSwitch.Requested += OnMusicClicked;
            _hapticsSwitch.Requested += OnHapticsClicked;
            if (_languageButton != null) _languageButton.onClick.AddListener(OnLanguageClicked);
            if (_privacyButton != null) _privacyButton.onClick.AddListener(OnPrivacyClicked);
            if (_promoButton != null) _promoButton.onClick.AddListener(OpenPromo);
            if (_promoSubmit != null) _promoSubmit.onClick.AddListener(OnPromoSubmit);
            if (_promoClose != null) _promoClose.onClick.AddListener(ClosePromo);
        }

        private void OnDisable()
        {
            if (_layout != null) _layout.Changed -= FitDialogToSafeArea;
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _notificationsSwitch.Requested -= OnNotificationsClicked;
            _soundsSwitch.Requested -= OnSoundsClicked;
            _musicSwitch.Requested -= OnMusicClicked;
            _hapticsSwitch.Requested -= OnHapticsClicked;
            if (_languageButton != null) _languageButton.onClick.RemoveListener(OnLanguageClicked);
            if (_privacyButton != null) _privacyButton.onClick.RemoveListener(OnPrivacyClicked);
            if (_promoButton != null) _promoButton.onClick.RemoveListener(OpenPromo);
            if (_promoSubmit != null) _promoSubmit.onClick.RemoveListener(OnPromoSubmit);
            if (_promoClose != null) _promoClose.onClick.RemoveListener(ClosePromo);
            if (_promoPanel != null) _promoPanel.SetActive(false);
        }

        public void ShowLanguages(IReadOnlyList<LanguageCVO> languages, string current, Func<string, string> localize)
        {
            _localize = localize;
            _languages = languages;
            _language = current;
            RefreshTexts();
        }

        public void ShowCurrent(string code)
        {
            _language = code;
            RefreshTexts();
        }

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

            var config = _layout != null ? _layout.Config : null;
            float designWidth = config != null ? config.DesignWidth : _designWidth;
            float horizontalMargin = config != null ? config.HorizontalMargin : _horizontalMargin;
            float verticalMargin = config != null ? config.VerticalMargin : _verticalMargin;
            Rect safe = Screen.safeArea;
            float width = safe.width / Screen.width * root.rect.width - horizontalMargin * 2;
            float height = safe.height / Screen.height * root.rect.height - verticalMargin * 2;
            float scale = Mathf.Max(.1f, width / designWidth);
            if (config != null && config.MaxScale > 0) scale = Mathf.Min(scale, config.MaxScale);
            _dialog.anchorMin = _dialog.anchorMax = _dialog.pivot = new Vector2(.5f, .5f);
            _dialog.localScale = Vector3.one * scale;
            _dialog.sizeDelta = new Vector2(designWidth, height / scale);
            if (_layout != null) _layout.SetDirty();

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
            if (_languageLabel != null) _languageLabel.text = L("settings.language", "Language") + ":";
            if (_languageValue != null) _languageValue.text = LanguageAt(IndexOf(_language))?.Name ?? _language;
            if (_privacyLabel != null) _privacyLabel.text = L("settings.privacy", "Privacy") + ":";
            if (_privacyValue != null) _privacyValue.text = L("settings.privacy_change", "Change");
            if (_promoLabel != null) _promoLabel.text = L("settings.promo", "Promo code") + ":";
            if (_promoValue != null) _promoValue.text = L("settings.promo_enter", "Enter");
            if (_promoTitle != null) _promoTitle.text = L("promo.title", "Promo Code");
            if (_promoSubmitLabel != null) _promoSubmitLabel.text = L("promo.redeem", "REDEEM");
            if (_promoInput != null && _promoInput.placeholder is TMP_Text hint) hint.text = L("promo.hint", "Type your code");
        }

        private bool _promoWaiting;

        private void OpenPromo()
        {
            if (_promoPanel == null) return;
            _promoPanel.SetActive(true);
            _promoWaiting = false;
            if (_promoInput != null) _promoInput.SetTextWithoutNotify(string.Empty);
            ShowPromoMessage(string.Empty, true);
            SetPromoBusy(false);
        }

        private void ClosePromo()
        {
            if (_promoPanel != null) _promoPanel.SetActive(false);
        }

        private void OnPromoSubmit()
        {
            string code = _promoInput != null ? _promoInput.text.Trim() : string.Empty;
            if (_promoWaiting) return;
            if (code.Length == 0)
            {
                ShowPromoMessage(L("promo.empty", "Type a code first."), false);
                return;
            }
            _promoWaiting = true;
            SetPromoBusy(true);
            ShowPromoMessage(L("promo.checking", "Checking..."), true);
            PromoSubmitted?.Invoke(code);
        }

        /// <summary>The code's answer: what it paid, or why it paid nothing.</summary>
        public void ShowPromoResult(Modules.GameplayModule.Shared.Data.ValueObjects.PromoResultRVO result)
        {
            _promoWaiting = false;
            SetPromoBusy(false);
            if (result == null) return;
            if (!result.Ok)
            {
                string why = result.Reason switch
                {
                    "expired" => L("promo.expired", "This code has expired."),
                    "used_up" => L("promo.used_up", "This code has been used up."),
                    "already" => L("promo.already", "You have already used this code."),
                    "offline" => L("promo.offline", "No connection. Try again."),
                    _ => L("promo.invalid", "This code is not valid."),
                };
                ShowPromoMessage(why, false);
                return;
            }
            var parts = new List<string>();
            if (result.Coins > 0) parts.Add(string.Format(L("promo.coins", "{0} coins"), result.Coins));
            if (result.Lives > 0) parts.Add(string.Format(L("promo.lives", "{0} lives"), result.Lives));
            if (result.Boosters > 0) parts.Add(string.Format(L("promo.boosters", "{0} boosters"), result.Boosters));
            if (result.Items.Count > 0) parts.Add(string.Format(L("promo.items", "{0} new items"), result.Items.Count));
            ShowPromoMessage(L("promo.success", "Code redeemed!") + (parts.Count > 0 ? "\n" + string.Join(", ", parts) : string.Empty), true);
            if (_promoInput != null) _promoInput.SetTextWithoutNotify(string.Empty);
        }

        private void ShowPromoMessage(string text, bool good)
        {
            if (_promoMessage == null) return;
            _promoMessage.text = text;
            _promoMessage.color = good ? _promoGood : _promoBad;
        }

        private void SetPromoBusy(bool busy)
        {
            if (_promoSubmit != null) _promoSubmit.interactable = !busy;
        }

        private int IndexOf(string code)
        {
            if (_languages != null)
                for (int i = 0; i < _languages.Count; i++)
                    if (_languages[i].Code == code) return i;
            return -1;
        }

        private LanguageCVO LanguageAt(int index) => _languages != null && index >= 0 && index < _languages.Count ? _languages[index] : null;

        // Raw input: a tap asks for the language after the one in use, round to the first.
        private void OnLanguageClicked()
        {
            if (_languages == null || _languages.Count < 2) return;
            var next = LanguageAt((IndexOf(_language) + 1) % _languages.Count);
            if (next != null) LanguagePicked?.Invoke(next.Code);
        }

        private string L(string key, string fallback)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        private void OnCloseClicked() => CloseClicked?.Invoke();
        private void OnPrivacyClicked() => PrivacyClicked?.Invoke();
        // The switch says which state it was asked for; the setting toggles, so only the request counts.
        private void OnNotificationsClicked(bool on) => OptionClicked?.Invoke(SettingsOption.Notifications);
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
