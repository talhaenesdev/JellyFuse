using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The board drawn flat with UI sprites, straight into the screen's canvas: no camera, no
    /// RenderTexture and no physics. Coordinates are in cell units, y up. The angled look is faked:
    /// the layers are squashed vertically by SpriteTilt, and a raised block draws a darker copy of
    /// itself under its top face, so it reads as a cube seen from above.
    ///
    /// Each piece is one blob: it slides along the route the model reports, lifts smoothly, and with
    /// AnimateJellies on stretches, wobbles and breathes. Every sprite can be replaced from
    /// CD_JellyPresentation; an empty slot uses the generated one.
    ///
    /// Nothing here allocates per frame, and a frame with nothing moving touches no UI at all. What
    /// allocates is a new snapshot arriving (pooled items are reused, only new ones are created).
    /// The class is split by job: this file draws and moves, .Walls builds the walls and doors,
    /// .Delivery slides delivered jellies into their door, .Follow trails the finger.
    /// </summary>
    public sealed partial class JellySpriteBoardView : MonoBehaviour
    {
        private enum Role { Board = 0, Tile = 1, Wall = 2, Door = 3, Marker = 4, Jelly = 5, Bridge = 6 }
        private sealed class Item
        {
            public RectTransform Root, Body, Side, Shadow;
            public Image BodyImage, SideImage, ShadowImage, HighlightImage;
            /// <summary>The top face's rig; on only for a jelly drawn as top and base.</summary>
            public JellyRigMesh Rig, BaseRig;
            public float Depth, SortY, SortX;
            /// <summary>How much of its picture's height, from the bottom, is the base band (CD_JellyFeel.BaseShare for one cell).</summary>
            public float BaseShare;
            public bool Jelly;
            /// <summary>Painted on the jelly's top face - an arrow block's arrow: it sways, bends and squashes with the top, not the base.</summary>
            public bool RidesTop;
            public bool OnTop;
            /// <summary>Drawn before the other parts of its own jelly: the held jelly's outline, behind its body.</summary>
            public bool Under;
            /// <summary>Drawn after everything, the held jelly too: the wall along the bottom, which is nearest the eye.</summary>
            public bool Front;
        }
        private sealed class Part
        {
            public Item Item;
            public Vector2 Relative, Size;
            public Color Colour;
        }
        private sealed class Blob
        {
            public Vector2 Centre, Offset, OffsetVelocity, Follow, Scale = Vector2.one, ScaleVelocity;
            public float Lift, LiftVelocity, LiftTarget, Phase;
            /// <summary>The most it may grow on each axis and still stay inside its own cells.</summary>
            public Vector2 MaxScale = Vector2.one;
            /// <summary>Which way the shape leans, per half of its size: negative trails behind, positive reaches ahead.</summary>
            public Vector2 Lean, LeanVelocity, Pull, Span = Vector2.one;
            /// <summary>The top face's own motion over its base: how far it trails, and how much it is squashed.</summary>
            public Vector2 TopOffset, TopOffsetVelocity;
            public float TopSquash, TopSquashVelocity;
            public bool Seen, FuseReady;
            /// <summary>Stops on the way to the cell, relative to it, walked in order before settling.</summary>
            public readonly Queue<Vector2> Route = new();
            public readonly List<Part> Parts = new();
        }
        private const float REST = .0005f;
        private readonly List<Item> _free = new();
        private readonly List<Item> _boardItems = new();
        private readonly Dictionary<int, Blob> _blobs = new();
        private readonly List<Part> _freeParts = new();
        // Buffers reused by every snapshot, so presenting one does not allocate them again.
        private readonly List<int> _gone = new();
        private readonly List<Item> _sortBuffer = new();
        private readonly List<(Sprite Sprite, List<Vector2Int> Cells)> _iceTiles = new();
        // Per piece drawn as one picture: the cell it is sorted by. Per piece: how far its parts are sorted forward.
        private readonly Dictionary<int, Vector2Int> _mergedFront = new();
        private readonly Dictionary<int, float> _stackShift = new();
        private readonly HashSet<int> _moved = new();
        /// <summary>Reused while a blob's walk is rebuilt, so a drag leaves no garbage behind.</summary>
        private readonly List<Vector2> _routeBuffer = new();
        private readonly Vector2Int[] _bridgeSides = { Vector2Int.right, Vector2Int.up };
        private readonly System.Comparison<Item> _drawOrder = (a, b) =>
            a.Front != b.Front ? (a.Front ? 1 : -1) : a.OnTop != b.OnTop ? (a.OnTop ? 1 : -1) : a.Under != b.Under ? (a.Under ? -1 : 1) : a.SortY != b.SortY ? b.SortY.CompareTo(a.SortY) : a.SortX.CompareTo(b.SortX);
        private RectTransform _rect, _boardLayer, _jellyLayer, _doorLayer;
        private Image _background;
        private JellySpriteLibrary _sprites;
        private CD_JellyPresentation _config;
        private CD_JellyFeel Feel => _config.Feel;
        private CD_JellyBoardLook Look => _config.Look;
        private JellyLevelCVO _level;
        private PuzzleSnapshotRVO _snapshot;
        private Vector2 _centre;
        private float _cell = 60, _time;
        private int _held = -1;
        /// <summary>Something changed that the next frame must draw, even if nothing is moving.</summary>
        private bool _dirty;

        public void Initialize(CD_JellyPresentation config)
        {
            _config = config;
            if (_rect != null) return;
            _rect = (RectTransform)transform;
            _sprites = new JellySpriteLibrary();
            // A board of its own canvas: the jellies move every frame and must not rebuild the HUD.
            gameObject.AddComponent<Canvas>();
            _background = gameObject.AddComponent<Image>();
            _background.raycastTarget = false;
            // Doors' faces (the jelly each waits for, and its counter) draw over everything else.
            _boardLayer = Layer("BoardLayer");
            // The grid's corners on the board's edge sit on the wall line: drawn over the floor, under the walls and doors.
            _edgeCornerLayer = Layer("EdgeCornerLayer");
            _jellyLayer = Layer("JellyLayer");
            _doorLayer = Layer("DoorLayer");
            // A jelly going into its door is drawn over the doors, with the bits it sheds over that.
            _deliveryLayer = Layer("DeliveryLayer"); _bitLayer = Layer("BitLayer");
            // The floor and the grid's corners change only when the board is built again: each on a canvas of its
            // own, so the jellies moving every frame rebuild only theirs.
            _boardLayer.gameObject.AddComponent<Canvas>();
            _edgeCornerLayer.gameObject.AddComponent<Canvas>();
        }
        private RectTransform Layer(string name)
        {
            var layer = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            layer.gameObject.layer = gameObject.layer;
            layer.SetParent(transform, false);
            layer.anchorMin = layer.anchorMax = layer.pivot = new Vector2(.5f, .5f);
            layer.sizeDelta = Vector2.zero;
            return layer;
        }

        // ---------- geometry ----------

        public Vector2 BoardPoint(Vector2 localPoint, Vector2 rectSize) => new Vector2(localPoint.x, localPoint.y / Look.SpriteTilt) / _cell + _centre;
        public bool Pick(Vector2 localPoint, Vector2 rectSize, out Vector2Int cell, out int id)
        {
            var point = BoardPoint(localPoint, rectSize);
            cell = new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
            var piece = _snapshot != null ? PieceAt(_snapshot, cell) : null;
            // Normal dragging only cares whether the jelly under the pointer is frozen. UFO's
            // colour-wide restrictions belong to PickColour and must not block movable siblings.
            if (piece != null && _snapshot.JellyFrozen(piece.Id) > 0) piece = null;
            id = piece?.Id ?? -1;
            return piece != null;
        }
        /// <summary>The door nearest the point, within a cell of it; frozen doors and doors waiting for nothing are left out.</summary>
        public bool PickDoor(Vector2 localPoint, Vector2 rectSize, out int door)
        {
            door = -1;
            if (_snapshot == null) return false;
            var point = BoardPoint(localPoint, rectSize);
            float best = 1.25f;
            for (int g = 0; g < _snapshot.Level.Gates.Count; g++)
            {
                if (_snapshot.DoorFrozen[g] > 0 || _snapshot.WaitingStage(g) == null) continue;
                foreach (var cell in _snapshot.Level.Gates[g].Cells)
                {
                    float distance = Vector2.Distance(point, cell);
                    if (distance < best) { best = distance; door = g; }
                }
            }
            return door >= 0;
        }
        /// <summary>The colour of the jelly under the point; a frozen jelly cannot be picked.</summary>
        public bool PickColour(Vector2 localPoint, Vector2 rectSize, out JellyColour colour)
        {
            var point = BoardPoint(localPoint, rectSize);
            var piece = _snapshot != null ? PieceAt(_snapshot, new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y))) : null;
            if (piece != null && _snapshot.JellyFrozen(piece.Id) > 0) piece = null;
            colour = piece?.Colour ?? default;
            return piece != null;
        }

        /// <summary>
        /// Whether a UFO may take this colour: no jelly of it is frozen and no frozen door still waits for it, as the
        /// game decides (JellyPuzzleModel.ClearColour).
        /// </summary>
        public bool UfoCanTake(JellyColour colour)
        {
            if (_snapshot == null) return false;
            foreach (var piece in _snapshot.Pieces) if (piece.Colour == colour && _snapshot.JellyFrozen(piece.Id) > 0) return false;
            var gates = _snapshot.Level.Gates;
            for (int g = 0; g < gates.Count; g++)
            {
                if (_snapshot.DoorFrozen.Count <= g || _snapshot.DoorFrozen[g] <= 0) continue;
                for (int s = _snapshot.DoorProgress[g]; s < gates[g].Stages.Count; s++) if (gates[g].Stages[s].Colour == colour) return false;
            }
            return true;
        }
        /// <summary>Every jelly of the colour gives a shake: a UFO cannot take it.</summary>
        public void ShakeColour(JellyColour colour)
        {
            if (_snapshot == null) return;
            foreach (var piece in _snapshot.Pieces)
                if (piece.Colour == colour && _blobs.TryGetValue(piece.Id, out var blob)) Kick(blob, -Feel.GrabPop * 1.5f);
            _dirty = true;
        }
        /// <summary>
        /// What a booster waiting for its target lets the player pick, as world-space squares (min, max) a veil leaves
        /// light: the doors a rocket may take, or every jelly a UFO may take (UfoCanTake). Read each frame, as the board may move.
        /// </summary>
        public void FocusAreas(Focus focus, List<(Vector3 Min, Vector3 Max)> into)
        {
            if (_snapshot == null || _doorLayer == null) return;
            if (focus == Focus.Doors)
                foreach (var pair in _doorFrames)
                {
                    int door = pair.Key; var frame = pair.Value;
                    if (door >= _snapshot.Level.Gates.Count || door >= _snapshot.DoorProgress.Count) continue;
                    if (_snapshot.DoorFrozen.Count > door && _snapshot.DoorFrozen[door] > 0 || _snapshot.WaitingStage(door) == null) continue;
                    into.Add((_doorLayer.TransformPoint(frame.Centre - frame.Size * .5f), _doorLayer.TransformPoint(frame.Centre + frame.Size * .5f)));
                }
            else if (focus == Focus.Jellies)
                foreach (var piece in _snapshot.Pieces)
                {
                    if (_snapshot.JellyFrozen(piece.Id) > 0 || !UfoCanTake(piece.Colour)) continue;
                    foreach (var cell in piece.Cells) if (CellCorners(cell, out var min, out var max)) into.Add((min, max));
                }
        }
        /// <summary>What a booster waiting for its target lets the player pick: the rest of the board is dimmed.</summary>
        public enum Focus { None = 0, Doors = 1, Jellies = 2 }
        private Focus _focus;
        private float _focusDim = 1, _focusPulse;
        private Color _focusGlow, _focusBlocked;
        private CanvasGroup _jellyGroup, _doorGroup, _boardGroup;
        /// <summary>Each door drawn now, waiting for a jelly: its centre and size on the door layer, and whether it is frozen.</summary>
        private readonly Dictionary<int, (Vector2 Centre, Vector2 Size, bool Frozen)> _doorFrames = new();
        private RectTransform _focusLayer;
        private readonly List<Image> _focusMarks = new();
        public void SetFocus(Focus focus, float dimAlpha, float pulseSpeed, Color glow = default, Color blocked = default)
        {
            _focus = focus; _focusDim = dimAlpha; _focusPulse = pulseSpeed; _focusGlow = glow; _focusBlocked = blocked;
            _dirty = true;
            if (_jellyLayer == null) return;
            if (_jellyGroup == null)
            {
                _jellyGroup = _jellyLayer.gameObject.AddComponent<CanvasGroup>(); _doorGroup = _doorLayer.gameObject.AddComponent<CanvasGroup>();
                _boardGroup = _boardLayer.gameObject.AddComponent<CanvasGroup>();
            }
            UpdateFocus();
        }
        private void UpdateFocus()
        {
            if (_jellyGroup == null) return;
            float pulse = _focusPulse > 0 ? Mathf.Lerp(.65f, 1, .5f + .5f * Mathf.Cos(Time.unscaledTime * _focusPulse * Mathf.PI * 2)) : 1;
            // A rocket picks a door: the jellies fade (in Apply, part by part), but not the layer they are on - the
            // doors in the top wall and the wall round the board are drawn on it too, and must stay whole.
            _jellyGroup.alpha = 1;
            // A UFO's jellies are shown by the screen's veil alone: no glow and no fade of the board here.
            _doorGroup.alpha = 1;
            _boardGroup.alpha = 1;
            UpdateDoorMarks(pulse);
        }
        /// <summary>While a rocket waits: a pulsing glow behind every door it can take, and a dark veil over every frozen one.</summary>
        private void UpdateDoorMarks(float pulse)
        {
            if (_focusLayer == null)
            {
                _focusLayer = Layer("FocusLayer");
                // Behind every door - those in the top wall are drawn with the jellies - so the glow shows round them, not over them.
                _focusLayer.SetSiblingIndex(_jellyLayer.GetSiblingIndex());
            }
            _focusLayer.localScale = _doorLayer.localScale;
            int used = 0;
            if (_focus == Focus.Doors && _snapshot != null)
                foreach (var pair in _doorFrames)
                {
                    // Read now, not when the door was drawn: a door may have thawed, frozen or taken its last jelly since.
                    int door = pair.Key; var frame = pair.Value;
                    // A frame left from another level's doors is not one of this board's.
                    if (door >= _snapshot.Level.Gates.Count || door >= _snapshot.DoorProgress.Count) continue;
                    bool frozen = _snapshot.DoorFrozen.Count > door && _snapshot.DoorFrozen[door] > 0;
                    if (frozen || _snapshot.WaitingStage(door) == null) continue;
                    if (used == _focusMarks.Count)
                    {
                        var mark = new GameObject("Img_BoosterMark", typeof(RectTransform)).AddComponent<Image>();
                        mark.rectTransform.SetParent(_focusLayer, false);
                        mark.raycastTarget = false; mark.type = Image.Type.Sliced;
                        _focusMarks.Add(mark);
                    }
                    var image = _focusMarks[used++];
                    image.gameObject.SetActive(true);
                    image.sprite = _sprites.Glow; image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 56 / Mathf.Max(1, _cell * .45f);
                    image.rectTransform.anchoredPosition = frame.Centre;
                    // A soft halo round the door, breathing with the pulse.
                    image.rectTransform.sizeDelta = frame.Size + Vector2.one * _cell * (.55f + .2f * pulse);
                    var colour = _focusGlow; colour.a *= Mathf.Lerp(.6f, 1, pulse);
                    image.color = colour;
                }
            for (int i = used; i < _focusMarks.Count; i++) _focusMarks[i].gameObject.SetActive(false);
        }

        private Vector2 Position(Vector2Int cell) => (new Vector2(cell.x, cell.y) - _centre) * _cell;
        /// <summary>A cell's square in world space, as the jellies are drawn; false before a board is shown.</summary>
        public bool CellCorners(Vector2Int cell, out Vector3 min, out Vector3 max) => CellCorners((Vector2)cell, out min, out max);
        /// <summary>The same for a point between cells: a cell-sized square round it.</summary>
        public bool CellCorners(Vector2 cell, out Vector3 min, out Vector3 max)
        {
            min = max = default;
            if (_jellyLayer == null || _level == null) return false;
            var centre = Position(cell);
            var half = Vector2.one * (_cell * .5f);
            min = _jellyLayer.TransformPoint(centre - half);
            max = _jellyLayer.TransformPoint(centre + half);
            return true;
        }
        private Vector2 Position(Vector2 cell) => (cell - _centre) * _cell;
        private static Vector2 CentreOf(IReadOnlyList<Vector2Int> cells)
        {
            var sum = Vector2.zero;
            for (int i = 0; i < cells.Count; i++) sum += cells[i];
            return sum / Mathf.Max(1, cells.Count);
        }
        private static bool Contains(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
        {
            for (int i = 0; i < cells.Count; i++) if (cells[i] == cell) return true;
            return false;
        }
        private static bool Contains(IReadOnlyList<int> values, int value)
        {
            for (int i = 0; i < values.Count; i++) if (values[i] == value) return true;
            return false;
        }
        private static JellyPieceVO PieceById(PuzzleSnapshotRVO snapshot, int id)
        {
            if (snapshot == null) return null;
            for (int i = 0; i < snapshot.Pieces.Count; i++) if (snapshot.Pieces[i].Id == id) return snapshot.Pieces[i];
            return null;
        }
        private static JellyPieceVO PieceAt(PuzzleSnapshotRVO snapshot, Vector2Int cell)
        {
            for (int i = 0; i < snapshot.Pieces.Count; i++) if (Contains(snapshot.Pieces[i].Cells, cell)) return snapshot.Pieces[i];
            return null;
        }
        // A palette colour is read from the presentation live; a level's own Extra colour from the level.
        /// <summary>The colour a jelly of <paramref name="colour"/> is drawn in.</summary>
        public Color JellyTint(JellyColour colour) => Tint((int)colour);
        public Sprite RoundedSprite => _sprites?.Rounded;
        private Color Tint(int colour) =>
            Look.Palette != null && Look.Palette.Find((JellyColour)colour) != null ? Look.Colour(colour) : _level.ColourTint(colour, Look.Colour(colour));

        /// <summary>On: the map is sized by its width alone, even where that makes it taller than its rect.</summary>
        public bool FillWidth { get; set; }
        private void Fit()
        {
            // Everything drawn: the floor, the wall frame round it (Map Frame Margin), and the doors with
            // their counters reaching past it - on their own side only, so a side with no door takes no
            // door's room. That box fills the rect and is centred in it, so nothing spills out and no room
            // is left unused.
            int floorMinX = int.MaxValue, floorMaxX = int.MinValue, floorMinY = int.MaxValue, floorMaxY = int.MinValue;
            for (int y = 0; y < _level.Height; y++) for (int x = 0; x < _level.Width; x++)
            {
                if (Out(new Vector2Int(x, y))) continue;
                floorMinX = Mathf.Min(floorMinX, x); floorMaxX = Mathf.Max(floorMaxX, x);
                floorMinY = Mathf.Min(floorMinY, y); floorMaxY = Mathf.Max(floorMaxY, y);
            }
            if (floorMinX > floorMaxX) { floorMinX = 0; floorMaxX = _level.Width - 1; floorMinY = 0; floorMaxY = _level.Height - 1; }
            float frame = .5f + Look.MapFrameMargin;
            float minX = floorMinX - .5f - Look.MapFrameLeft, maxX = floorMaxX + .5f + Look.MapFrameRight;
            float minY = floorMinY - frame - Look.SpriteBoardDepth, maxY = floorMaxY + frame + Look.SpriteWallDepth;
            // Across, the wall frame stays in the middle of the screen: whichever side reaches further out
            // (a door on it) is matched on the other side, so the frame's gaps left and right are equal on
            // every map.
            float frameCentre = (minX + maxX) * .5f;
            foreach (var gate in _level.Gates) DoorBox(gate, ref minX, ref maxX, ref minY, ref maxY);
            float half = Mathf.Max(frameCentre - minX, maxX - frameCentre);
            minX = frameCentre - half; maxX = frameCentre + half;
            _centre = new Vector2((minX + maxX) * .5f, (minY + maxY) * .5f);
            var size = _rect.rect.size;
            _cell = FillWidth ? size.x / (maxX - minX) : Mathf.Min(size.x / (maxX - minX), size.y / ((maxY - minY) * Look.SpriteTilt));
            var tilt = new Vector3(1, Look.SpriteTilt, 1);
            _boardLayer.localScale = tilt; _jellyLayer.localScale = tilt; _edgeCornerLayer.localScale = tilt; _doorLayer.localScale = tilt;
            _deliveryLayer.localScale = tilt; _bitLayer.localScale = tilt;
            SetSprite(_background, Pick(Look.BackgroundSprite, _sprites.Rounded), Mathf.Min(size.x, size.y) * .15f);
            _background.color = Look.Background;
            // A see-through backdrop is not drawn at all.
            _background.enabled = Look.Background.a > 0;
        }

        /// <summary>Grows the box, in cells, to take in a door's picture and its counter, placed as BuildDoor places them.</summary>
        private void DoorBox(JellyGateCVO gate, ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            var skin = Look.WallSkin;
            var painted = skin != null ? skin.Door(gate.Side, gate.Size) : null;
            var mid = (Vector2)(gate.Cells[0] + gate.Cells[^1]) * .5f;
            Vector2 centre, cells;
            if (painted != null && skin.PixelsPerCell > 0)
            {
                cells = painted.rect.size / skin.DoorPixels;
                centre = mid - (Vector2)gate.Direction * .5f + Vector2.Scale(new Vector2(.5f, .5f) - painted.pivot / painted.rect.size, cells);
            }
            else
            {
                float across = skin != null ? skin.DoorThickness : 1, overhang = skin != null ? skin.DoorOverhang : 0;
                centre = mid - (Vector2)gate.Direction * (skin != null ? skin.DoorInset : 0);
                cells = gate.Direction.x != 0 ? new Vector2(across, gate.Size + overhang * 2) : new Vector2(gate.Size + overhang * 2, across);
            }
            if (skin != null) centre += skin.DoorOffset(gate.Side) + skin.PieceOffset;
            Grow(centre, cells * .5f, ref minX, ref maxX, ref minY, ref maxY);
            if (gate.Stages.Count < 2) return;
            // The counter shows while more than one jelly is still to come.
            var anchor = skin != null ? skin.CounterAnchor(gate.Side) : new Vector2(0, .5f);
            var badge = centre + Vector2.Scale(anchor - new Vector2(.5f, .5f), cells) + (skin != null ? skin.CounterOffset : Vector2.zero);
            Grow(badge, Vector2.one * (skin != null ? skin.CounterSize : .34f) * .5f, ref minX, ref maxX, ref minY, ref maxY);
        }
        private static void Grow(Vector2 centre, Vector2 half, ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            minX = Mathf.Min(minX, centre.x - half.x); maxX = Mathf.Max(maxX, centre.x + half.x);
            minY = Mathf.Min(minY, centre.y - half.y); maxY = Mathf.Max(maxY, centre.y + half.y);
        }

        // ---------- sprites and items ----------

        private static Sprite Pick(Sprite custom, Sprite fallback) => custom != null ? custom : fallback;
        private Sprite ColourSprite(int colour)
        {
            // Compared with Unity's != on purpose: a reference to a sprite that is missing is not C# null.
            var entry = Look.Palette != null ? Look.Palette.Find((JellyColour)colour) : null;
            return entry != null && entry.Sprite != null ? entry.Sprite : null;
        }
        private bool Blocks => Look.JellySpriteIsBlock;
        /// <summary>Splits a block's picture into its base band and its top face at this share of its height.</summary>
        private void SetBaseShare(Item item, float share)
        {
            item.BaseShare = share;
            if (!item.BaseRig || !(Blocks && Feel.SplitTopAndBase)) return;
            item.SideImage.fillAmount = Mathf.Clamp01(share + Feel.BaseOverlap * share / Mathf.Max(.01f, Feel.BaseShare));
            item.BodyImage.fillAmount = 1 - share;
            // It squashes down onto the top of the base, not round its middle.
            item.Body.pivot = new Vector2(.5f, share);
        }
        private void SetSprite(Image image, Sprite sprite, float size)
        {
            if (sprite == null) sprite = _sprites.Rounded;
            image.sprite = sprite;
            if (sprite.border.sqrMagnitude > 0)
            {
                // Sliced corners are measured in sprite pixels; scale them to a fixed share of the block.
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = Mathf.Max(sprite.border.x, sprite.border.w) / Mathf.Max(1, Look.SpriteCornerRadius * size);
            }
            else image.type = Image.Type.Simple;
        }

        private Item Take(Transform layer)
        {
            Item item;
            if (_free.Count > 0) { item = _free[^1]; _free.RemoveAt(_free.Count - 1); }
            else item = Create();
            item.Root.SetParent(layer, false);
            item.Root.SetAsLastSibling();
            item.Under = false;
            item.Root.gameObject.SetActive(true);
            item.Root.localRotation = Quaternion.identity; item.Root.localScale = Vector3.one;
            item.OnTop = item.Front = false;
            return item;
        }
        private void Give(Item item) { item.Root.gameObject.SetActive(false); _free.Add(item); }
        private Item Create()
        {
            var item = new Item();
            item.Root = Child("Block", transform, out _, true);
            item.Shadow = Child("Shadow", item.Root, out item.ShadowImage);
            item.Side = Child("Side", item.Root, out item.SideImage);
            item.Body = Child("Body", item.Root, out item.BodyImage);
            item.Rig = item.Body.gameObject.AddComponent<JellyRigMesh>();
            item.Rig.enabled = false;
            // The base band's rig: with a mirrored top it only follows the top's bottom edge.
            item.BaseRig = item.Side.gameObject.AddComponent<JellyRigMesh>();
            item.BaseRig.enabled = false;
            var highlight = Child("Highlight", item.Body, out item.HighlightImage);
            highlight.anchorMin = new Vector2(.12f, .52f); highlight.anchorMax = new Vector2(.72f, .9f);
            return item;
        }
        private RectTransform Child(string name, Transform parent, out Image image, bool empty = false)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            image = null;
            if (empty) return rect;
            image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return rect;
        }

        private Item Place(Item item, Role role, Vector2 position, Vector2 size, Color colour, float depth = 0, float lift = 0, int colourId = -1, Sprite sprite = null)
        {
            bool jelly = role is Role.Jelly or Role.Bridge;
            bool block = jelly && Blocks;
            item.BodyImage.material = null;
            // A pooled item may have been an outline: its side carried the flat-fill material.
            item.SideImage.material = null;
            item.Jelly = jelly;
            item.RidesTop = false;
            item.Root.anchorMin = item.Root.anchorMax = new Vector2(.5f, .5f);
            item.Root.anchoredPosition = position; item.Root.sizeDelta = size;
            // Back rows first and, in a row, left to right: a cell's shadow falls right and down, under
            // the next cell's body, never over it.
            item.Depth = depth; item.SortY = position.y; item.SortX = position.x;
            float px = Mathf.Min(size.x, size.y);

            var custom = jelly ? (sprite != null ? sprite : ColourSprite(colourId)) : sprite;
            var body = custom != null ? custom : role switch
            {
                Role.Board => Pick(Look.BoardSprite, _sprites.Rounded),
                Role.Tile => Pick(Look.TileSprite, _sprites.Rounded),
                Role.Wall or Role.Door => Pick(Look.WallSprite, _sprites.Rounded),
                Role.Marker => _sprites.Rounded,
                _ => Pick(Look.JellySprite, _sprites.Rounded),
            };
            SetSprite(item.BodyImage, body, px);
            // A block sprite is its top face plus the side painted under it: the top fills the cell
            // and the side hangs below, over the cell in front, which is drawn after it.
            // Measured against the item's own shape, so a picture of a whole jelly fits its cells too.
            float aspect = block ? body.rect.height / Mathf.Max(1, body.rect.width) * size.x / Mathf.Max(1, size.y) : 1;
            item.Body.anchorMin = new Vector2(0, 1 - aspect);
            // A colour sprite is drawn as painted; everything else is a white shape tinted here.
            item.BodyImage.color = custom != null ? Color.white : colour;

            if (block) depth = item.Depth = 0;
            item.SideImage.enabled = depth > 0;
            item.Side.anchorMin = Vector2.zero; item.Side.anchorMax = Vector2.one;
            item.Body.pivot = new Vector2(.5f, .5f); item.Body.localScale = Vector3.one;
            item.Rig.enabled = false;
            item.BaseRig.enabled = false;
            if (block && Feel.SplitTopAndBase)
            {
                item.Rig.enabled = Feel.UseRig;
                item.Rig.Columns = Feel.RigColumns; item.Rig.Rows = Feel.RigRows;
                item.Rig.Curve = Feel.RigCurve; item.Rig.Bulge = Feel.RigBulge; item.Rig.TopDip = Feel.RigTopDip;
                item.Rig.Bend = Vector2.zero; item.Rig.Squash = 0;
                item.Rig.Mirror = Feel.RigMirrorBottom; item.Rig.Shift = Vector2.zero;
                item.BaseRig.enabled = Feel.UseRig && Feel.RigMirrorBottom;
                item.BaseRig.Columns = 1; item.BaseRig.Rows = 1; item.BaseRig.Shift = Vector2.zero;
                item.BaseRig.Bend = Vector2.zero; item.BaseRig.Squash = 0;
                // The base is the picture's bottom band (and a little above it, hidden under the top), and
                // over it the top face alone: the top moves on its own and never shows a second copy behind it.
                item.SideImage.enabled = true;
                SetSprite(item.SideImage, body, px);
                item.SideImage.type = Image.Type.Filled;
                item.SideImage.fillMethod = Image.FillMethod.Vertical;
                item.SideImage.fillOrigin = (int)Image.OriginVertical.Bottom;

                item.SideImage.color = item.BodyImage.color;
                item.Side.anchorMin = item.Body.anchorMin; item.Side.anchorMax = item.Body.anchorMax;
                item.BodyImage.type = Image.Type.Filled;
                item.BodyImage.fillMethod = Image.FillMethod.Vertical;
                item.BodyImage.fillOrigin = (int)Image.OriginVertical.Top;
                SetBaseShare(item, Feel.BaseShare);
            }
            else if (depth > 0)
            {
                SetSprite(item.SideImage, jelly && Look.JellySideSprite != null ? Look.JellySideSprite : body, px);
                var side = custom != null ? Color.white : colour;
                item.SideImage.color = Color.Lerp(side, Color.black, Look.SpriteSideDarken) * new Color(1, 1, 1, side.a);
            }

            item.ShadowImage.enabled = jelly;
            item.Shadow.anchorMin = Vector2.zero; item.Shadow.anchorMax = Vector2.one;
            if (jelly)
            {
                if (block && custom != null)
                {
                    // A painted jelly casts its own outline: a box round an L or a T would darken the jelly
                    // sitting in its empty corner.
                    item.ShadowImage.sprite = body; item.ShadowImage.type = Image.Type.Simple;
                    item.Shadow.anchorMin = item.Body.anchorMin; item.Shadow.anchorMax = item.Body.anchorMax;
                }
                else SetSprite(item.ShadowImage, Pick(Look.JellyShadowSprite, _sprites.Rounded), px);
                item.ShadowImage.color = new Color(0, 0, 0, Look.SpriteShadow);
            }

            // A painted wall has its shine painted in; only a generated one gets the faint highlight.
            bool wall = role is Role.Wall or Role.Door && custom == null;
            item.HighlightImage.enabled = (role == Role.Jelly && !block) || wall;
            if (item.HighlightImage.enabled)
            {
                SetSprite(item.HighlightImage, Pick(Look.JellyHighlightSprite, _sprites.Highlight), px);
                item.HighlightImage.color = new Color(1, 1, 1, wall ? Look.SpriteHighlight * .25f : Look.SpriteHighlight);
            }

            SetLift(item, lift, 1);
            return item;
        }
        private void SetLift(Item item, float lift, float height)
        {
            // The top face sits a block's height above its footprint and the darker side fills the gap
            // below it. Lift raises both and leaves the shadow on the board, so a held jelly reads as picked up.
            // A jelly's picture can be nudged off its cell to seat the art on the grid.
            var offset = item.Jelly ? Look.JellySpriteOffset * _cell : Vector2.zero;
            item.Side.anchoredPosition = offset + new Vector2(0, lift * _cell);
            item.Body.anchoredPosition = offset + new Vector2(0, (lift + item.Depth * height) * _cell);
            item.Shadow.anchoredPosition = offset + Look.SpriteShadowOffset * _cell;
        }

        // ---------- board ----------

        private void BuildBoard()
        {
            ReleaseGates();
            _doorFrames.Clear();
            foreach (var item in _boardItems) Give(item);
            _boardItems.Clear();
            // Which cells are outside comes first: the fit measures the floor inside them.
            FindOutside();
            Fit();
            // Back rows first, so a nearer row's slab covers the one behind it.
            float slab = Look.SpriteBoardDepth;
            for (int y = _level.Height - 1; y >= 0; y--) for (int x = 0; x < _level.Width; x++)
                // No board under the wall round the board: its painted outline stands on the floor's edge.
                if (!Contains(_level.Holes, new Vector2Int(x, y)) && !(_outside.Contains(new Vector2Int(x, y)) && Skin != null))
                    _boardItems.Add(Place(Take(_boardLayer), Role.Board, Position(new Vector2Int(x, y)), Vector2.one * _cell * 1.02f, Look.BoardColour, slab, -slab));
            for (int y = _level.Height - 1; y >= 0; y--) for (int x = 0; x < _level.Width; x++)
            {
                var cell = new Vector2Int(x, y);
                // A door is a gap in the wall, not floor: nothing is drawn under it.
                if (Contains(_level.Holes, cell) || Contains(_level.Walls, cell) || IsDoorCell(cell)) continue;
                var tile = (x + y) % 2 == 1 && Look.TileSpriteAlt != null ? Look.TileSpriteAlt : Look.TileSprite;
                _boardItems.Add(Place(Take(_boardLayer), Role.Tile, Position(cell), Vector2.one * _cell * Look.SpriteTileSize,
                    tile != null ? Color.white : Look.TileColour, 0, 0, -1, tile));
            }
            // At every corner of the floor tiles - between them, along the board's edge and at its corners - the grid's corner piece sits on the gap.
            _gridCorners.Clear();
            if (Look.GridCornerSprite != null)
                for (int y = -1; y < _level.Height; y++) for (int x = -1; x < _level.Width; x++)
                {
                    bool a = IsFloor(new Vector2Int(x, y)), b = IsFloor(new Vector2Int(x + 1, y)), c = IsFloor(new Vector2Int(x, y + 1)), d = IsFloor(new Vector2Int(x + 1, y + 1));
                    // Every corner of a floor tile gets one, the board's own corners included.
                    if (!a && !b && !c && !d) continue;
                    bool edge = !(a && b && c && d);
                    var corner = Place(Take(edge ? _edgeCornerLayer : _boardLayer), Role.Tile, Position(new Vector2(x + .5f, y + .5f)), Vector2.one * _cell * Look.GridCornerSize,
                        Color.white, 0, 0, -1, Look.GridCornerSprite);
                    _boardItems.Add(corner);
                    _gridCorners[new Vector2Int(x, y)] = corner;
                }
            UpdateFuseGlow(0);
            BuildWalls();
        }

        /// <summary>The grid's corner pieces, by the cell to their lower left.</summary>
        private readonly Dictionary<Vector2Int, Item> _gridCorners = new();
        private RectTransform _edgeCornerLayer;
        /// <summary>The corners round the jellies just fused, and how long they still blink.</summary>
        private readonly HashSet<Vector2Int> _glowCorners = new();
        private float _glowLeft;
        /// <summary>A jelly grew: the grid corners at its cells - or every one on the board - blink for a while.</summary>
        private void GlowAround(JellyPieceVO piece)
        {
            if (Look.GridCornerLightSprite == null || Look.FuseGlowSeconds <= 0) return;
            if (Look.FuseGlowWholeGrid) _glowCorners.UnionWith(_gridCorners.Keys);
            else
                foreach (var c in piece.Cells)
                    for (int dx = -1; dx <= 0; dx++) for (int dy = -1; dy <= 0; dy++) _glowCorners.Add(new Vector2Int(c.x + dx, c.y + dy));
            _glowLeft = Look.FuseGlowSeconds;
        }
        private RectTransform _bloomLayer;
        private readonly List<Image> _blooms = new();
        private void UpdateFuseGlow(float dt)
        {
            if (_glowCorners.Count == 0) return;
            _glowLeft -= dt;
            bool done = _glowLeft <= 0 || Look.GridCornerLightSprite == null;
            // Lit on the first half of each blink, plain on the second.
            float t = 1 - Mathf.Clamp01(_glowLeft / Mathf.Max(.01f, Look.FuseGlowSeconds));
            float phase = Mathf.Repeat(t * Look.FuseGlowBlinks, 1);
            bool lit = !done && phase < .5f;
            // The glow swells and fades with each blink instead of snapping, and dims a little blink by blink.
            float swell = lit ? Mathf.Sin(phase * 2 * Mathf.PI) : 0;
            float bright = swell * (1 - t * .35f);
            if (_bloomLayer == null)
            {
                _bloomLayer = Layer("BloomLayer");
                _bloomLayer.SetSiblingIndex(_edgeCornerLayer.GetSiblingIndex() + 1);
            }
            _bloomLayer.localScale = _edgeCornerLayer.localScale;
            int used = 0;
            foreach (var key in _glowCorners)
            {
                if (!_gridCorners.TryGetValue(key, out var item)) continue;
                item.BodyImage.sprite = lit ? Look.GridCornerLightSprite : Look.GridCornerSprite;
                item.Root.localScale = Vector3.one * (lit ? Mathf.Lerp(1, Look.FuseGlowCornerScale, swell) : 1);
                if (bright <= 0 || Look.FuseGlowBloomColour.a <= 0 || Look.FuseGlowBloomStrength <= 0 || Look.FuseGlowBloomSize <= 0) continue;
                if (used == _blooms.Count)
                {
                    var bloom = new GameObject("Img_CornerBloom", typeof(RectTransform)).AddComponent<Image>();
                    bloom.gameObject.layer = gameObject.layer;
                    bloom.rectTransform.SetParent(_bloomLayer, false);
                    bloom.raycastTarget = false;
                    _blooms.Add(bloom);
                }
                var image = _blooms[used++];
                image.gameObject.SetActive(true);
                image.sprite = _sprites.Highlight;
                image.rectTransform.anchoredPosition = item.Root.anchoredPosition;
                image.rectTransform.sizeDelta = Vector2.one * _cell * Look.FuseGlowBloomSize * (.8f + .2f * swell);
                var colour = Look.FuseGlowBloomColour; colour.a = Mathf.Clamp01(colour.a * bright * Look.FuseGlowBloomStrength);
                image.color = colour;
            }
            for (int i = used; i < _blooms.Count; i++) _blooms[i].gameObject.SetActive(false);
            if (done)
            {
                foreach (var key in _glowCorners) if (_gridCorners.TryGetValue(key, out var item)) item.Root.localScale = Vector3.one;
                _glowCorners.Clear();
            }
        }
        private bool IsFloor(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < _level.Width && cell.y < _level.Height && !Out(cell) && !Contains(_level.Holes, cell) && !Contains(_level.Walls, cell) && !IsDoorCell(cell);

        // ---------- snapshot ----------

        /// <summary>Whether the snapshot shows the same board as the last one: a repeated pointer update must not kick the springs again.</summary>
        private bool SameBoard(PuzzleSnapshotRVO snapshot, int held)
        {
            if (_snapshot == null || _snapshot.Level != snapshot.Level || _held != held) return false;
            if (!SameInts(_snapshot.DoorProgress, snapshot.DoorProgress)) return false;
            if (_snapshot.Pieces.Count != snapshot.Pieces.Count) return false;
            foreach (var piece in snapshot.Pieces)
            {
                var old = PieceById(_snapshot, piece.Id);
                if (old == null || old.Cells.Count != piece.Cells.Count) return false;
                for (int i = 0; i < piece.Cells.Count; i++) if (old.Cells[i] != piece.Cells[i]) return false;
            }
            return true;
        }
        private static bool SameInts(IReadOnlyList<int> a, IReadOnlyList<int> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }

        public void Present(PuzzleSnapshotRVO snapshot, int held, bool forceRefresh = false, IReadOnlyList<PuzzleSnapshotRVO> route = null)
        {
            if (!forceRefresh && SameBoard(snapshot, held))
            {
                // Same cells, but the fuse hint and free sides may differ.
                _snapshot = snapshot;
                foreach (var pair in _blobs) RefreshHint(pair.Key, pair.Value);
                UpdateFollow();
                _dirty = true;
                return;
            }
            var previous = _snapshot != null && _snapshot.Level == snapshot.Level ? _snapshot : null;
            int previousHeld = _held;
            _snapshot = snapshot; _held = held;
            // A new level or a settings change starts every blob afresh where it belongs.
            if (previous == null || forceRefresh) ClearBlobs();
            if (_level != snapshot.Level || previous == null || forceRefresh || !SameInts(previous.DoorProgress, snapshot.DoorProgress))
            {
                // Only a rebuild changes which doors are drawn, so only then are the closing ones found.
                FindClosingDoors(forceRefresh ? null : previous, snapshot);
                _level = snapshot.Level; BuildBoard();
            }

            foreach (var blob in _blobs.Values)
            {
                foreach (var part in blob.Parts) { Give(part.Item); _freeParts.Add(part); }
                blob.Parts.Clear(); blob.Seen = false;
            }
            _moved.Clear();
            _mergedFront.Clear();
            float width = Look.JellyWidth * _cell, depth = Look.SpriteJellyDepth;
            foreach (var piece in snapshot.Pieces)
            {
                var centre = Position(CentreOf(piece.Cells));
                var old = PieceById(previous, piece.Id);
                if (old != null && piece.Cells.Count > old.Cells.Count && FormsWaitingShape(piece)) GlowAround(piece);
                if (!_blobs.TryGetValue(piece.Id, out var blob))
                    _blobs[piece.Id] = blob = new Blob { Centre = centre, Phase = piece.Id * 1.7f };
                else if (old != null && piece.Cells.Count < old.Cells.Count && ShiftWithin(piece, old) is var shift && shift != Vector2Int.zero)
                {
                    // An undo took it apart: it slides back from where its cells sat inside the bigger piece.
                    blob.Centre = centre; blob.Follow = Vector2.zero; blob.Route.Clear();
                    blob.Offset = (Vector2)shift * _cell;
                    _moved.Add(piece.Id);
                }
                else if (old != null && old.Cells.Count != piece.Cells.Count)
                {
                    // A fuse changes the shape: sliding the grown shape from the old place would push
                    // the absorbed cells through their neighbours, so it settles in place and pops instead.
                    blob.Centre = centre; blob.Offset = blob.Follow = Vector2.zero; blob.Route.Clear();
                }
                else if ((blob.Centre - centre).sqrMagnitude > 1)
                {
                    // Keep the blob where it is on screen; the slide then carries it to its new cells.
                    var frame = blob.Centre - centre;
                    // A fast drag sends a new turn while the blob is still walking the last one. What is
                    // left of that walk is kept, and the cell it was heading for with it: jumping from
                    // where it is drawn straight to the new route would cut corners through the jellies
                    // the route went round.
                    _routeBuffer.Clear();
                    foreach (var point in blob.Route) _routeBuffer.Add(point + frame);
                    if (blob.Route.Count > 0 || blob.Offset.sqrMagnitude > _cell * _cell * .25f) _routeBuffer.Add(frame);
                    blob.Offset += frame; blob.Centre = centre;
                    // Walk the cells the piece passed through, so a detour is seen as one.
                    blob.Route.Clear();
                    foreach (var point in _routeBuffer) blob.Route.Enqueue(point);
                    if (route != null)
                        for (int f = 0; f < route.Count - 1; f++)
                        {
                            var step = PieceById(route[f], piece.Id);
                            if (step != null) blob.Route.Enqueue(Position(CentreOf(step.Cells)) - centre);
                        }
                    _moved.Add(piece.Id);
                }
                blob.Seen = true;
                Span(piece.Cells, out float spanX, out float spanY);
                blob.MaxScale = new Vector2(spanX / (spanX - 1 + Look.JellyWidth), spanY / (spanY - 1 + Look.JellyWidth));
                blob.Span = new Vector2(spanX - 1 + Look.JellyWidth, spanY - 1 + Look.JellyWidth);
                if (old != null && piece.Cells.Count > old.Cells.Count)
                {
                    Kick(blob, Feel.FusePop);
                    BurstFuse(piece.Cells, Tint((int)piece.Colour));
                    // The flash, ring and shake are kept for the fuse that completes a shape a door waits for.
                    if (FormsWaitingShape(piece)) Impact(piece.Id, piece.Cells, Tint((int)piece.Colour));
                }
                if (held == piece.Id && previousHeld != piece.Id) Kick(blob, Feel.GrabPop);
                if (previousHeld == piece.Id && held != piece.Id) Kick(blob, Feel.LandSquash);
                RefreshHint(piece.Id, blob);

                var colour = Tint((int)piece.Colour);
                bool top = held == piece.Id;
                if (Blocks)
                {
                    // A whole-jelly picture for this shape is drawn once over all its cells.
                    var merged = Look.Merged(piece.Colour, piece.Cells);
                    if (merged != null)
                    {
                        float inset = (_cell - width) * .5f;
                        var extent = new Vector2(spanX * _cell - inset * 2, spanY * _cell - inset * 2);
                        var whole = AddPart(blob, Role.Jelly, BoxCentre(piece.Cells), extent, colour, depth, (int)piece.Colour, top, merged);
                        // The base band is as tall as one cell's, so it is a smaller share of a taller picture.
                        float cellPixels = merged.rect.width / spanX, baseShare = Feel.BaseShare;
                        float basePixels = cellPixels * baseShare / Mathf.Max(.01f, 1 - baseShare);
                        SetBaseShare(whole, basePixels / Mathf.Max(1, merged.rect.height));
                        // Sorted by its front row, not its middle: an L's short leg is in front of the jelly sitting on it,
                        // and that jelly's front face hanging over the leg must be drawn under the L, not over it.
                        var front = piece.Cells[0];
                        foreach (var cell in piece.Cells) if (cell.y < front.y || cell.y == front.y && cell.x < front.x) front = cell;
                        var frontAt = Position(front); whole.SortY = frontAt.y; whole.SortX = frontAt.x;
                        _mergedFront[piece.Id] = front;
                        continue;
                    }
                    // Each cell reaches the edge of the cell on any side the piece goes on, so the
                    // blocks of one piece sit flush and read as one jelly.
                    foreach (var cell in piece.Cells)
                    {
                        float half = width * .5f, edge = _cell * .5f;
                        float left = Contains(piece.Cells, cell + Vector2Int.left) ? edge : half, right = Contains(piece.Cells, cell + Vector2Int.right) ? edge : half;
                        float down = Contains(piece.Cells, cell + Vector2Int.down) ? edge : half, up = Contains(piece.Cells, cell + Vector2Int.up) ? edge : half;
                        var item = AddPart(blob, Role.Jelly, Position(cell) + new Vector2(right - left, up - down) * .5f, new Vector2(left + right, down + up),
                            colour, depth, (int)piece.Colour, top);
                        // Sorted by its cell, not by where it reaches: cells of one row stay in one row.
                        var at = Position(cell); item.SortY = at.y; item.SortX = at.x;
                    }
                    continue;
                }
                // Bridges first, so the rounded cells sit on top and the seams disappear.
                foreach (var cell in piece.Cells)
                    foreach (var direction in _bridgeSides)
                        if (Contains(piece.Cells, cell + direction))
                        {
                            var size = direction.x != 0 ? new Vector2(_cell * Look.SpriteBridgeLength, width * Look.SpriteBridgeWidth)
                                                        : new Vector2(width * Look.SpriteBridgeWidth, _cell * Look.SpriteBridgeLength);
                            AddPart(blob, Role.Bridge, Position(cell + (Vector2)direction * .5f), size, colour, depth, (int)piece.Colour, top);
                        }
                foreach (var cell in piece.Cells)
                    AddPart(blob, Role.Jelly, Position(cell), Vector2.one * width, colour, depth, (int)piece.Colour, top);
            }
            AddHeldOutline();
            // A frozen jelly is drawn towards the ice colour until it thaws.
            foreach (var piece in snapshot.Pieces)
            {
                if (snapshot.JellyFrozen(piece.Id) <= 0 || !_blobs.TryGetValue(piece.Id, out var frozenBlob)) continue;
                foreach (var part in frozenBlob.Parts)
                {
                    part.Colour = Color.Lerp(part.Colour, Look.FrozenTint, Look.FrozenJellyAmount);
                    part.Item.BodyImage.color = part.Colour;
                    part.Item.SideImage.color = Color.Lerp(part.Item.SideImage.color, Look.FrozenTint * new Color(.75f, .75f, .75f, 1), Look.FrozenJellyAmount);
                }
                // A picture of ice for the whole shape covers it in one piece, sized like the jelly's own picture,
                // and is sorted with it: the jellies in the rows in front still cover the ice hanging under it.
                // A shape with no picture of its own is covered by the pieces there are: an L by a bar and a single.
                if (!Look.IceTiles(piece.Cells, _iceTiles)) continue;
                float lowest = float.MaxValue;
                foreach (var cell in piece.Cells) lowest = Mathf.Min(lowest, Position(cell).y);
                foreach (var (ice, tileCells) in _iceTiles)
                {
                    Span(tileCells, out float iceX, out float iceY);
                    float iceInset = (_cell - width) * .5f;
                    var iceSize = new Vector2(iceX * _cell - iceInset * 2, iceY * _cell - iceInset * 2);
                    var iceItem = AddPart(frozenBlob, Role.Marker, BoxCentre(tileCells) + Look.JellySpriteOffset * _cell, iceSize, Look.FrozenIceColour, 0, (int)piece.Colour, false, ice);
                    iceItem.BodyImage.color = Look.FrozenIceColour;
                    frozenBlob.Parts[^1].Colour = Look.FrozenIceColour;
                    iceItem.Body.anchorMin = new Vector2(0, 1 - ice.rect.height / Mathf.Max(1, ice.rect.width) * iceSize.x / Mathf.Max(1, iceSize.y));
                    // Over its own jelly, which sorts at the same place.
                    iceItem.SortY = lowest; iceItem.SortX = float.MaxValue;
                }
            }
            // An arrow block carries an arrow along the one way it moves, riding with it.
            foreach (var piece in snapshot.Pieces)
            {
                int ways = snapshot.MoveWays(piece.Id);
                if (ways == PuzzleSnapshotRVO.AnyWay || ways == 0 || !_blobs.TryGetValue(piece.Id, out var arrowBlob)) continue;
                bool across = ways == PuzzleSnapshotRVO.Horizontal;
                // A painted arrow in the jelly's colour, laid along its longest run of cells the way it moves, so on an L
                // or a T it stays on the jelly instead of hanging over the empty corner.
                ArrowLine(piece.Cells, across, out var arrowAt, out int arrowLength);
                var painted = Look.Arrow(piece.Colour, across ? JellyAxis.Horizontal : JellyAxis.Vertical, arrowLength);
                var picture = painted != null ? painted : across ? Pick(Look.ArrowHorizontalSprite, _sprites.ArrowHorizontal) : Pick(Look.ArrowVerticalSprite, _sprites.ArrowVertical);
                var arrowColour = painted != null ? Color.white : Look.ArrowColour;
                // ArrowSize is the picture's longer side; the other follows the picture's own shape.
                float arrowSide = Look.ArrowSize * _cell;
                var arrowSize = Vector2.one * arrowSide;
                if (painted != null) arrowSize = painted.rect.size * (_cell / Look.ArrowPixelsPerCell);
                else if (picture != null && picture.rect.width > 0 && picture.rect.height > 0)
                {
                    float aspect = picture.rect.width / picture.rect.height;
                    arrowSize = aspect >= 1 ? new Vector2(arrowSide, arrowSide / aspect) : new Vector2(arrowSide * aspect, arrowSide);
                }
                var arrow = AddPart(arrowBlob, Role.Marker, Position(arrowAt) + Look.JellySpriteOffset * _cell, arrowSize,
                    arrowColour, 0, (int)piece.Colour, held == piece.Id, picture);
                arrow.BodyImage.color = arrowColour;
                arrowBlob.Parts[^1].Colour = arrowColour;
                arrow.RidesTop = true;
                // Drawn after every cell of its own jelly.
                float lowest = float.MaxValue;
                foreach (var cell in piece.Cells) lowest = Mathf.Min(lowest, Position(cell).y);
                arrow.SortY = lowest - _cell * .49f;
            }
            // Only what touches a moving piece feels it, and only as much as NeighbourWobble allows.
            if (Feel.NeighbourWobble > 0) WobbleNeighbours(snapshot);
            // Delivered jellies start sliding into their door from where their blob is on screen, so
            // this runs before the blobs of pieces that are gone are dropped.
            StartDelivery(previous, snapshot, route, width, depth);
            BurstVanished(previous, snapshot);
            _gone.Clear();
            foreach (var pair in _blobs) if (!pair.Value.Seen) _gone.Add(pair.Key);
            foreach (var id in _gone) _blobs.Remove(id);
            StackInColumns(snapshot);
            SortBlocks();
            UpdateFollow();
            if (!Feel.AnimateJellies) ResetFeel();
            _dirty = true;
            Apply();
        }
        private void RefreshHint(int id, Blob blob)
        {
            // The held jelly is lifted, and so is any jelly it would fuse with if released now.
            blob.FuseReady = Contains(_snapshot.FuseReady, id);
            blob.LiftTarget = _held == id ? Feel.GrabLift : blob.FuseReady ? Feel.FuseReadyLift : 0;
        }
        private void WobbleNeighbours(PuzzleSnapshotRVO snapshot)
        {
            foreach (var piece in snapshot.Pieces)
            {
                if (_moved.Contains(piece.Id)) continue;
                foreach (var movedId in _moved)
                {
                    var moved = PieceById(snapshot, movedId);
                    if (moved == null || !Touch(moved.Cells, piece.Cells)) continue;
                    Kick(_blobs[piece.Id], Feel.LandSquash * Feel.NeighbourWobble, Feel.NeighbourWobble);
                    break;
                }
            }
        }
        private static bool Touch(IReadOnlyList<Vector2Int> a, IReadOnlyList<Vector2Int> b)
        {
            for (int i = 0; i < a.Count; i++) for (int j = 0; j < b.Count; j++)
                if (Mathf.Abs(a[i].x - b[j].x) + Mathf.Abs(a[i].y - b[j].y) == 1) return true;
            return false;
        }
        /// <summary>
        /// The longest unbroken run of the cells along a row (or a column), and its middle: where an arrow lies
        /// on the jelly. Of runs as long, the one nearest the jelly's middle.
        /// </summary>
        private static void ArrowLine(IReadOnlyList<Vector2Int> cells, bool across, out Vector2 middle, out int length)
        {
            var centre = Vector2.zero;
            foreach (var c in cells) centre += c;
            centre /= cells.Count;
            middle = centre; length = 0;
            float nearest = float.MaxValue;
            foreach (var start in cells)
            {
                var step = across ? Vector2Int.right : Vector2Int.up;
                // Only from the first cell of a run.
                if (Contains(cells, start - step)) continue;
                int run = 1;
                while (Contains(cells, start + step * run)) run++;
                var runMiddle = (Vector2)start + (Vector2)step * ((run - 1) * .5f);
                float distance = (runMiddle - centre).sqrMagnitude;
                if (run > length || run == length && distance < nearest) { length = run; middle = runMiddle; nearest = distance; }
            }
        }
        private static void Span(IReadOnlyList<Vector2Int> cells, out float x, out float y)
        {
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var c in cells) { minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x); minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y); }
            x = maxX - minX + 1; y = maxY - minY + 1;
        }
        /// <summary>The middle of the box round these cells, on the board.</summary>
        private Vector2 BoxCentre(IReadOnlyList<Vector2Int> cells)
        {
            Vector2Int min = cells[0], max = cells[0];
            for (int i = 1; i < cells.Count; i++) { min = Vector2Int.Min(min, cells[i]); max = Vector2Int.Max(max, cells[i]); }
            return Position((Vector2)(min + max) * .5f);
        }
        /// <summary>
        /// The held jelly's outline: a copy of each of its parts, a little bigger and painted flat in the
        /// outline colour, drawn before all of the jelly's own parts - so only the rim around the whole
        /// shape shows, never a line between two of its cells.
        /// </summary>
        private void AddHeldOutline()
        {
            if (!Look.HeldOutline || Look.HeldOutlineMaterial == null || Look.HeldOutlineWidth <= 0 || _held < 0 || !_blobs.TryGetValue(_held, out var blob)) return;
            float grow = Look.HeldOutlineWidth * _cell * 2;
            int count = blob.Parts.Count;
            for (int i = 0; i < count; i++)
            {
                var source = blob.Parts[i].Item;
                if (!source.Jelly) continue;
                var item = Take(_jellyLayer);
                item.Jelly = true; item.Depth = source.Depth; item.OnTop = true; item.Under = true; item.Front = false;
                item.SortY = source.SortY; item.SortX = source.SortX;
                item.Root.anchorMin = item.Root.anchorMax = new Vector2(.5f, .5f);
                Silhouette(item.BodyImage, source.BodyImage);
                item.Body.anchorMin = source.Body.anchorMin; item.Body.anchorMax = source.Body.anchorMax;
                item.SideImage.enabled = source.SideImage.enabled;
                if (item.SideImage.enabled) Silhouette(item.SideImage, source.SideImage);
                // Cut, pivoted and rigged like its jelly, so it bends, squashes and lifts with it every frame
                // instead of staying flat on the floor while the jelly moves.
                Follow(item.BodyImage, source.BodyImage); Follow(item.SideImage, source.SideImage);
                item.Body.pivot = source.Body.pivot;
                item.Side.anchorMin = source.Side.anchorMin; item.Side.anchorMax = source.Side.anchorMax;
                item.BaseShare = source.BaseShare;
                Follow(item.Rig, source.Rig); Follow(item.BaseRig, source.BaseRig);
                item.ShadowImage.enabled = false;
                item.HighlightImage.enabled = false;
                Part part;
                if (_freeParts.Count > 0) { part = _freeParts[^1]; _freeParts.RemoveAt(_freeParts.Count - 1); }
                else part = new Part();
                part.Item = item; part.Relative = blob.Parts[i].Relative; part.Size = blob.Parts[i].Size + Vector2.one * grow; part.Colour = Look.HeldOutlineColour;
                blob.Parts.Add(part);
            }
        }
        private static void Follow(Image image, Image source)
        {
            image.fillMethod = source.fillMethod; image.fillOrigin = source.fillOrigin; image.fillAmount = source.fillAmount;
        }
        private static void Follow(JellyRigMesh rig, JellyRigMesh source)
        {
            rig.enabled = source.enabled;
            rig.Columns = source.Columns; rig.Rows = source.Rows; rig.Curve = source.Curve; rig.Bulge = source.Bulge; rig.TopDip = source.TopDip;
            rig.Mirror = source.Mirror; rig.Bend = source.Bend; rig.Squash = source.Squash; rig.Shift = source.Shift;
        }
        private void Silhouette(Image image, Image source)
        {
            image.sprite = source.sprite; image.type = source.type; image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            image.preserveAspect = source.preserveAspect;
            image.material = Look.HeldOutlineMaterial;
            image.color = Look.HeldOutlineColour;
        }
        /// <summary>
        /// Jellies that are gone without going through a door or fusing into another - a rocket or a UFO took them -
        /// burst apart where they were drawn.
        /// </summary>
        private void BurstVanished(PuzzleSnapshotRVO previous, PuzzleSnapshotRVO snapshot)
        {
            if (previous == null) return;
            foreach (var old in previous.Pieces)
            {
                if (PieceById(snapshot, old.Id) != null || _delivered.Contains(old.Id)) continue;
                // Fused into another: its cells are now part of a jelly of its colour.
                bool fused = false;
                foreach (var piece in snapshot.Pieces)
                    if (piece.Colour == old.Colour && Contains(piece.Cells, old.Cells[0])) { fused = true; break; }
                if (fused) continue;
                var drawn = _blobs.TryGetValue(old.Id, out var blob) ? blob.Centre + blob.Offset - Position(CentreOf(old.Cells)) : Vector2.zero;
                var colour = Tint((int)old.Colour);
                BurstVanish(old.Cells, colour, drawn);
                if (Feel.VanishRing) SpreadRing(old.Cells, colour);
            }
        }
        private Item AddPart(Blob blob, Role role, Vector2 position, Vector2 size, Color colour, float depth, int colourId, bool top, Sprite sprite = null)
        {
            var item = Place(Take(_jellyLayer), role, position, size, colour, depth, 0, colourId, sprite);
            item.OnTop = top;
            Part part;
            if (_freeParts.Count > 0) { part = _freeParts[^1]; _freeParts.RemoveAt(_freeParts.Count - 1); }
            else part = new Part();
            part.Item = item; part.Relative = position - blob.Centre; part.Size = size; part.Colour = item.BodyImage.color;
            blob.Parts.Add(part);
            return item;
        }
        /// <summary>A sudden spread: the blob widens by about this much and springs back through a wobble.</summary>
        /// <summary>A pop or squash of the whole jelly; its top is pressed down onto its base by Top Squash times <paramref name="top"/>.</summary>
        private void Kick(Blob blob, float amount, float top = 1)
        {
            blob.ScaleVelocity += Vector2.one * amount * Mathf.Sqrt(Feel.JellyStiffness);
            if (Feel.SplitTopAndBase) blob.TopSquashVelocity -= top * Feel.TopSquash * Mathf.Sqrt(Feel.TopStiffness);
        }
        /// <summary>
        /// A jelly drawn as one picture is sorted by its front row, but in each column it shares with another jelly the
        /// lower of the two must still be drawn over the upper, whose face hangs down onto it. A jelly under the long arm
        /// of an L, in front of the L's arm but behind its leg, is moved forward until it is drawn after the L.
        /// </summary>
        private void StackInColumns(PuzzleSnapshotRVO snapshot)
        {
            _stackShift.Clear();
            if (_mergedFront.Count == 0) return;
            float step = _cell * .01f;
            // Where a cell of the piece sits in the drawing order, shift included: one place for a whole picture.
            float At(JellyPieceVO piece, Vector2Int cell)
            {
                float y = Position(_mergedFront.TryGetValue(piece.Id, out var front) ? front : cell).y;
                return _stackShift.TryGetValue(piece.Id, out var shift) ? y + shift : y;
            }
            for (int pass = 0; pass < 4; pass++)
            {
                bool changed = false;
                foreach (var upper in snapshot.Pieces)
                foreach (var lower in snapshot.Pieces)
                {
                    if (upper.Id == lower.Id || !_mergedFront.ContainsKey(upper.Id) && !_mergedFront.ContainsKey(lower.Id)) continue;
                    foreach (var cell in upper.Cells)
                    {
                        var below = cell + Vector2Int.down;
                        if (!Contains(lower.Cells, below)) continue;
                        float need = At(upper, cell) - step;
                        float now = At(lower, below);
                        if (now <= need) continue;
                        _stackShift.TryGetValue(lower.Id, out var shift);
                        _stackShift[lower.Id] = shift - (now - need);
                        changed = true;
                    }
                }
                if (!changed) break;
            }
            foreach (var pair in _stackShift)
                if (_blobs.TryGetValue(pair.Key, out var blob))
                    foreach (var part in blob.Parts) part.Item.SortY += pair.Value;
        }
        private void SortBlocks()
        {
            // Farther blocks first, so a nearer block's top covers the side of the one behind it; the held piece last.
            _sortBuffer.Clear();
            foreach (var item in _boardItems) if (item.Root.parent == _jellyLayer) _sortBuffer.Add(item);
            foreach (var blob in _blobs.Values) foreach (var part in blob.Parts) _sortBuffer.Add(part.Item);
            _sortBuffer.Sort(_drawOrder);
            foreach (var block in _sortBuffer) block.Root.SetAsLastSibling();
        }

        // ---------- motion ----------

        private void Update()
        {
            if (_snapshot == null || _config == null) return;
            if (_focus != Focus.None) UpdateFocus();
            UpdateFuseGlow(Time.unscaledDeltaTime);
            UpdateGates(Time.unscaledDeltaTime);
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1 / 30f);
            _time += dt;
            // Two half steps keep a stiff spring stable at low frame rates. Sliding and lifting always
            // run, so a move, an undo and a lift stay smooth; AnimateJellies only adds the jelly feel.
            bool moving = false;
            for (int i = 0; i < 2; i++) moving |= Step(dt * .5f);
            if (!Feel.AnimateJellies) ResetFeel();
            // A still board is left alone: nothing is written to the UI, so its canvas is not rebuilt.
            bool impact = StepImpact(Time.unscaledDeltaTime);
            bool animated = Feel.AnimateJellies && Feel.BreathAmount > 0 || AnyFuseHint() || DeliveryRunning || impact;
            if (moving || animated || _dirty) { Apply(); _dirty = moving; }
        }
        private bool AnyFuseHint()
        {
            foreach (var blob in _blobs.Values) if (blob.FuseReady) return true;
            return false;
        }
        /// <summary>Advances every blob by <paramref name="h"/> seconds; returns whether any of them is still moving.</summary>
        private bool Step(float h)
        {
            bool moving = false;
            foreach (var blob in _blobs.Values)
            {
                // Slide along the route through the centre of every cell it passed, then to the cell
                // (or the finger's lead). Straight lines at a set speed: it never overshoots into a
                // neighbour, never cuts a corner, and never leaves the board.
                var before = blob.Offset;
                float travel = Feel.SlideSpeed * _cell * h;
                while (travel > 0)
                {
                    var goal = blob.Route.Count > 0 ? blob.Route.Peek() : blob.Follow;
                    float distance = Vector2.Distance(blob.Offset, goal);
                    blob.Offset = Vector2.MoveTowards(blob.Offset, goal, travel);
                    travel -= distance;
                    if (blob.Route.Count == 0 || distance > 0 && travel < 0) break;
                    blob.Route.Dequeue();
                }
                blob.OffsetVelocity = (blob.Offset - before) / h;
                // Moving, the jelly drags its back behind it: it stretches backwards from its front, as
                // if it would rather stay where it was. Pulled by the finger past where it can follow,
                // it stretches forwards from its back toward the finger instead.
                var velocity = blob.OffsetVelocity / Mathf.Max(1, _cell);
                float speed = velocity.magnitude;
                var lean = Vector2.zero;
                if (speed > .01f) lean -= velocity / speed * Mathf.Min(speed * Feel.MoveStretch, Feel.MaxStretch);
                lean += Vector2.ClampMagnitude(blob.Pull * Feel.PullStretch, Feel.MaxStretch);
                var target = new Vector2(1 + Mathf.Abs(lean.x) - .5f * Mathf.Abs(lean.y), 1 + Mathf.Abs(lean.y) - .5f * Mathf.Abs(lean.x));
                blob.ScaleVelocity += (Feel.JellyStiffness * (target - blob.Scale) - Feel.JellyDamping * blob.ScaleVelocity) * h;
                blob.Scale += blob.ScaleVelocity * h;
                blob.LeanVelocity += (Feel.JellyStiffness * (lean - blob.Lean) - Feel.JellyDamping * blob.LeanVelocity) * h;
                blob.Lean += blob.LeanVelocity * h;
                // The top trails a moving jelly a little and its squash springs back on its own, damped so
                // it settles rather than dancing.
                var lag = Vector2.ClampMagnitude(-velocity * Feel.TopLag, Feel.TopMaxOffset);
                blob.TopOffsetVelocity += (Feel.TopStiffness * (lag - blob.TopOffset) - Feel.TopDamping * blob.TopOffsetVelocity) * h;
                blob.TopOffset += blob.TopOffsetVelocity * h;
                blob.TopSquashVelocity += (-Feel.TopStiffness * blob.TopSquash - Feel.TopDamping * blob.TopSquashVelocity) * h;
                blob.TopSquash += blob.TopSquashVelocity * h;
                // Ease up and down without overshooting, so a lift never bounces through the floor.
                blob.Lift = Mathf.SmoothDamp(blob.Lift, blob.LiftTarget, ref blob.LiftVelocity, Mathf.Max(.001f, Feel.LiftTime), Mathf.Infinity, h);
                moving |= !AtRest(blob);
            }
            StepDelivery(h);
            return moving;
        }
        private static bool AtRest(Blob blob) =>
            blob.Route.Count == 0 && (blob.Offset - blob.Follow).sqrMagnitude < REST && blob.OffsetVelocity.sqrMagnitude < REST
            && (blob.Scale - Vector2.one).sqrMagnitude < REST && blob.ScaleVelocity.sqrMagnitude < REST
            && blob.Lean.sqrMagnitude < REST && blob.LeanVelocity.sqrMagnitude < REST && Mathf.Abs(blob.Lift - blob.LiftTarget) < REST
            && blob.TopOffset.sqrMagnitude < REST && blob.TopOffsetVelocity.sqrMagnitude < REST && Mathf.Abs(blob.TopSquash) < REST && Mathf.Abs(blob.TopSquashVelocity) < REST;
        /// <summary>
        /// With AnimateJellies off: no wobble or stretch. Position and lift are left to
        /// slide on, so moves and undos stay smooth.
        /// </summary>
        private void ResetFeel()
        {
            foreach (var blob in _blobs.Values)
            {
                blob.ScaleVelocity = blob.Lean = blob.LeanVelocity = blob.TopOffset = blob.TopOffsetVelocity = Vector2.zero;
                blob.Scale = Vector2.one;
                blob.TopSquash = blob.TopSquashVelocity = 0;
            }
        }

        /// <summary>
        /// Where the piece's cells sat inside <paramref name="whole"/>, as a shift from where they are
        /// now; zero when they did not move. The smallest shift that fits wins.
        /// </summary>
        private static Vector2Int ShiftWithin(JellyPieceVO piece, JellyPieceVO whole)
        {
            var best = Vector2Int.zero;
            foreach (var cell in whole.Cells)
            {
                var shift = cell - piece.Cells[0];
                if (shift == Vector2Int.zero) continue;
                bool inside = true;
                for (int i = 0; i < piece.Cells.Count && inside; i++) inside = Contains(whole.Cells, piece.Cells[i] + shift);
                if (inside && (best == Vector2Int.zero || shift.sqrMagnitude < best.sqrMagnitude)) best = shift;
            }
            return best;
        }
        private void Apply()
        {
            // While a rocket waits for its door, the jellies fade back.
            float fade = _focus == Focus.Doors ? _focusDim : 1;
            foreach (var pair in _blobs)
            {
                var blob = pair.Value;
                // A jelly that has just fused flashes white for a moment.
                float flash = FlashOf(pair.Key);
                float breath = Feel.AnimateJellies ? Mathf.Sin(_time * Feel.BreathSpeed + blob.Phase) * Feel.BreathAmount : 0;
                // A jelly the held one would fuse with on release pulses and glows.
                float hint = 0;
                if (blob.FuseReady)
                {
                    hint = .5f + .5f * Mathf.Sin(_time * Feel.FuseHintSpeed);
                    breath += hint * Feel.FuseHintPulse;
                }
                // Grow, stretch and pop only up to the edge of its own cells, never over a neighbour.
                // Along the way it leans it may stretch past its cell: that side is the cell it left or
                // the free cell it is pulled into. Across, it stays inside its own cells.
                var cap = new Vector2(Mathf.Abs(blob.Lean.x) > .001f ? 1 + Feel.MaxStretch : blob.MaxScale.x,
                                      Mathf.Abs(blob.Lean.y) > .001f ? 1 + Feel.MaxStretch : blob.MaxScale.y);
                var scale = Vector2.Min(blob.Scale * (1 + breath), cap);
                // Spreading flattens it and narrowing makes it taller, so it keeps its volume.
                float height = Mathf.Clamp(1 / Mathf.Max(.01f, scale.x * scale.y), .4f, 1.8f);
                // Keep one end in place: the stretch shows on the side it leans to.
                var shift = new Vector2(Mathf.Sign(blob.Lean.x) * (scale.x - 1) * blob.Span.x, Mathf.Sign(blob.Lean.y) * (scale.y - 1) * blob.Span.y) * (.5f * _cell);
                if (Mathf.Abs(blob.Lean.x) < .001f) shift.x = 0;
                if (Mathf.Abs(blob.Lean.y) < .001f) shift.y = 0;
                // Split, the base takes only Base Motion of the stretch and the top makes up the rest.
                bool split = Feel.SplitTopAndBase && Blocks;
                var baseScale = split ? Vector2.Lerp(Vector2.one, scale, Feel.BaseMotion) : scale;
                var baseShift = split ? shift * Feel.BaseMotion : shift;
                // With the rig, the trail bends the top and the squash presses it into the mesh; without it
                // the top slides and scales as one piece.
                bool rig = split && Feel.UseRig;
                float bend = rig ? Feel.RigBend : 0;
                var topScale = new Vector2(scale.x / baseScale.x * (rig ? 1 : 1 - blob.TopSquash * .5f), scale.y / baseScale.y * (rig ? 1 : 1 + blob.TopSquash));
                var topShift = (shift - baseShift) + blob.TopOffset * _cell * (1 - bend);
                foreach (var part in blob.Parts)
                {
                    var root = part.Item.Root;
                    bool splitPart = split && part.Item.Jelly;
                    // What rides the top is placed with the base, then moved as the top moves, so it never slides off it.
                    bool ridesTop = split && part.Item.RidesTop;
                    bool withBase = splitPart || ridesTop;
                    root.anchoredPosition = blob.Centre + blob.Offset + (withBase ? baseShift : shift) + Vector2.Scale(part.Relative, withBase ? baseScale : scale);
                    root.sizeDelta = Vector2.Scale(part.Size, withBase ? baseScale : scale);
                    SetLift(part.Item, blob.Lift, height);
                    if (ridesTop)
                    {
                        // The top face rises and sinks with the jelly's height, trails with the bend and is pressed by the squash.
                        var ride = topShift + blob.TopOffset * _cell * bend;
                        ride.y += Look.SpriteJellyDepth * (height - 1) * _cell - (rig ? blob.TopSquash * part.Size.y * .5f : 0);
                        part.Item.Body.anchoredPosition += ride;
                        part.Item.Body.localScale = new Vector3(topScale.x, topScale.y * (rig ? 1 - blob.TopSquash : 1), 1);
                    }
                    if (splitPart)
                    {
                        part.Item.Body.anchoredPosition += topShift;
                        part.Item.Body.localScale = new Vector3(topScale.x, topScale.y, 1);
                        if (rig && part.Item.Rig.enabled)
                        {
                            // The rig works in the Body's own units, before its scale.
                            part.Item.Rig.Bend = new Vector2(blob.TopOffset.x / Mathf.Max(.01f, topScale.x), blob.TopOffset.y * Feel.RigVerticalStretch / Mathf.Max(.01f, topScale.y)) * _cell * bend;
                            part.Item.Rig.Squash = -blob.TopSquash;
                            part.Item.Rig.Apply();
                            if (part.Item.BaseRig.enabled)
                            {
                                // The base band rides with the top's bottom edge: as far sideways as that edge
                                // bends and as far up as it is pressed, in the band's own units (it is not scaled).
                                float topHeight = part.Item.Body.rect.height * (1 - part.Item.BaseShare);
                                part.Item.BaseRig.Shift = new Vector2(part.Item.Rig.Bend.x * topScale.x,
                                    ((part.Item.Rig.Mirror ? -1 : 1) * part.Item.Rig.Bend.y + part.Item.Rig.Squash * topHeight * .5f) * topScale.y) + topShift;
                                part.Item.BaseRig.Apply();
                            }
                        }
                    }
                    part.Item.BodyImage.color = Color.Lerp(part.Colour, Color.white, Mathf.Max(hint * Feel.FuseHintGlow, flash)) * new Color(1, 1, 1, part.Colour.a * fade);
                    if (part.Item.SideImage.enabled) { var side = part.Item.SideImage.color; side.a = fade; part.Item.SideImage.color = side; }
                    if (part.Item.ShadowImage.enabled) part.Item.ShadowImage.color = new Color(0, 0, 0, Look.SpriteShadow * fade);
                }
            }
            ApplyDelivery();
        }

        public void RefreshSettings()
        {
            if (_snapshot == null) return;
            Present(_snapshot, _held, true);
        }
        private void OnRectTransformDimensionsChange() { if (_snapshot != null && isActiveAndEnabled) RefreshSettings(); }
        private void ClearBlobs()
        {
            foreach (var blob in _blobs.Values) foreach (var part in blob.Parts) { Give(part.Item); _freeParts.Add(part); }
            _blobs.Clear();
            ClearDelivery();
        }

        public void Clear()
        {
            ClearImpact();
            ClearDust();
            ReleaseGates(); _gateOpen.Clear();
            ClearBlobs();
            _following = false;
            foreach (var item in _boardItems) Give(item);
            _boardItems.Clear();
            ClearWalls();
            _level = null; _snapshot = null; _held = -1;
        }
        private void OnDestroy() => _sprites?.Destroy();
    }
}
