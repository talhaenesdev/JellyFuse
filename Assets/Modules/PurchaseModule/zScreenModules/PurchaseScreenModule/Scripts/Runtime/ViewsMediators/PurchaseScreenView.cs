using System;
using System.Collections;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.PurchaseModule.Shared.Data.ValueObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.PurchaseModule.PurchaseScreenModule.ViewsMediators
{
    /// <summary>
    /// The shared purchase popup: a title, the icon with what the player holds over it, an info
    /// line that may count down, and a buy button with the price. Every offer looks the same; what
    /// it is comes in a PurchaseOfferVO.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class PurchaseScreenView : ScreenView
    {
        [Tooltip("The popup's column; scaled so it takes Width Share of the screen's width.")]
        [SerializeField] private RectTransform _content;
        [SerializeField, Range(.3f, 1)] private float _widthShare = .82f;
        [Tooltip("The popup never grows taller than this share of the screen's height.")]
        [SerializeField, Range(.3f, 1)] private float _heightShare = .7f;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private TMP_Text _info;
        [SerializeField] private Button _buyButton;
        [SerializeField] private TMP_Text _buyLabel;
        [Tooltip("Shows an ad for the offer's reward; hidden for an offer with no watch text.")]
        [SerializeField] private Button _watchButton;
        [SerializeField] private TMP_Text _watchLabel;
        [SerializeField] private Button _closeButton;
        [Tooltip("How see-through the buy button is when the offer cannot be bought.")]
        [SerializeField, Range(0, 1)] private float _disabledAlpha = .5f;

        public event Action BuyClicked;
        public event Action WatchClicked;
        public event Action CloseClicked;
        /// <summary>The countdown ran out: the offer should be sent again.</summary>
        public event Action RefreshRequested;

        public PurchaseOfferVO Offer { get; private set; }
        private Func<string, string> _localize;
        private float _seconds;
        private Coroutine _countdown;

        public void SetLocalizer(Func<string, string> localize) { _localize = localize; if (Offer != null) Draw(); }

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
            _buyButton.onClick.AddListener(OnBuyClicked);
            _closeButton.onClick.AddListener(OnCloseClicked);
            if (_watchButton != null) _watchButton.onClick.AddListener(OnWatchClicked);
        }
        private void OnDisable()
        {
            _buyButton.onClick.RemoveListener(OnBuyClicked);
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_watchButton != null) _watchButton.onClick.RemoveListener(OnWatchClicked);
            if (_countdown != null) { StopCoroutine(_countdown); _countdown = null; }
        }
        private void OnBuyClicked() => BuyClicked?.Invoke();
        private void OnCloseClicked() => CloseClicked?.Invoke();
        private void OnWatchClicked() => WatchClicked?.Invoke();

        public void ShowOffer(PurchaseOfferVO offer)
        {
            Offer = offer;
            _seconds = offer.CountdownSeconds;
            Draw();
            if (_countdown != null) { StopCoroutine(_countdown); _countdown = null; }
            if (_seconds > 0 && isActiveAndEnabled) _countdown = StartCoroutine(CountDown());
        }

        private void Draw()
        {
            _title.text = L(Offer.TitleKey, Offer.Title);
            if (Offer.Icon != null) _icon.sprite = Offer.Icon;
            _amount.text = Offer.Amount;
            DrawInfo();
            _buyLabel.text = Format(L(Offer.BuyKey, Offer.Buy), Offer.Price.ToString());
            DrawButton(_buyButton, Offer.CanBuy);
            if (_watchButton == null) return;
            bool watch = !string.IsNullOrEmpty(Offer.Watch) || !string.IsNullOrEmpty(Offer.WatchKey);
            _watchButton.gameObject.SetActive(watch);
            if (!watch) return;
            if (_watchLabel != null) _watchLabel.text = Format(L(Offer.WatchKey, Offer.Watch), Offer.WatchAmount.ToString());
            DrawButton(_watchButton, Offer.CanWatch);
        }
        private void DrawButton(Button button, bool usable)
        {
            button.interactable = usable;
            var group = button.GetComponent<CanvasGroup>();
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = usable ? 1 : _disabledAlpha;
        }
        private void DrawInfo() => _info.text = Format(L(Offer.InfoKey, Offer.Info), Clock(_seconds));

        private IEnumerator CountDown()
        {
            var wait = new WaitForSecondsRealtime(1);
            while (_seconds > 0)
            {
                yield return wait;
                _seconds = Mathf.Max(0, _seconds - 1);
                DrawInfo();
            }
            _countdown = null;
            RefreshRequested?.Invoke();
        }

        private static string Clock(float seconds)
        {
            int whole = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return whole >= 3600 ? $"{whole / 3600}:{whole / 60 % 60:00}:{whole % 60:00}" : $"{whole / 60:00}:{whole % 60:00}";
        }
        private string L(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? "";
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback ?? "" : text;
        }
        private static string Format(string text, string value)
        {
            try { return string.Format(text, value); }
            catch (FormatException) { return text; }
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
