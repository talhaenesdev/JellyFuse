using System;
using System.Collections;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The gameplay screen. Its HUD and result panels are authored in the prefab (GameplayScreen):
    /// every object is named for what it is - Btn_Undo, Txt_Time, Pnl_WinCard - and is moved,
    /// resized and recoloured there. A text's English in the prefab is its fallback; its key reads the
    /// language in use. The board is drawn in code inside Pnl_Board.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public partial class GameplayScreenView : ScreenView
    {
        [SerializeField] private CD_JellyPresentation _presentation;
        [Tooltip("How popups open (UI Widgets' PanelAnimator). Only read here to offer its numbers on the developer panel.")]
        [SerializeField] private Modules.UiWidgetsModule.Shared.Data.UnityObjects.CD_PanelAnimation _panelAnimation;
        [Header("Layout")]
        [Tooltip("The 760-unit-wide column everything sits in; scaled to the screen's width.")]
        [SerializeField] private RectTransform _content;
        [Tooltip("Where the column's size and the listed objects' places come from. Empty keeps 760 x 1000 and the prefab's places.")]
        [SerializeField] private Modules.UiWidgetsModule.Entities.ScreenLayout _layout;
        [Tooltip("The area the board is drawn in. Its edges come from CD_GameplayHud's map placement.")]
        [SerializeField] private RectTransform _board;
        [SerializeField] private JellySpriteBoardView _spriteBoard;
        [Header("HUD")]
        [SerializeField] private TMP_Text _coins;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private TMP_Text _time;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private Button _undo;
        [SerializeField] private TMP_Text _undoLabel;
        [SerializeField] private Button _settings;
        [Tooltip("The row of boosters under the map; the map keeps the side gap from its top.")]
        [SerializeField] private RectTransform _boosters;
        [Tooltip("Tapped three times within Dev Tools Tap Window seconds, it opens the developer panel.")]
        [SerializeField] private Button _devToolsTrigger;
        [SerializeField, Min(.1f)] private float _devToolsTapWindow = 1f;
        [Tooltip("The bar behind the HUD at the top; a developer switch shows or hides it.")]
        [SerializeField] private GameObject _topBar;
        [Tooltip("The bar behind the boosters at the bottom; a developer switch shows or hides it.")]
        [SerializeField] private GameObject _bottomBar;
        [SerializeField] private Button _restart;
        [SerializeField] private TMP_Text _restartLabel;
        [Header("Win, lose and retry panels")]
        [Tooltip("Each panel is a variant of the UiWidgets popup. Its dim layer is the panel's parent: shown and hidden as one.")]
        [SerializeField] private RectTransform _winPanel;
        [SerializeField] private TMP_Text _winTitle;
        [SerializeField] private TMP_Text _winLevel;
        [SerializeField] private TMP_Text _winReward;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _nextLabel;
        [SerializeField] private Button _winClose;
        [Tooltip("The level failed: the boosters on offer and Try Again. Its close button goes on to the retry panel.")]
        [SerializeField] private RectTransform _losePanel;
        [SerializeField] private TMP_Text _loseTitle;
        [SerializeField] private TMP_Text _loseBody;
        [SerializeField] private TMP_Text _loseBoostersLabel;
        [SerializeField] private Button _tryAgain;
        [SerializeField] private TMP_Text _tryAgainLabel;
        [SerializeField] private Button _loseClose;
        [Tooltip("The last word before leaving: Retry starts the level again, the close button leaves for the main menu.")]
        [SerializeField] private RectTransform _retryPanel;
        [SerializeField] private TMP_Text _retryTitle;
        [SerializeField] private TMP_Text _retryBody;
        [SerializeField] private Button _retryButton;
        [SerializeField] private TMP_Text _retryLabel;
        [SerializeField] private Button _retryClose;

        public event Action<Vector2Int, Vector2Int, bool> Dragged;
        public event Action UndoClicked;
        public event Action RestartClicked;
        public event Action NextLevelClicked;
        public event Action SettingsClicked;
        /// <summary>A jelly started melting into its door: the seconds it takes.</summary>
        public event Action<float> JellyMelting;
        /// <summary>The retry panel's close button: the player leaves the level for the main menu.</summary>
        public event Action QuitClicked;
        /// <summary>Three quick taps on the level label.</summary>
        public event Action DevToolsRequested;
#if UNITY_EDITOR
        /// <summary>Editor-only cheats: N next level, B previous level, W win, L lose.</summary>
        public event Action<int> SkipLevelPressed;
        public event Action<PuzzleStatus> ForceResultPressed;
        /// <summary>
        /// Editor-only play shortcuts. N / B jump a level, R restarts, U undoes, W wins and L loses
        /// the level at once. None of this is compiled into a build.
        /// </summary>
        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || _liveDrag) return;
            if (keyboard.nKey.wasPressedThisFrame) { HidePanels(); StopPresentation(); SkipLevelPressed?.Invoke(1); }
            else if (keyboard.bKey.wasPressedThisFrame) { HidePanels(); StopPresentation(); SkipLevelPressed?.Invoke(-1); }
            else if (keyboard.rKey.wasPressedThisFrame) { HidePanels(); StopPresentation(); RestartClicked?.Invoke(); }
            else if (keyboard.uKey.wasPressedThisFrame) UndoClicked?.Invoke();
            else if (keyboard.wKey.wasPressedThisFrame) ForceResultPressed?.Invoke(PuzzleStatus.Won);
            else if (keyboard.lKey.wasPressedThisFrame) ForceResultPressed?.Invoke(PuzzleStatus.Lost);
        }
#endif
        public CD_JellyPresentation Presentation => _presentation;
        private JellyDragSurface _dragSurface;
        private bool _built, _hasBoard;
        private PuzzleSnapshotRVO _snapshot;
        private Vector2 _dragStart;
        private Vector2Int _grabbedCell, _dragTarget;
        private int _heldId = -1;
        private bool _liveDrag;
        private Coroutine _panelDelay;
        private int _levelNumber = 1;
        private bool _hasNextLevel;
        private float _remaining;
        private Color _timeColour;
        private readonly Dictionary<int,string> _timeTexts = new();
        // Texts by key (LocalizationModule); the English the prefab shows is the fallback when a key has no text.
        private Func<string,string> _localize;
        private readonly List<(TMP_Text Label,string Key,string English)> _keyedTexts = new();
        private string _levelFormat;

        // The layout is authored on a 760-unit-wide column that stretches to the screen's height:
        // the HUD hugs the top, the buttons hug the bottom, and the board takes all that is left.
        private const float LAYOUT_WIDTH = 760, MIN_HEIGHT = 1000;

        /// <summary>Readies what the prefab cannot hold: the drag surface, the keyed texts. Runs once.</summary>
        private void Build()
        {
            if (_built) return;
            _built = true;
            _dragSurface = _board.gameObject.AddComponent<JellyDragSurface>();
            _dragSurface.Initialize(_board, OnGrab, OnDrag, OnDrop, CancelDrag);
            _spriteBoard.gameObject.SetActive(false);
            _spriteBoard.DeliveryStarted += seconds => JellyMelting?.Invoke(seconds);
            _timeColour = _time.color;
            Keyed(_undoLabel, "gameplay.undo");
            Keyed(_restartLabel, "gameplay.restart");
            Keyed(_winTitle, "gameplay.perfect");
            Keyed(_tryAgainLabel, "gameplay.try_again");
            Keyed(_loseBody, "gameplay.level_failed");
            Keyed(_loseBoostersLabel, "gameplay.select_boosters");
            Keyed(_retryBody, "gameplay.lose_life");
            Keyed(_retryLabel, "gameplay.retry");
            // The texts filled in with values keep the English they are written with here.
            _levelFormat = "lv {0}";
            HidePanels();
            FitLayout();
        }

        private void OnEnable()
        {
            if (_layout != null) _layout.Changed += FitLayout;
            _undo.onClick.AddListener(OnUndoClicked);
            _settings.onClick.AddListener(OnSettingsClicked);
            _restart.onClick.AddListener(OnRestartClicked);
            if (_devToolsTrigger != null) _devToolsTrigger.onClick.AddListener(OnDevToolsTapped);
            _nextButton.onClick.AddListener(OnNextClicked);
            _winClose.onClick.AddListener(OnNextClicked);
            _tryAgain.onClick.AddListener(OnRestartClicked);
            _loseClose.onClick.AddListener(OnLoseClosed);
            _retryButton.onClick.AddListener(OnRestartClicked);
            _retryClose.onClick.AddListener(OnQuitClicked);
            WireBoosters(true);
            WireInfo(true);
            if (_livesState != null) _lifeCountdown = StartCoroutine(CountLifeTime());
        }
        private void OnDisable()
        {
            if (_layout != null) _layout.Changed -= FitLayout;
            _undo.onClick.RemoveListener(OnUndoClicked);
            _settings.onClick.RemoveListener(OnSettingsClicked);
            _restart.onClick.RemoveListener(OnRestartClicked);
            if (_devToolsTrigger != null) _devToolsTrigger.onClick.RemoveListener(OnDevToolsTapped);
            _nextButton.onClick.RemoveListener(OnNextClicked);
            _winClose.onClick.RemoveListener(OnNextClicked);
            _tryAgain.onClick.RemoveListener(OnRestartClicked);
            _loseClose.onClick.RemoveListener(OnLoseClosed);
            _retryButton.onClick.RemoveListener(OnRestartClicked);
            _retryClose.onClick.RemoveListener(OnQuitClicked);
            WireBoosters(false);
            WireInfo(false);
            StopBoosters();
            StopLives();
            StopPresentation(); HidePanels();
            if (_spriteBoard != null) _spriteBoard.Clear();
            _hasBoard = false; _tutorial = null;
        }
        private void OnUndoClicked() { if (!_liveDrag) UndoClicked?.Invoke(); }
        private void OnSettingsClicked() => SettingsClicked?.Invoke();
        // Sent to the background mid-level: the pause panel is opened, as its button would, so the player comes back to
        // it and goes on when ready. Only while a level is being played - not over a win or a loss.
        private void OnApplicationPause(bool paused)
        {
            if (!paused || !isActiveAndEnabled || !_hasBoard || _snapshot == null || _snapshot.Status != PuzzleStatus.Playing) return;
            SettingsClicked?.Invoke();
        }
        private int _devToolsTaps;
        private float _devToolsFirstTap;
        // Raw input: three taps close together are one request.
        private void OnDevToolsTapped()
        {
            float now = Time.unscaledTime;
            if (_devToolsTaps == 0 || now - _devToolsFirstTap > _devToolsTapWindow) { _devToolsTaps = 0; _devToolsFirstTap = now; }
            if (++_devToolsTaps < 3) return;
            _devToolsTaps = 0;
            DevToolsRequested?.Invoke();
        }
        private void OnRestartClicked() { HidePanels(); StopPresentation(); RestartClicked?.Invoke(); }
        // With no level after this one, Continue plays this one again.
        private void OnNextClicked()
        {
            if (!_hasNextLevel) { OnRestartClicked(); return; }
            HidePanels(); NextLevelClicked?.Invoke();
        }
        // Closing the lose panel asks once more before the level is left.
        private void OnLoseClosed() { HidePanels(); ShowDim(_retryPanel); }
        private void OnQuitClicked() { HidePanels(); StopPresentation(); QuitClicked?.Invoke(); }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            if (_presentation == null || _presentation.Feel == null || _presentation.Look == null || _presentation.Hud == null)
                throw new InvalidOperationException("GameplayScreen requires CD_JellyPresentation with its Feel, Look and Hud set.");
            Build(); ApplySavedFeel(); StopPresentation(); _snapshot = null;
        }

        public void ConfigurePresentation()
        {
            if (_hasBoard) return;
            _hasBoard = true;
            _spriteBoard.gameObject.SetActive(true);
            _spriteBoard.Initialize(_presentation);
            if (_snapshot != null) _spriteBoard.Present(_snapshot, _heldId);
            if (_tutorial != null) ApplyTutorial(_tutorial);
        }

        public override void AfterScreenActivation()
        {
            base.AfterScreenActivation();
            var layer = transform.parent;
            if (layer.GetComponent<Canvas>() == null) layer.gameObject.AddComponent<Canvas>();
            if (layer.GetComponent<GraphicRaycaster>() == null) layer.gameObject.AddComponent<GraphicRaycaster>();
            FitLayout();
        }

        private void OnRectTransformDimensionsChange() { if (_built) FitLayout(); }

        private void FitLayout()
        {
            // Fit the column's width to the screen; a short, wide screen fits its height instead.
            var rect = ((RectTransform)transform).rect;
            var config = _layout != null ? _layout.Config : null;
            float scale = config != null
                ? config.FitScale(rect.size)
                : Mathf.Max(.01f, Mathf.Min(rect.width / LAYOUT_WIDTH, rect.height / MIN_HEIGHT));
            _content.localScale = Vector3.one * scale;
            _content.sizeDelta = new Vector2(rect.width / scale, rect.height / scale);
            // The listed objects take their places before the board measures the buttons around it.
            if (_layout != null) _layout.Apply();
            var hud = _presentation.Hud;
            float left = _content.sizeDelta.x * hud.MapSideMargin, right = left;
            float top = hud.MapTopSpace, bottom = hud.MapBottomSpace;
            bool aligned = hud.MapAlignToButtons && _undo != null && _settings != null;
            if (aligned)
            {
                // The map spans the row of buttons above it: the undo button's left edge to the settings button's right edge.
                var parent = (RectTransform)_board.parent;
                Canvas.ForceUpdateCanvases();
                var undo = (RectTransform)_undo.transform;
                left = Edge(undo, parent, true) - parent.rect.xMin + hud.MapInsetLeft;
                right = parent.rect.xMax - Edge((RectTransform)_settings.transform, parent, false) + hud.MapInsetRight;
                // Above and below, it may come as close to the buttons as the buttons are to the screen's side;
                // a map taller than that shrinks to fit.
                float gap = Edge(undo, parent, true) - parent.rect.xMin + hud.MapVerticalGapExtra;
                top = parent.rect.yMax - Mathf.Min(Bottom(undo, parent), Bottom((RectTransform)_settings.transform, parent)) + gap;
                if (_boosters != null && _boosters.gameObject.activeInHierarchy) bottom = Top(_boosters, parent) - parent.rect.yMin + gap;
            }
            // Full width: the map reaches the screen's sides, a few screen pixels in.
            if (hud.MapFullWidth)
            {
                // Screen pixels into the column's units: through the canvas's scale and the column's own.
                var canvas = GetComponentInParent<Canvas>();
                float pixels = canvas != null ? canvas.rootCanvas.scaleFactor : 1;
                left = right = hud.MapEdgePixels / Mathf.Max(.01f, pixels * scale);
            }
            _board.offsetMin = new Vector2(left, bottom); _board.offsetMax = new Vector2(-right, -top);
            if (_spriteBoard != null) _spriteBoard.FillWidth = !aligned && !hud.MapFullWidth && hud.MapFillWidth;
        }
        private static float Bottom(RectTransform rect, RectTransform space) => Corner(rect, space, 0).y;
        private static float Top(RectTransform rect, RectTransform space) => Corner(rect, space, 1).y;
        private static Vector3 Corner(RectTransform rect, RectTransform space, int corner)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return space.InverseTransformPoint(corners[corner]);
        }
        /// <summary>The left or right edge of a rect, in another rect's local space.</summary>
        private static float Edge(RectTransform rect, RectTransform space, bool leftEdge)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return space.InverseTransformPoint(corners[leftEdge ? 0 : 2]).x;
        }

        private void OnGrab(Vector2 point)
        {
            if (_liveDrag || !_hasBoard || _snapshot == null || _snapshot.Status != PuzzleStatus.Playing) return;
            if (PickBoosterTarget(point)) return;
            bool picked = _spriteBoard.Pick(point, _board.rect.size, out _grabbedCell, out _heldId);
            if (!picked) return;
            _dragStart = BoardPoint(point); _dragTarget = _grabbedCell; _liveDrag = true;
            _spriteBoard.BeginFollow(point, _board.rect.size);
            Dragged?.Invoke(_grabbedCell, _dragTarget, false);
        }

        private void OnDrag(Vector2 point)
        {
            if (!_liveDrag) return;
            _spriteBoard.Follow(point, _board.rect.size);
            var delta = BoardPoint(point) - _dragStart;
            var target = _grabbedCell + new Vector2Int(Mathf.RoundToInt(delta.x), Mathf.RoundToInt(delta.y));
            if (target == _dragTarget) return;
            _dragTarget = target; Dragged?.Invoke(_grabbedCell, target, false);
        }

        private void OnDrop(Vector2 point)
        {
            if (!_liveDrag) return; OnDrag(point); if (!_liveDrag) return;
            _liveDrag = false; _heldId = -1; EndFollow(); Dragged?.Invoke(_grabbedCell, _dragTarget, true);
        }
        private void EndFollow() { if (_hasBoard) _spriteBoard.EndFollow(); }

        private Vector2 BoardPoint(Vector2 point) => _spriteBoard.BoardPoint(point, _board.rect.size);

        private void CancelDrag()
        {
            if (!_liveDrag) return; _liveDrag = false; _heldId = -1; EndFollow(); Dragged?.Invoke(_grabbedCell, _dragTarget, true);
        }

        public void ApplyBoard(PuzzleTurnVO turn)
        {
            if (turn.Frames.Count == 0) return;
            _snapshot = turn.Frames[turn.Frames.Count - 1];
            bool heldExists = false;
            foreach (var piece in _snapshot.Pieces) if (piece.Id == _heldId) heldExists = true;
            if (!heldExists || _snapshot.Status != PuzzleStatus.Playing) StopPresentation();
            if (_snapshot.Status != PuzzleStatus.Playing) Disarm();
            if (_hasBoard) _spriteBoard.Present(_snapshot, _heldId, false, turn.Frames);
            UpdateHud();
            ShowDifficulty();
        }
        private void HidePanels()
        {
            // Leaving the win panel early shows the whole count at once.
            if (_pendingReward > 0 || _coinFlight != null) FinishCoinFlight();
            if (_panelDelay != null) { StopCoroutine(_panelDelay); _panelDelay = null; }
            EndOutOfLives();
            if (_winPanel != null) { _winPanel.parent.gameObject.SetActive(false); _losePanel.parent.gameObject.SetActive(false); _retryPanel.parent.gameObject.SetActive(false); }
        }
        private void UpdatePanels()
        {
            if (_snapshot.Status == PuzzleStatus.Playing) { HidePanels(); return; }
            var panel = _snapshot.Status == PuzzleStatus.Won ? _winPanel : _losePanel;
            if (panel.parent.gameObject.activeSelf || _retryPanel.parent.gameObject.activeSelf || _panelDelay != null) return;
            FillPanels();
            // The win waits a moment so the last jelly is seen going into its door.
            _panelDelay = StartCoroutine(ShowPanel(panel, _snapshot.Status == PuzzleStatus.Won ? _presentation.Hud.WinPanelDelay : _presentation.Hud.LosePanelDelay));
        }
        /// <summary>The result texts, in the language in use; also redrawn when the language changes.</summary>
        private void FillPanels()
        {
            string level = LF("gameplay.level_title", "Level {0}", _levelNumber);
            _winLevel.text = level; _loseTitle.text = level; _retryTitle.text = level;
            _nextLabel.text = _hasNextLevel ? L("gameplay.continue", "Continue") : L("gameplay.play_again", "Play again");
        }
        private IEnumerator ShowPanel(RectTransform panel, float delay)
        {
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            _panelDelay = null;
            ShowDim(panel);
            if (panel == _winPanel) StartCoinFlight();
        }
        private static void ShowDim(RectTransform panel) { panel.parent.gameObject.SetActive(true); panel.parent.SetAsLastSibling(); }
        public void ApplyLevel(int number, bool hasNext) { _levelNumber = number; _hasNextLevel = hasNext; if (_built) _levelLabel.text = LF("gameplay.level", _levelFormat, number); HidePanels(); }
        /// <summary>The function that turns a key into text in the language in use (ILocalizationService.Get). Redraws every text.</summary>
        public void SetLocalizer(Func<string,string> localize) { _localize = localize; RefreshTexts(); }
        /// <summary>The game's language changed.</summary>
        public void ApplyLanguage(string code) => RefreshTexts();
        private void RefreshTexts()
        {
            foreach (var keyed in _keyedTexts) if (keyed.Label != null) keyed.Label.text = L(keyed.Key, keyed.English);
            if (_built) _levelLabel.text = LF("gameplay.level", _levelFormat, _levelNumber);
            if (_snapshot != null && _built && _snapshot.Status != PuzzleStatus.Playing) FillPanels();
            DrawLifeTime();
            ShowTutorialText();
            ShowDifficulty();
        }
        private TMP_Text _difficulty;
        /// <summary>Hard or Super Hard under the timer, from the level being played; nothing on a Normal level.</summary>
        private void ShowDifficulty()
        {
            var difficulty = _snapshot?.Level?.Difficulty ?? LevelDifficulty.Normal;
            if (difficulty == LevelDifficulty.Normal || _time == null) { if (_difficulty != null) _difficulty.gameObject.SetActive(false); return; }
            var clock = (RectTransform)_time.transform.parent;
            var hud = _presentation.Hud;
            if (_difficulty == null)
            {
                _difficulty = new GameObject("Txt_Difficulty", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                _difficulty.gameObject.layer = clock.gameObject.layer;
                _difficulty.rectTransform.SetParent(clock.parent, false);
                _difficulty.font = _time.font;
                _difficulty.fontStyle = FontStyles.Bold;
                _difficulty.alignment = TextAlignmentOptions.Top;
                _difficulty.outlineWidth = .25f; _difficulty.outlineColor = new Color32(40, 30, 60, 255);
                _difficulty.raycastTarget = false;
            }
            // Under the clock, as wide as it, read again each time so a change to the HUD shows.
            var rect = _difficulty.rectTransform;
            rect.anchorMin = clock.anchorMin; rect.anchorMax = clock.anchorMax; rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = clock.anchoredPosition - new Vector2(0, clock.rect.height * clock.pivot.y + hud.DifficultyOffset);
            rect.sizeDelta = new Vector2(Mathf.Max(clock.rect.width, 300), hud.DifficultyTextSize * 1.4f);
            _difficulty.fontSize = hud.DifficultyTextSize;
            bool super = difficulty == LevelDifficulty.SuperHard;
            _difficulty.color = super ? hud.SuperHardColour : hud.HardColour;
            _difficulty.text = super ? L("gameplay.difficulty.super_hard", "Super Hard") : L("gameplay.difficulty.hard", "Hard");
            _difficulty.gameObject.SetActive(true);
        }
        private string L(string key, string english)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? english : text;
        }
        private string LF(string key, string english, params object[] values)
        {
            try { return string.Format(L(key, english), values); }
            catch (FormatException) { return string.Format(english, values); }
        }
        /// <summary>A text read by key; the English the prefab shows is its fallback.</summary>
        private void Keyed(TMP_Text label, string key)
        {
            if (label == null) return;
            _keyedTexts.Add((label, key, label.text));
            label.text = L(key, label.text);
        }
        public void ShowTopBar(bool on) { if (_topBar != null) _topBar.SetActive(on); }
        public void ShowBottomBar(bool on) { if (_bottomBar != null) _bottomBar.SetActive(on); }
        private TutorialVeil _tutorialVeil;
        // The tutorial can arrive before the board is set up; it is shown once the board is.
        private JellyTutorialRVO _tutorial;
        /// <summary>The level's tutorial: dark round the cells it uses, with a hand showing the move; None hides it.</summary>
        public void ApplyTutorial(JellyTutorialRVO tutorial)
        {
            _tutorial = tutorial;
            if (tutorial == null || !tutorial.Active || !_hasBoard) { if (_tutorialVeil != null) _tutorialVeil.Hide(); return; }
            if (_tutorialVeil == null) _tutorialVeil = TutorialVeil.Create((RectTransform)transform);
            _tutorialVeil.Show(_spriteBoard, _presentation.Look, tutorial, () => _liveDrag);
            ShowTutorialText();
        }
        /// <summary>The tutorial step's text in the language in use - again when the language changes.</summary>
        private void ShowTutorialText()
        {
            if (_tutorialVeil == null || _tutorial == null || !_tutorial.Active) return;
            bool none = string.IsNullOrEmpty(_tutorial.TextKey) && string.IsNullOrEmpty(_tutorial.Text);
            _tutorialVeil.ShowText(none ? null : string.IsNullOrEmpty(_tutorial.TextKey) ? _tutorial.Text : L(_tutorial.TextKey, _tutorial.Text ?? ""));
        }
        public void StopPresentation() { if (_tutorialVeil != null) _tutorialVeil.Hide(); _liveDrag = false; _heldId = -1; EndFollow(); if (_dragSurface != null) _dragSurface.ResetPointer(); }
        [ContextMenu("Refresh presentation from config")]
        public void RefreshPresentation() { if (_built) FitLayout(); if (_hasBoard) _spriteBoard.RefreshSettings(); }
        public void ApplyTime(float seconds)
        {
            _remaining = seconds;
            if (!_built) return;
            int whole = Mathf.Max(0, Mathf.CeilToInt(seconds));
            // Each mm:ss is built once and reused, so the clock leaves no garbage behind each second.
            if (!_timeTexts.TryGetValue(whole, out var text)) _timeTexts[whole] = text = $"{whole / 60:00}:{whole % 60:00}";
            if (!ReferenceEquals(_time.text, text)) _time.text = text;
            _time.color = _clockFrozen ? _presentation.Hud.FrostTimeColour : seconds <= _presentation.Hud.TimeWarningSeconds ? _presentation.Hud.TimeWarningColour : _timeColour;
        }
        private void UpdateHud()
        {
            _levelLabel.text = LF("gameplay.level", _levelFormat, _levelNumber);
            _undo.interactable = _snapshot.CanUndo && !_liveDrag;
            _message.text = _snapshot.Warning ?? "";
            UpdatePanels();
        }
    }
}
