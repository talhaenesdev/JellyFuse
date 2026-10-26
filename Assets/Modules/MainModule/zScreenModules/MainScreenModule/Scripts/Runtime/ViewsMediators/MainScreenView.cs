using System;
using System.Collections.Generic;
using DG.Tweening;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Modules.MainModule.MainScreenModule.Data.UnityObjects;
using Modules.ProfileModule.Shared.Data.ValueObjects;
using Modules.UiWidgetsModule.Entities;

namespace Modules.MainModule.MainScreenModule.ViewsMediators
{
    /// <summary>
    /// The main menu. Everything is authored in the prefab (MainScreen), each object named for what it
    /// is: Txt_Coins top left, Btn_Settings top right, Btn_Play above Pnl_NavBar, and in the bar five
    /// tabs with Btn_Home in the middle. Img_NavSelector slides behind the tab picked. A text's English
    /// in the prefab is its fallback; its key reads the language in use.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class MainScreenView : ScreenView
    {
        [Tooltip("The 760-unit-wide column everything sits in; scaled to the screen's width.")]
        [SerializeField] private RectTransform _content;
        [Tooltip("Where the column's size and the listed objects' places come from. Empty keeps 760 x 1000 and the prefab's places.")]
        [SerializeField] private ScreenLayout _layout;
        [SerializeField] private Button _playButton;
        [SerializeField] private TMP_Text _playLabel;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private TMP_Text _homeLabel;
        [Header("Theme")]
        [Tooltip("Main menu button colours. Edit CD_MainScreenTheme in the Project window.")]
        [SerializeField] private CD_MainScreenTheme _theme;
        [Tooltip("The presentation-only purchase panels shown on the Shop page.")]
        [SerializeField] private Graphic[] _shopBuyPanels;
        [Header("Pages")]
        [Tooltip("Home content shown by the Home navigation tab.")]
        [SerializeField] private GameObject _homePage;
        [Tooltip("Shop content shown by the Shop navigation tab.")]
        [SerializeField] private GameObject _shopPage;
        [SerializeField] private TMP_Text _coins;
        [Tooltip("The lives the player holds, as lives/max.")]
        [SerializeField] private TMP_Text _lives;
        [Tooltip("The + by the lives: opens the purchase popup on lives.")]
        [SerializeField] private Button _livesPlusButton;
        [Header("Profile")]
        [SerializeField] private Button _profileButton;
        [SerializeField] private ProfileAvatar _profileAvatar;
        [Header("Navigation")]
        [Tooltip("The bar's tabs, left to right. Each one's label is the TMP_Text under it, keyed main.nav.<index>.")]
        [SerializeField] private Button[] _navTabs;
        [Tooltip("The tab picked at the start: Home.")]
        [SerializeField] private int _homeTab = 1;
        [Tooltip("The highlight that slides behind the tab picked.")]
        [SerializeField] private RectTransform _navSelector;
        [Tooltip("Seconds the highlight takes to slide to a tab.")]
        [SerializeField] private float _selectorTime = .2f;
        [Tooltip("The scale multiplier applied to the selected tab image. The selector's size is never changed.")]
        [Min(1)] [SerializeField] private float _selectedTabScale = 1.15f;
        [Tooltip("Seconds a tab image takes to grow or return to its authored scale.")]
        [Min(0)] [SerializeField] private float _tabScaleTime = .16f;

        public Action PlayClicked;
        public Action SettingsClicked;
        public Action HomeClicked;
        /// <summary>A tab of the navigation bar was picked: its index, left to right.</summary>
        public Action<int> NavTabClicked;
        public Action ProfileIconClicked;
        public Action LivesPlusClicked;

        private const float LAYOUT_WIDTH = 760, MIN_HEIGHT = 1000;
        private Func<string, string> _localize;
        private readonly List<(TMP_Text Label, string Key, string English)> _keyedTexts = new();
        private bool _built;
        private int _nextLevel;
        private bool _coinsInitialized;
        private string _playEnglish = "Play";
        private readonly List<UnityEngine.Events.UnityAction> _tabActions = new();
        private RectTransform[] _navImages;
        private Vector3[] _navImageScales;
        private Tween _selectorTween;
        private int _selectedTab;

        private void OnEnable()
        {
            if (_layout != null) _layout.Changed += FitLayout;
            _playButton.onClick.AddListener(OnPlayClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _homeButton.onClick.AddListener(OnHomeClicked);
            _profileButton.onClick.AddListener(OnProfileIconClicked);
            if (_livesPlusButton != null) _livesPlusButton.onClick.AddListener(OnLivesPlusClicked);
            for (int i = 0; i < _tabActions.Count; i++) _navTabs[i].onClick.AddListener(_tabActions[i]);
        }

        private void OnDisable()
        {
            if (_layout != null) _layout.Changed -= FitLayout;
            _playButton.onClick.RemoveListener(OnPlayClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            _homeButton.onClick.RemoveListener(OnHomeClicked);
            _profileButton.onClick.RemoveListener(OnProfileIconClicked);
            if (_livesPlusButton != null) _livesPlusButton.onClick.RemoveListener(OnLivesPlusClicked);
            for (int i = 0; i < _tabActions.Count; i++) _navTabs[i].onClick.RemoveListener(_tabActions[i]);
            KillNavigationTweens();
        }

        private void OnPlayClicked() => PlayClicked?.Invoke();
        private void OnSettingsClicked() => SettingsClicked?.Invoke();
        private void OnHomeClicked() => HomeClicked?.Invoke();
        private void OnProfileIconClicked() => ProfileIconClicked?.Invoke();
        private void OnLivesPlusClicked() => LivesPlusClicked?.Invoke();

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            _coinsInitialized = false;
            Build();
            ApplyTheme();
            // A pooled screen opens on Home again, whatever was picked last time.
            PickTab(_homeTab, false);
        }
        public override void AfterScreenActivation()
        {
            base.AfterScreenActivation();
            FitLayout();
            PickTab(_selectedTab, false);
        }
        private void OnRectTransformDimensionsChange()
        {
            if (!_built) return;
            FitLayout();
            PickTab(_selectedTab, false);
        }

        private void Build()
        {
            if (_built) return;
            _built = true;
            if (_playLabel != null) _playEnglish = _playLabel.text;
            UpdatePlayLabel();
            Keyed(_homeLabel, "main.home");
            for (int i = 0; i < _navTabs.Length; i++)
            {
                int index = i;
                _tabActions.Add(() => OnTabClicked(index));
                if (isActiveAndEnabled) _navTabs[i].onClick.AddListener(_tabActions[i]);
                if (_navTabs[i] != _homeButton) Keyed(_navTabs[i].GetComponentInChildren<TMP_Text>(true), "main.nav." + i);
            }
            CacheNavigationImages();
            ApplyTheme();
            FitLayout();
        }

        private void OnTabClicked(int index)
        {
            PickTab(index, true);
            NavTabClicked?.Invoke(index);
        }

        /// <summary>Moves the selector without resizing it and scales only the selected tab image.</summary>
        private void PickTab(int index, bool slide)
        {
            if (_navSelector == null || index < 0 || index >= _navTabs.Length) return;
            _selectedTab = index;
            ShowPage(index);
            var tab = (RectTransform)_navTabs[index].transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)tab.parent);
            KillNavigationTweens();

            Vector3 targetWorld = tab.TransformPoint(tab.rect.center);
            Vector3 targetLocal = _navSelector.parent.InverseTransformPoint(targetWorld);
            // The selector's vertical placement is authored in the prefab. Navigation only moves it horizontally.
            targetLocal.y = _navSelector.localPosition.y;
            targetLocal.z = _navSelector.localPosition.z;

            bool animate = slide && isActiveAndEnabled;
            if (animate && _selectorTime > 0)
            {
                _selectorTween = _navSelector.DOLocalMove(targetLocal, _selectorTime)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            else _navSelector.localPosition = targetLocal;

            for (int i = 0; i < _navImages.Length; i++)
            {
                if (_navImages[i] == null) continue;
                Vector3 targetScale = _navImageScales[i] * (i == index ? _selectedTabScale : 1);
                if (animate && _tabScaleTime > 0)
                {
                    _navImages[i].DOScale(targetScale, _tabScaleTime)
                        .SetEase(i == index ? Ease.OutBack : Ease.OutCubic)
                        .SetUpdate(true)
                        .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                }
                else _navImages[i].localScale = targetScale;
            }
            ApplyNavigationColors(index);
        }

        private void ApplyTheme()
        {
            if (_theme == null) return;
            SetButtonColor(_playButton, _theme.PlayButton);
            SetButtonColor(_settingsButton, _theme.UtilityButton);
            SetButtonColor(_profileButton, _theme.UtilityButton);
            SetButtonColor(_livesPlusButton, _theme.UtilityButton);
            if (_playLabel != null) _playLabel.color = _theme.ButtonText;
            if (_homeLabel != null) _homeLabel.color = _theme.ButtonText;
            if (_shopBuyPanels != null)
                foreach (Graphic panel in _shopBuyPanels)
                    if (panel != null) panel.color = _theme.ShopBuyButton;
            if (_navSelector != null)
            {
                Image selector = _navSelector.GetComponent<Image>();
                if (selector != null) selector.color = _theme.NavigationSelector;
            }
            ApplyNavigationColors(_selectedTab);
        }

        private void ApplyNavigationColors(int selectedIndex)
        {
            if (_theme == null || _navTabs == null) return;
            for (int i = 0; i < _navTabs.Length; i++)
            {
                Button tab = _navTabs[i];
                SetButtonColor(tab, i == selectedIndex ? _theme.NavigationSelected : _theme.NavigationNormal);
                TMP_Text label = tab != null ? tab.GetComponentInChildren<TMP_Text>(true) : null;
                if (label != null)
                    label.color = i == selectedIndex ? _theme.NavigationTextSelected : _theme.NavigationTextNormal;
            }
        }

        // The theme tints, it does not reveal: a button whose graphic is an invisible tap area (the
        // lives "+", drawn into the capsule art) stays invisible. Every other graphic takes the colour
        // as given, so recolouring a tab again and again never compounds.
        private static void SetButtonColor(Button button, Color color)
        {
            if (button == null || button.targetGraphic == null || button.targetGraphic.color.a <= 0f) return;
            button.targetGraphic.color = color;
        }

        private void ShowPage(int index)
        {
            bool showShop = index == 0;
            if (_homePage != null) _homePage.SetActive(!showShop);
            if (_shopPage != null) _shopPage.SetActive(showShop);
        }

        private void CacheNavigationImages()
        {
            _navImages = new RectTransform[_navTabs.Length];
            _navImageScales = new Vector3[_navTabs.Length];
            for (int i = 0; i < _navTabs.Length; i++)
            {
                Graphic graphic = _navTabs[i] != null ? _navTabs[i].targetGraphic : null;
                _navImages[i] = graphic != null ? graphic.rectTransform : (RectTransform)_navTabs[i].transform;
                _navImageScales[i] = _navImages[i].localScale;
            }
        }

        private void KillNavigationTweens()
        {
            _selectorTween?.Kill();
            _selectorTween = null;
            if (_navImages == null) return;
            foreach (RectTransform image in _navImages) image?.DOKill();
        }

        /// <summary>Fits the column's width to the screen; a short, wide screen fits its height instead.</summary>
        private void FitLayout()
        {
            if (_content == null) return;
            var rect = ((RectTransform)transform).rect;
            var config = _layout != null ? _layout.Config : null;
            float scale = config != null
                ? config.FitScale(rect.size)
                : Mathf.Max(.01f, Mathf.Min(rect.width / LAYOUT_WIDTH, rect.height / MIN_HEIGHT));
            _content.localScale = Vector3.one * scale;
            _content.sizeDelta = new Vector2(rect.width / scale, rect.height / scale);
            if (_layout != null) _layout.SetDirty();
        }

        /// <summary>The function that turns a key into text in the language in use (ILocalizationService.Get). Redraws every text.</summary>
        public void SetLocalizer(Func<string, string> localize) { _localize = localize; ApplyLanguage(null); }
        /// <summary>The game's language changed.</summary>
        public void ApplyLanguage(string code)
        {
            foreach (var keyed in _keyedTexts) if (keyed.Label != null) keyed.Label.text = L(keyed.Key, keyed.English);
            UpdatePlayLabel();
        }
        /// <summary>The level Play starts, counted from 1; 0 or less shows just Play.</summary>
        public void ApplyNextLevel(int number)
        {
            _nextLevel = number;
            UpdatePlayLabel();
        }
        private void UpdatePlayLabel()
        {
            if (_playLabel == null) return;
            _playLabel.text = L("main.play", _playEnglish);
            if (_levelLabel != null && _nextLevel > 0)
                _levelLabel.text = string.Format(L("main.play.level", "Level {0}"), _nextLevel);
        }
        public void ApplyCoins(int coins)
        {
            if (_coins == null) return;
            string text = coins.ToString();
            if (!_coinsInitialized)
            {
                _coinsInitialized = true;
                var spendEffect = _coins.GetComponent<CounterSpendEffect>();
                if (spendEffect != null)
                {
                    spendEffect.SetWithoutEffect(text);
                    return;
                }
            }
            _coins.text = text;
        }
        public void ApplyLives(Modules.GameplayModule.Shared.Data.ValueObjects.LivesStateRVO state) { if (_lives != null) _lives.text = $"{state.Lives}/{state.MaxLives}"; }
        public void ApplyProfileIcon(ProfileIconVO icon)
        {
            if (_profileAvatar == null || icon == null) return;
            _profileAvatar.SetIcon(icon.Sprite, icon.FallbackColor);
        }
        public void ApplyProfileFrame(ProfileFrameVO frame)
        {
            if (_profileAvatar == null || frame == null) return;
            _profileAvatar.SetFrame(frame.Sprite, frame.FallbackColor);
        }

        private string L(string key, string english)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? english : text;
        }
        private void Keyed(TMP_Text label, string key)
        {
            if (label == null) return;
            _keyedTexts.Add((label, key, label.text));
            label.text = L(key, label.text);
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
