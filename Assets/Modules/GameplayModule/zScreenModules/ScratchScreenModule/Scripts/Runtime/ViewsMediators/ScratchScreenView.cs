using System;
using System.Collections.Generic;
using DG.Tweening;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.ScratchScreenModule.Entities;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.ScratchScreenModule.ViewsMediators
{
    /// <summary>
    /// The daily scratch card: nine foiled cells the player rubs open with a finger, three alike win the prize under
    /// them, and a button that takes a card while one is held - today's free one, or one an ad gave - and otherwise
    /// watches an ad for one more. The card is already paid when it is dealt; scratching only shows it.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class ScratchScreenView : ScreenView
    {
        [Tooltip("The popup, authored at its own size and scaled to fit the screen.")]
        [SerializeField] private RectTransform _content;
        [SerializeField, Range(.3f, 1)] private float _widthShare = .92f;
        [SerializeField, Range(.3f, 1)] private float _heightShare = .85f;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backdropButton;

        [Header("Card")]
        [Tooltip("The nine cells row by row from the top left: each holds an Image named Prize and a ScratchSurface over it.")]
        [SerializeField] private RectTransform[] _cells;
        [Tooltip("The prizes' art in CD_Events' scratch prize order.")]
        [SerializeField] private Sprite[] _prizes;
        [Tooltip("What an empty cell shows.")]
        [SerializeField] private Sprite _empty;
        [Tooltip("The foil laid over each cell.")]
        [SerializeField] private Sprite _foil;
        [Tooltip("The foil texture's side, in pixels.")]
        [SerializeField, Range(32, 256)] private int _foilSize = 96;
        [Tooltip("The coin that follows the finger while it scratches.")]
        [SerializeField] private RectTransform _coinCursor;

        [Header("Buttons")]
        [SerializeField] private Button _cardButton;
        [SerializeField] private TMP_Text _cardLabel;
        [SerializeField] private Button _adCardButton;
        [SerializeField] private TMP_Text _adCardLabel;
        [Tooltip("Opens every cell left at once.")]
        [SerializeField] private Button _openAllButton;
        [SerializeField] private TMP_Text _openAllLabel;

        [Header("Result")]
        [Tooltip("Shown behind the card when it wins.")]
        [SerializeField] private GameObject _winGlow;
        [SerializeField] private TMP_Text _result;
        [SerializeField] private Sprite _coinIcon;
        [Tooltip("The booster icons in BoosterType order: Rocket, Freeze Time, Ufo.")]
        [SerializeField] private Sprite[] _boosterIcons;
        [SerializeField] private RewardFlyer _flyer;

        public Action CloseClicked;
        public Action CardClicked;
        public Action AdCardClicked;

        private Func<string, string> _localize;
        private EventsRVO _events = new();
        private ScratchCardRVO _card;
        private Color32[] _foilPixels;
        private readonly List<ScratchSurface> _surfaces = new();
        private readonly List<Image> _icons = new();
        private Tween _cursorFade;

        /// <summary>A card is on the table and not all of it is open.</summary>
        private bool Scratching => _card != null && _surfaces.Exists(s => !s.IsOpen);

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
            _cardButton.onClick.AddListener(OnCardClicked);
            _adCardButton.onClick.AddListener(OnAdCardClicked);
            _openAllButton.onClick.AddListener(OpenAll);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _cardButton.onClick.RemoveListener(OnCardClicked);
            _adCardButton.onClick.RemoveListener(OnAdCardClicked);
            _openAllButton.onClick.RemoveListener(OpenAll);
            if (_backdropButton != null) _backdropButton.onClick.RemoveListener(OnCloseClicked);
            _cursorFade?.Kill();
        }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            Collect();
            // A card left half scratched was paid when it was dealt; the next opening starts from a fresh foil.
            _card = null;
            if (_winGlow != null) _winGlow.SetActive(false);
            if (_result != null) _result.text = "";
            if (_coinCursor != null) _coinCursor.gameObject.SetActive(false);
            for (int i = 0; i < _surfaces.Count; i++)
            {
                _icons[i].enabled = false;
                _icons[i].transform.localScale = Vector3.one;
                _surfaces[i].Cover(FoilPixels(), _foilSize);
                _surfaces[i].Interactable = false;
            }
        }

        public void SetLocalizer(Func<string, string> localize) => _localize = localize;

        public void ShowEvents(EventsRVO events)
        {
            _events = events ?? new EventsRVO();
            _title.text = L("scratch.title", "Scratch & Win");
            DrawButtons();
        }

        /// <summary>Lays the card dealt under fresh foil, ready for the finger.</summary>
        public void LayCard(ScratchCardRVO card)
        {
            if (card == null || card.Cells == null) return;
            Collect();
            _card = card;
            if (_winGlow != null) _winGlow.SetActive(false);
            if (_result != null) _result.text = L("scratch.hint", "Scratch the cells!");
            for (int i = 0; i < _surfaces.Count; i++)
            {
                int prize = i < card.Cells.Length ? card.Cells[i] : -1;
                _icons[i].sprite = prize >= 0 && _prizes != null && prize < _prizes.Length ? _prizes[prize] : _empty;
                _icons[i].enabled = true;
                _icons[i].transform.localScale = Vector3.one;
                _surfaces[i].Cover(FoilPixels(), _foilSize);
                _surfaces[i].Interactable = true;
            }
            DrawButtons();
        }

        private void OpenAll()
        {
            foreach (var surface in _surfaces) surface.Open();
        }

        private void OnOpened(ScratchSurface surface)
        {
            if (_card == null || Scratching) return;
            // The last cell is open: the winning three pulse and the prize flies off the first of them.
            var reward = _card.Reward;
            bool won = _card.WonPrize >= 0;
            if (_winGlow != null) _winGlow.SetActive(won);
            RectTransform from = null;
            for (int i = 0; i < _icons.Count && i < _card.Cells.Length; i++)
            {
                if (!won || _card.Cells[i] != _card.WonPrize) continue;
                var icon = _icons[i].transform;
                if (from == null) from = (RectTransform)icon;
                icon.DOPunchScale(Vector3.one * .25f, .6f, 6).SetUpdate(true).SetLink(icon.gameObject);
            }
            if (_result != null)
                _result.text = string.Format(L(won ? "scratch.won" : "scratch.consolation", won ? "You won {0}!" : "No match - here's {0}"), RewardText(reward));
            if (_flyer != null)
                _flyer.Fly(RewardIcon(reward), from != null ? from : _content, reward.Coins > 0, Mathf.Clamp(reward.Coins / 25, 3, 12));
            if (_coinCursor != null) _coinCursor.gameObject.SetActive(false);
            _card = null;
            DrawButtons();
        }

        private void OnScratched(Vector2 screen, Camera eye)
        {
            if (_coinCursor == null) return;
            var parent = (RectTransform)_coinCursor.parent;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, eye, out var local)) return;
            _coinCursor.gameObject.SetActive(true);
            _coinCursor.anchoredPosition = local;
            _cursorFade?.Kill();
            _cursorFade = DOVirtual.DelayedCall(.35f, () => _coinCursor.gameObject.SetActive(false), true).SetLink(gameObject);
        }

        /// <summary>Finds each cell's prize image and foil once, and listens to the foils.</summary>
        private void Collect()
        {
            if (_surfaces.Count > 0 || _cells == null) return;
            foreach (var cell in _cells)
            {
                var surface = cell.GetComponentInChildren<ScratchSurface>(true);
                var icon = cell.Find("Prize")?.GetComponent<Image>();
                if (surface == null || icon == null) continue;
                surface.Opened += OnOpened;
                surface.Scratched += OnScratched;
                _surfaces.Add(surface);
                _icons.Add(icon);
            }
        }

        /// <summary>The foil's art read back into pixels once, through a render texture, so the art needs no Read/Write.</summary>
        private Color32[] FoilPixels()
        {
            if (_foilPixels != null) return _foilPixels;
            var target = RenderTexture.GetTemporary(_foilSize, _foilSize, 0, RenderTextureFormat.ARGB32);
            var before = RenderTexture.active;
            if (_foil != null) Graphics.Blit(_foil.texture, target);
            else { RenderTexture.active = target; GL.Clear(true, true, Color.gray); }
            RenderTexture.active = target;
            var read = new Texture2D(_foilSize, _foilSize, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, _foilSize, _foilSize), 0, 0);
            read.Apply();
            RenderTexture.active = before;
            RenderTexture.ReleaseTemporary(target);
            _foilPixels = read.GetPixels32();
            Destroy(read);
            return _foilPixels;
        }

        private void DrawButtons()
        {
            bool scratching = Scratching;
            bool held = _events.ScratchCards > 0;
            _cardButton.gameObject.SetActive(!scratching && held);
            if (_cardLabel != null)
                _cardLabel.text = _events.ScratchCards > 0 ? string.Format(L("scratch.card_many", "New Card ({0})"), _events.ScratchCards) : L("scratch.card", "New Card");
            _adCardButton.gameObject.SetActive(!scratching && !held);
            if (_adCardLabel != null) _adCardLabel.text = L("scratch.ad_card", "+1 Card");
            _openAllButton.gameObject.SetActive(scratching);
            if (_openAllLabel != null) _openAllLabel.text = L("scratch.open_all", "Reveal All");
        }

        private Sprite RewardIcon(RewardCVO reward)
        {
            int booster = (int)reward.Booster;
            return reward.Coins > 0 || _boosterIcons == null || booster >= _boosterIcons.Length ? _coinIcon : _boosterIcons[booster];
        }

        private string RewardText(RewardCVO reward) =>
            reward.Coins > 0 ? string.Format(L("wheel.coins", "{0} coins"), reward.Coins)
            : reward.BoosterCount + " " + L($"info.booster.{reward.Booster.ToString().ToLowerInvariant()}.title", reward.Booster.ToString());

        private string L(string key, string fallback)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        // Closing mid-card is allowed: the card was paid when it was dealt.
        private void OnCloseClicked() => CloseClicked?.Invoke();
        private void OnCardClicked() { if (!Scratching) CardClicked?.Invoke(); }
        private void OnAdCardClicked() { if (!Scratching) AdCardClicked?.Invoke(); }

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
