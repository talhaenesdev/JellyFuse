using Modules.GameplayModule.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{

    // Snapshots own their cell arrays. Views never receive the model's mutable state.
    public sealed class JellyPieceVO
    {
        public int Id { get; }
        public JellyColour Colour { get; }
        public IReadOnlyList<Vector2Int> Cells { get; }
        public JellyPieceVO(int id, JellyColour colour, IEnumerable<Vector2Int> cells)
        { Id = id; Colour = colour; Cells = Array.AsReadOnly(cells.ToArray()); }
    }

    /// <summary>
    /// A door in the wall round the board. It covers Cells, the board cells along its side that a
    /// jelly leaves from, and takes the jellies in Stages one after another: each must match the
    /// waiting stage in colour and in shape (moved, never turned). Stage cells start at (0, 0).
    /// </summary>
    public sealed class JellyGateCVO
    {
        public JellySide Side { get; }
        /// <summary>The way a jelly leaves through it.</summary>
        public Vector2Int Direction { get; }
        public string Label { get; }
        /// <summary>The board cells along the side it opens from, in order.</summary>
        public IReadOnlyList<Vector2Int> Cells { get; }
        public IReadOnlyList<JellyPieceVO> Stages { get; }
        public int Size => Cells.Count;
        /// <summary>Whether the door opens only as wide as the jelly it waits for (see OpenCells).</summary>
        public bool FitToJelly { get; }
        /// <summary>How many free cells a jelly this door waits for may be drawn across into it when let go of in front of it; -1 uses the game's default.</summary>
        public int PullReach { get; }
        /// <summary>How many jellies must go through doors before this one thaws and takes jellies; 0 when it is not frozen.</summary>
        public int Frozen { get; }
        private readonly IReadOnlyList<Vector2Int>[] _open;
        public JellyGateCVO(JellySide side, IEnumerable<Vector2Int> cells, IEnumerable<JellyPieceVO> stages, string label = "Door", bool fitToJelly = false, int pullReach = -1, int frozen = 0)
        {
            Side = side; Label = label; Direction = DirectionOf(side); FitToJelly = fitToJelly; PullReach = Mathf.Max(-1, pullReach);
            Frozen = Mathf.Max(0, frozen);
            Cells = Array.AsReadOnly(cells.ToArray()); Stages = Array.AsReadOnly(stages.ToArray());
            _open = new IReadOnlyList<Vector2Int>[Stages.Count];
            for (int i = 0; i < Stages.Count; i++)
            {
                int width = Across(Stages[i].Cells);
                _open[i] = fitToJelly && width < Cells.Count ? Array.AsReadOnly(Cells.Take(width).ToArray()) : Cells;
            }
        }
        /// <summary>
        /// The cells the door is open on while it waits for stage <paramref name="stage"/>: all of them,
        /// or, when it fits its jelly, as many from its first cell as that jelly is wide along the side.
        /// Past the last stage it keeps the last stage's opening.
        /// </summary>
        public IReadOnlyList<Vector2Int> OpenCells(int stage) =>
            _open.Length == 0 ? Cells : _open[Mathf.Clamp(stage, 0, _open.Length - 1)];
        /// <summary>How wide the cells are along the door's side.</summary>
        private int Across(IReadOnlyList<Vector2Int> cells)
        {
            int min = int.MaxValue, max = int.MinValue;
            foreach (var c in cells) { int lane = Direction.x != 0 ? c.y : c.x; min = Mathf.Min(min, lane); max = Mathf.Max(max, lane); }
            return cells.Count == 0 ? Cells.Count : max - min + 1;
        }
        /// <summary>A door of <paramref name="size"/> cells on a <paramref name="width"/> × <paramref name="height"/> board, from cell <paramref name="start"/> along its side.</summary>
        public JellyGateCVO(JellySide side, int start, int size, int width, int height, IEnumerable<JellyPieceVO> stages, string label = "Door", bool fitToJelly = false, int pullReach = -1, int frozen = 0)
            : this(side, EdgeCells(side, start, size, width, height), stages, label, fitToJelly, pullReach, frozen) { }
        public static Vector2Int DirectionOf(JellySide side) => side switch
        {
            JellySide.Bottom => Vector2Int.down, JellySide.Top => Vector2Int.up, JellySide.Left => Vector2Int.left, _ => Vector2Int.right,
        };
        public static Vector2Int[] EdgeCells(JellySide side, int start, int size, int width, int height)
        {
            var cells = new Vector2Int[Mathf.Max(0, size)];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = side switch
                {
                    JellySide.Bottom => new Vector2Int(start + i, 0), JellySide.Top => new Vector2Int(start + i, height - 1),
                    JellySide.Left => new Vector2Int(0, start + i), _ => new Vector2Int(width - 1, start + i),
                };
            return cells;
        }
        /// <summary>Whether the cell lies in a row (left and right doors) or column (top and bottom) the door spans.</summary>
        public bool Spans(Vector2Int cell) => Spans(cell, Cells);
        /// <summary>Whether the cell lies in a row or column the door is open on while it waits for this stage.</summary>
        public bool Spans(Vector2Int cell, int stage) => Spans(cell, OpenCells(stage));
        private bool Spans(Vector2Int cell, IReadOnlyList<Vector2Int> open)
        {
            int lane = Direction.x != 0 ? cell.y : cell.x, first = Direction.x != 0 ? open[0].y : open[0].x;
            return lane >= first && lane < first + open.Count;
        }
    }

    public sealed class JellyLevelCVO
    {
        private readonly Dictionary<int, (string Name, Color Tint)> _extraColours;
        public Color ColourTint(int id, Color fallback) => _extraColours.TryGetValue(id, out var colour) ? colour.Tint : fallback;
        public string ColourName(int id, string fallback) => _extraColours.TryGetValue(id, out var colour) ? colour.Name : fallback;
        public int Width { get; }
        public int Height { get; }
        public float Seconds { get; }
        public IReadOnlyList<Vector2Int> Walls { get; }
        public IReadOnlyList<Vector2Int> Holes { get; }
        public string Name { get; }
        public string Hint { get; }
        public IReadOnlyList<JellyPieceVO> Pieces { get; }
        public IReadOnlyList<JellyGateCVO> Gates { get; }
        /// <summary>The pieces that start frozen, by id: how many jellies must go through doors before each thaws.</summary>
        public IReadOnlyDictionary<int, int> FrozenPieces { get; }
        /// <summary>The arrow blocks, by id: the one way each can move.</summary>
        public IReadOnlyDictionary<int, JellyAxis> ArrowPieces { get; }
        /// <summary>Whether a successful fusion removes the resulting jelly's arrow restriction.</summary>
        public bool FreeArrowAfterFusion { get; }
        /// <summary>How hard the level is said to be; Hard and Super Hard are shown under the timer.</summary>
        public LevelDifficulty Difficulty { get; }
        public JellyLevelCVO(int width, int height, float seconds, IEnumerable<Vector2Int> walls,
            IEnumerable<JellyPieceVO> pieces, IEnumerable<JellyGateCVO> gates, IEnumerable<Vector2Int> holes = null, string name = "Jelly Fuse", string hint = "", IEnumerable<JellyColourCVO> extraColours = null,
            IReadOnlyDictionary<int, int> frozenPieces = null, IReadOnlyDictionary<int, JellyAxis> arrowPieces = null, bool freeArrowAfterFusion = false,
            LevelDifficulty difficulty = LevelDifficulty.Normal)
        {
            Difficulty = difficulty;
            FrozenPieces = frozenPieces ?? new Dictionary<int, int>();
            ArrowPieces = arrowPieces ?? new Dictionary<int, JellyAxis>();
            FreeArrowAfterFusion = freeArrowAfterFusion;
            _extraColours = (extraColours ?? Array.Empty<JellyColourCVO>()).ToDictionary(c => c.Id, c => (c.Name, c.Tint));
            Width = width; Height = height; Seconds = seconds; Name = name; Hint = hint;
            Holes = Array.AsReadOnly((holes ?? Array.Empty<Vector2Int>()).ToArray());
            Walls = Array.AsReadOnly(walls.ToArray()); Pieces = Array.AsReadOnly(pieces.ToArray());
            Gates = Array.AsReadOnly(gates.ToArray());
        }
    }

    public sealed class PuzzleSnapshotRVO
    {
        public JellyLevelCVO Level { get; }
        public IReadOnlyList<JellyPieceVO> Pieces { get; }
        public PuzzleStatus Status { get; }
        public int Moves { get; }
        public int Exited { get; }
        public bool CanUndo { get; }
        public string Warning { get; }
        /// <summary>Per door: how many of its stages have passed.</summary>
        public IReadOnlyList<int> DoorProgress { get; }
        /// <summary>The jelly the door waits for now, or null once its queue is done.</summary>
        public JellyPieceVO WaitingStage(int door)
        {
            var stages = Level.Gates[door].Stages;
            return DoorProgress[door] < stages.Count ? stages[DoorProgress[door]] : null;
        }
        /// <summary>How many more jellies the door takes after the one it waits for now.</summary>
        public int QueuedAfter(int door) => Mathf.Max(0, Level.Gates[door].Stages.Count - DoorProgress[door] - 1);
        /// <summary>While a jelly is held: the pieces it would fuse with if it were released now.</summary>
        public IReadOnlyList<int> FuseReady { get; }
        /// <summary>While a jelly is held: the directions it can take one step in (see CanHeldStep).</summary>
        public int HeldFreeSides { get; }
        /// <summary>Per door: how many jellies must still go through doors before it thaws; 0 when it is open.</summary>
        public IReadOnlyList<int> DoorFrozen { get; }
        private readonly Dictionary<int, int> _jellyFrozen;
        /// <summary>How many jellies must still go through doors before this piece thaws; 0 when it can move.</summary>
        public int JellyFrozen(int pieceId) => _jellyFrozen.TryGetValue(pieceId, out var left) ? left : 0;
        public const int Horizontal = 1, Vertical = 2, AnyWay = Horizontal | Vertical;
        private readonly Dictionary<int, int> _moveWays;
        /// <summary>The ways a piece can move, as Horizontal and Vertical added up: AnyWay for an ordinary jelly, one of them for an arrow block, none when two arrow blocks of different ways fused.</summary>
        public int MoveWays(int pieceId) => _moveWays.TryGetValue(pieceId, out var ways) ? ways : AnyWay;
        public const int Right = 1, Left = 2, Up = 4, Down = 8;
        /// <summary>Whether the held jelly can take one step this way: onto free floor, or out through its exit.</summary>
        public bool CanHeldStep(Vector2Int step) =>
            (HeldFreeSides & (step.x > 0 ? Right : step.x < 0 ? Left : step.y > 0 ? Up : step.y < 0 ? Down : 0)) != 0;
        public PuzzleSnapshotRVO(JellyLevelCVO level, IEnumerable<JellyPieceVO> pieces, PuzzleStatus status,
            int moves, int exited, bool canUndo, string warning, IEnumerable<int> doorProgress = null,
            IEnumerable<int> fuseReady = null, int heldFreeSides = 0, IEnumerable<int> doorFrozen = null, IReadOnlyDictionary<int, int> jellyFrozen = null,
            IReadOnlyDictionary<int, int> moveWays = null)
        {
            _moveWays = moveWays != null ? new Dictionary<int, int>(moveWays) : new Dictionary<int, int>();
            HeldFreeSides = heldFreeSides;
            DoorFrozen = Array.AsReadOnly(doorFrozen?.ToArray() ?? new int[level?.Gates.Count ?? 0]);
            _jellyFrozen = jellyFrozen != null ? new Dictionary<int, int>(jellyFrozen) : new Dictionary<int, int>();
            Level = level; Pieces = Array.AsReadOnly(pieces.ToArray()); Status = status;
            Moves = moves; Exited = exited; CanUndo = canUndo; Warning = warning;
            DoorProgress = Array.AsReadOnly(doorProgress?.ToArray() ?? new int[level?.Gates.Count ?? 0]);
            FuseReady = Array.AsReadOnly((fuseReady ?? Array.Empty<int>()).ToArray());
        }
    }

    public sealed class PuzzleTurnVO
    {
        public IReadOnlyList<PuzzleSnapshotRVO> Frames { get; }
        public PuzzleTurnVO(IEnumerable<PuzzleSnapshotRVO> frames) { Frames = Array.AsReadOnly(frames.ToArray()); }
    }

}


