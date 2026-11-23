using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.PetHomeModule.PetHomeScreenModule.Enums;
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

        [Header("Happiness")]
        [Tooltip("The bar's fill: its width is the pet's happiness.")]
        [SerializeField] private RectTransform _happyFill;
        [SerializeField] private RectTransform _happyBar;

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
        /// <summary>Something the player should hear and feel: a bounce, a pick-up, a meal.</summary>
        public Action<PetHomeFeel> Felt;
        /// <summary>Something was done for the pet that makes it happier.</summary>
        public Action<PetHomeCare> Cared;

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
            ShowHappiness(home.Happiness);
            PlaceItems(home.Placed);
            RefreshCards();
        }

        public void ApplyCoins(int coins) => _coinsText.text = coins.ToString("N0");

        // ---- happiness ----

        private float _happyShown = -1;
        private Coroutine _happyFilling;

        private void ShowHappiness(int happiness)
        {
            float target = Mathf.Clamp01(happiness / 100f);
            if (_happyShown < 0 || !isActiveAndEnabled) { _happyShown = target; SetHappyFill(target); return; }
            if (_happyFilling != null) StopCoroutine(_happyFilling);
            _happyFilling = StartCoroutine(FillHappiness(target));
        }

        private IEnumerator FillHappiness(float target)
        {
            float from = _happyShown;
            for (float t = 0; t < .5f; t += Time.deltaTime)
            {
                _happyShown = Mathf.Lerp(from, target, Mathf.SmoothStep(0, 1, t / .5f));
                SetHappyFill(_happyShown);
                yield return null;
            }
            _happyShown = target;
            SetHappyFill(target);
            if (target > from) Wobble(_happyBar, .08f);
        }

        private void SetHappyFill(float share) => _happyFill.anchorMax = new Vector2(Mathf.Max(.06f, share), 1);

        /// <summary>The pet's happiness filled up and earned coins: hearts, a cheer, and the coins said out loud.</summary>
        public void ShowReward(int coins)
        {
            Hearts(6);
            Wobble(_happyBar, .2f);
            Felt?.Invoke(PetHomeFeel.Happy);
            Toast(string.Format(L("pethome.happy_reward", "Your pet is so happy! +{0} coins"), coins));
        }

        /// <summary>The pet tells the screen it was cared for.</summary>
        internal void Care(PetHomeCare care) => Cared?.Invoke(care);

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
        private Vector2 Foot(RectTransform rect) =>
            rect.anchoredPosition - new Vector2(0, rect.sizeDelta.y * rect.pivot.y + (_lift.TryGetValue(rect, out float lift) ? lift : 0));

        /// <summary>How high a bouncing ball is off the floor right now.</summary>
        private readonly Dictionary<Transform, float> _lift = new();

        /// <summary>The jelly squash a thing gives as it lands: wide and low, then back with a little wobble.</summary>
        private IEnumerator Squash(Transform target, float amount = .16f)
        {
            const float TIME = .35f;
            for (float t = 0; t < TIME; t += Time.deltaTime)
            {
                float k = t / TIME;
                float wobble = Mathf.Sin(k * Mathf.PI * 2.5f) * (1 - k) * amount;
                target.localScale = new Vector3(target.localScale.x < 0 ? -(1 + wobble) : 1 + wobble, 1 - wobble, 1);
                yield return null;
            }
            target.localScale = new Vector3(target.localScale.x < 0 ? -1 : 1, 1, 1);
        }

        internal void Wobble(Transform target, float amount = .16f)
        {
            if (target != null && target.gameObject.activeInHierarchy) StartCoroutine(Squash(target, amount));
        }

        /// <summary>A ball held under the finger, ready to be thrown: it stays on the floor under it.</summary>
        internal void HoldBall(PetHomeItemView item, Vector2 screen, Camera camera)
        {
            if (_rolling.TryGetValue(item, out var running) && running != null) { StopCoroutine(running); _rolling.Remove(item); }
            _lift.Remove(item.transform);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_floor, screen, camera, out var local);
            var floor = LocalToFloor(local - _floor.rect.min);
            item.Floor = new Vector2(Mathf.Clamp01(floor.x), Mathf.Clamp01(floor.y));
            LayOut(item);
        }

        // ---- kicked toys ----

        /// <summary>The toys that roll when the pet swats them.</summary>
        private static readonly HashSet<string> ROLLS = new()
            { "Toy_Ball", "Toy_Yarn", "Toy_Tennis", "Toy_Soccer", "Toy_BellBall", "Toy_TreatBall" };

        internal bool Rolls(string id) => ROLLS.Contains(id);

        /// <summary>The pet swatted a toy: it rolls away from the pet and comes to rest where it stops.</summary>
        internal void Kick(string id, bool left)
        {
            if (_editing || !_items.TryGetValue(id, out var view) || !view.gameObject.activeSelf) return;
            var dir = new Vector2(left ? -1 : 1, UnityEngine.Random.Range(-.35f, .35f)).normalized;
            StartRoll(view, dir, FloorHeight * UnityEngine.Random.Range(1.4f, 2f), false);
        }

        // ---- thrown by the player, fetched by the pet ----

        private readonly Dictionary<PetHomeItemView, Coroutine> _rolling = new();
        private string _carried;

        internal bool CanFlick(PetHomeItemView item) => !_editing && ROLLS.Contains(item.Id) && item.Id != _carried;

        /// <summary>A ball let go of mid-swipe flies off with the finger's speed; the pet then runs to bring it back.</summary>
        internal void Flick(PetHomeItemView item, Vector2 screenVelocity)
        {
            var canvas = _floor.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1;
            var velocity = screenVelocity / Mathf.Max(.01f, scale);
            float speed = Mathf.Min(velocity.magnitude, FloorHeight * 4f);
            if (speed < FloorHeight * .3f) return;
            StartRoll(item, velocity.normalized, speed, true);
        }

        private void StartRoll(PetHomeItemView view, Vector2 dir, float speed, bool thrown)
        {
            if (_rolling.TryGetValue(view, out var running) && running != null) StopCoroutine(running);
            _rolling[view] = StartCoroutine(Roll(view, dir, speed, thrown));
        }

        /// <summary>The pet picked a ball up: from now on it is drawn in the pet's mouth.</summary>
        internal void PickUp(string id)
        {
            _carried = _items.ContainsKey(id) ? id : null;
            if (_carried != null) Felt?.Invoke(PetHomeFeel.PickUp);
        }

        /// <summary>The pet tells the screen of a moment worth hearing.</summary>
        internal void Feel(PetHomeFeel feel) => Felt?.Invoke(feel);

        /// <summary>The pet let go of what it carried: the ball lies where its mouth was, and is saved there.</summary>
        internal void DropCarried()
        {
            if (_carried == null || !_items.TryGetValue(_carried, out var view)) { _carried = null; return; }
            _carried = null;
            LayOut(view);
            Wobble(view.transform, .1f);
            Felt?.Invoke(PetHomeFeel.Drop);
            Settle(view);
            if (Overlaps(view)) Kick(view.Id, ((RectTransform)_pet.transform).localScale.x < 0);
        }

        /// <summary>The front of what the player sees of the room: where the pet brings a ball back to.</summary>
        internal Vector2 FetchPoint()
        {
            var viewport = (RectTransform)_roomScroll.transform;
            var r = viewport.rect;
            var world = viewport.TransformPoint(new Vector2(r.center.x, r.yMin + r.height * .3f));
            var local = (Vector2)_floor.InverseTransformPoint(world) - _floor.rect.min;
            var floor = LocalToFloor(local);
            return new Vector2(Mathf.Clamp(floor.x, .05f, .95f), Mathf.Clamp(floor.y, .03f, .9f));
        }

        /// <summary>A carried ball sits in the pet's mouth, drawn in front of it.</summary>
        private void DrawCarried()
        {
            if (_carried == null || !_items.TryGetValue(_carried, out var view)) return;
            var pet = (RectTransform)_pet.transform;
            float h = pet.sizeDelta.y;
            float side = pet.localScale.x < 0 ? -1 : 1;
            var foot = Foot(pet);
            var mouth = _pet.CarryMouth;
            view.Floor = LocalToFloor(foot + new Vector2(side * h * mouth.x, -2));
            var rect = (RectTransform)view.transform;
            rect.anchoredPosition = foot + new Vector2(side * h * mouth.x, h * mouth.y);
            rect.SetSiblingIndex(Mathf.Min(_floor.childCount - 1, pet.GetSiblingIndex() + 1));
        }

        private void Settle(PetHomeItemView view)
        {
            view.Committed = view.Floor;
            Placed?.Invoke(new PlacedItemSVO(view.Id, view.Floor.x, view.Floor.y));
            TellPet();
        }

        private IEnumerator Roll(PetHomeItemView view, Vector2 dir, float speed, bool thrown)
        {
            var rect = (RectTransform)view.transform;
            bool bounced = false;
            // A thrown ball leaves the floor and bounces down to a roll; a swatted one only rolls.
            float hop = thrown ? Mathf.Min(speed * .1f, rect.sizeDelta.y * 1.6f) : 0;
            float phase = 0;
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
                    Felt?.Invoke(PetHomeFeel.Bounce);
                    if (bounced) break;
                    dir = -dir;
                    speed *= .5f;
                    bounced = true;
                }
                if (hop > 2)
                {
                    float before2 = phase;
                    phase += Time.deltaTime * 7f;
                    if ((int)(phase / Mathf.PI) != (int)(before2 / Mathf.PI))
                    {
                        hop *= .45f;
                        Felt?.Invoke(PetHomeFeel.Bounce);
                    }
                    float lift = hop * Mathf.Abs(Mathf.Sin(phase));
                    _lift[rect] = lift;
                    rect.anchoredPosition += new Vector2(0, lift);
                }
                else _lift.Remove(rect);
                float turn = speed * Time.deltaTime / Mathf.Max(1, rect.sizeDelta.y * .5f) * Mathf.Rad2Deg;
                rect.localRotation *= Quaternion.Euler(0, 0, dir.x > 0 ? -turn : turn);
                speed *= Mathf.Pow(.2f, Time.deltaTime);
                yield return null;
            }
            _rolling.Remove(view);
            _lift.Remove(rect);
            LayOut(view);
            Settle(view);
            if (thrown && !_editing && view.gameObject.activeSelf)
                _pet.Fetch(new PetHomePetView.Spot { Id = view.Id, Floor = view.Floor, Category = PetHomeCategory.Toy, Width = rect.sizeDelta.x });
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
            DrawCarried();
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
            if (cvo.Category == PetHomeCategory.Bowl && item.Empty)
            {
                RefillClicked?.Invoke(item.Id);
                Wobble(item.transform);
                Felt?.Invoke(PetHomeFeel.Refill);
                return;
            }
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
                Wobble(item.transform);
                Felt?.Invoke(PetHomeFeel.Place);
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

        private void OnPetTapped()
        {
            _pet.Pet();
            Hearts(3);
            Felt?.Invoke(PetHomeFeel.Petted);
            Cared?.Invoke(PetHomeCare.Petted);
        }

        // ---- hearts ----

        private RectTransform _fx;
        private Sprite _heartSprite;

        /// <summary>A few hearts float up from the pet's head and fade: it is pleased.</summary>
        internal void Hearts(int count)
        {
            if (!_pet.GetComponent<Image>().enabled) return;
            if (_fx == null)
            {
                // Over the floor, so the room's depth sort never puts a heart behind a bed.
                var go = new GameObject("Pnl_Fx", typeof(RectTransform));
                go.layer = gameObject.layer;
                _fx = (RectTransform)go.transform;
                _fx.SetParent(_floor.parent, false);
            }
            _fx.SetAsLastSibling();
            _fx.anchorMin = _floor.anchorMin;
            _fx.anchorMax = _floor.anchorMax;
            _fx.offsetMin = _fx.offsetMax = Vector2.zero;
            var pet = (RectTransform)_pet.transform;
            var head = Foot(pet) + new Vector2(0, pet.sizeDelta.y * .8f);
            for (int i = 0; i < count; i++) StartCoroutine(Heart(head, i * .15f, pet.sizeDelta.y));
        }

        private IEnumerator Heart(Vector2 from, float delay, float petHeight)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            var go = new GameObject("Img_Heart", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            var image = go.GetComponent<Image>();
            image.sprite = HeartSprite();
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.SetParent(_fx, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            float size = petHeight * UnityEngine.Random.Range(.16f, .24f);
            rect.sizeDelta = new Vector2(size, size);
            float drift = UnityEngine.Random.Range(-.35f, .35f) * petHeight;
            const float TIME = 1.1f;
            for (float t = 0; t < TIME; t += Time.deltaTime)
            {
                float k = t / TIME;
                rect.anchoredPosition = from + new Vector2(drift * k + Mathf.Sin(k * 9) * size * .15f, petHeight * .6f * k);
                float pop = k < .15f ? k / .15f * 1.2f : Mathf.Lerp(1.2f, 1f, (k - .15f) * 4);
                rect.localScale = Vector3.one * pop;
                image.color = new Color(1, 1, 1, 1 - Mathf.Max(0, k - .6f) / .4f);
                yield return null;
            }
            Destroy(go);
        }

        /// <summary>A soft pink heart, made once.</summary>
        private Sprite HeartSprite()
        {
            if (_heartSprite != null) return _heartSprite;
            const int S = 96;
            var texture = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                // The classic heart curve, a little blurred at its rim, lit from the top left.
                float px = ((x + .5f) / S - .5f) * 2.6f, py = ((y + .5f) / S - .45f) * 2.6f;
                float f = Mathf.Pow(px * px + py * py - 1, 3) - px * px * py * py * py;
                float a = Mathf.Clamp01(-f * 18 + .5f);
                float light = Mathf.Clamp01(1.15f - Mathf.Sqrt((px + .45f) * (px + .45f) + (py - .55f) * (py - .55f)) * .55f);
                var c = Color.Lerp(new Color(.93f, .3f, .55f), new Color(1f, .78f, .88f), light * light);
                pixels[y * S + x] = new Color(c.r, c.g, c.b, a);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            _heartSprite = Sprite.Create(texture, new Rect(0, 0, S, S), new Vector2(.5f, .5f));
            return _heartSprite;
        }

        /// <summary>A tap on the room where nothing stands: the pet walks over, or in edit mode the choice is let go.</summary>
        internal void RoomTapped(Vector2 screen, Camera camera)
        {
            if (_editing) { Select(null); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_floor, screen, camera, out var local);
            var rect = _floor.rect;
            _pet.WalkTo(new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height));
        }

        /// <summary>The pet finished a bowl.</summary>
        internal void PetAte(string id)
        {
            Ate?.Invoke(id);
            Felt?.Invoke(PetHomeFeel.Ate);
            Cared?.Invoke(PetHomeCare.Ate);
            if (_items.TryGetValue(id, out var bowl)) Wobble(bowl.transform, .1f);
        }

        // ---- edit mode ----

        private void StartEditing()
        {
            _pet.LetGo();
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
