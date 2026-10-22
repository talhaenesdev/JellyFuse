using System;
using System.Collections;
using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The booster row under the map. Each button of Pnl_Boosters shows one booster, in the order
    /// CD_Boosters lists them: its uses left, its price once none are left, or the level it opens at.
    /// A rocket and a UFO wait for a target - a door, a jelly - before they are used; a freeze is
    /// used at once. Whether a booster can be used at all is decided by the game, and arrives as
    /// BoosterStateRVO.Usable.
    /// </summary>
    public partial class GameplayScreenView
    {
        /// <summary>A booster and its target: the door for a rocket, the colour (JellyColour) for a UFO, 0 for a freeze.</summary>
        public event Action<BoosterType, int> BoosterChosen;

        private sealed class BoosterButton
        {
            public Button Button;
            public Image Icon;
            // The tile behind the button: a sibling laid under it, so the button's own image stays the icon.
            public Image Slot;
            public TMP_Text Badge;
            public UnityEngine.Events.UnityAction Clicked;
            public BoosterStateRVO State;
        }
        private readonly List<BoosterButton> _boosterButtons = new();
        private bool _boosterArmed;
        private BoosterType _armedType;
        private bool _clockFrozen;
        private Image _frost;
        private Image _ufo;
        private Coroutine _ufoFlight;
        private TMP_Text _boosterHint;
        private Coroutine _hintTimeout;
        // The tutorial's veil, laid over everything but what the armed booster may take.
        private TutorialVeil _boosterVeil;
        // A booster lesson: after its info page the player must use this booster; everything else is dark until then.
        private bool _lesson;
        private BoosterType _lessonType;

        private void BuildBoosters()
        {
            if (_boosterButtons.Count > 0 || _boosters == null) return;
            var hud = _presentation.Hud;
            var buttons = new List<Transform>();
            foreach (Transform child in _boosters) buttons.Add(child);
            foreach (Transform child in buttons)
            {
                var button = child.GetComponent<Button>();
                if (button == null) continue;
                var entry = new BoosterButton { Button = button, Icon = button.image };
                if (hud.BoosterSlotSprite != null)
                {
                    var slot = new GameObject("Img_BoosterSlot", typeof(RectTransform)).AddComponent<Image>();
                    var rect = (RectTransform)child;
                    slot.rectTransform.SetParent(_boosters, false);
                    slot.rectTransform.SetSiblingIndex(child.GetSiblingIndex());
                    slot.rectTransform.anchorMin = rect.anchorMin; slot.rectTransform.anchorMax = rect.anchorMax; slot.rectTransform.pivot = rect.pivot;
                    slot.rectTransform.anchoredPosition = rect.anchoredPosition;
                    slot.rectTransform.sizeDelta = rect.sizeDelta * hud.BoosterSlotScale;
                    slot.sprite = hud.BoosterSlotSprite; slot.preserveAspect = true; slot.raycastTarget = false;
                    slot.gameObject.SetActive(false);
                    entry.Slot = slot;
                }
                var badge = new GameObject("Txt_BoosterBadge", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                badge.rectTransform.SetParent(button.transform, false);
                badge.rectTransform.anchorMin = new Vector2(.5f, 0); badge.rectTransform.anchorMax = new Vector2(1, .4f);
                badge.rectTransform.offsetMin = badge.rectTransform.offsetMax = Vector2.zero;
                badge.font = _time.font; badge.fontSize = 34; badge.alignment = TextAlignmentOptions.BottomRight;
                badge.fontStyle = FontStyles.Bold; badge.outlineWidth = .25f; badge.outlineColor = new Color32(40, 30, 60, 255);
                badge.raycastTarget = false;
                entry.Badge = badge;
                int index = _boosterButtons.Count;
                entry.Clicked = () => OnBoosterClicked(index);
                _boosterButtons.Add(entry);
                child.gameObject.SetActive(false);
            }
        }
        private void WireBoosters(bool on)
        {
            BuildBoosters();
            foreach (var entry in _boosterButtons)
                if (on) entry.Button.onClick.AddListener(entry.Clicked); else entry.Button.onClick.RemoveListener(entry.Clicked);
        }

        public void ApplyBoosters(IReadOnlyList<BoosterStateRVO> states)
        {
            BuildBoosters();
            for (int i = 0; i < _boosterButtons.Count; i++)
            {
                var entry = _boosterButtons[i];
                entry.State = i < states.Count ? states[i] : null;
                entry.Button.gameObject.SetActive(entry.State != null);
                if (entry.Slot != null) entry.Slot.gameObject.SetActive(entry.State != null);
                if (entry.State == null) continue;
                if (entry.State.Icon != null) entry.Icon.sprite = entry.State.Icon;
                entry.Badge.text = !entry.State.Unlocked ? LF("gameplay.booster_locked", "Lv {0}", entry.State.UnlockLevel)
                    : entry.State.Count > 0 ? entry.State.Count.ToString() : entry.State.Price.ToString();
            }
            if (_boosterArmed && !ArmedStillUsable()) Disarm();
            if (_lesson && !LessonButton(out _)) EndBoosterLesson();
            PaintBoosters();
        }
        private bool ArmedStillUsable()
        {
            foreach (var entry in _boosterButtons) if (entry.State != null && entry.State.Type == _armedType) return entry.State.Usable;
            return false;
        }
        private void PaintBoosters()
        {
            var hud = _presentation.Hud;
            foreach (var entry in _boosterButtons)
            {
                if (entry.State == null) continue;
                entry.Icon.color = _boosterArmed && entry.State.Type == _armedType ? hud.BoosterArmedColour : entry.State.Usable ? Color.white : hud.BoosterLockedColour;
                if (entry.Slot != null) entry.Slot.color = entry.State.Usable ? hud.BoosterSlotColour : hud.BoosterSlotLockedColour;
            }
        }

        // Raw input: the button offers its booster - at once for a freeze, after a target for the others.
        private void OnBoosterClicked(int index)
        {
            var state = _boosterButtons[index].State;
            if (state == null || _liveDrag || _snapshot == null || _snapshot.Status != PuzzleStatus.Playing) return;
            // In a lesson only the booster taught is taken, and once armed it cannot be let go.
            if (_lesson && state.Type != _lessonType) return;
            if (_boosterArmed && _armedType == state.Type) { if (!_lesson) Disarm(); return; }
            if (!state.Usable) return;
            Disarm();
            if (state.Type == BoosterType.FreezeTime) { EndBoosterLesson(); BoosterChosen?.Invoke(state.Type, 0); ShowHint(state.Type, 2); return; }
            ShowHint(state.Type, 0);
            _boosterArmed = true; _armedType = state.Type;
            if (_hasBoard)
                _spriteBoard.SetFocus(state.Type == BoosterType.Rocket ? JellySpriteBoardView.Focus.Doors : JellySpriteBoardView.Focus.Jellies,
                    _presentation.Hud.BoosterDimAlpha, _presentation.Hud.BoosterPulseSpeed, _presentation.Hud.BoosterDoorGlow, _presentation.Hud.BoosterFrozenDoorVeil);
            ShowBoosterVeil(state.Type);
            PaintBoosters();
        }
        private void ShowBoosterVeil(BoosterType type, RectTransform alsoOpen = null)
        {
            if (!_hasBoard) return;
            if (_boosterVeil == null) _boosterVeil = TutorialVeil.Create((RectTransform)transform);
            var focus = type == BoosterType.Rocket ? JellySpriteBoardView.Focus.Doors : JellySpriteBoardView.Focus.Jellies;
            // Only the armed button stays clear, so it can be tapped again to let go; the other boosters and the hint are dark.
            // In a lesson it is dark too: the booster must be used.
            RectTransform armed = null;
            if (!_lesson) foreach (var entry in _boosterButtons) if (entry.State != null && entry.State.Type == type) armed = (RectTransform)entry.Button.transform;
            _boosterVeil.ShowFocus(_spriteBoard, _presentation.Look, areas => _spriteBoard.FocusAreas(focus, areas), armed, alsoOpen);
        }
        /// <summary>After a booster's info page: everything is dark but its button, until the player has used it.</summary>
        private void StartBoosterLesson(BoosterType type)
        {
            _lesson = true; _lessonType = type;
            if (!LessonButton(out var button)) { EndBoosterLesson(); return; }
            if (_boosterVeil == null) _boosterVeil = TutorialVeil.Create((RectTransform)transform);
            _boosterVeil.ShowFocus(_hasBoard ? _spriteBoard : null, _presentation.Look, null, button);
        }
        /// <summary>The taught booster's button, while it can be used.</summary>
        private bool LessonButton(out RectTransform button)
        {
            button = null;
            foreach (var entry in _boosterButtons)
                if (entry.State != null && entry.State.Type == _lessonType && entry.State.Usable && entry.Button.gameObject.activeInHierarchy)
                    button = (RectTransform)entry.Button.transform;
            return button != null;
        }
        private void EndBoosterLesson()
        {
            if (!_lesson) return;
            _lesson = false;
            if (!_boosterArmed && _boosterVeil != null) _boosterVeil.Hide();
        }
        /// <summary>A tap on the board while a booster waits for its target; true when the tap was taken for it.</summary>
        private bool PickBoosterTarget(Vector2 point)
        {
            if (!_boosterArmed) return false;
            var type = _armedType;
            if (type == BoosterType.Rocket && _spriteBoard.PickDoor(point, _board.rect.size, out int door)) { _lesson = false; Disarm(); BoosterChosen?.Invoke(type, door); }
            else if (type == BoosterType.Ufo && _spriteBoard.PickColour(point, _board.rect.size, out var colour))
            {
                // A colour a frozen jelly or a frozen door still needs cannot be taken: it shakes, the UFO stays ready and says why.
                if (!_spriteBoard.UfoCanTake(colour)) { _spriteBoard.ShakeColour(colour); ShowBlockedHint(); return true; }
                _lesson = false; Disarm(); BoosterChosen?.Invoke(type, (int)colour);
            }
            return true;
        }
        private void Disarm()
        {
            if (!_boosterArmed) return;
            _boosterArmed = false;
            if (_boosterVeil != null) _boosterVeil.Hide();
            HideHint();
            if (_hasBoard) _spriteBoard.SetFocus(JellySpriteBoardView.Focus.None, 1, 0);
            PaintBoosters();
        }

        /// <summary>Why the tapped colour cannot be taken, for a moment - lit through the veil - then the UFO's own hint again.</summary>
        private void ShowBlockedHint()
        {
            ShowHint(BoosterType.Ufo, 0);
            _boosterHint.text = L("gameplay.booster_hint.ufo_frozen", "A frozen jelly or door still needs this colour. Pick another one.");
            ShowBoosterVeil(BoosterType.Ufo, _boosterHint.rectTransform);
            _hintTimeout = StartCoroutine(RestoreHintAfter(2));
        }
        private IEnumerator RestoreHintAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _hintTimeout = null;
            if (!_boosterArmed) yield break;
            ShowHint(_armedType, 0);
            ShowBoosterVeil(_armedType);
        }
        /// <summary>What the booster does, under the board; for <paramref name="seconds"/> when above 0, else until it is used or let go.</summary>
        private void ShowHint(BoosterType type, float seconds)
        {
            if (_boosters == null) return;
            if (_boosterHint == null)
            {
                _boosterHint = new GameObject("Txt_BoosterHint", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                var rect = _boosterHint.rectTransform;
                rect.SetParent(_boosters.parent, false);
                rect.anchorMin = _boosters.anchorMin; rect.anchorMax = _boosters.anchorMax; rect.pivot = new Vector2(.5f, 0);
                rect.anchoredPosition = _boosters.anchoredPosition + new Vector2(0, _boosters.rect.height * (1 - _boosters.pivot.y) + 8);
                rect.sizeDelta = new Vector2(680, 80);
                _boosterHint.font = _time.font; _boosterHint.fontSize = 30; _boosterHint.alignment = TextAlignmentOptions.Bottom;
                _boosterHint.textWrappingMode = TextWrappingModes.Normal;
                _boosterHint.outlineWidth = .2f; _boosterHint.outlineColor = new Color32(40, 30, 60, 255);
                _boosterHint.raycastTarget = false;
            }
            _boosterHint.text = type switch
            {
                BoosterType.Rocket => L("gameplay.booster_hint.rocket", "Tap a door: the jellies it is waiting for disappear."),
                BoosterType.FreezeTime => L("gameplay.booster_hint.freeze", "Time is frozen!"),
                _ => L("gameplay.booster_hint.ufo", "Tap a jelly: every jelly of its colour and its doors disappear."),
            };
            _boosterHint.gameObject.SetActive(true);
            if (_hintTimeout != null) { StopCoroutine(_hintTimeout); _hintTimeout = null; }
            if (seconds > 0) _hintTimeout = StartCoroutine(HideHintAfter(seconds));
        }
        private IEnumerator HideHintAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _hintTimeout = null;
            HideHint();
        }
        private void HideHint()
        {
            if (_hintTimeout != null) { StopCoroutine(_hintTimeout); _hintTimeout = null; }
            if (_boosterHint != null) _boosterHint.gameObject.SetActive(false);
        }

        public void ApplyBoosterUsed(BoosterType type, int target)
        {
            if (type == BoosterType.Ufo) FlyUfo();
        }

        /// <summary>Seconds the clock stands still for; 0 when it runs again. Frost lies over the clock meanwhile.</summary>
        public void ApplyClockFrozen(float seconds)
        {
            _clockFrozen = seconds > 0;
            if (_clockFrozen && _frost == null) BuildFrost();
            if (_frost != null) _frost.gameObject.SetActive(_clockFrozen);
            if (_built) ApplyTime(_remaining);
        }
        private void BuildFrost()
        {
            var clock = (RectTransform)_time.transform.parent;
            var background = clock.GetComponent<Image>();
            _frost = new GameObject("Img_Frost", typeof(RectTransform)).AddComponent<Image>();
            _frost.rectTransform.SetParent(clock, false);
            _frost.rectTransform.SetAsLastSibling();
            _frost.rectTransform.anchorMin = Vector2.zero; _frost.rectTransform.anchorMax = Vector2.one;
            _frost.rectTransform.offsetMin = new Vector2(-6, -6); _frost.rectTransform.offsetMax = new Vector2(6, 6);
            var hud = _presentation.Hud;
            _frost.sprite = hud.FrostSprite != null ? hud.FrostSprite : background != null ? background.sprite : null;
            if (hud.FrostSprite == null && background != null) { _frost.type = background.type; _frost.pixelsPerUnitMultiplier = background.pixelsPerUnitMultiplier; }
            _frost.color = hud.FrostColour;
            _frost.raycastTarget = false;
        }

        private void FlyUfo()
        {
            var hud = _presentation.Hud;
            if (_ufo == null)
            {
                _ufo = new GameObject("Img_Ufo", typeof(RectTransform)).AddComponent<Image>();
                _ufo.rectTransform.SetParent(_board, false);
                _ufo.raycastTarget = false;
                _ufo.preserveAspect = true;
            }
            _ufo.sprite = hud.UfoSprite;
            _ufo.rectTransform.sizeDelta = hud.UfoSize;
            _ufo.rectTransform.anchorMin = _ufo.rectTransform.anchorMax = new Vector2(.5f, 1);
            _ufo.rectTransform.SetAsLastSibling();
            if (_ufoFlight != null) StopCoroutine(_ufoFlight);
            _ufoFlight = StartCoroutine(Fly(hud.UfoFlightSeconds, hud.UfoSize));
        }
        private IEnumerator Fly(float seconds, Vector2 size)
        {
            _ufo.gameObject.SetActive(true);
            float half = _content.sizeDelta.x * .5f + size.x;
            float y = -_board.rect.height * .3f;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                _ufo.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-half, half, k), y + Mathf.Sin(k * Mathf.PI * 4) * size.y * .15f);
                _ufo.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 6) * 6);
                yield return null;
            }
            _ufo.gameObject.SetActive(false);
            _ufoFlight = null;
        }
        private void StopBoosters()
        {
            _lesson = false;
            if (_boosterVeil != null) _boosterVeil.Hide();
            Disarm();
            HideHint();
            if (_ufoFlight != null) { StopCoroutine(_ufoFlight); _ufoFlight = null; }
            if (_ufo != null) _ufo.gameObject.SetActive(false);
        }
    }
}
