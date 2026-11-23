using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.PetHomeModule.PetHomeScreenModule.ViewsMediators
{
    /// <summary>
    /// The pet home: the room with the player's animal living in it, the items standing on its floor, the shop sheet
    /// and the edit mode that drags items about. It shows what it is told and reports what the player did; which item
    /// may be bought and where it stands is decided outside it.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class PetHomeScreenView : ScreenView
    {
        [Header("Room")]
        [SerializeField] private Image _room;
        [SerializeField] private ScrollRect _roomScroll;
        [Tooltip("How much bigger than the screen the room is drawn, so there is room to slide it about.")]
        [SerializeField] private float _roomZoom = 1.25f;
        [Tooltip("How tall things are drawn, as a share of the screen's height - the same in every room.")]
        [SerializeField] private float _itemScale = .42f;
        [SerializeField] private RectTransform _floor;
        [SerializeField] private PetHomeItemView _itemTemplate;
        [SerializeField] private PetHomePetView _pet;
        [SerializeField] private Image _floorMark;
        [SerializeField] private Sprite _validMark;
        [SerializeField] private Sprite _invalidMark;

        [Header("Top bar")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _editButton;
        [SerializeField] private Button _doneButton;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private TMP_Text _toastText;

        [Header("Edit")]
        [SerializeField] private Button _storeButton;

        [Header("Shop")]
        [SerializeField] private GameObject _shopPanel;
        [SerializeField] private Button _shopCloseButton;
        [Tooltip("One tab per category, in PetHomeCategory order: bowl, bed, toy, decor, room.")]
        [SerializeField] private Button[] _tabs;
        [SerializeField] private ScrollRect _cardScroll;
        [SerializeField] private PetHomeCardView _cardTemplate;

        public Action CloseClicked;
        public Action<string> PickClicked;
        public Action<PlacedItemSVO> Placed;
        public Action<string> Stored;
        public Action<string> Ate;
        public Action<string> RefillClicked;

        private readonly Dictionary<string, PetHomeItemView> _items = new();
        private readonly List<PetHomeCardView> _cards = new();
        private readonly Dictionary<string, PetHomeItemCVO> _catalogue = new();
        private PetHomeRVO _home;
        private PetHomeCategory _tab = PetHomeCategory.Bowl;
        private bool _editing;
        private PetHomeItemView _selected;
        private Func<string, string> _localize;
        private Coroutine _toast;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
            _shopButton.onClick.AddListener(OpenShop);
            _shopCloseButton.onClick.AddListener(CloseShop);
            _editButton.onClick.AddListener(StartEditing);
            _doneButton.onClick.AddListener(StopEditing);
            _storeButton.onClick.AddListener(OnStoreClicked);
            for (int i = 0; i < _tabs.Length; i++)
            {
                var tab = (PetHomeCategory)i;
                _tabs[i].onClick.AddListener(() => ShowTab(tab));
            }
            _pet.Tapped = OnPetTapped;
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _shopButton.onClick.RemoveListener(OpenShop);
            _shopCloseButton.onClick.RemoveListener(CloseShop);
            _editButton.onClick.RemoveListener(StartEditing);
            _doneButton.onClick.RemoveListener(StopEditing);
            _storeButton.onClick.RemoveListener(OnStoreClicked);
            foreach (var tab in _tabs) tab.onClick.RemoveAllListeners();
            _pet.Tapped = null;
        }

        /// <summary>A pooled screen comes back as the last opening left it: shop shut, edit mode off.</summary>
        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            _shopPanel.SetActive(false);
            _editing = false;
            Select(null);
            _floorMark.gameObject.SetActive(false);
            _toastText.gameObject.SetActive(false);
            _roomScroll.StopMovement();
            _room.rectTransform.anchoredPosition = Vector2.zero;
            _itemTemplate.gameObject.SetActive(false);
            _cardTemplate.gameObject.SetActive(false);
            RefreshButtons();
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);

        public void ApplyTexts(Func<string, string> localize)
        {
            _localize = localize;
            _hintText.text = L("pethome.edit_hint", "Drag things to move them. Tap one to put it away.");
        }

        /// <summary>The pet home as it stands: room, items on the floor, the shop's cards.</summary>
        public void Apply(PetHomeRVO home)
        {
            if (home == null) return;
            _home = home;
            _catalogue.Clear();
            foreach (var item in home.Items) if (item != null && !string.IsNullOrEmpty(item.Id)) _catalogue[item.Id] = item;

            if (home.Room != null && _catalogue.TryGetValue(home.Room, out var room) && room.Sprite != null) ShowRoom(room);
            PlaceItems(home.Placed);
            RefreshCards();
        }

        public void ApplyCoins(int coins) => _coinsText.text = coins.ToString("N0");

        public void ShowNotBought(string id) => Toast(L("pethome.not_enough", "Not enough coins!"));

        public void ApplyAnimal(CD_BoardAnimal animal) => _pet.SetAnimal(animal);

        // ---- the room ----

        /// <summary>The room's picture, keeping its shape, and its floor where the room says its painted floor is.</summary>
        private void ShowRoom(PetHomeItemCVO room)
        {
            _room.sprite = room.Sprite;
            SizeRoom();
            var floor = room.Floor;
            _floor.anchorMin = new Vector2(floor.xMin, floor.yMin);
            _floor.anchorMax = new Vector2(floor.xMax, floor.yMax);
            _floor.offsetMin = _floor.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            foreach (var view in _items.Values) if (view.gameObject.activeSelf) LayOut(view);
        }

        /// <summary>The picture keeps its shape and covers the screen with some to spare on every side, centred.</summary>
        private void SizeRoom()
        {
            var sprite = _room.sprite;
            var screen = ((RectTransform)_roomScroll.transform).rect.size;
            float aspect = sprite != null && sprite.rect.height > 0 ? sprite.rect.width / sprite.rect.height : 1;
            float height = Mathf.Max(screen.y * _roomZoom, screen.x * _roomZoom / aspect);
            _room.rectTransform.sizeDelta = new Vector2(height * aspect, height);
            _roomScroll.StopMovement();
            _room.rectTransform.anchoredPosition = Vector2.zero;
        }

        /// <summary>While something is dragged near the screen's edge, the room slides that way.</summary>
        private void SlideAtEdge(Vector2 screen)
        {
            var size = new Vector2(Screen.width, Screen.height);
            Vector2 push = Vector2.zero;
            const float EDGE = .12f;
            float x = screen.x / size.x, y = screen.y / size.y;
            if (x < EDGE) push.x = 1 - x / EDGE; else if (x > 1 - EDGE) push.x = -(1 - (1 - x) / EDGE);
            if (y < EDGE) push.y = 1 - y / EDGE; else if (y > 1 - EDGE) push.y = -(1 - (1 - y) / EDGE);
            if (push == Vector2.zero) return;
            var content = _room.rectTransform;
            var view = ((RectTransform)_roomScroll.transform).rect.size;
            var room = content.rect.size;
            var half = new Vector2(Mathf.Max(0, room.x - view.x), Mathf.Max(0, room.y - view.y)) / 2;
            var pos = content.anchoredPosition + push * (view.y * .02f);
            content.anchoredPosition = new Vector2(Mathf.Clamp(pos.x, -half.x, half.x), Mathf.Clamp(pos.y, -half.y, half.y));
        }

        private void PlaceItems(IReadOnlyList<PlacedItemSVO> placed)
        {
            var keep = new HashSet<string>();
            foreach (var spot in placed)
            {
                if (!_catalogue.TryGetValue(spot.Id, out var item)) continue;
                keep.Add(spot.Id);
                if (!_items.TryGetValue(spot.Id, out var view))
                {
                    view = Instantiate(_itemTemplate, _floor);
                    view.name = "Item_" + spot.Id;
                    view.Begin(this, spot.Id);
                    _items[spot.Id] = view;
                }
                view.gameObject.SetActive(true);
                bool empty = _home.Empty != null && _home.Empty.Contains(spot.Id);
                view.Show(empty && item.EmptySprite != null ? item.EmptySprite : item.Sprite, item.Height);
                view.Empty = empty;
                view.Floor = new Vector2(spot.X, spot.Y);
                LayOut(view);
            }
            foreach (var pair in _items) if (!keep.Contains(pair.Key)) pair.Value.gameObject.SetActive(false);
            if (_selected != null && !keep.Contains(_selected.Id)) Select(null);
            TellPet();
        }

        /// <summary>Where on the floor's rectangle a floor point falls, and how big things are drawn there - smaller at the back.</summary>
        public Vector2 FloorToLocal(Vector2 floor) => new(floor.x * _floor.rect.width, floor.y * _floor.rect.height);
        public float DepthScale(float y) => Mathf.Lerp(1f, .72f, y);
        /// <summary>The height things are sized from: the screen's, so a bigger room is more floor rather than bigger furniture.</summary>
        public float FloorHeight => ((RectTransform)_roomScroll.transform).rect.height * _itemScale;

        private void LayOut(PetHomeItemView view)
        {
            var rect = (RectTransform)view.transform;
            rect.anchoredPosition = FloorToLocal(view.Floor);
            view.Size(FloorHeight * DepthScale(view.Floor.y));
        }

        private void LateUpdate()
        {
            // Things further back are drawn first: sort the floor's children by how far back they stand.
            int count = _floor.childCount;
            for (int i = 1; i < count; i++)
            {
                var a = (RectTransform)_floor.GetChild(i - 1);
                var b = (RectTransform)_floor.GetChild(i);
                if (b.anchoredPosition.y > a.anchoredPosition.y + .5f) b.SetSiblingIndex(i - 1);
            }
        }

        private void TellPet()
        {
            var spots = new List<PetHomePetView.Spot>();
            foreach (var pair in _items)
            {
                if (!pair.Value.gameObject.activeSelf || !_catalogue.TryGetValue(pair.Key, out var item)) continue;
                spots.Add(new PetHomePetView.Spot { Id = pair.Key, Floor = pair.Value.Floor, Category = item.Category, Empty = pair.Value.Empty });
            }
            _pet.SetSpots(spots);
        }

        // ---- items: tapped, dragged ----

        internal void ItemTapped(PetHomeItemView item)
        {
            if (_editing) { Select(item == _selected ? null : item); return; }
            if (!_catalogue.TryGetValue(item.Id, out var cvo)) return;
            if (cvo.Category == PetHomeCategory.Bowl && item.Empty) { RefillClicked?.Invoke(item.Id); return; }
            _pet.GoTo(new PetHomePetView.Spot { Id = item.Id, Floor = item.Floor, Category = cvo.Category, Empty = item.Empty });
        }

        internal bool CanDrag => _editing;

        internal void ItemDragged(PetHomeItemView item, Vector2 screen, Camera camera)
        {
            Select(null);
            SlideAtEdge(screen);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_floor, screen, camera, out var local);
            var rect = _floor.rect;
            var floor = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
            bool valid = floor.x >= 0 && floor.x <= 1 && floor.y >= 0 && floor.y <= 1;
            item.Floor = new Vector2(Mathf.Clamp01(floor.x), Mathf.Clamp01(floor.y));
            item.Valid = valid;
            LayOut(item);
            _floorMark.gameObject.SetActive(true);
            _floorMark.sprite = valid ? _validMark : _invalidMark;
            var mark = _floorMark.rectTransform;
            mark.SetParent(_floor, false);
            mark.SetAsFirstSibling();
            mark.anchoredPosition = FloorToLocal(item.Floor);
            float width = FloorHeight * .32f * DepthScale(item.Floor.y);
            mark.sizeDelta = new Vector2(width, width * .42f);
        }

        internal void ItemDropped(PetHomeItemView item)
        {
            _floorMark.gameObject.SetActive(false);
            if (item.Valid)
            {
                Placed?.Invoke(new PlacedItemSVO(item.Id, item.Floor.x, item.Floor.y));
                item.Committed = item.Floor;
            }
            else item.Floor = item.Committed;
            LayOut(item);
            TellPet();
        }

        private void Select(PetHomeItemView item)
        {
            _selected = item;
            _storeButton.gameObject.SetActive(item != null);
            if (item == null) return;
            var button = (RectTransform)_storeButton.transform;
            button.SetParent(item.transform, false);
            button.anchoredPosition = new Vector2(0, ((RectTransform)item.transform).sizeDelta.y + 24f);
        }

        private void OnStoreClicked()
        {
            if (_selected == null) return;
            string id = _selected.Id;
            _storeButton.transform.SetParent(transform, false);
            Select(null);
            Stored?.Invoke(id);
        }

        private void OnPetTapped() => _pet.Pet();

        /// <summary>The pet finished a bowl.</summary>
        internal void PetAte(string id) => Ate?.Invoke(id);

        // ---- edit mode ----

        private void StartEditing()
        {
            _editing = true;
            CloseShop();
            RefreshButtons();
        }

        private void StopEditing()
        {
            _editing = false;
            Select(null);
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            _editButton.gameObject.SetActive(!_editing);
            _shopButton.gameObject.SetActive(!_editing);
            _doneButton.gameObject.SetActive(_editing);
            _hintText.gameObject.SetActive(_editing);
            _pet.Paused = _editing;
        }

        // ---- shop ----

        private void OpenShop()
        {
            _shopPanel.SetActive(true);
            ShowTab(_tab);
        }

        private void CloseShop() => _shopPanel.SetActive(false);

        private void ShowTab(PetHomeCategory tab)
        {
            _tab = tab;
            for (int i = 0; i < _tabs.Length; i++) _tabs[i].transform.localScale = Vector3.one * (i == (int)tab ? 1.12f : .92f);
            RefreshCards();
            if (_cardScroll != null) _cardScroll.verticalNormalizedPosition = 1;
        }

        private void RefreshCards()
        {
            if (_home == null || !_shopPanel.activeSelf) return;
            int used = 0;
            foreach (var item in _home.Items)
            {
                if (item == null || item.Category != _tab) continue;
                if (used == _cards.Count)
                {
                    var made = Instantiate(_cardTemplate, _cardTemplate.transform.parent);
                    made.Clicked = id => PickClicked?.Invoke(id);
                    _cards.Add(made);
                }
                var card = _cards[used++];
                card.gameObject.SetActive(true);
                bool owned = _home.Owned.Contains(item.Id);
                bool inUse = item.Category == PetHomeCategory.Room ? _home.Room == item.Id : IsPlaced(item.Id);
                card.Show(item.Id, item.Sprite, item.Price, owned, inUse);
            }
            for (int i = used; i < _cards.Count; i++) _cards[i].gameObject.SetActive(false);
        }

        private bool IsPlaced(string id)
        {
            foreach (var spot in _home.Placed) if (spot.Id == id) return true;
            return false;
        }

        // ---- the rest ----

        private void OnCloseClicked() => CloseClicked?.Invoke();

        private void Toast(string text)
        {
            if (_toast != null) StopCoroutine(_toast);
            _toast = StartCoroutine(ShowToast(text));
        }

        private IEnumerator ShowToast(string text)
        {
            _toastText.text = text;
            _toastText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(1.6f);
            _toastText.gameObject.SetActive(false);
            _toast = null;
        }

        private string L(string key, string english)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? english : text;
        }
    }
}
