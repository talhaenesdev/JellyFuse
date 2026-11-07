using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators
{
    /// <summary>
    /// The errors page: the errors and exceptions this run logged, newest first, in one scrolling text -
    /// what a store build would otherwise only tell a cable. Built in code over the tuning page's area.
    /// </summary>
    public sealed partial class DevToolsScreenView
    {
        [Header("Errors page")]
        [Tooltip("Opens the errors page. A copy of any panel button, its label set from code.")]
        [SerializeField] private Button _errorsButton;

        /// <summary>The errors page opened, or Refresh was pressed on it.</summary>
        public Action ErrorsRequested;
        /// <summary>Clear was pressed on the errors page.</summary>
        public Action ErrorsClearClicked;

        private RectTransform _errorsPage;
        private RectTransform _errorsContent;
        private TMP_Text _errorsText;
        private Button _errorsBack, _errorsClear, _errorsRefresh;

        private void WireErrors(bool on)
        {
            if (_errorsButton == null) return;
            if (on)
            {
                _errorsButton.onClick.AddListener(OpenErrors);
                var label = _errorsButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = L("devtools.errors", "Errors");
            }
            else
            {
                _errorsButton.onClick.RemoveListener(OpenErrors);
                CloseErrors();
            }
        }

        private void OpenErrors()
        {
            BuildErrorsPage();
            foreach (var go in _mainPage) if (go != null) go.SetActive(false);
            _errorsPage.gameObject.SetActive(true);
            ErrorsRequested?.Invoke();
        }

        private void CloseErrors()
        {
            if (_errorsPage == null || !_errorsPage.gameObject.activeSelf) return;
            _errorsPage.gameObject.SetActive(false);
            foreach (var go in _mainPage) if (go != null) go.SetActive(true);
        }

        /// <summary>The entries to list, oldest first as logged; shown newest on top.</summary>
        public void ShowErrors(IReadOnlyList<string> entries)
        {
            if (_errorsText == null) return;
            if (entries == null || entries.Count == 0)
            {
                _errorsText.text = L("devtools.errors_empty", "No errors logged since the game started.");
            }
            else
            {
                var lines = new System.Text.StringBuilder();
                for (int i = entries.Count - 1; i >= 0; i--)
                    lines.Append("<color=#FF8A80><noparse>").Append(entries[i]).Append("</noparse></color>\n\n");
                _errorsText.text = lines.ToString();
            }
            _errorsText.ForceMeshUpdate();
            _errorsContent.sizeDelta = new Vector2(0, _errorsText.preferredHeight + 24);
            _errorsContent.anchoredPosition = Vector2.zero;
        }

        private void BuildErrorsPage()
        {
            if (_errorsPage != null) return;
            var font = _title != null ? _title.font : TMP_Settings.defaultFontAsset;
            _errorsPage = new GameObject("Pnl_Errors", typeof(RectTransform)).GetComponent<RectTransform>();
            _errorsPage.SetParent(_slidersArea, false);
            _errorsPage.anchorMin = Vector2.zero; _errorsPage.anchorMax = Vector2.one;
            _errorsPage.offsetMin = _errorsPage.offsetMax = Vector2.zero;

            var view = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            view.SetParent(_errorsPage, false);
            view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one; view.offsetMin = new Vector2(0, 92); view.offsetMax = Vector2.zero;
            view.GetComponent<Image>().color = new Color(0, 0, 0, .55f);
            _errorsContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _errorsContent.SetParent(view, false);
            _errorsContent.anchorMin = new Vector2(0, 1); _errorsContent.anchorMax = new Vector2(1, 1); _errorsContent.pivot = new Vector2(.5f, 1);
            _errorsContent.sizeDelta = Vector2.zero;
            var scroll = _errorsPage.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view; scroll.content = _errorsContent; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            _errorsText = Text("Txt_Errors", _errorsContent, font, 18, TextAlignmentOptions.TopLeft);
            _errorsText.textWrappingMode = TextWrappingModes.Normal;
            _errorsText.richText = true;
            _errorsText.rectTransform.anchorMin = Vector2.zero; _errorsText.rectTransform.anchorMax = Vector2.one;
            _errorsText.rectTransform.offsetMin = new Vector2(12, 12); _errorsText.rectTransform.offsetMax = new Vector2(-12, -12);

            // Back, Refresh and Clear along the bottom: copies of the panel's own button.
            _errorsBack = BottomButton("Btn_Back", 0, 1 / 3f, L("devtools.back", "Back"), CloseErrors);
            _errorsRefresh = BottomButton("Btn_Refresh", 1 / 3f, 2 / 3f, L("devtools.refresh", "Refresh"), () => ErrorsRequested?.Invoke());
            _errorsClear = BottomButton("Btn_Clear", 2 / 3f, 1, L("devtools.clear", "Clear"), () => ErrorsClearClicked?.Invoke());
            _errorsPage.gameObject.SetActive(false);
        }

        private Button BottomButton(string name, float from, float to, string text, UnityEngine.Events.UnityAction onClick)
        {
            var button = Instantiate(_errorsButton, _errorsPage);
            button.name = name;
            button.onClick.RemoveAllListeners();
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(from, 0); rect.anchorMax = new Vector2(to, 0); rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = Vector2.zero; rect.offsetMin = new Vector2(4, 0); rect.offsetMax = new Vector2(-4, 72);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;
            button.onClick.AddListener(onClick);
            return button;
        }
    }
}
