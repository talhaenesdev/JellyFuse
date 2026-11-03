using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.SlotScreenModule.ViewsMediators
{
    /// <summary>
    /// The daily slot machine: three reels that roll and stop one by one on the symbols Gameplay rolled, a lever and a
    /// spin button while a spin is held - today's free one, or one an ad gave - and otherwise a button that watches an
    /// ad for one more. Each reel's symbols are made in code inside its window, so the art carries none.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class SlotScreenView : ScreenView
    {
        [Tooltip("The popup, authored at its own size and scaled to fit the screen.")]
        [SerializeField] private RectTransform _content;
        [SerializeField, Range(.3f, 1)] private float _widthShare = .92f;
        [SerializeField, Range(.3f, 1)] private float _heightShare = .85f;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backdropButton;

        [Header("Reels")]
        [Tooltip("The three reel windows left to right, each clipping its symbols.")]
        [SerializeField] private RectTransform[] _reels;
        [Tooltip("The symbols' art in CD_Events' slot symbol order.")]
        [SerializeField] private Sprite[] _symbols;
        [Tooltip("A symbol's size against its window's width.")]
        [SerializeField, Range(.3f, 1)] private float _symbolShare = .82f;
        [Tooltip("Seconds the first reel rolls; each next one rolls Reel Gap longer.")]
        [SerializeField, Min(.2f)] private float _rollSeconds = 1.2f;
        [SerializeField, Min(0)] private float _reelGap = .45f;
        [Tooltip("Symbols a second a reel rolls past.")]
        [SerializeField, Min(1)] private float _rollSpeed = 14;

        [Header("Spin")]
        [Tooltip("The lever, pivoted at its foot; pulled when a spin starts, and pressing it spins.")]
        [SerializeField] private RectTransform _lever;
        [SerializeField] private Button _leverButton;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TMP_Text _spinLabel;
        [SerializeField] private Button _adSpinButton;
        [SerializeField] private TMP_Text _adSpinLabel;
        [Tooltip("Shown behind the machine on three alike.")]
        [SerializeField] private GameObject _winGlow;
        [Tooltip("What the last spin gave, on the machine's lower plate.")]
        [SerializeField] private TMP_Text _result;
        [SerializeField] private Sprite _coinIcon;
        [Tooltip("The booster icons in BoosterType order: Rocket, Freeze Time, Ufo.")]
        [SerializeField] private Sprite[] _boosterIcons;
        [Tooltip("Plays the reward won flying off the machine: coins to the counter, the rest upwards.")]
        [SerializeField] private RewardFlyer _flyer;

        public Action CloseClicked;
        public Action SpinClicked;
        public Action AdSpinClicked;

        private Func<string, string> _localize;
        private EventsRVO _events = new();
        private readonly List<Image[]> _cells = new();
        private Coroutine _spin;
        private Tween _lift;
        private bool Spinning => _spin != null;

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
            _spinButton.onClick.AddListener(OnSpinClicked);
            _adSpinButton.onClick.AddListener(OnAdSpinClicked);
            if (_leverButton != null) _leverButton.onClick.AddListener(OnSpinClicked);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _spinButton.onClick.RemoveListener(OnSpinClicked);
            _adSpinButton.onClick.RemoveListener(OnAdSpinClicked);
            if (_leverButton != null) _leverButton.onClick.RemoveListener(OnSpinClicked);
            if (_backdropButton != null) _backdropButton.onClick.RemoveListener(OnCloseClicked);
            // A spin cut short by closing ends where it was meant to: the reward is already given.
            if (_spin != null) { StopCoroutine(_spin); _spin = null; }
            _lift?.Kill();
            if (_lever != null) _lever.localScale = Vector3.one;
        }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            if (_winGlow != null) _winGlow.SetActive(false);
            if (_result != null) _result.text = "";
            BuildReels();
            for (int r = 0; r < _cells.Count; r++)
            for (int i = 0; i < _cells[r].Length; i++)
                Place(r, i, (i - 1) * Step(r), RandomSymbol());
        }

        public void SetLocalizer(Func<string, string> localize) => _localize = localize;

        public void ShowEvents(EventsRVO events)
        {
            _events = events ?? new EventsRVO();
            _title.text = L("slot.title", "Lucky Slots");
            // While the reels roll the buttons wait; the spin's end draws them from the events last given.
            if (!Spinning) DrawButtons();
        }

        /// <summary>Rolls the reels and stops them, left to right, on the symbols given, then shows what they paid.</summary>
        public void SpinTo(SlotResultRVO result)
        {
            if (result == null || result.Reels == null) return;
            BuildReels();
            if (_spin != null) StopCoroutine(_spin);
            if (_winGlow != null) _winGlow.SetActive(false);
            if (_result != null) _result.text = "";
            PullLever();
            _spin = StartCoroutine(Spin(result));
            DrawButtons();
        }

        private IEnumerator Spin(SlotResultRVO result)
        {
            int reels = Mathf.Min(_cells.Count, result.Reels.Length);
            var stopAt = new float[reels];
            var landing = new int[reels];   // the cell carrying the target in, -1 until it is placed; -2 once landed
            for (int r = 0; r < reels; r++) { stopAt[r] = _rollSeconds + r * _reelGap; landing[r] = -1; }
            float elapsed = 0;
            int landed = 0;
            while (landed < reels)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                for (int r = 0; r < reels; r++)
                {
                    if (landing[r] == -2) continue;
                    float step = Step(r);
                    float move = _rollSpeed * step * dt;
                    var cells = _cells[r];
                    for (int i = 0; i < cells.Length; i++)
                    {
                        var rect = cells[i].rectTransform;
                        float y = rect.anchoredPosition.y - move;
                        if (y < -1.5f * step)
                        {
                            y += cells.Length * step;
                            // Past its stop time, the next cell to come in from the top carries the target.
                            bool target = elapsed >= stopAt[r] && landing[r] == -1;
                            if (target) landing[r] = i;
                            cells[i].sprite = target ? Symbol(result.Reels[r]) : RandomSymbol();
                        }
                        rect.anchoredPosition = new Vector2(0, y);
                    }
                    if (landing[r] >= 0 && cells[landing[r]].rectTransform.anchoredPosition.y <= 0)
                    {
                        // Snapped to the middle, its neighbours a step either side, with a little bounce.
                        int middle = landing[r];
                        for (int k = 0; k < cells.Length; k++)
                        {
                            int offset = ((k - middle) % cells.Length + cells.Length + 1) % cells.Length - 1;
                            cells[k].rectTransform.anchoredPosition = new Vector2(0, offset * step);
                        }
                        var symbolRect = cells[middle].rectTransform;
                        symbolRect.localScale = Vector3.one * 1.15f;
                        symbolRect.DOScale(1, .25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(symbolRect.gameObject);
                        landing[r] = -2;
                        landed++;
                    }
                }
                yield return null;
            }
            _spin = null;
            if (_winGlow != null) _winGlow.SetActive(result.Jackpot);
            var reward = result.Reward;
            if (_result != null)
                _result.text = string.Format(L(result.Jackpot ? "slot.jackpot" : "slot.won", result.Jackpot ? "Jackpot! {0}" : "You won {0}!"), RewardText(reward));
            if (_flyer != null && _cells.Count > 1)
                _flyer.Fly(RewardIcon(reward), _reels[1], reward.Coins > 0, Mathf.Clamp(reward.Coins / 25, 3, 12));
            DrawButtons();
        }

        private void PullLever()
        {
            if (_lever == null) return;
            _lift?.Kill();
            _lever.localScale = Vector3.one;
            _lift = DOTween.Sequence().SetUpdate(true).SetLink(_lever.gameObject)
                .Append(DOTween.To(() => _lever.localScale.y, y => _lever.localScale = new Vector3(1, y, 1), -.55f, .18f).SetEase(Ease.InQuad))
                .Append(DOTween.To(() => _lever.localScale.y, y => _lever.localScale = new Vector3(1, y, 1), 1, .45f).SetEase(Ease.OutBack));
        }

        /// <summary>Makes each reel's symbols once: three a step apart, the middle one showing, and one waiting above.</summary>
        private void BuildReels()
        {
            if (_cells.Count > 0 || _reels == null) return;
            foreach (var reel in _reels)
            {
                var cells = new Image[4];
                float size = reel.rect.width * _symbolShare;
                for (int i = 0; i < cells.Length; i++)
                {
                    var go = new GameObject("Symbol", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.layer = reel.gameObject.layer;
                    var rect = (RectTransform)go.transform;
                    rect.SetParent(reel, false);
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    rect.sizeDelta = Vector2.one * size;
                    var image = go.GetComponent<Image>();
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    cells[i] = image;
                }
                _cells.Add(cells);
            }
        }

        private void Place(int reel, int cell, float y, Sprite sprite)
        {
            var image = _cells[reel][cell];
            image.sprite = sprite;
            image.rectTransform.anchoredPosition = new Vector2(0, y);
            image.rectTransform.localScale = Vector3.one;
        }

        /// <summary>A symbol's step on its reel: one window high, so the middle one fills it.</summary>
        private float Step(int reel) => Mathf.Max(1, _reels[reel].rect.height);

        private Sprite Symbol(int index) => _symbols != null && index >= 0 && index < _symbols.Length ? _symbols[index] : null;

        private Sprite RandomSymbol() => _symbols == null || _symbols.Length == 0 ? null : _symbols[UnityEngine.Random.Range(0, _symbols.Length)];

        private void DrawButtons()
        {
            bool held = _events.SlotSpins > 0;
            _spinButton.gameObject.SetActive(held);
            _spinButton.interactable = held && !Spinning;
            if (_spinLabel != null)
                _spinLabel.text = _events.SlotSpins > 1 ? string.Format(L("slot.spin_many", "Spin ({0})"), _events.SlotSpins) : L("slot.spin", "Spin");
            _adSpinButton.gameObject.SetActive(!held);
            _adSpinButton.interactable = !held && !Spinning;
            if (_adSpinLabel != null) _adSpinLabel.text = L("slot.ad_spin", "+1 Spin");
            if (_leverButton != null) _leverButton.interactable = held && !Spinning;
            _closeButton.interactable = !Spinning;
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

        private void OnCloseClicked() { if (!Spinning) CloseClicked?.Invoke(); }
        private void OnSpinClicked() { if (!Spinning) SpinClicked?.Invoke(); }
        private void OnAdSpinClicked() { if (!Spinning) AdSpinClicked?.Invoke(); }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
