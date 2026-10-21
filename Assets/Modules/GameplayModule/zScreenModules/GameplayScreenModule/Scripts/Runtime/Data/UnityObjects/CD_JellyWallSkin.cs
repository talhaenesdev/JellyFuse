using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.LocalizationModule.Shared.Constants;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects
{
    /// <summary>
    /// The art of the walls and doors (Sprites/Walls). Every slot is optional: a painted sprite is
    /// drawn as it is, one cell per piece; an empty slot falls back to the next one named in its tooltip, and in the
    /// end to CD_JellyPresentation's WallSprite, tinted and raised like a block.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_JellyWallSkin", menuName = "Jelly Fuse/Wall Skin")]
    public sealed class CD_JellyWallSkin : ScriptableObject
    {
        [Section("Frame - the wall round the board")]
        [Tooltip("The straight wall along the top of the board.")] public Sprite Top;
        [Tooltip("The straight wall along the bottom of the board.")] public Sprite Bottom;
        [Tooltip("The straight wall down the left of the board.")] public Sprite Left;
        [Tooltip("The straight wall down the right of the board.")] public Sprite Right;
        public Sprite CornerTopLeft;
        public Sprite CornerTopRight;
        public Sprite CornerBottomLeft;
        public Sprite CornerBottomRight;
        [Tooltip("Where the floor turns inward: the piece in the outside cell up and to the left of the floor's corner. Used only with Pixels Per Cell.")]
        public Sprite ConcaveTopLeft;
        public Sprite ConcaveTopRight;
        public Sprite ConcaveBottomLeft;
        public Sprite ConcaveBottomRight;

        [Section("Walls on the board - picked by which neighbours are walls too")]
        [Tooltip("Used for every piece below that is left empty.")] public Sprite Inner;
        [Tooltip("A wall with no wall beside it.")] public Sprite Single;
        [Tooltip("The end of a line of walls that carries on upward.")] public Sprite EndUp;
        [Tooltip("The end of a line of walls that carries on downward.")] public Sprite EndDown;
        [Tooltip("The end of a line of walls that carries on to the left.")] public Sprite EndLeft;
        [Tooltip("The end of a line of walls that carries on to the right.")] public Sprite EndRight;
        [Tooltip("The middle of a line of walls from left to right.")] public Sprite Horizontal;
        [Tooltip("The middle of a line of walls from top to bottom.")] public Sprite Vertical;
        [Tooltip("A corner joining the wall above and the wall to the right.")] public Sprite CornerUpRight;
        [Tooltip("A corner joining the wall above and the wall to the left.")] public Sprite CornerUpLeft;
        [Tooltip("A corner joining the wall below and the wall to the right.")] public Sprite CornerDownRight;
        [Tooltip("A corner joining the wall below and the wall to the left.")] public Sprite CornerDownLeft;
        [Tooltip("Walls to the left, right and above.")] public Sprite TeeUp;
        [Tooltip("Walls to the left, right and below.")] public Sprite TeeDown;
        [Tooltip("Walls above, below and to the left.")] public Sprite TeeLeft;
        [Tooltip("Walls above, below and to the right.")] public Sprite TeeRight;
        [Tooltip("Walls on all four sides.")] public Sprite Cross;

        [Section("Doors - element 0 is the 1x door, 3 the 4x one")]
        [Tooltip("How far the door is pulled in from the board's edge cell onto the wall, in cells. The wall runs on under it.")]
        public float DoorInset = .42f;
        [Tooltip("The door's size across the wall, in cells. Make it cover the wall under it.")]
        [Range(.2f, 1.5f)] public float DoorThickness = .62f;
        [Tooltip("How far the door reaches past its cells at each end, in cells, so no wall shows at its ends.")]
        [Range(0, .5f)] public float DoorOverhang = .1f;
        [Tooltip("Doors in the top wall. An empty slot uses the generated door, tinted with the jelly it waits for.")]
        public Sprite[] DoorTop = new Sprite[4];
        public Sprite[] DoorBottom = new Sprite[4];
        public Sprite[] DoorLeft = new Sprite[4];
        public Sprite[] DoorRight = new Sprite[4];
        [Tooltip("Ice over a frozen door in the top wall, per width, drawn at its own size over the door's middle. An empty slot lays the board look's Frozen Door overlay instead.")]
        public Sprite[] DoorTopIce = new Sprite[4];
        [Tooltip("The screen and grid painted on each top door, per width: the jelly it waits for is drawn on the grid, a square a cell, in the jelly's own picture. Filled in by measuring the pictures.")]
        public JellyDoorScreenCVO[] DoorTopScreen = new JellyDoorScreenCVO[4];
        public JellyDoorScreenCVO[] DoorBottomScreen = new JellyDoorScreenCVO[4];
        public JellyDoorScreenCVO[] DoorLeftScreen = new JellyDoorScreenCVO[4];
        public JellyDoorScreenCVO[] DoorRightScreen = new JellyDoorScreenCVO[4];
        [Tooltip("How much of a grid square a jelly cell on a door's screen fills.")]
        [Range(.5f, 1.2f)] public float ScreenCellFill = .9f;
        public Sprite[] DoorBottomIce = new Sprite[4];
        public Sprite[] DoorLeftIce = new Sprite[4];
        public Sprite[] DoorRightIce = new Sprite[4];
        [Tooltip("The left leaf of the gate over a top door's mouth, per door width. It slides left to open; what passes the mouth is cut away. An empty pair leaves the mouth open.")]
        public Sprite[] GateLeftLeaves = new Sprite[4];
        [Tooltip("The right leaf, sliding right.")]
        public Sprite[] GateRightLeaves = new Sprite[4];
        [Tooltip("Where the leaves' bottom-middle sits on the door's picture, in its pixels from the bottom-middle of the picture.")]
        public Vector2 GateOffsetPixels;
        [Tooltip("Per door width (element 0 is 1x): where the mask's bottom-middle sits, in the door picture's pixels from its bottom-middle. Only what lies inside the mask of a leaf is drawn. Changes show at once in Play Mode.")]
        public Vector2[] GateMaskOffset = new Vector2[4];
        [Tooltip("Per door width: the mask's width and height, in the door picture's pixels. 0 on an axis: as wide as both leaves, as tall as a leaf.")]
        public Vector2[] GateMaskSize = new Vector2[4];
        [Tooltip("Per door width: how far the mask's edges fade instead of cutting sharply, across and up, in the door picture's pixels. 0: a hard edge.")]
        public Vector2[] GateMaskSoftness = new Vector2[4];
        [Tooltip("The gate opens when the jelly the door waits for is in its columns, this many rows or fewer in front of it.")]
        [Range(0, 6)] public int GateOpenDistance = 1;
        [Tooltip("How fast the gate opens and closes: whole openings per second.")]
        [Range(.5f, 20)] public float GateOpenSpeed = 5;
        [Tooltip("Puffs of dust a gate throws out when it starts to open. 0: none.")]
        [Range(0, 30)] public int GateDustCount = 10;
        [Tooltip("The dust's colour; its alpha is how thick it is.")]
        public Color GateDustColour = new(1, 1, 1, .7f);
        [Tooltip("How big a puff grows, in cells.")]
        [Range(0, 2)] public float GateDustSize = .45f;
        [Tooltip("How far the puffs spread sideways from the mouth, in cells.")]
        [Range(0, 3)] public float GateDustSpread = .8f;
        [Tooltip("How far the puffs rise, in cells.")]
        [Range(-1, 2)] public float GateDustRise = .25f;
        [Tooltip("Seconds a puff lives.")]
        [Range(.1f, 2)] public float GateDustTime = .55f;

        [Section("Placement")]
        [Tooltip("Above 0, every frame piece and door is drawn at its own size, this many sprite pixels to a cell, with its pivot on the floor's edge (straights, doors) or the floor's corner (corners). Set the pivots in the Sprite Editor. At 0, every piece fills one cell.")]
        [Min(0)] public float PixelsPerCell;
        [Tooltip("Sprite pixels to a cell for the door pictures, when they are drawn at another scale than the walls. 0 uses Pixels Per Cell.")]
        [Min(0)] public float DoorPixelsPerCell;
        public float DoorPixels => DoorPixelsPerCell > 0 ? DoorPixelsPerCell : PixelsPerCell;
        [Tooltip("Size of a painted wall piece, in cells. Above 1 lets pieces overlap so the joins do not show.")]
        public Vector2 PieceSize = Vector2.one;
        [Tooltip("Moves painted wall pieces and doors off their cells, in cells (x right, y up).")]
        public Vector2 PieceOffset;
        [Section("Offsets per side - in cells, x right and y up, added to Piece Offset")]
        [Tooltip("The straight top wall.")] public Vector2 WallOffsetTop;
        [Tooltip("The straight bottom wall.")] public Vector2 WallOffsetBottom;
        [Tooltip("The left wall.")] public Vector2 WallOffsetLeft;
        [Tooltip("The right wall.")] public Vector2 WallOffsetRight;
        [Tooltip("The outer corner at the top left.")] public Vector2 CornerOffsetTopLeft;
        public Vector2 CornerOffsetTopRight;
        public Vector2 CornerOffsetBottomLeft;
        public Vector2 CornerOffsetBottomRight;
        [Tooltip("The inner corner where the floor turns inward, up and to the left of the floor's corner.")] public Vector2 ConcaveOffsetTopLeft;
        public Vector2 ConcaveOffsetTopRight;
        public Vector2 ConcaveOffsetBottomLeft;
        public Vector2 ConcaveOffsetBottomRight;
        [Tooltip("Doors in the top wall.")] public Vector2 DoorOffsetTop;
        [Tooltip("Doors in the bottom wall.")] public Vector2 DoorOffsetBottom;
        [Tooltip("Doors in the left wall.")] public Vector2 DoorOffsetLeft;
        [Tooltip("Doors in the right wall.")] public Vector2 DoorOffsetRight;
        [Tooltip("Moves the painted wall round the board toward the middle, in cells; corners move both ways. Near 1 sets a wall painted on the outer edge of its square against the floor.")]
        public float FrameInset = .8f;
        [Tooltip("Pictures of jelly shapes for a door's screen. A door whose jelly matches one, turned or mirrored, shows it in the jelly's colour; any other shape is drawn cell by cell.")]
        public List<JellyShapeIconCVO> ShapeIcons = new();
        [Tooltip("The material that redraws a shape picture in the jelly's colour (MT_UI_JellyTint). Empty: the picture is tinted the usual way.")]
        public Material IconTint;
        [Tooltip("Pixels from one cell of a shape picture to the next.")]
        [Min(1)] public float IconCellPixels = 19;
        [Tooltip("Size of one cell of the jelly drawn on a door, in board cells. The same on every door, whatever its size; a shape too big for its door is shrunk to fit.")]
        [Range(.05f, .6f)] public float IconCellSize = .22f;
        [Tooltip("Moves the jelly drawn on a door, in cells.")]
        public Vector2 IconOffset;
        [Tooltip("How many more jellies a door takes, shown as a number in a small square badge.")]
        public Color CounterColour = Color.white;
        [Tooltip("The badge's fill.")]
        public Color CounterBadge = new Color(.16f, .16f, .18f, 1);
        [Tooltip("The badge's outline.")]
        public Color CounterBorder = new Color(.78f, .78f, .8f, 1);
        [Tooltip("The outline's width, as a share of the badge's size. 0 draws none.")]
        [Range(0, .3f)] public float CounterBorderWidth = .1f;
        [Tooltip("Optional picture for the badge; empty draws a rounded square in the colours above.")]
        public Sprite CounterSprite;
        [Tooltip("On: the number is shown as +N. Off: just N.")]
        public bool CounterPlus;
        [Range(.2f, 1)] public float CounterSize = .34f;
        [Tooltip("For a door in the top wall: the point on the door the badge is pinned to, as a share of the door - (0, 0) bottom-left, (1, 1) top-right, (0, 0.5) the middle of its left edge. It stays there on a door of any size.")]
        public Vector2 CounterAnchorTop = new Vector2(0, .5f);
        [Tooltip("For a door in the bottom wall.")] public Vector2 CounterAnchorBottom = new Vector2(1, .5f);
        [Tooltip("For a door in the left wall.")] public Vector2 CounterAnchorLeft = new Vector2(.5f, 0);
        [Tooltip("For a door in the right wall.")] public Vector2 CounterAnchorRight = new Vector2(.5f, 1);
        [Tooltip("Moves the badge off that point, in cells (x right, y up).")]
        public Vector2 CounterOffset;

        public Vector2 WallOffset(JellySide side) => side switch { JellySide.Top => WallOffsetTop, JellySide.Bottom => WallOffsetBottom, JellySide.Left => WallOffsetLeft, _ => WallOffsetRight };
        public Vector2 CornerOffset(bool top, bool right) => top ? (right ? CornerOffsetTopRight : CornerOffsetTopLeft) : (right ? CornerOffsetBottomRight : CornerOffsetBottomLeft);
        public Vector2 ConcaveOffset(bool top, bool right) => top ? (right ? ConcaveOffsetTopRight : ConcaveOffsetTopLeft) : (right ? ConcaveOffsetBottomRight : ConcaveOffsetBottomLeft);
        public Vector2 CounterAnchor(JellySide side) => side switch { JellySide.Top => CounterAnchorTop, JellySide.Bottom => CounterAnchorBottom, JellySide.Left => CounterAnchorLeft, _ => CounterAnchorRight };
        public Vector2 DoorOffset(JellySide side) => side switch { JellySide.Top => DoorOffsetTop, JellySide.Bottom => DoorOffsetBottom, JellySide.Left => DoorOffsetLeft, _ => DoorOffsetRight };
        public Sprite Straight(JellySide side) => side switch { JellySide.Top => Top, JellySide.Bottom => Bottom, JellySide.Left => Left, _ => Right };
        public Sprite Corner(bool top, bool right) => top ? (right ? CornerTopRight : CornerTopLeft) : (right ? CornerBottomRight : CornerBottomLeft);
        public Sprite Concave(bool top, bool right) => top ? (right ? ConcaveTopRight : ConcaveTopLeft) : (right ? ConcaveBottomRight : ConcaveBottomLeft);

        public const int UP = 1, RIGHT = 2, DOWN = 4, LEFT = 8;
        /// <summary>The inner wall piece for the walls round it: UP, RIGHT, DOWN and LEFT added up.</summary>
        public Sprite InnerPiece(int neighbours)
        {
            var piece = neighbours switch
            {
                0 => Single,
                UP => EndUp, DOWN => EndDown, LEFT => EndLeft, RIGHT => EndRight,
                UP | DOWN => Vertical, LEFT | RIGHT => Horizontal,
                UP | RIGHT => CornerUpRight, UP | LEFT => CornerUpLeft, DOWN | RIGHT => CornerDownRight, DOWN | LEFT => CornerDownLeft,
                UP | LEFT | RIGHT => TeeUp, DOWN | LEFT | RIGHT => TeeDown, UP | DOWN | LEFT => TeeLeft, UP | DOWN | RIGHT => TeeRight,
                _ => Cross,
            };
            return piece != null ? piece : Inner;
        }

        /// <summary>
        /// The shape picture for these cells, and how to turn it: mirrored first (flip x), then turned
        /// a quarter anticlockwise <paramref name="quarters"/> times. Null when no picture fits.
        /// </summary>
        public Sprite ShapeIcon(IReadOnlyList<Vector2Int> cells, out int quarters, out bool mirrored)
        {
            quarters = 0; mirrored = false;
            if (ShapeIcons == null) return null;
            foreach (var icon in ShapeIcons)
            {
                if (icon.Sprite == null || icon.Cells.Count != cells.Count) continue;
                for (int m = 0; m < 2; m++) for (int q = 0; q < 4; q++)
                    if (SameCells(Turn(icon.Cells, q, m == 1), cells)) { quarters = q; mirrored = m == 1; return icon.Sprite; }
            }
            return null;
        }
        private static List<Vector2Int> Turn(List<Vector2Int> cells, int quarters, bool mirror)
        {
            var turned = new List<Vector2Int>(cells.Count);
            foreach (var cell in cells)
            {
                var c = mirror ? new Vector2Int(-cell.x, cell.y) : cell;
                for (int i = 0; i < quarters; i++) c = new Vector2Int(-c.y, c.x);
                turned.Add(c);
            }
            return turned;
        }
        private static bool SameCells(List<Vector2Int> a, IReadOnlyList<Vector2Int> b)
        {
            var minA = new Vector2Int(int.MaxValue, int.MaxValue); var minB = minA;
            foreach (var c in a) minA = Vector2Int.Min(minA, c);
            foreach (var c in b) minB = Vector2Int.Min(minB, c);
            foreach (var c in b) if (!a.Contains(c - minB + minA)) return false;
            return true;
        }

        public Vector2 GateMaskOffsetFor(int size) => GateMaskOffset != null && size >= 1 && size <= GateMaskOffset.Length ? GateMaskOffset[size - 1] : Vector2.zero;
        public Vector2 GateMaskSizeFor(int size) => GateMaskSize != null && size >= 1 && size <= GateMaskSize.Length ? GateMaskSize[size - 1] : Vector2.zero;
        public Vector2 GateMaskSoftnessFor(int size) => GateMaskSoftness != null && size >= 1 && size <= GateMaskSoftness.Length ? GateMaskSoftness[size - 1] : Vector2.zero;
        public Sprite GateLeft(int size) => GateLeftLeaves != null && size >= 1 && size <= GateLeftLeaves.Length ? GateLeftLeaves[size - 1] : null;
        public Sprite GateRight(int size) => GateRightLeaves != null && size >= 1 && size <= GateRightLeaves.Length ? GateRightLeaves[size - 1] : null;
        public JellyDoorScreenCVO DoorScreen(JellySide side, int size)
        {
            var set = side switch { JellySide.Top => DoorTopScreen, JellySide.Bottom => DoorBottomScreen, JellySide.Left => DoorLeftScreen, _ => DoorRightScreen };
            var screen = set != null && size >= 1 && size <= set.Length ? set[size - 1] : null;
            return screen != null && screen.Screen.width > 0 && screen.Pitch > 0 ? screen : null;
        }
        public Sprite DoorIce(JellySide side, int size)
        {
            var set = side switch { JellySide.Top => DoorTopIce, JellySide.Bottom => DoorBottomIce, JellySide.Left => DoorLeftIce, _ => DoorRightIce };
            return set != null && size >= 1 && size <= set.Length ? set[size - 1] : null;
        }
        public Sprite Door(JellySide side, int size)
        {
            var set = side switch { JellySide.Top => DoorTop, JellySide.Bottom => DoorBottom, JellySide.Left => DoorLeft, _ => DoorRight };
            return set != null && size >= 1 && size <= set.Length ? set[size - 1] : null;
        }
    }
}
