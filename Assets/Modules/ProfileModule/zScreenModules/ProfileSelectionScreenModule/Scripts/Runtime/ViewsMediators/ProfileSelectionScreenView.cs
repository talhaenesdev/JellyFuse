using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.ProfileModule.Shared.Data.ValueObjects;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.ProfileModule.ProfileSelectionScreenModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public sealed class ProfileSelectionScreenView : ScreenView
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _avatarTabButton;
        [SerializeField] private Button _frameTabButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private TMP_Text _avatarTabLabel;
        [SerializeField] private TMP_Text _frameTabLabel;
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private ProfileAvatar _profileAvatar;
        [Header("Responsive Layout")]
        [SerializeField] private RectTransform _dialog;
        [Tooltip("Safe-area space left on both horizontal sides, in canvas units.")]
        [Min(0)] [SerializeField] private float _horizontalMargin = 32;
        [Tooltip("Safe-area space left above and below the dialog, in canvas units.")]
        [Min(0)] [SerializeField] private float _verticalMargin = 120;
        [Tooltip("Prevents the dialog becoming excessively large on tablets.")]
        [Min(.1f)] [SerializeField] private float _maximumScale = 1.98f;
        [Tooltip("When set, the margins and Max Scale come from its config, and the objects it lists take its places.")]
        [SerializeField] private ScreenLayout _layout;
        [SerializeField] private RectTransform _itemsRoot;
        [SerializeField] private Button _itemTemplate;
        [SerializeField] private Color _selectedTabColor = Color.white;
        [SerializeField] private Color _normalTabColor = new(.18f, .13f, .28f, 1);

        public Action<ProfileEditVO> SaveRequested;

        private readonly List<Button> _items = new();
        private readonly List<UnityEngine.Events.UnityAction> _itemActions = new();
        private ProfileSelectionRVO _selection;
        private string _draftIconId;
        private string _draftFrameId;
        private bool _showingFrames;

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            FitDialogToSafeArea();
            ResetItemsScroll();
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

        private void OnEnable()
        {
            if (_layout != null) _layout.Changed += FitDialogToSafeArea;
            _closeButton.onClick.AddListener(Hide);
            _avatarTabButton.onClick.AddListener(ShowAvatars);
            _frameTabButton.onClick.AddListener(ShowFrames);
            _saveButton.onClick.AddListener(Save);
        }

        private void OnDisable()
        {
            if (_layout != null) _layout.Changed -= FitDialogToSafeArea;
            _closeButton.onClick.RemoveListener(Hide);
            _avatarTabButton.onClick.RemoveListener(ShowAvatars);
            _frameTabButton.onClick.RemoveListener(ShowFrames);
            _saveButton.onClick.RemoveListener(Save);
            UnwireItems();
        }

        public void ApplySelection(ProfileSelectionRVO selection)
        {
            _selection = selection;
            _draftIconId = selection?.SelectedIconId;
            _draftFrameId = selection?.SelectedFrameId;
            if (_nameInput != null) _nameInput.SetTextWithoutNotify(selection?.PlayerName ?? "Player");
            ShowAvatars();
            RefreshPreview();
        }

        public void ApplySavedProfile(ProfileSelectionRVO selection)
        {
            ApplySelection(selection);
            Hide();
        }

        private void ShowAvatars()
        {
            _showingFrames = false;
            RefreshTabs();
            BuildItems();
        }

        private void ShowFrames()
        {
            _showingFrames = true;
            RefreshTabs();
            BuildItems();
        }

        private void Save()
        {
            SaveRequested?.Invoke(new ProfileEditVO
            {
                PlayerName = _nameInput != null ? _nameInput.text : "Player",
                IconId = _draftIconId,
                FrameId = _draftFrameId
            });
        }

        private void BuildItems()
        {
            UnwireItems();
            for (int i = _itemsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _itemsRoot.GetChild(i);
                if (child != _itemTemplate.transform) Destroy(child.gameObject);
            }

            _itemTemplate.gameObject.SetActive(false);
            if (_selection == null) return;
            if (_showingFrames)
            {
                // Each frame is shown round the avatar picked, so the player sees what they would get.
                ProfileIconVO icon = _selection.Icons.Find(value => value.Id == _draftIconId);
                foreach (ProfileFrameVO frame in _selection.Frames)
                    CreateItem(frame.Id, frame.DisplayName, icon?.Sprite, icon?.FallbackColor ?? Color.white, frame.Sprite, frame.FallbackColor);
            }
            else
            {
                foreach (ProfileIconVO icon in _selection.Icons)
                    CreateItem(icon.Id, icon.DisplayName, icon.Sprite, icon.FallbackColor, null, Color.white);
            }
            RefreshSelection();
            ResetItemsScroll();
        }

        private void CreateItem(string id, string displayName, Sprite sprite, Color fallbackColor, Sprite frameSprite, Color frameColor)
        {
            Button item = Instantiate(_itemTemplate, _itemsRoot);
            item.name = (_showingFrames ? "Btn_ProfileFrame_" : "Btn_ProfileIcon_") + id;
            item.gameObject.SetActive(true);
            Image image = item.transform.Find("Img_Icon")?.GetComponent<Image>();
            Image frame = item.transform.Find("Img_Frame")?.GetComponent<Image>();
            TMP_Text label = item.GetComponentInChildren<TMP_Text>(true);
            if (image != null) { image.sprite = sprite; image.color = fallbackColor; image.enabled = sprite != null; }
            if (frame != null)
            {
                frame.gameObject.SetActive(frameSprite != null);
                frame.sprite = frameSprite;
                frame.color = frameColor;
            }
            if (label != null) label.text = displayName;
            UnityEngine.Events.UnityAction action = () => Select(id);
            item.onClick.AddListener(action);
            _items.Add(item);
            _itemActions.Add(action);
        }

        private void Select(string id)
        {
            if (_showingFrames) _draftFrameId = id;
            else _draftIconId = id;
            RefreshSelection();
            RefreshPreview();
        }

        private void RefreshSelection()
        {
            string selectedId = _showingFrames ? _draftFrameId : _draftIconId;
            string prefix = _showingFrames ? "Btn_ProfileFrame_" : "Btn_ProfileIcon_";
            foreach (Button item in _items)
            {
                Transform marker = item != null ? item.transform.Find("Img_Selected") : null;
                if (marker != null) marker.gameObject.SetActive(item.name == prefix + selectedId);
            }
        }

        private void RefreshPreview()
        {
            if (_selection == null) return;
            ProfileIconVO icon = _selection.Icons.Find(value => value.Id == _draftIconId);
            ProfileFrameVO frame = _selection.Frames.Find(value => value.Id == _draftFrameId);
            if (_profileAvatar == null) return;
            _profileAvatar.SetIcon(icon?.Sprite, icon?.FallbackColor ?? Color.white);
            _profileAvatar.SetFrame(frame?.Sprite, frame?.FallbackColor ?? Color.white);
        }

        private void RefreshTabs()
        {
            if (_avatarTabButton?.targetGraphic != null)
                _avatarTabButton.targetGraphic.color = !_showingFrames ? _selectedTabColor : _normalTabColor;
            if (_frameTabButton?.targetGraphic != null)
                _frameTabButton.targetGraphic.color = _showingFrames ? _selectedTabColor : _normalTabColor;
            if (_avatarTabLabel != null) _avatarTabLabel.color = Color.white;
            if (_frameTabLabel != null) _frameTabLabel.color = Color.white;
        }

        private void FitDialogToSafeArea()
        {
            if (_dialog == null || Screen.width <= 0 || Screen.height <= 0) return;
            RectTransform root = (RectTransform)transform;
            Rect rootRect = root.rect;
            Rect safe = Screen.safeArea;
            float safeWidth = rootRect.width * safe.width / Screen.width;
            float safeHeight = rootRect.height * safe.height / Screen.height;
            var config = _layout != null ? _layout.Config : null;
            float horizontalMargin = config != null ? config.HorizontalMargin : _horizontalMargin;
            float verticalMargin = config != null ? config.VerticalMargin : _verticalMargin;
            float maximumScale = config != null && config.MaxScale > 0 ? config.MaxScale : _maximumScale;
            float widthScale = Mathf.Max(.01f, (safeWidth - horizontalMargin * 2) / _dialog.sizeDelta.x);
            float heightScale = Mathf.Max(.01f, (safeHeight - verticalMargin * 2) / _dialog.sizeDelta.y);
            float scale = Mathf.Min(widthScale, heightScale, maximumScale);
            _dialog.localScale = Vector3.one * scale;

            float normalizedX = safe.center.x / Screen.width - .5f;
            float normalizedY = safe.center.y / Screen.height - .5f;
            _dialog.anchoredPosition = new Vector2(rootRect.width * normalizedX, rootRect.height * normalizedY);
            if (_layout != null) _layout.SetDirty();
        }

        private void UnwireItems()
        {
            for (int i = 0; i < _items.Count && i < _itemActions.Count; i++)
                if (_items[i] != null) _items[i].onClick.RemoveListener(_itemActions[i]);
            _items.Clear();
            _itemActions.Clear();
        }

        private void ResetItemsScroll()
        {
            ScrollRect scroll = _itemsRoot != null ? _itemsRoot.GetComponentInParent<ScrollRect>() : null;
            if (scroll == null) return;
            Canvas.ForceUpdateCanvases();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
