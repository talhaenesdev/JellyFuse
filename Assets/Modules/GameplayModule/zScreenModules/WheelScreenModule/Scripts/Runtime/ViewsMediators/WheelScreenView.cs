using System;
using System.Collections;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.WheelScreenModule.ViewsMediators
{
    /// <summary>
    /// The daily prize wheel: a disc of slices that turns to the slice Gameplay picked, a spin button while a spin is
    /// held - today's free one, or one an ad gave - and otherwise a button that watches an ad for one more. Each slice's reward - an icon and an amount - is
    /// laid on the disc from Slice Template, so the slices follow CD_Events and the art carries none.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class WheelScreenView : ScreenView
    {
        [Tooltip("The popup, authored at its own size and scaled to fit the screen.")]
        [SerializeField] private RectTransform _content;
        [SerializeField, Range(.3f, 1)] private float _widthShare = .92f;
        [SerializeField, Range(.3f, 1)] private float _heightShare = .85f;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backdropButton;

        [Header("Wheel")]
        [Tooltip("The part that turns: the disc and the rewards laid on it.")]
        [SerializeField] private RectTransform _disc;
        [Tooltip("One reward on the disc - an Image and a TMP_Text child; copied once per slice and hidden itself.")]
        [SerializeField] private RectTransform _sliceTemplate;
        [Tooltip("How far from the centre the rewards sit, as a share of the disc's radius.")]
        [SerializeField, Range(.2f, .95f)] private float _sliceRadius = .66f;
        [Tooltip("Degrees clockwise from the top to the middle of the first slice in the disc's art.")]
        [SerializeField] private float _firstSliceAngle = 22.5f;
        [SerializeField] private Sprite _coinIcon;
        [Tooltip("The booster icons in BoosterType order: Rocket, Freeze Time, Ufo.")]
        [SerializeField] private Sprite[] _boosterIcons;

        [Header("Spin")]
        [SerializeField, Min(.5f)] private float _spinSeconds = 4f;
        [SerializeField, Min(1)] private int _spinTurns = 5;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TMP_Text _spinLabel;
        [SerializeField] private Button _adSpinButton;
        [SerializeField] private TMP_Text _adSpinLabel;
        [Tooltip("Shown behind the wheel once it stops.")]
        [SerializeField] private GameObject _burst;
        [Tooltip("What the last spin gave, under the wheel.")]
        [SerializeField] private TMP_Text _result;
        [Tooltip("Plays the reward won flying off the wheel: coins to the counter, the rest upwards.")]
        [SerializeField] private RewardFlyer _flyer;

        public Action CloseClicked;
        public Action SpinClicked;
        public Action AdSpinClicked;

        private Func<string, string> _localize;
        private EventsRVO _events = new();
        private readonly List<RectTransform> _slices = new();
        private Coroutine _spin;
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
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _spinButton.onClick.RemoveListener(OnSpinClicked);
            _adSpinButton.onClick.RemoveListener(OnAdSpinClicked);
            if (_backdropButton != null) _backdropButton.onClick.RemoveListener(OnCloseClicked);
            // A spin cut short by closing ends where it was meant to: the reward is already given.
            if (_spin != null) { StopCoroutine(_spin); _spin = null; }
        }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            if (_burst != null) _burst.SetActive(false);
            if (_result != null) _result.text = "";
        }

        public void SetLocalizer(Func<string, string> localize) => _localize = localize;

        public void ShowEvents(EventsRVO events)
        {
            _events = events ?? new EventsRVO();
            _title.text = L("wheel.title", "Lucky Wheel");
            LaySlices();
            // While the wheel turns the buttons wait; the spin's end draws them from the events last given.
            if (!Spinning) DrawButtons();
        }

        /// <summary>Turns the wheel to the slice given, clockwise from the top, and shows what it gave.</summary>
        public void SpinTo(int slice)
        {
            if (_events.Wheel.Count == 0 || slice < 0 || slice >= _events.Wheel.Count) return;
            if (_spin != null) StopCoroutine(_spin);
            if (_burst != null) _burst.SetActive(false);
            if (_result != null) _result.text = "";
            _spin = StartCoroutine(Spin(slice));
            DrawButtons();
        }

        private IEnumerator Spin(int slice)
        {
            // The disc turns anticlockwise by z, so a slice at angle a from the top comes under the pointer at z = a.
            float from = _disc.localEulerAngles.z;
            float target = SliceAngle(slice);
            float to = from + _spinTurns * 360f + Mathf.Repeat(target - from, 360f);
            for (float t = 0; t < _spinSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / _spinSeconds;
                _disc.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(from, to, 1 - Mathf.Pow(1 - k, 3)));
                yield return null;
            }
            _disc.localEulerAngles = new Vector3(0, 0, to);
            _spin = null;
            if (_burst != null) _burst.SetActive(true);
            var reward = _events.Wheel[slice].Reward;
            if (_result != null) _result.text = string.Format(L("wheel.won", "You won {0}!"), RewardText(reward, true));
            if (_flyer != null && slice < _slices.Count)
                _flyer.Fly(RewardIcon(reward), (RectTransform)_slices[slice].GetComponentInChildren<Image>(true).transform,
                    reward.Coins > 0, Mathf.Clamp(reward.Coins / 25, 4, 12));
            DrawButtons();
        }

        private float SliceAngle(int slice) => _firstSliceAngle + slice * 360f / _events.Wheel.Count;

        private void LaySlices()
        {
            if (_sliceTemplate == null) return;
            _sliceTemplate.gameObject.SetActive(false);
            float radius = _disc.rect.width * .5f * _sliceRadius;
            for (int i = 0; i < _events.Wheel.Count; i++)
            {
                if (i >= _slices.Count) _slices.Add(Instantiate(_sliceTemplate, _sliceTemplate.parent));
                var slice = _slices[i];
                slice.gameObject.SetActive(true);
                float angle = SliceAngle(i);
                float rad = angle * Mathf.Deg2Rad;
                slice.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius;
                slice.localEulerAngles = new Vector3(0, 0, -angle);
                var reward = _events.Wheel[i].Reward;
                var icon = slice.GetComponentInChildren<Image>(true);
                if (icon != null) icon.sprite = RewardIcon(reward);
                var label = slice.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = RewardText(reward, false);
            }
            for (int i = _events.Wheel.Count; i < _slices.Count; i++) _slices[i].gameObject.SetActive(false);
        }

        private void DrawButtons()
        {
            bool held = _events.WheelSpins > 0;
            _spinButton.gameObject.SetActive(held);
            _spinButton.interactable = held && !Spinning;
            if (_spinLabel != null)
                _spinLabel.text = _events.WheelSpins > 1 ? string.Format(L("wheel.spin_many", "Spin ({0})"), _events.WheelSpins) : L("wheel.spin", "Spin");
            _adSpinButton.gameObject.SetActive(!held);
            _adSpinButton.interactable = !held && !Spinning;
            if (_adSpinLabel != null) _adSpinLabel.text = L("wheel.ad_spin", "+1 Spin");
            _closeButton.interactable = !Spinning;
        }

        private Sprite RewardIcon(RewardCVO reward)
        {
            int booster = (int)reward.Booster;
            return reward.Coins > 0 || _boosterIcons == null || booster >= _boosterIcons.Length ? _coinIcon : _boosterIcons[booster];
        }

        private string RewardText(RewardCVO reward, bool named) =>
            reward.Coins > 0 ? (named ? string.Format(L("wheel.coins", "{0} coins"), reward.Coins) : reward.Coins.ToString())
            : named ? reward.BoosterCount + " " + L($"info.booster.{reward.Booster.ToString().ToLowerInvariant()}.title", reward.Booster.ToString())
            : "x" + reward.BoosterCount;

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
