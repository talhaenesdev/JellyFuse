using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.ValueObjects;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.LocalizationModule.Shared.Constants;
using TMPro;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects
{
    /// <summary>
    /// How the board looks: the angle it is seen from, the jelly and floor art, the sizes and shading of
    /// blocks, and the colours. The walls and doors have their own asset (WallSkin), the jelly colours
    /// theirs (Palette). Every sprite slot is optional; an empty one uses the generated look.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_JellyBoardLook", menuName = "Jelly Fuse/Board Look")]
    public sealed class CD_JellyBoardLook : ScriptableObject
    {
        [Section("View")]
        [Tooltip("Vertical squash of the flat board: 1 looks straight down, lower looks more from the side.")]
        [Range(.5f, 1)] public float SpriteTilt = .78f;
        [Tooltip("Visible side height of blocks, in cells.")]
        [Range(0, .5f)] public float SpriteJellyDepth = .24f;
        [Range(0, .6f)] public float SpriteWallDepth = .32f;
        [Range(0, .3f)] public float SpriteBoardDepth = .14f;

        [Section("Held jelly outline")]
        [Tooltip("On: the jelly the player is holding gets an outline around its whole shape.")]
        public bool HeldOutline = true;
        public Color HeldOutlineColour = Color.white;
        [Tooltip("How thick the outline is, in cells.")]
        [Range(0, .3f)] public float HeldOutlineWidth = .06f;
        [Tooltip("Paints the jelly's shape in the outline colour (shader Jelly Fuse/UI Solid Fill). Without it the outline is left out.")]
        public Material HeldOutlineMaterial;

        [Section("Jelly art")]
        [Tooltip("A sprite with a 9-slice border keeps round corners at any size; one without is stretched.")]
        public Sprite JellySprite;
        [Tooltip("On: the jelly sprite is a finished block - top, side and shine painted in. No side, shine or bridge is added, and the cells of one piece join edge to edge.")]
        public bool JellySpriteIsBlock;
        [Tooltip("Whole-jelly pictures: a piece whose colour and shape match one is drawn with it in one piece. Only used when Jelly Sprite Is Block is on.")]
        public List<JellyMergedSpriteCVO> MergedSprites = new();
        [Tooltip("Moves a jelly's picture off the centre of its cell, in cells (x right, y up). Use it to seat the art on the grid; the jelly's cell does not change.")]
        public Vector2 JellySpriteOffset;
        [Tooltip("Optional. Drawn darker under the top face; empty uses the jelly sprite.")]
        public Sprite JellySideSprite;
        public Sprite JellyHighlightSprite;
        public Sprite JellyShadowSprite;
        [Tooltip("Jelly width as a share of a cell.")]
        [Range(.55f, .98f)] public float JellyWidth = .88f;
        [Range(.3f, 1)] public float SpriteBridgeLength = .7f;
        [Range(.3f, 1)] public float SpriteBridgeWidth = .8f;

        [Section("Board art")]
        [Tooltip("Floor tile. A painted tile is drawn as it is; with TileSpriteAlt the two alternate as a checkerboard.")]
        public Sprite TileSprite;
        public Sprite TileSpriteAlt;
        [Tooltip("Room kept above and below the floor for the wall frame, in cells; a door reaching further takes its own room on its side only.")]
        [Range(0, 1.5f)] public float MapFrameMargin = .62f;
        [Tooltip("Room kept left of the floor for the wall frame, in cells: how far the left wall's picture reaches out.")]
        [Range(0, 1.5f)] public float MapFrameLeft = .5f;
        [Tooltip("Room kept right of the floor for the wall frame, in cells: how far the right wall's picture, its shadow included, reaches out.")]
        [Range(0, 1.5f)] public float MapFrameRight = .5f;
        [Tooltip("Drawn where four floor tiles meet, over the gap between them. Empty draws nothing.")]
        public Sprite GridCornerSprite;
        [Tooltip("Its size, in cells.")]
        [Range(.05f, 1)] public float GridCornerSize = .36f;
        [Tooltip("Shown at the grid corners round a jelly just fused, blinking with Grid Corner Sprite. Empty: no blink.")]
        public Sprite GridCornerLightSprite;
        [Tooltip("Seconds the corners round a fused jelly blink.")]
        [Range(0, 5)] public float FuseGlowSeconds = 1.5f;
        [Tooltip("How many times they light up in that time.")]
        [Range(1, 10)] public int FuseGlowBlinks = 3;
        [Tooltip("On: every grid corner on the board blinks when jellies fuse. Off: only the corners round the fused jelly.")]
        public bool FuseGlowWholeGrid = true;
        [Tooltip("A soft glow laid round each lit corner while it blinks, in this colour; its alpha is how bright it gets. Clear: none.")]
        public Color FuseGlowBloomColour = new Color(.75f, 1f, 1f, .85f);
        [Tooltip("How bright the glow gets, times the colour's alpha. 0: no glow.")]
        [Range(0, 2)] public float FuseGlowBloomStrength = 0;
        [Tooltip("The glow's size, in cells.")]
        [Range(0, 2)] public float FuseGlowBloomSize = .9f;
        [Tooltip("How much bigger a lit corner is drawn than a plain one.")]
        [Range(1, 2)] public float FuseGlowCornerScale = 1.3f;

        [Header("Tutorial")]
        [Tooltip("The hand that shows a tutorial's move.")]
        public Sprite TutorialHandSprite;
        [Tooltip("Where on the hand picture its fingertip is, 0-1 from the bottom-left: that point touches the jelly.")]
        public Vector2 TutorialHandTip = new(.29f, .85f);
        [Tooltip("The hand's size, in cells.")]
        [Range(.5f, 4)] public float TutorialHandSize = 1.6f;
        [Tooltip("How fast the hand leads the jelly, in cells per second.")]
        [Range(.5f, 10)] public float TutorialHandSpeed = 2.5f;
        [Tooltip("How dark the screen goes round the cells the tutorial uses.")]
        [Range(0, 1)] public float TutorialDim = .88f;
        [Tooltip("Light left round each cell the tutorial uses, as a share of a cell.")]
        [Range(0, .5f)] public float TutorialHolePadding = .06f;
        [Tooltip("Extra light above each of those cells, as a share of a cell, where a lifted jelly rises.")]
        [Range(0, 1)] public float TutorialHoleLift = .1f;
        [Tooltip("How far the dark fades in from the edge of the light, as a share of a cell. 0: a hard edge.")]
        [Range(0, 1.5f)] public float TutorialEdgeSoftness = .35f;
        [Tooltip("How strongly the cells the jelly is to be led to are marked in its colour, at the top of their pulse.")]
        [Range(0, 1)] public float TutorialTargetAlpha = .55f;
        [Tooltip("Pulses per second of that mark.")]
        [Range(0, 4)] public float TutorialTargetPulse = 1.2f;
        [Tooltip("Where a tutorial step's text sits on the screen: (0, 0) bottom-left, (1, 1) top-right, (0.5, 0.8) the middle near the top.")]
        public Vector2 TutorialTextAnchor = new(.5f, .8f);
        [Tooltip("Moves the text off that point, in canvas units (x right, y up).")]
        public Vector2 TutorialTextOffset;
        [Tooltip("How wide the text may run, as a share of the screen's width; it wraps past that.")]
        [Range(.2f, 1)] public float TutorialTextWidth = .85f;
        public float TutorialTextSize = 52;
        public Color TutorialTextColour = Color.white;
        [Tooltip("The text's font. Empty: TextMeshPro's default font.")]
        public TMP_FontAsset TutorialTextFont;
        [Tooltip("An outline round the letters, so they read over the board; its alpha is how strong. Clear: none.")]
        public Color TutorialTextOutline = new(0, 0, 0, .8f);
        [Range(0, .5f)] public float TutorialTextOutlineWidth = .2f;
        [Tooltip("The wall round the board, the walls on it and the doors: a sprite for every straight side, corner, wall join and door width.")]
        public CD_JellyWallSkin WallSkin;
        [Tooltip("The generated wall and door block, used wherever WallSkin leaves a slot empty.")]
        public Sprite WallSprite;
        public Sprite BoardSprite;
        public Sprite BackgroundSprite;
        [Tooltip("Sizes as a fraction of one cell.")]
        [Range(.5f, 1)] public float SpriteTileSize = .9f;
        [Range(.5f, 1)] public float SpriteWallSize = .92f;

        [Section("Shading")]
        [Tooltip("Corner radius of 9-sliced sprites, as a share of the block.")]
        [Range(.05f, .5f)] public float SpriteCornerRadius = .3f;
        [Tooltip("How much darker a block's side is than its top.")]
        [Range(0, 1)] public float SpriteSideDarken = .38f;
        [Range(0, 1)] public float SpriteHighlight = .45f;
        [Range(0, 1)] public float SpriteShadow = .35f;
        [Tooltip("Shadow offset from a block's footprint, in cells.")]
        public Vector2 SpriteShadowOffset = new Vector2(.04f, -.05f);

        [Section("Frozen doors and jellies")]
        [Tooltip("The ice colour a frozen jelly is drawn towards.")]
        public Color FrozenTint = new Color(.62f, .86f, 1f, 1f);
        [Tooltip("How far a frozen jelly's colour goes towards the ice colour: 0 keeps its own, 1 is all ice.")]
        [Range(0, 1)] public float FrozenJellyAmount = .6f;
        [Tooltip("The layer of ice laid over each cell of a frozen jelly; its alpha is how much of the jelly shows through.")]
        public Color FrozenJellyOverlay = new Color(.7f, .9f, 1f, .55f);
        [Tooltip("Optional picture for the ice over a frozen jelly's cell; empty lays a rounded block.")]
        public Sprite FrozenJellySprite;
        [Tooltip("Whole-jelly ice: a frozen jelly whose shape matches one is covered by it in one piece, in place of the ice over each cell. Colour is not read.")]
        public List<JellyMergedSpriteCVO> IceSprites = new();
        [Tooltip("The tint of those ice pictures; its alpha is how much of the jelly shows through.")]
        public Color FrozenIceColour = Color.white;
        [Tooltip("The tint of the painted ice over a frozen door; its alpha is how much of the door shows through. Below full, the jelly the door waits for shows on its screen under the ice.")]
        public Color FrozenDoorIceColour = new Color(1, 1, 1, .8f);
        [Tooltip("The layer of ice laid over a frozen door; its alpha is how much of the door shows through.")]
        public Color FrozenDoorOverlay = new Color(.62f, .86f, 1f, .6f);
        [Tooltip("Optional picture for the ice over a frozen door; empty lays a rounded block.")]
        public Sprite FrozenDoorSprite;
        [Tooltip("The number on a frozen door or jelly: how many jellies must still go through doors before it thaws.")]
        public Color FrozenTextColour = Color.white;
        [Tooltip("Size of that number, in cells.")]
        [Range(.2f, 1.2f)] public float FrozenTextSize = .6f;
        [Tooltip("Font of that number. Empty uses TextMeshPro's default font (TMP Settings).")]
        public TMP_FontAsset FrozenTextFont;
        public FontStyles FrozenTextStyle = FontStyles.Bold;

        [Section("Arrow blocks")]
        [Tooltip("Optional picture of the arrow on a jelly that moves only left and right; empty draws a generated one.")]
        public Sprite ArrowHorizontalSprite;
        [Tooltip("Optional picture of the arrow on a jelly that moves only up and down; empty draws a generated one.")]
        public Sprite ArrowVerticalSprite;
        public Color ArrowColour = new Color(1, 1, 1, .9f);
        [Tooltip("Size of the arrow, in cells.")]
        [Range(.2f, 1.2f)] public float ArrowSize = .7f;
        [Tooltip("Painted arrows per colour, way and length. A jelly with one is drawn with it, at its own size and as painted; any other gets the arrow above.")]
        public List<JellyArrowSpriteCVO> ArrowSprites = new();
        [Tooltip("Sprite pixels to a cell for the painted arrows.")]
        [Min(1)] public float ArrowPixelsPerCell = 128;

        [Section("Colours")]
        [Tooltip("The jelly colours. Their tints are edited right here, and the level editor and the game read the same palette.")]
        public CD_JellyPalette Palette;
        public Color BoardColour = new Color(.11f, .16f, .25f);
        public Color TileColour = new Color(.19f, .25f, .35f);
        public Color WallColour = new Color(.37f, .43f, .54f);
        public Color Background = new Color(.063f, .086f, .157f);

        /// <summary>A palette colour's tint, read live so a change shows in Play Mode at once; gray for an unknown one.</summary>
        /// <summary>The whole-jelly picture for these cells in this colour, or null. Cells may sit anywhere on the board.</summary>
        public Sprite Merged(JellyColour colour, IReadOnlyList<Vector2Int> cells)
        {
            if (MergedSprites == null || cells.Count < 2) return null;
            BuildShapeTables();
            return ShapeKey(cells, out var key) && _merged.TryGetValue(((int)colour, key), out var sprite) ? sprite : null;
        }

        // The pictures by colour and shape, built once from the lists: a jelly is drawn at every step of every drag,
        // and the lists hold hundreds of pictures.
        [System.NonSerialized] private Dictionary<(int, ulong), Sprite> _merged;
        [System.NonSerialized] private Dictionary<ulong, Sprite> _ice;
        private void BuildShapeTables()
        {
            if (_merged != null) return;
            _merged = new Dictionary<(int, ulong), Sprite>();
            _ice = new Dictionary<ulong, Sprite>();
            _iceTiles = new Dictionary<ulong, List<(Sprite, Vector2Int[])>>();
            _iceShapes = new List<(Sprite, Vector2Int[])>();
            if (MergedSprites != null)
                foreach (var entry in MergedSprites)
                    if (entry.Sprite != null && ShapeKey(entry.Cells, out var key)) _merged.TryAdd(((int)entry.Colour, key), entry.Sprite);
            if (IceSprites != null)
                foreach (var entry in IceSprites)
                    if (entry.Sprite != null && ShapeKey(entry.Cells, out var key) && _ice.TryAdd(key, entry.Sprite))
                    {
                        var min = entry.Cells[0];
                        foreach (var cell in entry.Cells) min = Vector2Int.Min(min, cell);
                        var cells = new Vector2Int[entry.Cells.Count];
                        for (int i = 0; i < cells.Length; i++) cells[i] = entry.Cells[i] - min;
                        _iceShapes.Add((entry.Sprite, cells));
                    }
            // Bigger pieces are tried first, so a shape is covered by as few as it can be.
            _iceShapes.Sort((a, b) => b.Cells.Length.CompareTo(a.Cells.Length));
        }
        /// <summary>A shape, moved to its bottom-left corner, as one bit a cell of an 8 by 8 square; false for a shape bigger than that.</summary>
        private static bool ShapeKey(IReadOnlyList<Vector2Int> cells, out ulong key)
        {
            key = 0;
            if (cells == null || cells.Count == 0) return false;
            var min = cells[0];
            for (int i = 1; i < cells.Count; i++) min = Vector2Int.Min(min, cells[i]);
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i] - min;
                if (c.x >= 8 || c.y >= 8) return false;
                key |= 1UL << (c.y * 8 + c.x);
            }
            return true;
        }
        private void OnValidate() { _merged = null; _ice = null; }

        /// <summary>The painted arrow for a jelly of this colour, moving this way, this many cells long that way; the longest there is for a longer one. Null when there is none.</summary>
        public Sprite Arrow(JellyColour colour, JellyAxis axis, int length)
        {
            if (ArrowSprites == null) return null;
            Sprite best = null; int bestLength = 0;
            foreach (var entry in ArrowSprites)
            {
                if (entry.Colour != colour || entry.Axis != axis || entry.Sprite == null || entry.Length > length) continue;
                if (entry.Length > bestLength) { best = entry.Sprite; bestLength = entry.Length; }
            }
            return best;
        }
        /// <summary>
        /// The ice pictures that together cover these cells exactly, each with the cells it covers: the whole-shape
        /// picture when there is one, else the fewest pieces of the shapes there are - an L as a two-cell bar and a single.
        /// False when the shapes there are cannot cover it.
        /// </summary>
        public bool IceTiles(IReadOnlyList<Vector2Int> cells, List<(Sprite Sprite, List<Vector2Int> Cells)> into)
        {
            into.Clear();
            if (IceSprites == null || cells == null || cells.Count == 0 || !ShapeKey(cells, out var key)) return false;
            BuildShapeTables();
            if (!_iceTiles.TryGetValue(key, out var tiles))
            {
                tiles = TileIce(key);
                _iceTiles[key] = tiles;
            }
            if (tiles == null) return false;
            var min = cells[0];
            for (int i = 1; i < cells.Count; i++) min = Vector2Int.Min(min, cells[i]);
            foreach (var tile in tiles)
            {
                var placed = new List<Vector2Int>(tile.Cells.Length);
                foreach (var cell in tile.Cells) placed.Add(cell + min);
                into.Add((tile.Sprite, placed));
            }
            return true;
        }
        private Dictionary<ulong, List<(Sprite Sprite, Vector2Int[] Cells)>> _iceTiles;
        private List<(Sprite Sprite, Vector2Int[] Cells)> _iceShapes;
        /// <summary>The fewest ice pieces covering the shape exactly, in its own cells from its bottom-left; null when none do.</summary>
        private List<(Sprite Sprite, Vector2Int[] Cells)> TileIce(ulong key)
        {
            var left = new HashSet<Vector2Int>();
            for (int i = 0; i < 64; i++) if ((key & (1UL << i)) != 0) left.Add(new Vector2Int(i % 8, i / 8));
            for (int most = 1; most <= left.Count; most++)
            {
                var found = new List<(Sprite, Vector2Int[])>();
                if (Cover(left, most, found)) return found;
            }
            return null;
        }
        private bool Cover(HashSet<Vector2Int> left, int most, List<(Sprite Sprite, Vector2Int[] Cells)> found)
        {
            if (left.Count == 0) return true;
            if (most == 0) return false;
            // The bottom-left cell still bare must be covered by some piece: try every piece on it, biggest first.
            var first = new Vector2Int(int.MaxValue, int.MaxValue);
            foreach (var cell in left) if (cell.y < first.y || cell.y == first.y && cell.x < first.x) first = cell;
            foreach (var shape in _iceShapes)
                foreach (var anchor in shape.Cells)
                {
                    var shift = first - anchor;
                    bool fits = true;
                    foreach (var cell in shape.Cells) if (!left.Contains(cell + shift)) { fits = false; break; }
                    if (!fits) continue;
                    var placed = new Vector2Int[shape.Cells.Length];
                    for (int i = 0; i < placed.Length; i++) { placed[i] = shape.Cells[i] + shift; left.Remove(placed[i]); }
                    found.Add((shape.Sprite, placed));
                    if (Cover(left, most - 1, found)) return true;
                    found.RemoveAt(found.Count - 1);
                    foreach (var cell in placed) left.Add(cell);
                }
            return false;
        }

        public Color Colour(int index) => Palette != null && Palette.Find((JellyColour)index) is { } entry ? entry.Tint : Color.gray;
    }
}
