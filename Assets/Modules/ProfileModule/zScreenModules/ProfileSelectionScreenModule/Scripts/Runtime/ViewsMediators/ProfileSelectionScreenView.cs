using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.ProfileModule.Shared.Data.ValueObjects;
using Modules.UiWidgetsModule.Entities;
using Modules.UiWidgetsModule.Enums;
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
        [Tooltip("Opens the player's stats popup over this screen.")]
        [SerializeField] private Button _statsButton;
        [SerializeField] private TMP_Text _avatarTabLabel;
        [SerializeField] private TMP_Text _frameTabLabel;
        /// <summary>The title on the ribbon.</summary>
        [SerializeField] private TMP_Text _titleLabel;
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
        /// <summary>The tab that is open, and the one that is not.</summary>
        [SerializeField] private Sprite _tabOnSprite;
        [SerializeField] private Sprite _tabOffSprite;
        /// <summary>An item's slot: owned, owned and picked, locked with coins enough, locked without.</summary>
        [SerializeField] private Sprite _slotOwnedSprite;
        [SerializeField] private Sprite _slotSelectedSprite;
        [SerializeField] private Sprite _slotLockedSprite;
        [SerializeField] private Sprite _slotPoorSprite;
        /// <summary>Drawn over the animal: the lock on a locked item, the tick on the picked one.</summary>
        [SerializeField] private Sprite _lockSprite;
        [SerializeField] private Sprite _checkSprite;
        /// <summary>The price strip's tint while the coins are not enough.</summary>
        [SerializeField] private Color _poorStripColor = new(1f, .42f, .42f, 1f);
        /// <summary>The slot's tint while the coins are not enough.</summary>
        [SerializeField] private Color _poorSlotColor = new(.68f, .66f, .74f, 1f);
        /// <summary>The bottom button: save, buy with coins enough, buy without.</summary>
        [SerializeField] private Sprite _saveSprite;
        [SerializeField] private Sprite _buySprite;
        [SerializeField] private Sprite _buyOffSprite;
        /// <summary>The label's left margin while the buy button's coin sits there.</summary>
        [SerializeField] private float _buyLabelMargin = 70f;

        public Action<ProfileEditVO> SaveRequested;
        public Action StatsClicked;
        /// <summary>The save button was pressed on a locked icon: it is to be bought, by id.</summary>
        public Action<string> BuyRequested;

        private Func<string, string> _localize;
        private string _saveText;
        private string _message;

        public void SetLocalizer(Func<string, string> localize)
        {
            _localize = localize;
            ApplyTexts();
        }

        /// <summary>Every fixed word on the screen, in the language in use.</summary>
        private void ApplyTexts()
        {
            // Fixed words are LocalizedTextViews in the prefab, edited in the inspector: profile.title, profile.tab_avatar, profile.tab_frame.
            if (_nameInput != null && _nameInput.placeholder is TMP_Text placeholder)
                placeholder.text = L("profile.name_placeholder", "Enter your name");
            _saveText = L("profile.save", "SAVE");
            RefreshSaveLabel();
        }

        private string L(string key, string english)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? english : text;
        }

        /// <summary>An icon was bought: the same choices, it unlocked, the draft kept.</summary>
        public void ApplyIconBought(ProfileSelectionRVO selection)
        {
            string icon = _draftIconId, frame = _draftFrameId;
            _selection = selection;
            _draftIconId = icon;
            _draftFrameId = frame;
            _message = null;
            BuildItems();
            RefreshPreview();
        }

        /// <summary>Not enough coins for the icon: the button says so until another icon is picked.</summary>
        public void ApplyIconNotBought(string iconId)
        {
            _message = L("profile.not_enough", "Not enough coins");
            RefreshSaveLabel();
        }

        private ProfileIconVO DraftIcon => _selection?.Icons.Find(value => value.Id == _draftIconId);
        private ProfileFrameVO DraftFrame => _selection?.Frames.Find(value => value.Id == _draftFrameId);

        /// <summary>A frame is bought under its id with this in front; the profile module reads it the same way.</summary>
        private const string FRAME_PREFIX = "frame:";

        /// <summary>The picked item that still has to be bought - the avatar first, then the frame - and its price; null when both are the player's.</summary>
        private string LockedDraft(out int price)
        {
            var icon = DraftIcon;
            if (icon != null && !icon.Owned) { price = icon.Price; return icon.Id; }
            var frame = DraftFrame;
            if (frame != null && !frame.Owned) { price = frame.Price; return FRAME_PREFIX + frame.Id; }
            price = 0;
            return null;
        }

        private void RefreshSaveLabel()
        {
            var label = _saveButton != null ? _saveButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null) return;
            _saveText ??= label.text;
            bool buying = LockedDraft(out int price) != null;
            label.text = _message ?? (buying ? string.Format(L("profile.buy", "Buy {0}"), price) : _saveText);
            label.margin = new Vector4(buying ? _buyLabelMargin : 0, 0, 0, 0);
            if (_saveButton.targetGraphic is Image background)
            {
                Sprite sprite = !buying ? _saveSprite : _coins >= price ? _buySprite : _buyOffSprite;
                if (sprite != null) background.sprite = sprite;
            }
        }

        private int _coins;

        /// <summary>The player's coins: which locked items can be bought now.</summary>
        public void ApplyCoins(int coins)
        {
            _coins = coins;
            RefreshSelection();
            RefreshSaveLabel();
        }

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
            if (_statsButton != null) _statsButton.onClick.AddListener(OnStatsClicked);
        }

        private void OnDisable()
        {
            if (_layout != null) _layout.Changed -= FitDialogToSafeArea;
            _closeButton.onClick.RemoveListener(Hide);
            _avatarTabButton.onClick.RemoveListener(ShowAvatars);
            _frameTabButton.onClick.RemoveListener(ShowFrames);
            _saveButton.onClick.RemoveListener(Save);
            if (_statsButton != null) _statsButton.onClick.RemoveListener(OnStatsClicked);
            UnwireItems();
        }

        private void OnStatsClicked() => StatsClicked?.Invoke();

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
            string locked = LockedDraft(out _);
            if (locked != null)
            {
                BuyRequested?.Invoke(locked);
                return;
            }
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
                {
                    CreateItem(frame.Id, frame.DisplayName, icon?.Sprite, icon?.FallbackColor ?? Color.white, frame.Sprite, frame.FallbackColor);
                    ShowPrice(frame.Owned, frame.Price);
                }
            }
            else
            {
                foreach (ProfileIconVO icon in _selection.Icons)
                {
                    // A locked icon carries its price on the slot's strip.
                    CreateItem(icon.Id, icon.DisplayName, icon.Sprite, icon.FallbackColor, null, Color.white);
                    ShowPrice(icon.Owned, icon.Price);
                }
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
            if (image != null && sprite != null)
            {
                IconIdleAnimator animator = image.GetComponent<IconIdleAnimator>();
                if (animator == null) animator = image.gameObject.AddComponent<IconIdleAnimator>();
                animator.Configure(IconIdleStyle.AvatarFloat, .25f, .055f);
            }
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

        /// <summary>The item just made carries its price on the slot's strip while it is locked.</summary>
        private void ShowPrice(bool owned, int price)
        {
            var strip = _items[_items.Count - 1].transform.Find("Txt_Price");
            if (strip == null) return;
            strip.gameObject.SetActive(!owned);
            if (strip.TryGetComponent(out TMP_Text text)) text.text = price.ToString();
        }

        private void Select(string id)
        {
            if (_showingFrames) _draftFrameId = id;
            else _draftIconId = id;
            _message = null;
            RefreshSelection();
            RefreshPreview();
        }

        private void RefreshSelection()
        {
            string selectedId = _showingFrames ? _draftFrameId : _draftIconId;
            string prefix = _showingFrames ? "Btn_ProfileFrame_" : "Btn_ProfileIcon_";
            foreach (Button item in _items)
            {
                if (item == null) continue;
                bool selected = item.name == prefix + selectedId;
                Transform marker = item.transform.Find("Img_Selected");
                if (marker != null) marker.gameObject.SetActive(false);
                ProfileIconVO icon = _showingFrames ? null : _selection?.Icons.Find(value => prefix + value.Id == item.name);
                ProfileFrameVO frame = _showingFrames ? _selection?.Frames.Find(value => prefix + value.Id == item.name) : null;
                bool locked = (icon != null && !icon.Owned) || (frame != null && !frame.Owned);
                bool poor = locked && _coins < (icon != null ? icon.Price : frame.Price);
                Sprite slot = selected && !locked ? _slotSelectedSprite : _slotOwnedSprite;
                if (slot != null && item.targetGraphic is Image background)
                {
                    background.sprite = slot;
                    background.color = poor ? _poorSlotColor : Color.white;
                }
                // The lock, the tick and the price strip are drawn over the animal, never behind it.
                Image badge = item.transform.Find("Img_Badge")?.GetComponent<Image>();
                if (badge != null)
                {
                    Sprite mark = locked ? _lockSprite : selected ? _checkSprite : null;
                    badge.sprite = mark;
                    badge.enabled = mark != null;
                }
                Image strip = item.transform.Find("Img_PriceStrip")?.GetComponent<Image>();
                if (strip != null)
                {
                    strip.enabled = locked;
                    strip.color = poor ? _poorStripColor : Color.white;
                }
                // A locked item picked to look at stands a little forward.
                item.transform.localScale = Vector3.one * (locked && selected ? 1.06f : 1f);
            }
        }

        private void RefreshPreview()
        {
            if (_selection == null) return;
            RefreshSaveLabel();
            ProfileIconVO icon = _selection.Icons.Find(value => value.Id == _draftIconId);
            ProfileFrameVO frame = _selection.Frames.Find(value => value.Id == _draftFrameId);
            if (_profileAvatar == null) return;
            _profileAvatar.SetIcon(icon?.Sprite, icon?.FallbackColor ?? Color.white);
            _profileAvatar.SetFrame(frame?.Sprite, frame?.FallbackColor ?? Color.white);
        }

        private void RefreshTabs()
        {
            ApplyTab(_avatarTabButton, !_showingFrames);
            ApplyTab(_frameTabButton, _showingFrames);
            // The open tab's label is bright, the other one dimmed.
            if (_avatarTabLabel != null) _avatarTabLabel.color = !_showingFrames ? Color.white : new Color(1f, 1f, 1f, .7f);
            if (_frameTabLabel != null) _frameTabLabel.color = _showingFrames ? Color.white : new Color(1f, 1f, 1f, .7f);
        }

        private void ApplyTab(Button tab, bool open)
        {
            if (tab == null) return;
            // The tab's own background shows which list is open - not whatever the button tints.
            Sprite sprite = open ? _tabOnSprite : _tabOffSprite;
            Image image = tab.GetComponent<Image>();
            if (sprite != null && image != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                tab.transform.localScale = Vector3.one * (open ? 1.06f : 0.96f);
            }
            else if (tab.targetGraphic != null) tab.targetGraphic.color = open ? _selectedTabColor : _normalTabColor;
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
