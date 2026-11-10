#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Editor
{
    /// <summary>
    /// Plays a level with the game's own rules (JellyPuzzleModel) until it is won: a search over every place each
    /// jelly can be dragged to and let go, the ones that fuse or send a jelly through a door tried first. Says whether
    /// the level can be won, in how many drags, and how much searching that took - a measure of how hard it is.
    /// Editor only; the clock is not run.
    /// </summary>
    public sealed class JellyLevelSolver
    {
        public sealed class Result
        {
            public bool Solved;
            /// <summary>Drags of the solution found; not always the shortest.</summary>
            public int Moves;
            /// <summary>Boards looked at before the solution, or before giving up.</summary>
            public int Explored;
            /// <summary>Drags of the solution that fused or sent a jelly through a door, against those that only made room.</summary>
            public int Scoring, Making;
            /// <summary>The search ran out of its budget: the level may still be winnable.</summary>
            public bool OutOfBudget;
            public List<(Vector2Int from, Vector2Int to)> Solution = new();
            public override string ToString() => Solved
                ? $"solved in {Moves} drags ({Scoring} scoring, {Making} making room), {Explored} boards looked at"
                : OutOfBudget ? $"not solved within {Explored} boards" : $"cannot be won ({Explored} boards)";
        }

        private readonly JellyPuzzleModel _model = new();
        private readonly HashSet<string> _seen = new();
        private readonly List<(Vector2Int from, Vector2Int to)> _path = new();
        private readonly List<bool> _pathScores = new();
        private JellyLevelCVO _level;
        private HashSet<Vector2Int> _floor;
        private int _budget, _explored;
        private Result _result;

        public int Budget { get; set; } = 60000;
        /// <summary>A search deeper than this many drags gives up on that line.</summary>
        public int MaxDepth { get; set; } = 80;
        /// <summary>The most drags in a row that only make room, on the last pass.</summary>
        public int MaxIdle { get; set; } = 4;
        /// <summary>The search gives up after this many milliseconds, so the Editor is never held for long.</summary>
        public int TimeLimitMs { get; set; } = 8000;
        /// <summary>How far a drag that only makes room is tried, in steps.</summary>
        public int NearSteps { get; set; } = 2;
        private int _maxIdle;
        private readonly HashSet<Vector2Int> _nearDoor = new();
        private readonly HashSet<Vector2Int> _doorCells = new();
        private bool _pulls;
        private readonly System.Diagnostics.Stopwatch _clock = new();

        public Result Solve(CD_JellyLevel level, CD_JellyLevels list)
        {
            _level = level.CreateLevel(list != null ? list.Palette : null);
            _model.DefaultPullReach = list != null ? list.DefaultPullReach : 1;
            _model.ReleaseFuseLookBack = list != null ? list.ReleaseFuseLookBack : 0;
            _model.PathFinding = true;
            _model.RecordFrames = false;
            _model.BiggestFuseFirst = true;
            _model.Start(_level);
            _floor = new HashSet<Vector2Int>();
            var blocked = new HashSet<Vector2Int>(_level.Walls.Concat(_level.Holes));
            for (int x = 0; x < _level.Width; x++)
            for (int y = 0; y < _level.Height; y++)
            {
                var c = new Vector2Int(x, y);
                if (!blocked.Contains(c)) _floor.Add(c);
            }
            // The floor in front of a door, as far as it pulls a jelly in, and a step more.
            _nearDoor.Clear(); _doorCells.Clear();
            _pulls = false;
            foreach (var gate in _level.Gates)
            {
                int pull = gate.PullReach >= 0 ? gate.PullReach : _model.DefaultPullReach;
                _pulls |= pull > 0;
                foreach (var cell in gate.Cells)
                {
                    _doorCells.Add(cell);
                    for (int k = 1; k <= pull + 1; k++) _nearDoor.Add(cell - gate.Direction * k);
                }
            }
            _budget = Budget; _explored = 0;
            _clock.Restart();
            _result = new Result();
            // Few drags that only make room are allowed in a row at first, more on each pass, so a short
            // solution is found before a long wander.
            for (_maxIdle = 0; _maxIdle <= MaxIdle && !_result.Solved && _budget > 0; _maxIdle++)
            {
                _seen.Clear(); _path.Clear(); _pathScores.Clear();
                _model.Start(_level);
                Search(0, 0);
            }
            _result.Explored = _explored;
            if (!_result.Solved) _result.OutOfBudget = _budget <= 0;
            return _result;
        }

        private bool Search(int depth, int idle)
        {
            var snapshot = _model.Snapshot();
            if (snapshot.Status == PuzzleStatus.Won)
            {
                _result.Solved = true;
                _result.Moves = _path.Count;
                _result.Solution = new List<(Vector2Int, Vector2Int)>(_path);
                _result.Scoring = _pathScores.Count(s => s);
                _result.Making = _pathScores.Count - _result.Scoring;
                return true;
            }
            if (_clock.ElapsedMilliseconds > TimeLimitMs) _budget = 0;
            if (depth >= MaxDepth || _budget-- <= 0) return false;
            _explored++;
            if (!_seen.Add(Key(snapshot))) return false;

            // Every move from here, scored by what it achieved; the same board reached twice is tried once.
            var children = new List<(Move move, int score)>();
            var reached = new HashSet<string>();
            int pieces = snapshot.Pieces.Count, exited = snapshot.Exited;
            foreach (var piece in snapshot.Pieces.ToList())
            {
                if (snapshot.JellyFrozen(piece.Id) > 0) continue;
                foreach (var move in Moves(snapshot, piece))
                {
                    int undos = Apply(move);
                    if (undos == 0) continue;
                    var after = _model.Snapshot();
                    var key = Key(after);
                    if (!Lost(after) && reached.Add(key) && !_seen.Contains(key))
                    {
                        int score = (after.Exited - exited) * 100 + (pieces - after.Pieces.Count) * 10
                            + (after.Status == PuzzleStatus.Won ? 100000 : 0);
                        children.Add((move, score));
                    }
                    TakeBack(undos);
                }
            }
            children.Sort((a, b) => b.score.CompareTo(a.score));
            foreach (var (move, score) in children)
            {
                if (score <= 0 && idle >= _maxIdle) continue;
                int undos = Apply(move);
                _path.Add((move.From, move.To)); _pathScores.Add(score > 0);
                if (Search(depth + 1, score > 0 ? 0 : idle + 1)) return true;
                _path.RemoveAt(_path.Count - 1); _pathScores.RemoveAt(_pathScores.Count - 1);
                TakeBack(undos);
                if (_budget <= 0) return false;
            }
            return false;
        }

        /// <summary>One move: the jelly at From let go with its first cell at To - dropped straight there when the
        /// route was checked here, or dragged when the door's pull has to play out as in the game.</summary>
        private readonly struct Move
        {
            public readonly Vector2Int From, To;
            public readonly bool Drag;
            /// <summary>After reaching To, a step out this way through a door: one drag in the game.</summary>
            public readonly Vector2Int Out;
            public Move(Vector2Int from, Vector2Int to, bool drag, Vector2Int @out = default) { From = from; To = to; Drag = drag; Out = @out; }
        }

        /// <summary>Plays the move; returns how many undos take it back.</summary>
        private int Apply(Move move)
        {
            int before = _model.Snapshot().Moves;
            if (move.Drag) _model.Drag(move.From, move.To, true);
            else _model.Drop(move.From, move.To);
            int undos = _model.Snapshot().Moves - before;
            if (move.Out != Vector2Int.zero && undos > 0)
            {
                int middle = _model.Snapshot().Moves;
                _model.Drop(move.To, move.To + move.Out);
                if (_model.Snapshot().Moves > middle) undos++;
            }
            return undos;
        }
        private void TakeBack(int undos)
        {
            for (int i = 0; i < undos; i++) _model.Undo();
        }

        private static readonly Vector2Int[] STEPS = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        /// <summary>
        /// The moves worth trying for one jelly: every place its route reaches that touches a jelly of its colour, lies
        /// near a door, or is a short step away - the moves that make room - and a step out through each door.
        /// The route is found once for all of them, so each place is then reached by a drop instead of a walk.
        /// </summary>
        private IEnumerable<Move> Moves(PuzzleSnapshotRVO snapshot, JellyPieceVO piece)
        {
            var taken = new HashSet<Vector2Int>();
            var kin = new HashSet<Vector2Int>();
            foreach (var other in snapshot.Pieces)
            {
                if (other.Id == piece.Id) continue;
                foreach (var c in other.Cells)
                {
                    taken.Add(c);
                    if (other.Colour == piece.Colour) kin.Add(c);
                }
            }
            var anchor = piece.Cells[0];
            int ways = snapshot.MoveWays(piece.Id);
            bool Free(Vector2Int offset)
            {
                foreach (var c in piece.Cells)
                {
                    var moved = c + offset;
                    if (!_floor.Contains(moved) || _doorCells.Contains(moved) || taken.Contains(moved)) return false;
                }
                return true;
            }
            // Every offset the jelly can walk to, the way the game leads it: one free step at a time.
            var reach = new HashSet<Vector2Int> { Vector2Int.zero };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(Vector2Int.zero);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                foreach (var step in STEPS)
                {
                    if (step.x != 0 && (ways & PuzzleSnapshotRVO.Horizontal) == 0) continue;
                    if (step.y != 0 && (ways & PuzzleSnapshotRVO.Vertical) == 0) continue;
                    var next = at + step;
                    if (reach.Contains(next) || !Free(next)) continue;
                    reach.Add(next);
                    queue.Enqueue(next);
                }
            }
            foreach (var offset in reach)
            {
                if (offset == Vector2Int.zero) continue;
                bool useful = Mathf.Abs(offset.x) + Mathf.Abs(offset.y) <= NearSteps, nearDoor = false;
                foreach (var c in piece.Cells)
                {
                    var moved = c + offset;
                    nearDoor |= _nearDoor.Contains(moved);
                    if (kin.Contains(moved + Vector2Int.up) || kin.Contains(moved + Vector2Int.down)
                        || kin.Contains(moved + Vector2Int.left) || kin.Contains(moved + Vector2Int.right)) useful = true;
                }
                // In front of a door that pulls, let go the game's way, so the door draws it in as it would.
                if (useful || nearDoor) yield return new Move(anchor, anchor + offset, nearDoor && _pulls);
                // Right in front of a door, the same drag carries on out through it.
                foreach (var gate in _level.Gates)
                    if (piece.Cells.Any(c => _doorCells.Contains(c + offset + gate.Direction) && gate.Cells.Contains(c + offset + gate.Direction)))
                        yield return new Move(anchor, anchor + offset, false, gate.Direction);
            }
            foreach (var gate in _level.Gates) yield return new Move(anchor, anchor + gate.Direction, false);
        }

        /// <summary>
        /// A board that can no longer be won: a jelly that fits inside none of the requests still to come for its
        /// colour, as it stands - jellies are never turned - or more cells of a colour than those requests take.
        /// </summary>
        private bool Lost(PuzzleSnapshotRVO snapshot)
        {
            var cellsLeft = new Dictionary<JellyColour, int>();
            var stages = new List<JellyPieceVO>();
            for (int g = 0; g < _level.Gates.Count; g++)
            {
                var gate = _level.Gates[g];
                for (int s = snapshot.DoorProgress[g]; s < gate.Stages.Count; s++)
                {
                    var stage = gate.Stages[s];
                    stages.Add(stage);
                    cellsLeft[stage.Colour] = (cellsLeft.TryGetValue(stage.Colour, out var n) ? n : 0) + stage.Cells.Count;
                }
            }
            var cellsHeld = new Dictionary<JellyColour, int>();
            foreach (var piece in snapshot.Pieces)
            {
                cellsHeld[piece.Colour] = (cellsHeld.TryGetValue(piece.Colour, out var n) ? n : 0) + piece.Cells.Count;
                bool fits = false;
                foreach (var stage in stages)
                    if (stage.Colour == piece.Colour && FitsInside(piece.Cells, stage.Cells)) { fits = true; break; }
                if (!fits) return true;
            }
            foreach (var held in cellsHeld)
                if (!cellsLeft.TryGetValue(held.Key, out var left) || held.Value > left) return true;
            return false;
        }

        /// <summary>Whether the cells, moved but not turned, sit inside the shape somewhere.</summary>
        private static bool FitsInside(IReadOnlyList<Vector2Int> cells, IReadOnlyList<Vector2Int> shape)
        {
            if (cells.Count > shape.Count) return false;
            var set = new HashSet<Vector2Int>(shape);
            foreach (var anchor in shape)
            {
                var offset = anchor - cells[0];
                bool all = true;
                foreach (var c in cells) if (!set.Contains(c + offset)) { all = false; break; }
                if (all) return true;
            }
            return false;
        }

        private static string Key(PuzzleSnapshotRVO snapshot)
        {
            var parts = snapshot.Pieces
                .Select(p => (int)p.Colour + ":" + string.Join(";", p.Cells.OrderBy(c => c.x).ThenBy(c => c.y).Select(c => c.x + "," + c.y))
                    + (snapshot.JellyFrozen(p.Id) > 0 ? "f" + snapshot.JellyFrozen(p.Id) : "") + "w" + snapshot.MoveWays(p.Id))
                .OrderBy(s => s);
            var key = new StringBuilder();
            foreach (var p in parts) key.Append(p).Append('|');
            key.Append('#');
            foreach (var p in snapshot.DoorProgress) key.Append(p).Append(',');
            foreach (var f in snapshot.DoorFrozen) key.Append(f).Append(',');
            foreach (var c in snapshot.Crates.OrderBy(c => c.Key.x).ThenBy(c => c.Key.y)) key.Append('c').Append(c.Key.x).Append(',').Append(c.Key.y).Append('=').Append(c.Value);
            return key.ToString();
        }
    }
}
#endif
