using System;
using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Models
{
    /// <summary>
    /// The puzzle's state and rules. The paths a drag runs on every pointer update reuse the
    /// buffers below instead of allocating; what still allocates is the immutable snapshot each
    /// change hands to the presentation, and the history kept for undo.
    /// </summary>
    public sealed class JellyPuzzleModel : IJellyPuzzleModel
    {
        private sealed class History
        {
            public List<JellyPieceVO> Pieces;
            public int Moves;
            public int Exited;
            public int[] Progress;
            public int[] DoorFrozen;
            public Dictionary<int, int> JellyFrozen;
            public Dictionary<int, int> MoveWays;
        }
        private readonly Stack<History> _history = new();
        private List<JellyPieceVO> _pieces = new();
        private JellyLevelCVO _level;
        /// <summary>Per door: how many of its queued jellies have passed.</summary>
        private int[] _progress = Array.Empty<int>();
        /// <summary>
        /// Frozen doors and jellies: how many jellies must still go through doors before each thaws.
        /// Every jelly a door takes counts one off all of them at once.
        /// </summary>
        private int[] _doorFrozen = Array.Empty<int>();
        private Dictionary<int, int> _jellyFrozen = new();
        /// <summary>
        /// The arrow blocks: the ways each can move (PuzzleSnapshotRVO.Horizontal / Vertical). A piece
        /// not listed moves any way. A fused piece keeps only the ways all of its parts could move.
        /// </summary>
        private Dictionary<int, int> _moveWays = new();
        private History _dragBefore;
        private Vector2Int _dragAnchor;
        private int _dragId;
        private bool _dragChanged;
        private int _moves;
        private int _exited;
        /// <summary>Colours a UFO took: a door request for one of them is passed over when reached.</summary>
        private readonly HashSet<JellyColour> _clearedColours = new();
        public float ClockFreezeSeconds { get; private set; }

        // Reused on every drag step and snapshot, so neither leaves garbage behind.
        private readonly Vector2Int[] _steps = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        private readonly Dictionary<Vector2Int, Vector2Int> _routeFirst = new();
        private readonly Queue<Vector2Int> _routeQueue = new();
        private readonly HashSet<Vector2Int> _blocked = new();
        /// <summary>The cells the doors stand in. They block like walls: a jelly only passes through one, and only out.</summary>
        private readonly HashSet<Vector2Int> _doorCells = new();
        private readonly List<int> _fuseReady = new();
        private readonly List<Vector2Int> _fusedCells = new();
        private readonly HashSet<int> _joined = new();
        /// <summary>The last snapshot, handed out again until the state changes (Changed clears it).</summary>
        private PuzzleSnapshotRVO _cached;
        private void Changed() => _cached = null;

        public float RemainingSeconds { get; private set; }
        public PuzzleStatus Status { get; private set; }
        public bool IsDragging => _dragBefore != null;
        public bool IsTicking { get; set; }
        public bool IsHooked { get; set; }
        public int LevelIndex { get; set; } = -1;
        public bool WinPaid { get; set; }
        public bool LifeTaken { get; set; }
        public int WinReward { get; set; }
        public bool RewardDoubled { get; set; }
        public bool TimeExtended { get; private set; }
        public bool InPlay { get; set; }
        public bool ClockStarted { get; set; }
        /// <summary>The last stage of a door that counts when judging a shape: the one it waits for now, or its whole queue.</summary>
        // A shape is judged only against the jelly each door waits for now, never the ones queued behind it.
        private int LastJudgedStage(int door, int stageCount) => Mathf.Min(stageCount, _progress[door] + 1);

        public void Start(JellyLevelCVO level)
        {
            Changed();
            _dragBefore = null;
            _level = level;
            _pieces = new List<JellyPieceVO>(level.Pieces); _progress = new int[level.Gates.Count];
            _doorFrozen = new int[level.Gates.Count];
            for (int i = 0; i < level.Gates.Count; i++) _doorFrozen[i] = level.Gates[i].Frozen;
            _jellyFrozen = new Dictionary<int, int>(level.FrozenPieces);
            _moveWays = new Dictionary<int, int>();
            foreach (var arrow in level.ArrowPieces)
                _moveWays[arrow.Key] = arrow.Value == JellyAxis.Horizontal ? PuzzleSnapshotRVO.Horizontal : PuzzleSnapshotRVO.Vertical;
            _doorCells.Clear();
            foreach (var gate in level.Gates) foreach (var cell in gate.Cells) _doorCells.Add(cell);
            _history.Clear(); _moves = 0; _exited = 0;
            _clearedColours.Clear(); ClockFreezeSeconds = 0;
            RemainingSeconds = level.Seconds; Status = PuzzleStatus.Playing;
            // The clock waits for the first move.
            ClockStarted = false;
            IsTicking = false;
            WinPaid = false;
            LifeTaken = false;
            WinReward = 0;
            RewardDoubled = false;
            TimeExtended = false;
            InPlay = true;
        }

        public bool ExtendTime(float seconds)
        {
            if (_level == null || Status != PuzzleStatus.Lost || RemainingSeconds > 0 || TimeExtended || seconds <= 0) return false;
            TimeExtended = true;
            RemainingSeconds = seconds;
            Status = PuzzleStatus.Playing;
            IsTicking = ClockStarted;
            Changed();
            return true;
        }

        public bool Tick(float seconds)
        {
            if (_level == null || Status != PuzzleStatus.Playing || seconds <= 0 || float.IsNaN(seconds)) return false;
            // A frozen clock spends the freeze first, and only what is left of the frame counts down.
            if (ClockFreezeSeconds > 0)
            {
                float spent = Mathf.Min(ClockFreezeSeconds, seconds);
                ClockFreezeSeconds -= spent; seconds -= spent;
                if (seconds <= 0) return false;
            }
            RemainingSeconds = Mathf.Max(0, RemainingSeconds - seconds);
            if (RemainingSeconds > 0) return false;
            FinishDrag();
            Status = PuzzleStatus.Lost;
            IsTicking = false;
            Changed();
            return true;
        }

        public PuzzleSnapshotRVO Snapshot()
        {
            if (_cached != null) return _cached;
            FillFuseReady();
            return _cached = new PuzzleSnapshotRVO(_level, _pieces, Status, _moves, _exited,
                _history.Count > 0 && Status != PuzzleStatus.Lost, GetWarning(), _progress, _fuseReady, HeldFreeSides(), _doorFrozen, _jellyFrozen, _moveWays);
        }
        /// <summary>The sides the held jelly can step to: free floor, or out through its open exit. The board leads it only that way.</summary>
        private int HeldFreeSides()
        {
            if (_dragBefore == null || _level == null || Status != PuzzleStatus.Playing) return 0;
            var held = FindPiece(_dragId);
            if (held == null) return 0;
            int sides = 0;
            if (Allowed(held.Id, Vector2Int.right) && (Fits(held, Vector2Int.right) || ExitFor(held, Vector2Int.right) >= 0)) sides |= PuzzleSnapshotRVO.Right;
            if (Allowed(held.Id, Vector2Int.left) && (Fits(held, Vector2Int.left) || ExitFor(held, Vector2Int.left) >= 0)) sides |= PuzzleSnapshotRVO.Left;
            if (Allowed(held.Id, Vector2Int.up) && (Fits(held, Vector2Int.up) || ExitFor(held, Vector2Int.up) >= 0)) sides |= PuzzleSnapshotRVO.Up;
            if (Allowed(held.Id, Vector2Int.down) && (Fits(held, Vector2Int.down) || ExitFor(held, Vector2Int.down) >= 0)) sides |= PuzzleSnapshotRVO.Down;
            return sides;
        }

        // ---------- lookups (no LINQ, no closures) ----------

        private JellyPieceVO FindPiece(int id)
        {
            for (int i = 0; i < _pieces.Count; i++) if (_pieces[i].Id == id) return _pieces[i];
            return null;
        }
        private JellyPieceVO PieceAt(Vector2Int cell)
        {
            for (int i = 0; i < _pieces.Count; i++) if (Contains(_pieces[i].Cells, cell)) return _pieces[i];
            return null;
        }
        private static bool Contains(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
        {
            for (int i = 0; i < cells.Count; i++) if (cells[i] == cell) return true;
            return false;
        }
        private static bool Adjacent(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        private static bool Touches(IReadOnlyList<Vector2Int> a, IReadOnlyList<Vector2Int> b)
        {
            for (int i = 0; i < a.Count; i++) for (int j = 0; j < b.Count; j++) if (Adjacent(a[i], b[j])) return true;
            return false;
        }
        private int Ways(int pieceId) => _moveWays.TryGetValue(pieceId, out var ways) ? ways : PuzzleSnapshotRVO.AnyWay;
        /// <summary>Whether the piece may take this step: an arrow block only along its own way.</summary>
        private bool Allowed(int pieceId, Vector2Int step) =>
            step == Vector2Int.zero || (Ways(pieceId) & (step.x != 0 ? PuzzleSnapshotRVO.Horizontal : PuzzleSnapshotRVO.Vertical)) != 0;
        private bool IsFrozen(int pieceId) => _jellyFrozen.TryGetValue(pieceId, out var left) && left > 0;
        private History Remember() => new History
        {
            Pieces = new List<JellyPieceVO>(_pieces), Moves = _moves, Exited = _exited, Progress = (int[])_progress.Clone(),
            DoorFrozen = (int[])_doorFrozen.Clone(), JellyFrozen = new Dictionary<int, int>(_jellyFrozen), MoveWays = new Dictionary<int, int>(_moveWays),
        };
        /// <summary>A jelly went through a door: every frozen door and jelly is one closer to thawing.</summary>
        private void Thaw()
        {
            for (int i = 0; i < _doorFrozen.Length; i++) if (_doorFrozen[i] > 0) _doorFrozen[i]--;
            _thawing.Clear(); _thawing.AddRange(_jellyFrozen.Keys);
            foreach (var id in _thawing) if (--_jellyFrozen[id] <= 0) _jellyFrozen.Remove(id);
        }
        private readonly List<int> _thawing = new();
        private bool Inside(Vector2Int c) => c.x >= 0 && c.x < _level.Width && c.y >= 0 && c.y < _level.Height;
        private JellyPieceVO WaitingStage(int door)
        {
            var stages = _level.Gates[door].Stages;
            return _progress[door] < stages.Count ? stages[_progress[door]] : null;
        }
        /// <summary>Whether the cells are the stage's shape, moved but never turned.</summary>
        private static bool SameShape(IReadOnlyList<Vector2Int> cells, IReadOnlyList<Vector2Int> shape)
        {
            if (cells.Count != shape.Count) return false;
            int minX = int.MaxValue, minY = int.MaxValue;
            for (int i = 0; i < cells.Count; i++) { minX = Mathf.Min(minX, cells[i].x); minY = Mathf.Min(minY, cells[i].y); }
            var origin = new Vector2Int(minX, minY);
            for (int i = 0; i < cells.Count; i++) if (!Contains(shape, cells[i] - origin)) return false;
            return true;
        }

        // ---------- fuse hint ----------

        /// <summary>
        /// The pieces the held jelly would fuse with if released now, but only when that fuse is a
        /// good one: the shape it makes, with every piece it chains into, still fits a jelly some door
        /// is yet to take. A fuse that would spoil the shape is not hinted.
        /// </summary>
        private void FillFuseReady()
        {
            _fuseReady.Clear();
            if (_dragBefore == null || _level == null) return;
            var held = FindPiece(_dragId);
            if (held == null) return;
            foreach (var other in _pieces)
                if (other.Id != held.Id && other.Colour == held.Colour && !IsFrozen(other.Id) && Touches(held.Cells, other.Cells)) _fuseReady.Add(other.Id);
            if (_fuseReady.Count == 0) return;
            // Follow the chain the release would fuse, as Settle does.
            _fusedCells.Clear(); _fusedCells.AddRange(held.Cells);
            _joined.Clear(); _joined.Add(held.Id);
            for (bool grew = true; grew;)
            {
                grew = false;
                foreach (var other in _pieces)
                {
                    if (_joined.Contains(other.Id) || other.Colour != held.Colour || IsFrozen(other.Id) || !Touches(other.Cells, _fusedCells)) continue;
                    _fusedCells.AddRange(other.Cells); _joined.Add(other.Id); grew = true;
                }
            }
            int biggest = 0;
            foreach (var piece in _pieces) if (_joined.Contains(piece.Id)) biggest = Mathf.Max(biggest, piece.Cells.Count);
            if (!FitsAFuse(_fusedCells, held.Colour, biggest)) _fuseReady.Clear();
        }
        private bool FitsJoined(JellyPieceVO a, JellyPieceVO b)
        {
            _fusedCells.Clear(); _fusedCells.AddRange(a.Cells); _fusedCells.AddRange(b.Cells);
            return FitsAFuse(_fusedCells, a.Colour, Mathf.Max(a.Cells.Count, b.Cells.Count));
        }
        /// <summary>
        /// A good fuse: the cells fit inside a jelly a door waits for now, and the fuse builds on the biggest jelly
        /// of the colour that fits that one - <paramref name="biggestPart"/> is the biggest of the jellies fusing.
        /// Making the shape out of small jellies while a bigger one that fits it is on the board would use up the
        /// small ones the later doors need: the jelly the door waits for, already whole, goes first, and a part of it
        /// grows from the biggest part there is.
        /// </summary>
        private bool FitsAFuse(IReadOnlyList<Vector2Int> cells, JellyColour colour, int biggestPart)
        {
            for (int g = 0; g < _level.Gates.Count; g++)
            {
                var stages = _level.Gates[g].Stages;
                for (int s = _progress[g]; s < LastJudgedStage(g, stages.Count); s++)
                {
                    var stage = stages[s];
                    if (stage.Colour != colour || !FitsInside(cells, stage.Cells)) continue;
                    int biggestOnBoard = 0;
                    foreach (var piece in _pieces)
                        if (piece.Colour == colour && !IsFrozen(piece.Id) && piece.Cells.Count > biggestOnBoard && FitsInside(piece.Cells, stage.Cells))
                            biggestOnBoard = piece.Cells.Count;
                    if (!BiggestFuseFirst) return true;
                    if (biggestPart >= biggestOnBoard) return true;
                }
            }
            return false;
        }
        /// <summary>Whether the cells, moved but never turned, lie inside the shape.</summary>
        private static bool FitsInside(IReadOnlyList<Vector2Int> cells, IReadOnlyList<Vector2Int> shape)
        {
            if (cells.Count == 0 || cells.Count > shape.Count) return false;
            for (int a = 0; a < shape.Count; a++)
            {
                var shift = shape[a] - cells[0];
                bool fits = true;
                for (int c = 0; c < cells.Count && fits; c++) fits = Contains(shape, cells[c] + shift);
                if (fits) return true;
            }
            return false;
        }
        /// <summary>Whether the cells, moved but never turned, fit inside a jelly of the colour that a door has still to take.</summary>
        private bool FitsADoor(IReadOnlyList<Vector2Int> cells, JellyColour colour)
        {
            for (int g = 0; g < _level.Gates.Count; g++)
            {
                var stages = _level.Gates[g].Stages;
                for (int s = _progress[g]; s < LastJudgedStage(g, stages.Count); s++)
                {
                    var stage = stages[s];
                    if (stage.Colour != colour) continue;
                    for (int a = 0; a < stage.Cells.Count; a++)
                    {
                        var shift = stage.Cells[a] - cells[0];
                        bool fits = true;
                        for (int c = 0; c < cells.Count && fits; c++) fits = Contains(stage.Cells, cells[c] + shift);
                        if (fits) return true;
                    }
                }
            }
            return false;
        }

        // ---------- drag ----------

        // A drag is one undoable turn, even when it contains many pointer updates.
        public PuzzleTurnVO Drag(Vector2Int selectedCell, Vector2Int targetCell, bool released)
        {
            if (_level == null) return new PuzzleTurnVO(Array.Empty<PuzzleSnapshotRVO>());
            if (Status != PuzzleStatus.Playing) { FinishDrag(); return new PuzzleTurnVO(new[] { Snapshot() }); }
            if (_dragBefore == null)
            {
                var selected = PieceAt(selectedCell);
                // A frozen jelly stays where it is until it thaws.
                if (selected == null || IsFrozen(selected.Id)) return new PuzzleTurnVO(new[] { Snapshot() });
                _dragBefore = Remember();
                _dragAnchor = selectedCell; _dragId = selected.Id; _dragChanged = false;
                _sinceGoodFuse.Clear();
                TrackGoodFuse(selected);
                Changed();
            }
            // One cardinal grid step at a time, along the shortest free route to the target, so a
            // jelly is led round anything in its way and never steps back on itself. When no free
            // route reaches the target (it is off the board, or taken) the piece steps straight
            // toward it while it can, which is also how it leaves through an exit. Steps only move
            // it: fusing waits for the release, so a jelly can pass its own colour without sticking to it.
            int budget = _level.Width * _level.Height;
            // One frame per step, so the presentation can walk the route instead of cutting across it.
            var frames = new List<PuzzleSnapshotRVO>();
            // An arrow block heads only for the part of the target along its own way.
            int ways = Ways(_dragId);
            if ((ways & PuzzleSnapshotRVO.Horizontal) == 0) targetCell.x = _dragAnchor.x;
            if ((ways & PuzzleSnapshotRVO.Vertical) == 0) targetCell.y = _dragAnchor.y;
            while (_dragAnchor != targetCell && budget-- > 0 && Status == PuzzleStatus.Playing)
            {
                var dragged = FindPiece(_dragId);
                if (dragged == null) break;
                var step = PathFinding ? FirstStepAround(dragged, targetCell - _dragAnchor) : StraightStep(dragged, targetCell - _dragAnchor);
                bool moved;
                if (step != Vector2Int.zero) moved = Shift(dragged, step);
                else
                {
                    var delta = targetCell - _dragAnchor;
                    step = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? new Vector2Int(Math.Sign(delta.x), 0) : new Vector2Int(0, Math.Sign(delta.y));
                    moved = ExitThrough(dragged, step) || (Fits(dragged, step) && Shift(dragged, step));
                }
                if (!moved) break;
                _dragAnchor += step; _dragChanged = true;
                TrackGoodFuse(FindPiece(_dragId));
                _moves = _dragBefore.Moves + 1;
                Changed();
                frames.Add(Snapshot());
            }
            var held = FindPiece(_dragId);
            if (released && Status == PuzzleStatus.Playing && held != null)
            {
                // Let go where it fuses with nothing, just past a spot where it would have fused well: it
                // walks back there, a frame a step, and fuses as if it had been let go of there.
                FillFuseReady();
                int back = _sinceGoodFuse.Count - 1;
                if (_fuseReady.Count == 0 && back > 0 && back <= ReleaseFuseLookBack)
                {
                    for (int i = _sinceGoodFuse.Count - 2; i >= 0; i--)
                    {
                        var piece = FindPiece(_dragId);
                        _pieces[_pieces.IndexOf(piece)] = new JellyPieceVO(piece.Id, piece.Colour, _sinceGoodFuse[i]);
                        Changed();
                        frames.Add(Snapshot());
                    }
                    var start = FindPiece(_dragId);
                    _dragAnchor += start.Cells[0] - held.Cells[0];
                    held = start;
                }
                _sinceGoodFuse.Clear();
                // Let go: now it fuses with what it touches.
                if (Settle(held, null))
                {
                    if (!_dragChanged) _moves = _dragBefore.Moves + 1;
                    _dragChanged = true;
                    Changed();
                    // The fused jelly gets a frame of its own, so it is seen whole before a door takes it.
                    frames.Add(Snapshot());
                }
                // Then, let go of in front of a door that waits for it, it is drawn in.
                held = FindPiece(_dragId);
                if (held != null && PullIntoDoor(held, frames))
                {
                    _moves = _dragBefore.Moves + 1;
                    _dragChanged = true;
                    Changed();
                    // The board without it; the frames before still hold it, for its slide into the door.
                    frames.Add(Snapshot());
                }
            }
            if (released || FindPiece(_dragId) == null) FinishDrag();
            if (frames.Count == 0) frames.Add(Snapshot());
            else frames[frames.Count - 1] = Snapshot();
            return new PuzzleTurnVO(frames);
        }

        /// <summary>
        /// Without path finding: one step straight toward the finger, along the axis it is further away on, else
        /// the other one. Zero when both are blocked - the jelly waits there, still pulled toward the finger.
        /// </summary>
        private Vector2Int StraightStep(JellyPieceVO piece, Vector2Int delta)
        {
            var across = new Vector2Int(Math.Sign(delta.x), 0);
            var upDown = new Vector2Int(0, Math.Sign(delta.y));
            bool acrossFirst = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            var first = acrossFirst ? across : upDown;
            var second = acrossFirst ? upDown : across;
            if (first != Vector2Int.zero && Fits(piece, first)) return first;
            if (second != Vector2Int.zero && Fits(piece, second)) return second;
            return Vector2Int.zero;
        }
        /// <summary>Whether the piece fits where <paramref name="offset"/> would put it: on the board, on floor, clear of every other piece.</summary>
        private bool Fits(JellyPieceVO piece, Vector2Int offset)
        {
            foreach (var c in piece.Cells)
            {
                var cell = c + offset;
                if (!Inside(cell) || Contains(_level.Walls, cell) || Contains(_level.Holes, cell) || _doorCells.Contains(cell)) return false;
                foreach (var other in _pieces) if (other.Id != piece.Id && Contains(other.Cells, cell)) return false;
            }
            return true;
        }
        /// <summary>Moves the piece by one step. The caller has checked that it fits.</summary>
        private bool Shift(JellyPieceVO piece, Vector2Int step)
        {
            var cells = new Vector2Int[piece.Cells.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = piece.Cells[i] + step;
            _pieces[_pieces.IndexOf(piece)] = new JellyPieceVO(piece.Id, piece.Colour, cells);
            Changed();
            return true;
        }
        public int DefaultPullReach { get; set; } = 1;
        public int ReleaseFuseLookBack { get; set; }
        public bool PathFinding { get; set; } = true;
        public bool BiggestFuseFirst { get; set; } = true;
        /// <summary>
        /// The held jelly's cells at each step since the last spot of this drag where it would have fused
        /// well, that spot first; empty while it has passed none.
        /// </summary>
        private readonly List<Vector2Int[]> _sinceGoodFuse = new();
        /// <summary>Notes where the held jelly is now: a spot that fuses well starts the trail again, any other spot goes on it.</summary>
        private void TrackGoodFuse(JellyPieceVO held)
        {
            if (held == null || ReleaseFuseLookBack <= 0) return;
            FillFuseReady();
            var cells = new Vector2Int[held.Cells.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = held.Cells[i];
            if (_fuseReady.Count > 0) _sinceGoodFuse.Clear();
            else if (_sinceGoodFuse.Count == 0) return;
            _sinceGoodFuse.Add(cells);
        }

        /// <summary>
        /// A jelly a door waits for, let go of in the door's rows (or columns) with up to the door's
        /// pull reach (its own, or DefaultPullReach) free cells between it and the door, is drawn the rest of the way in - one frame
        /// per cell, so it is seen sliding - and leaves. Only the placed piece is taken.
        /// </summary>
        private bool PullIntoDoor(JellyPieceVO piece, List<PuzzleSnapshotRVO> frames)
        {
            for (int door = 0; door < _level.Gates.Count; door++)
            {
                var gate = _level.Gates[door];
                var direction = gate.Direction;
                // A door draws its jelly in whichever way the jelly's arrow points: the arrow limits
                // only how the player can drag it.
                int limit = gate.PullReach >= 0 ? gate.PullReach : DefaultPullReach;
                for (int reach = 0; reach <= limit; reach++)
                {
                    var offset = direction * reach;
                    if (reach > 0 && !Fits(piece, offset)) break;
                    var cells = new Vector2Int[piece.Cells.Count];
                    for (int i = 0; i < cells.Length; i++) cells[i] = piece.Cells[i] + offset;
                    if (ExitFor(new JellyPieceVO(piece.Id, piece.Colour, cells), direction) != door) continue;
                    for (int i = 0; i < reach; i++)
                    {
                        Shift(FindPiece(piece.Id), direction);
                        frames.Add(Snapshot());
                    }
                    return ExitThrough(FindPiece(piece.Id), direction);
                }
            }
            return false;
        }

        /// <summary>An outward step through a door that waits for this very jelly: it leaves the board.</summary>
        private bool ExitThrough(JellyPieceVO piece, Vector2Int step)
        {
            int index = ExitFor(piece, step);
            if (index < 0) return false;
            _pieces.Remove(piece); _progress[index]++; _exited++;
            Thaw();
            SkipClearedRequests();
            if (_pieces.Count == 0) { Status = PuzzleStatus.Won; IsTicking = false; }
            Changed();
            return true;
        }
        /// <summary>
        /// The door the piece leaves through on this step, or -1. The door must wait for a jelly of the
        /// piece's colour and shape, the piece must be right in front of the door (or already in it),
        /// and every one of its rows (or columns) must pass within the door.
        /// </summary>
        private int ExitFor(JellyPieceVO piece, Vector2Int step)
        {
            for (int i = 0; i < _level.Gates.Count; i++)
            {
                var gate = _level.Gates[i];
                if (step != gate.Direction || _doorFrozen[i] > 0) continue;
                var stage = WaitingStage(i);
                if (stage == null || stage.Colour != piece.Colour || !SameShape(piece.Cells, stage.Cells)) continue;
                // A door that fits its jelly is open on only part of its cells for this one.
                var open = gate.OpenCells(_progress[i]);
                bool through = false, inside = true;
                for (int c = 0; c < piece.Cells.Count && inside; c++)
                {
                    inside = gate.Spans(piece.Cells[c], _progress[i]);
                    var ahead = piece.Cells[c] + step;
                    through |= !Inside(ahead) || Contains(open, ahead);
                }
                if (inside && through) return i;
            }
            return -1;
        }

        /// <summary>
        /// The first step of the shortest route that moves the piece by <paramref name="goal"/> through
        /// free floor only, or zero when there is none. Breadth-first over the piece's offsets, on
        /// buffers kept between calls.
        /// </summary>
        private Vector2Int FirstStepAround(JellyPieceVO piece, Vector2Int goal)
        {
            _blocked.Clear();
            foreach (var wall in _level.Walls) _blocked.Add(wall);
            foreach (var hole in _level.Holes) _blocked.Add(hole);
            foreach (var door in _doorCells) _blocked.Add(door);
            foreach (var other in _pieces) if (other.Id != piece.Id) foreach (var cell in other.Cells) _blocked.Add(cell);
            if (!FreeAt(piece, goal)) return Vector2Int.zero;
            _routeFirst.Clear(); _routeQueue.Clear();
            _routeFirst[Vector2Int.zero] = Vector2Int.zero;
            _routeQueue.Enqueue(Vector2Int.zero);
            while (_routeQueue.Count > 0)
            {
                var offset = _routeQueue.Dequeue();
                foreach (var step in _steps)
                {
                    var next = offset + step;
                    if (!Allowed(piece.Id, step) || _routeFirst.ContainsKey(next) || !FreeAt(piece, next)) continue;
                    var first = offset == Vector2Int.zero ? step : _routeFirst[offset];
                    if (next == goal) return first;
                    _routeFirst[next] = first;
                    _routeQueue.Enqueue(next);
                }
            }
            return Vector2Int.zero;
        }
        private bool FreeAt(JellyPieceVO piece, Vector2Int offset)
        {
            foreach (var c in piece.Cells)
            {
                var cell = c + offset;
                if (!Inside(cell) || _blocked.Contains(cell)) return false;
            }
            return true;
        }
        private void FinishDrag()
        {
            if (_dragBefore == null) return;
            if (_dragChanged) _history.Push(_dragBefore);
            _dragBefore = null; _dragChanged = false;
            Changed();
        }

        // ---------- drop (one step with settle; the rule checks drive it) ----------

        public PuzzleTurnVO Drop(Vector2Int selectedCell, Vector2Int targetCell)
        {
            if (_level == null) return new PuzzleTurnVO(Array.Empty<PuzzleSnapshotRVO>());
            var piece = PieceAt(selectedCell);
            var offset = targetCell - selectedCell;
            if (Status != PuzzleStatus.Playing || piece == null || offset == Vector2Int.zero || IsFrozen(piece.Id)
                || (offset.x != 0 && !Allowed(piece.Id, Vector2Int.right)) || (offset.y != 0 && !Allowed(piece.Id, Vector2Int.up)))
                return new PuzzleTurnVO(new[] { Snapshot() });
            var before = Remember();
            // A door takes the exact assembled shape against its side, on an outward step.
            if (ExitThrough(piece, offset))
            {
                _history.Push(before); _moves++; Changed();
                return new PuzzleTurnVO(new[] { Snapshot() });
            }
            // Every occupied cell collides.
            if (!Fits(piece, offset)) return new PuzzleTurnVO(new[] { Snapshot() });
            var cells = new Vector2Int[piece.Cells.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = piece.Cells[i] + offset;
            piece = new JellyPieceVO(piece.Id, piece.Colour, cells);
            _pieces.Remove(FindPiece(piece.Id)); _pieces.Add(piece);
            Changed();
            var frames = new List<PuzzleSnapshotRVO> { Snapshot() };
            Settle(piece, frames);
            _history.Push(before); _moves++;
            Changed();
            frames.Add(Snapshot());
            return new PuzzleTurnVO(frames);
        }

        /// <summary>
        /// Fuses the piece with every touching piece of its colour. Adds a frame per change to
        /// <paramref name="frames"/> when given; returns whether anything happened.
        /// </summary>
        private bool Settle(JellyPieceVO piece, List<PuzzleSnapshotRVO> frames)
        {
            bool changed = false;
            while (true)
            {
                JellyPieceVO neighbour = null;
                foreach (var other in _pieces)
                {
                    if (other.Id == piece.Id || other.Colour != piece.Colour || IsFrozen(other.Id) || !Touches(piece.Cells, other.Cells)) continue;
                    // A fuse that would make a shape no door takes is left undone, when wrong fuses are blocked.
                    // Two jellies fuse only into a shape a door is waiting for now; otherwise they stay apart side by side.
                    if (!FitsJoined(piece, other)) continue;
                    neighbour = other; break;
                }
                if (neighbour == null) break;
                var cells = new Vector2Int[piece.Cells.Count + neighbour.Cells.Count];
                for (int i = 0; i < piece.Cells.Count; i++) cells[i] = piece.Cells[i];
                for (int i = 0; i < neighbour.Cells.Count; i++) cells[piece.Cells.Count + i] = neighbour.Cells[i];
                _pieces.Remove(piece); _pieces.Remove(neighbour);
                int fusedWays = Ways(piece.Id) & Ways(neighbour.Id);
                _moveWays.Remove(neighbour.Id);
                if (fusedWays == PuzzleSnapshotRVO.AnyWay) _moveWays.Remove(piece.Id); else _moveWays[piece.Id] = fusedWays;
                piece = new JellyPieceVO(piece.Id, piece.Colour, cells);
                _pieces.Add(piece);
                changed = true;
                Changed();
                frames?.Add(Snapshot());
            }
            if (changed && _level.FreeArrowAfterFusion)
            {
                _moveWays.Remove(piece.Id);
                Changed();
                frames?.Add(Snapshot());
            }
            return changed;
        }

        // ---------- boosters ----------

        public bool FreezeClock(float seconds)
        {
            if (_level == null || Status != PuzzleStatus.Playing || seconds <= 0) return false;
            ClockFreezeSeconds += seconds;
            return true;
        }

        private readonly List<JellyPieceVO> _candidates = new();
        private readonly List<JellyPieceVO> _picked = new();
        private readonly List<JellyPieceVO> _best = new();
        private int _bestCells;

        public bool ClearDoor(int door, bool biggestFirst, bool withoutEnough)
        {
            if (_level == null || Status != PuzzleStatus.Playing || door < 0 || door >= _level.Gates.Count || _doorFrozen[door] > 0) return false;
            var stage = WaitingStage(door);
            if (stage == null) return false;
            FinishDrag();
            _candidates.Clear();
            foreach (var piece in _pieces)
                // Only a jelly that could be part of what the door waits for, unturned: a standing 3 never passes for a lying one.
                if (piece.Colour == stage.Colour && FitsInside(piece.Cells, stage.Cells) && !IsFrozen(piece.Id)) _candidates.Add(piece);
            // Tried in this order, so the first set found is made of the biggest (or smallest) pieces.
            _candidates.Sort((a, b) => biggestFirst ? b.Cells.Count.CompareTo(a.Cells.Count) : a.Cells.Count.CompareTo(b.Cells.Count));
            _picked.Clear(); _best.Clear(); _bestCells = 0;
            bool exact = PickCells(0, stage.Cells.Count);
            if (!exact && (!withoutEnough || _best.Count == 0)) return false;
            foreach (var piece in _best) RemovePiece(piece);
            _progress[door]++; _exited++;
            Thaw();
            AfterBooster();
            return true;
        }
        /// <summary>Picks candidates from <paramref name="from"/> on whose cells add up to <paramref name="left"/>; the best partial set is kept in _best.</summary>
        private bool PickCells(int from, int left)
        {
            int cells = 0;
            foreach (var piece in _picked) cells += piece.Cells.Count;
            if (cells > _bestCells) { _bestCells = cells; _best.Clear(); _best.AddRange(_picked); }
            if (left == 0) return true;
            for (int i = from; i < _candidates.Count; i++)
            {
                int size = _candidates[i].Cells.Count;
                if (size > left) continue;
                _picked.Add(_candidates[i]);
                if (PickCells(i + 1, left - size)) return true;
                _picked.RemoveAt(_picked.Count - 1);
            }
            return false;
        }

        public bool ClearColour(JellyColour colour)
        {
            if (_level == null || Status != PuzzleStatus.Playing) return false;
            // A booster never touches what is frozen: not a frozen jelly of the colour, nor a frozen door still
            // waiting for it - taking the colour away would leave that jelly, or that request, with no way out.
            foreach (var piece in _pieces) if (piece.Colour == colour && IsFrozen(piece.Id)) return false;
            for (int g = 0; g < _level.Gates.Count; g++)
            {
                if (_doorFrozen[g] <= 0) continue;
                for (int s = _progress[g]; s < _level.Gates[g].Stages.Count; s++) if (_level.Gates[g].Stages[s].Colour == colour) return false;
            }
            FinishDrag();
            bool any = false;
            for (int i = _pieces.Count - 1; i >= 0; i--)
                if (_pieces[i].Colour == colour) { RemovePiece(_pieces[i]); any = true; }
            for (int g = 0; g < _level.Gates.Count && !any; g++)
                for (int s = _progress[g]; s < _level.Gates[g].Stages.Count && !any; s++) any = _level.Gates[g].Stages[s].Colour == colour;
            if (!any) return false;
            _clearedColours.Add(colour);
            AfterBooster();
            return true;
        }

        private void RemovePiece(JellyPieceVO piece)
        {
            _pieces.Remove(piece);
            _jellyFrozen.Remove(piece.Id);
            _moveWays.Remove(piece.Id);
        }
        /// <summary>
        /// After a booster: doors move past requests for a colour a UFO took, the level is won when the
        /// board is empty, and undo starts over - a booster is not taken back.
        /// </summary>
        private void AfterBooster()
        {
            SkipClearedRequests();
            if (_pieces.Count == 0) { Status = PuzzleStatus.Won; IsTicking = false; }
            _history.Clear();
            Changed();
        }
        private void SkipClearedRequests()
        {
            if (_clearedColours.Count == 0) return;
            for (int g = 0; g < _level.Gates.Count; g++)
            {
                var stages = _level.Gates[g].Stages;
                // A request taken this way counts like a jelly gone through a door: frozen things thaw by one.
                while (_progress[g] < stages.Count && _clearedColours.Contains(stages[_progress[g]].Colour)) { _progress[g]++; Thaw(); }
            }
        }

        // Undo changes the board, never the clock. A timeout remains a failure until Restart.
#if UNITY_EDITOR
        public void ForceResult(PuzzleStatus status)
        {
            if (_level == null || Status != PuzzleStatus.Playing || status == PuzzleStatus.Playing) return;
            FinishDrag();
            if (status == PuzzleStatus.Lost) RemainingSeconds = 0;
            Status = status;
            IsTicking = false;
            Changed();
        }
#endif

        public bool Undo()
        {
            if (_dragBefore != null || _history.Count == 0 || Status == PuzzleStatus.Lost) return false;
            var before = _history.Pop();
            _progress = before.Progress;
            _doorFrozen = before.DoorFrozen; _jellyFrozen = before.JellyFrozen; _moveWays = before.MoveWays;
            _pieces = before.Pieces; _moves = before.Moves; _exited = before.Exited;
            Status = PuzzleStatus.Playing;
            IsTicking = true;
            Changed();
            return true;
        }

        private string GetWarning()
        {
            if (_level == null) return "";
            foreach (var piece in _pieces)
            {
                bool smallerStage = false;
                for (int g = 0; g < _level.Gates.Count && !smallerStage; g++)
                {
                    var stages = _level.Gates[g].Stages;
                    for (int s = _progress[g]; s < LastJudgedStage(g, stages.Count) && !smallerStage; s++)
                        smallerStage = stages[s].Colour == piece.Colour && piece.Cells.Count <= stages[s].Cells.Count;
                }
                if (!smallerStage)
                    return _level.ColourName((int)piece.Colour, piece.Colour.ToString()) + " jelly is bigger than every jelly a door waits for! You can undo.";
                // A permanent piece must fit as a translated subset of some waiting jelly to be salvageable.
                if (!FitsADoor(piece.Cells, piece.Colour))
                    return _level.ColourName((int)piece.Colour, piece.Colour.ToString()) + " shape cannot fit any door. Undo and fuse from another side.";
            }
            return "";
        }
    }
}
