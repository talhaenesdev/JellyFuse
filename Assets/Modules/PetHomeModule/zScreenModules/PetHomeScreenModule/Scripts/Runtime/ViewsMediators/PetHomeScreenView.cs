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
        [Tooltip("Same place as the floor, drawn before it: the soft shadow under each thing and the pet.")]
        [SerializeField] private RectTransform _shadows;
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
            _shadows.anchorMin = _floor.anchorMin;
            _shadows.anchorMax = _floor.anchorMax;
            _shadows.offsetMin = _shadows.offsetMax = Vector2.zero;
            _shadowColor = room.Shadow;
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
        public Vector2 LocalToFloor(Vector2 local) => new(
            _floor.rect.width > 0 ? local.x / _floor.rect.width : 0,
            _floor.rect.height > 0 ? local.y / _floor.rect.height : 0);
        public float DepthScale(float y) => Mathf.Lerp(1f, .72f, y);
        /// <summary>The height things are sized from: the screen's, so a bigger room is more floor rather than bigger furniture.</summary>
        public float FloorHeight => ((RectTransform)_roomScroll.transform).rect.height * _itemScale;

        private void LayOut(PetHomeItemView view)
        {
            var rect = (RectTransform)view.transform;
            // A ball turns about its middle as it rolls; everything else stands on its bottom edge.
            rect.pivot = new Vector2(.5f, ROLLS.Contains(view.Id) ? .5f : 0);
            view.Size(FloorHeight * DepthScale(view.Floor.y));
            rect.anchoredPosition = FloorToLocal(view.Floor) + new Vector2(0, rect.sizeDelta.y * rect.pivot.y);
        }

        /// <summary>Where a thing on the floor touches it, whatever point it turns about.</summary>
        private static Vector2 Foot(RectTransform rect) => rect.anchoredPosition - new Vector2(0, rect.sizeDelta.y * rect.pivot.y);

        // ---- kicked toys ----

        /// <summary>The toys that roll when the pet swats them.</summary>
        private static readonly HashSet<string> ROLLS = new()
            { "Toy_Ball", "Toy_Yarn", "Toy_Tennis", "Toy_Soccer", "Toy_BellBall", "Toy_TreatBall" };

        internal bool Rolls(string id) => ROLLS.Contains(id);

        /// <summary>The pet swatted a toy: it rolls away from the pet and comes to rest where it stops.</summary>
        internal void Kick(string id, bool left)
        {
            if (_editing || !_items.TryGetValue(id, out var view) || !view.gameObject.activeSelf) return;
            StartCoroutine(Roll(view, left));
        }

        private IEnumerator Roll(PetHomeItemView view, bool left)
        {
            var rect = (RectTransform)view.transform;
            var dir = new Vector2(left ? -1 : 1, UnityEngine.Random.Range(-.35f, .35f)).normalized;
            float speed = FloorHeight * UnityEngine.Random.Range(1.4f, 2f);
            bool bounced = false;
            while (speed > FloorHeight * .05f && !_editing && view.gameObject.activeSelf)
            {
                var before = view.Floor;
                var floor = LocalToFloor(FloorToLocal(before) + dir * speed * Time.deltaTime);
                // The room's edge sends it back the way it came.
                if (floor.x < 0 || floor.x > 1) { dir.x = -dir.x; floor.x = Mathf.Clamp01(floor.x); }
                if (floor.y < 0 || floor.y > 1) { dir.y = -dir.y; floor.y = Mathf.Clamp01(floor.y); }
                view.Floor = floor;
                LayOut(view);
                // So does anything standing in its way, and it loses half its speed doing so.
                if (Overlaps(view))
                {
                    view.Floor = before;
                    LayOut(view);
                    if (bounced) break;
                    dir = -dir;
                    speed *= .5f;
                    bounced = true;
                }
                float turn = speed * Time.deltaTime / Mathf.Max(1, rect.sizeDelta.y * .5f) * Mathf.Rad2Deg;
                rect.localRotation *= Quaternion.Euler(0, 0, dir.x > 0 ? -turn : turn);
                speed *= Mathf.Pow(.2f, Time.deltaTime);
                yield return null;
            }
            view.Committed = view.Floor;
            Placed?.Invoke(new PlacedItemSVO(view.Id, view.Floor.x, view.Floor.y));
            TellPet();
        }

        // ---- shadows ----

        /// <summary>Things that lie flat on the floor or hang on the wall cast nothing on it.</summary>
        private static readonly HashSet<string> FLAT = new() { "Decor_Rug", "Decor_Frame", "Decor_Lights" };
        private readonly Dictionary<Transform, Image> _shadowOf = new();
        private Color _shadowColor = new(.22f, .1f, .32f, .3f);
        private Sprite _shadowSprite;

        /// <summary>A soft round blot, made once: dark in the middle, gone at the rim.</summary>
        private Sprite ShadowSprite()
        {
            if (_shadowSprite != null) return _shadowSprite;
            const int W = 128, H = 64;
            var texture = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x + .5f) / W * 2 - 1, dy = (y + .5f) / H * 2 - 1;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1 - d);
                pixels[y * W + x] = new Color32(255, 255, 255, (byte)(a * a * (3 - 2 * a) * 255));
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            _shadowSprite = Sprite.Create(texture, new Rect(0, 0, W, H), new Vector2(.5f, .5f));
            return _shadowSprite;
        }

        /// <summary>Each thing on the floor, and the pet, gets a blot under its feet that follows it about.</summary>
        private void DrawShadows()
        {
            for (int i = 0; i < _floor.childCount; i++)
            {
                var child = (RectTransform)_floor.GetChild(i);
                bool pet = child == _pet.transform;
                var item = pet ? null : child.GetComponent<PetHomeItemView>();
                bool shows = child.gameObject.activeSelf && (pet ? _pet.GetComponent<Image>().enabled : item != null && !FLAT.Contains(item.Id));
                if (!_shadowOf.TryGetValue(child, out var shadow))
                {
                    if (!shows) continue;
                    var go = new GameObject("Shadow_" + child.name, typeof(RectTransform), typeof(Image));
                    go.layer = gameObject.layer;
                    shadow = go.GetComponent<Image>();
                    shadow.sprite = ShadowSprite();
                    shadow.raycastTarget = false;
                    var r = shadow.rectTransform;
                    r.SetParent(_shadows, false);
                    r.anchorMin = r.anchorMax = Vector2.zero;
                    r.pivot = new Vector2(.5f, .5f);
                    _shadowOf[child] = shadow;
                }
                shadow.gameObject.SetActive(shows);
                if (!shows) continue;
                // The pet's frames carry empty space around the body; furniture fills its picture.
                float width = pet ? child.sizeDelta.y * .7f : child.sizeDelta.x * .85f;
                shadow.rectTransform.anchoredPosition = Foot(child) + new Vector2(0, width * .04f);
                shadow.rectTransform.sizeDelta = new Vector2(width, width * .3f);
                shadow.color = _shadowColor;
            }
        }

        private void LateUpdate()
        {
            DrawShadows();
            // Things further back are drawn first: sort the floor's children by how far back they stand.
            int count = _floor.childCount;
            for (int i = 1; i < count; i++)
            {
                var a = (RectTransform)_floor.GetChild(i - 1);
                var b = (RectTransform)_floor.GetChild(i);
                if (Foot(b).y > Foot(a).y + .5f) b.SetSiblingIndex(i - 1);
            }
        }

        private void TellPet()
        {
            var spots = new List<PetHomePetView.Spot>();
            foreach (var pair in _items)
            {
                if (!pair.Value.gameObject.activeSelf || !_catalogue.TryGetValue(pair.Key, out var item)) continue;
                spots.Add(new PetHomePetView.Spot { Id = pair.Key, Floor = pair.Value.Floor, Category = item.Category, Empty = pair.Value.Empty, Width = ((RectTransform)pair.Value.transform).sizeDelta.x });
            }
            _pet.SetSpots(spots);
        }

        // ---- items: tapped, dragged ----

        internal void ItemTapped(PetHomeItemView item)
        {
            if (_editing) { Select(item == _selected ? null : item); return; }
            if (!_catalogue.TryGetValue(item.Id, out var cvo)) return;
            if (cvo.Category == PetHomeCategory.Bowl && item.Empty) { RefillClicked?.Invoke(item.Id); return; }
            _pet.GoTo(new PetHomePetView.Spot { Id = item.Id, Floor = item.Floor, Category = cvo.Category, Empty = item.Empty, Width = ((RectTransform)item.transform).sizeDelta.x });
        }

        internal bool CanDrag => _editing;

        internal void ItemDragged(PetHomeItemView item, Vector2 screen, Camera camera)
        {
            Select(null);
            SlideAtEdge(screen);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_floor, screen, camera, out var local);
            var rect = _floor.rect;
            var floor = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
            bool onFloor = floor.x >= 0 && floor.x <= 1 && floor.y >= 0 && floor.y <= 1;
            item.Floor = new Vector2(Mathf.Clamp01(floor.x), Mathf.Clamp01(floor.y));
            LayOut(item);
            bool valid = onFloor && !Overlaps(item);
            item.Valid = valid;
            _floorMark.gameObject.SetActive(true);
            _floorMark.sprite = valid ? _validMark : _invalidMark;
            var mark = _floorMark.rectTransform;
            mark.SetParent(_floor, false);
            mark.SetAsFirstSibling();
            mark.anchoredPosition = FloorToLocal(item.Floor);
            float width = FloorHeight * .32f * DepthScale(item.Floor.y);
            mark.sizeDelta = new Vector2(width, width * .42f);
        }

        /// <summary>
        /// Whether the patch of floor an item stands on - an oval as wide as most of it, shallow front to back -
        /// touches another's. A rug and the wall's things take no floor, so anything may stand on or under them.
        /// </summary>
        private bool Overlaps(PetHomeItemView item)
        {
            if (FLAT.Contains(item.Id)) return false;
            var a = (RectTransform)item.transform;
            float aw = a.sizeDelta.x * .4f;
            foreach (var other in _items.Values)
            {
                if (other == item || !other.gameObject.activeSelf || FLAT.Contains(other.Id)) continue;
                var b = (RectTransform)other.transform;
                float bw = b.sizeDelta.x * .4f;
                var d = Foot(b) - Foot(a);
                float x = d.x / (aw + bw), y = d.y / ((aw + bw) * .35f);
                if (x * x + y * y < 1) return true;
            }
            return false;
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

        /// <summary>A tap on the room where nothing stands: the pet walks over, or in edit mode the choice is let go.</summary>
        internal void RoomTapped(Vector2 screen, Camera camera)
        {
            if (_editing) { Select(null); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_floor, screen, camera, out var local);
            var rect = _floor.rect;
            _pet.WalkTo(new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height));
        }

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
