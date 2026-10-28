using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The info panel: one panel for every page of CD_InfoPopups - title, subtitle, picture, description and a
    /// button - filled from the page asked for. Pages asked for while one shows wait their turn.
    /// </summary>
    public partial class GameplayScreenView
    {
        [Header("Info panel")]
        [SerializeField] private CD_InfoPopups _infoPages;
        [Tooltip("The panel's root, dim and all; shown while a page is up.")]
        [SerializeField] private GameObject _infoPanel;
        [SerializeField] private TMP_Text _infoTitle, _infoSubtitle, _infoDescription, _infoButtonLabel;
        [SerializeField] private Image _infoPanelTheme;
        [Tooltip("The drawn card, hidden while a page brings its own panel art.")]
        [SerializeField] private GameObject _infoPanelBackground;
        [Tooltip("Text colours on a page's panel art, whose inside is light: title, subtitle, description.")]
        [SerializeField] private Color _infoThemedTitle = new Color32(0x9B, 0x3C, 0xF5, 0xFF);
        [SerializeField] private Color _infoThemedSubtitle = new Color32(0x6E, 0x22, 0xD6, 0xFF);
        [SerializeField] private Color _infoThemedText = new Color32(0x4A, 0x13, 0x9E, 0xFF);
        [SerializeField] private Image _infoImage;
        [SerializeField] private Button _infoButton, _infoClose;

        private readonly Queue<string> _infoQueue = new();
        // The tutorial's dark, laid behind the panel.
        private TutorialVeil _infoVeil;
        private string _infoShown;
        // The colours as authored, for pages without panel art.
        private Color[] _infoPlainColours;

        /// <summary>Shows the page with this key; one asked for while another shows comes after it.</summary>
        public void ShowInfo(string key)
        {
            if (_infoPages == null || _infoPanel == null || _infoPages.Find(key) == null) return;
            if (_infoShown != null) { _infoQueue.Enqueue(key); return; }
            FillInfo(key);
        }
        private void FillInfo(string key)
        {
            var page = _infoPages.Find(key);
            if (page == null) { NextInfo(); return; }
            _infoShown = key;
            SetInfoText(_infoTitle, page.TitleKey, page.Title);
            SetInfoText(_infoSubtitle, page.SubtitleKey, page.Subtitle);
            SetInfoText(_infoDescription, page.DescriptionKey, page.Description);
            SetInfoText(_infoButtonLabel, page.ButtonKey, page.Button);
            if (_infoPanelTheme != null)
            {
                _infoPanelTheme.gameObject.SetActive(page.PanelSprite != null);
                _infoPanelTheme.sprite = page.PanelSprite;
                _infoPanelTheme.preserveAspect = false;
            }
            bool themed = _infoPanelTheme != null && page.PanelSprite != null;
            if (_infoPanelBackground != null) _infoPanelBackground.SetActive(!themed);
            TintInfoText(themed);
            if (_infoImage != null)
            {
                _infoImage.gameObject.SetActive(page.Image != null);
                _infoImage.sprite = page.Image;
                _infoImage.preserveAspect = true;
                if (page.ImageSize.x > 0 && page.ImageSize.y > 0) _infoImage.rectTransform.sizeDelta = page.ImageSize;
            }
            _infoPanel.SetActive(true);
            ShowInfoVeil(true);
            _infoPanel.transform.SetAsLastSibling();
        }
        private void TintInfoText(bool themed)
        {
            TMP_Text[] labels = { _infoTitle, _infoSubtitle, _infoDescription };
            Color[] tints = { _infoThemedTitle, _infoThemedSubtitle, _infoThemedText };
            if (_infoPlainColours == null)
            {
                _infoPlainColours = new Color[labels.Length];
                for (int i = 0; i < labels.Length; i++) if (labels[i] != null) _infoPlainColours[i] = labels[i].color;
            }
            for (int i = 0; i < labels.Length; i++) if (labels[i] != null) labels[i].color = themed ? tints[i] : _infoPlainColours[i];
        }
        private void SetInfoText(TMP_Text label, string key, string english)
        {
            if (label == null) return;
            string text = string.IsNullOrEmpty(key) ? english : L(key, english ?? "");
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));
            label.text = text ?? "";
        }
        private void CloseInfo()
        {
            var page = _infoPages != null ? _infoPages.Find(_infoShown) : null;
            if (_infoPanel != null) _infoPanel.SetActive(false);
            ShowInfoVeil(false);
            _infoShown = null;
            NextInfo();
            // A page about a booster leads on to using it, once no other page is waiting.
            if (_infoShown == null && page != null && page.LeadToBooster) StartBoosterLesson(page.Booster);
        }
        /// <summary>The dark behind the panel: the tutorial's veil in place of the panel's own dim, which is hidden meanwhile.</summary>
        private void ShowInfoVeil(bool on)
        {
            var dim = _infoPanel != null ? _infoPanel.GetComponent<Image>() : null;
            if (!on || _presentation == null || _infoPanel.transform.parent == null)
            {
                if (_infoVeil != null) _infoVeil.Hide();
                if (dim != null) dim.enabled = true;
                return;
            }
            if (_infoVeil == null) _infoVeil = TutorialVeil.Create((RectTransform)_infoPanel.transform.parent);
            _infoVeil.ShowFocus(_hasBoard ? _spriteBoard : null, _presentation.Look, null);
            if (dim != null) dim.enabled = false;
        }
        private void NextInfo() { if (_infoQueue.Count > 0) FillInfo(_infoQueue.Dequeue()); }
        private void WireInfo(bool on)
        {
            if (_infoButton != null) { if (on) _infoButton.onClick.AddListener(CloseInfo); else _infoButton.onClick.RemoveListener(CloseInfo); }
            if (_infoClose != null) { if (on) _infoClose.onClick.AddListener(CloseInfo); else _infoClose.onClick.RemoveListener(CloseInfo); }
            if (!on) { _infoQueue.Clear(); _infoShown = null; if (_infoPanel != null) _infoPanel.SetActive(false); ShowInfoVeil(false); }
        }
    }
}
