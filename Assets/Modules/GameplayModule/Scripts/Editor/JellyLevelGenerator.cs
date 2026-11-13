#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
using Random = System.Random;

namespace Modules.GameplayModule.Editor
{
    /// <summary>
    /// Drafts levels the way the hand-made ones are built: a framed board with notched corners, two to four doors
    /// each waiting for a short queue of shapes, and every shape broken into small jellies - mostly single cells -
    /// scattered over the floor for the player to bring back together. Frozen jellies, arrow blocks and frozen doors
    /// come in as the level number grows. A draft is kept only when JellyLevelSolver wins it, and its clock is set
    /// from the drags that took.
    /// </summary>
    public sealed class JellyLevelGenerator
    {
        public sealed class Draft
        {
            public CD_JellyLevel Level;
            public JellyLevelSolver.Result Solve;
        }

        private static readonly JellyColour[] COLOURS =
        {
            JellyColour.Yellow, JellyColour.Red, JellyColour.Green, JellyColour.Navy, JellyColour.Pink,
            JellyColour.Purple, JellyColour.Black, JellyColour.Gray, JellyColour.Blue,
        };

        /// <summary>Shapes a door may wait for, as the hand-made levels use them: dominoes up to tetrominoes, unturned.</summary>
        private static readonly Vector2Int[][] SHAPES = BuildShapes();

        public int MinMoves { get; set; } = 14;
        public int MinMaking { get; set; } = 1;
        /// <summary>The draft is marked Super Hard instead of judged from its solution.</summary>
        public bool SuperHard { get; set; }

        /// <summary>
        /// The targets for a level number: every fiftieth level is Super Hard, every other tenth one Hard, and the rest
        /// Normal, climbing slowly over each ten - so a hard level stands out, and the one after it is a rest.
        /// </summary>
        public static JellyLevelGenerator ForLevel(int number)
        {
            int inTen = (number - 1) % 10;        // 0 rests after a hard one ... 9 is the hard one
            // The climb stops at level 151: past it a level is made harder by crates and doors, not by length.
            int lift = Mathf.Clamp((number - 51) / 25, 0, 4);
            // After level 101 brings in the map that slides, every ten has two of them, away from the hard one.
            if (number > 101 && number % 50 != 0 && (number % 10 == 3 || number % 10 == 7))
                return new JellyLevelGenerator { MinMoves = 12 + lift, MinMaking = 1, Wide = true, SolveMs = 4500 };
            if (number % 50 == 0) return new JellyLevelGenerator { MinMoves = 22, MinMaking = 4, SuperHard = true, SolveMs = 6000 };
            if (number % 10 == 0) return new JellyLevelGenerator { MinMoves = 18 + Mathf.Min(lift, 2), MinMaking = 2, Hard = true, SolveMs = 3500 };
            return new JellyLevelGenerator { MinMoves = 12 + inTen / 3 + lift, MinMaking = inTen / 4, SolveMs = 3500 };
        }
        /// <summary>The draft is marked Hard instead of judged from its solution.</summary>
        public bool Hard { get; set; }
        /// <summary>
        /// Two boards side by side, wider than the screen, joined by a bridge or two one cell high across a gap of
        /// holes; the level shows WIDE_COLUMNS of them and the map slides to the rest.
        /// </summary>
        public bool Wide { get; set; }
        public const int WIDE_COLUMNS = 9;
        public int SolveMs { get; set; } = 3000;
        /// <summary>What the solver said of each draft tried.</summary>
        public List<string> Log { get; } = new();

        /// <summary>Tries drafts from the seed until one is won by the solver within the targets; null after <paramref name="tries"/>.</summary>
        public Draft Make(int levelNumber, int seed, CD_JellyLevels list, int tries = 30)
        {
            var rng = new Random(seed);
            // A try is a draft the solver plays; boards turned down before that - a pocket, a door against a notch -
            // are cheap and do not count, up to a bound.
            for (int i = 0, built = 0; i < tries && built < tries * 60; built++)
            {
                var level = Build(rng, levelNumber);
                if (level == null || level.Validate().Count > 0) { UnityEngine.Object.DestroyImmediate(level); continue; }
                i++;
                var solver = new JellyLevelSolver { TimeLimitMs = SolveMs };
                var result = solver.Solve(level, list);
                Log.Add(result.ToString());
                if (result.Solved && result.Moves >= MinMoves && result.Making >= MinMaking)
                {
                    // A minute to read the board and three seconds a drag, as the hand-made levels run.
                    level.Seconds = Mathf.Clamp(Mathf.Round((60 + result.Moves * 3f) / 5) * 5, 75, 150);
                    level.Difficulty = SuperHard ? LevelDifficulty.SuperHard : Hard ? LevelDifficulty.Hard : LevelDifficulty.Normal;
                    // A Super Hard level is given the time a hard one needs, not more.
                    if (SuperHard) level.Seconds = Mathf.Clamp(Mathf.Round((45 + result.Moves * 3f) / 5) * 5, 90, 150);
                    return new Draft { Level = level, Solve = result };
                }
                UnityEngine.Object.DestroyImmediate(level);
            }
            return null;
        }

        private CD_JellyLevel Build(Random rng, int number)
        {
            var level = ScriptableObject.CreateInstance<CD_JellyLevel>();
            level.LevelName = "Level " + number;
            level.Width = Wide ? rng.Next(17, 22) : rng.Next(8, 11);
            level.Height = Wide ? rng.Next(8, 11) : rng.Next(9, 13);
            int W = level.Width, H = level.Height;

            // Notched corners and a stub or two in the frame, so the floor is not a plain box.
            var walls = new HashSet<Vector2Int>();
            // A wide map: a gap of holes down the middle, the full height of the board and its frame, crossed by one
            // or two bridges one cell high. While the board is built the gap counts as wall, so no door, jelly or
            // check reaches into it; it becomes holes at the end, which the map draws as open ground.
            var gap = new HashSet<Vector2Int>();
            if (Wide)
            {
                level.ViewColumns = WIDE_COLUMNS;
                int gapWidth = rng.Next(2, 5), gapStart = W / 2 - gapWidth / 2;
                var bridges = new List<int> { rng.Next(2, H - 2) };
                if (rng.NextDouble() < .5)
                {
                    int second = rng.Next(2, H - 2);
                    if (Mathf.Abs(second - bridges[0]) >= 3) bridges.Add(second);
                }
                for (int x = gapStart; x < gapStart + gapWidth; x++)
                for (int y = 0; y < H; y++)
                    if (!bridges.Contains(y)) gap.Add(new Vector2Int(x, y));
                foreach (var cell in gap) walls.Add(cell);
            }
            foreach (var corner in new[] { new Vector2Int(1, 1), new Vector2Int(W - 2, 1), new Vector2Int(1, H - 2), new Vector2Int(W - 2, H - 2) })
            {
                if (rng.NextDouble() > .45) continue;
                walls.Add(corner);
                var inX = new Vector2Int(corner.x == 1 ? 2 : W - 3, corner.y);
                var inY = new Vector2Int(corner.x, corner.y == 1 ? 2 : H - 3);
                if (rng.NextDouble() < .5) walls.Add(rng.NextDouble() < .5 ? inX : inY);
            }
            for (int s = rng.Next(0, 3); s > 0; s--)
            {
                int side = rng.Next(4);
                var cell = side switch
                {
                    0 => new Vector2Int(rng.Next(2, W - 2), 1),
                    1 => new Vector2Int(rng.Next(2, W - 2), H - 2),
                    2 => new Vector2Int(1, rng.Next(2, H - 2)),
                    _ => new Vector2Int(W - 2, rng.Next(2, H - 2)),
                };
                // A stub stands clear of the other wall along the frame: a single floor cell left between two would
                // be a slot the rim's art cannot turn into without its corners overlapping.
                var along = side < 2 ? Vector2Int.right : Vector2Int.up;
                if (walls.Contains(cell + along * 2) || walls.Contains(cell - along * 2)) continue;
                walls.Add(cell);
            }

            // Doors on different sides where it can, clear of the notches.
            // A Super Hard board is fuller and has more doors to juggle.
            int doorCount = SuperHard ? (rng.NextDouble() < .5 ? 4 : 3)
                : number >= 60 && rng.NextDouble() < .35 ? 4 : rng.NextDouble() < .55 ? 3 : 2;
            var sides = new List<JellySide> { JellySide.Top, JellySide.Bottom, JellySide.Left, JellySide.Right }.OrderBy(_ => rng.Next()).ToList();
            // A wide map has a door at each end, so both islands are played.
            if (Wide) sides = new List<JellySide> { JellySide.Left, JellySide.Right }.Concat(sides.Where(s => s is JellySide.Top or JellySide.Bottom)).ToList();
            var colours = COLOURS.OrderBy(_ => rng.Next()).Take(rng.Next(4, 7)).ToList();
            int floorCells = (W - 2) * (H - 2) - walls.Count;
            int target = (int)(floorCells * ((SuperHard ? .52 : Hard ? .44 : Wide ? .24 : .36) + rng.NextDouble() * .12));
            int perDoor = Mathf.Max(6, target / doorCount);
            for (int d = 0; d < doorCount; d++)
            {
                var side = sides[d % 4];
                int length = side is JellySide.Top or JellySide.Bottom ? W : H;
                int size = rng.NextDouble() < .6 ? 3 : rng.NextDouble() < .5 ? 2 : 4;
                var door = new JellyDoorCVO { Name = "Door " + (d + 1), Side = side, Size = size };
                // A second door on a side keeps away from the first.
                var sameSide = level.Doors.Where(o => o.Side == side).ToList();
                bool placed = false;
                for (int attempt = 0; attempt < 20 && !placed; attempt++)
                {
                    door.Start = rng.Next(1, Mathf.Max(2, length - size));
                    if (door.Start + size > length - 1) continue;
                    if (sameSide.Any(o => door.Start < o.Start + o.Size + 1 && o.Start < door.Start + size + 1)) continue;
                    // The floor in front of the door, and two cells either side of its mouth, stays clear of wall, so the
                    // door never sits against a notch or a stub, nor one frame cell short of one - the rim's corners
                    // there pile onto each other.
                    var inward = -JellyGateCVOStep(side);
                    var along = side is JellySide.Top or JellySide.Bottom ? Vector2Int.right : Vector2Int.up;
                    var cellsOf = level.DoorCells(door);
                    var mouth = cellsOf.Select(c => c + inward).Concat(new[] { cellsOf.First() + inward - along, cellsOf.Last() + inward + along, cellsOf.First() + inward - along * 2, cellsOf.Last() + inward + along * 2 });
                    if (mouth.Any(walls.Contains)) continue;
                    placed = true;
                }
                if (!placed) { UnityEngine.Object.DestroyImmediate(level); return null; }
                int cells = 0;
                while (cells < perDoor || door.Queue.Count < 2)
                {
                    if (door.Queue.Count >= 4) break;
                    var fitting = SHAPES.Where(s => Across(s, side) <= size).ToList();
                    var shape = fitting[rng.Next(fitting.Count)];
                    door.Queue.Add(new JellyShapeCVO { Name = "Jelly", Colour = colours[rng.Next(colours.Count)], Cells = shape.ToList() });
                    cells += shape.Length;
                }
                // A frozen door belongs to the hard levels; a normal one gets it now and then.
                if (number >= 55 && d > 0 && rng.NextDouble() < (Hard || SuperHard ? .45 : .15))
                {
                    door.Frozen = true;
                    door.ThawAfter = rng.Next(2, 5);
                }
                level.Doors.Add(door);
            }
            // The frame cells a door opens are not wall.
            var doorCells = new HashSet<Vector2Int>(level.Doors.SelectMany(level.DoorCells));
            level.Walls = walls.Where(c => !doorCells.Contains(c) && !gap.Contains(c)).ToList();
            level.Holes = gap.ToList();
            // No floor cell closed in on three sides by wall or frame: such a pocket is where the rim's corner pieces
            // pile onto each other.
            for (int x = 1; x < W - 1; x++)
            for (int y = 1; y < H - 1; y++)
            {
                var c = new Vector2Int(x, y);
                if (walls.Contains(c)) continue;
                int closed = 0;
                foreach (var step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var n = c + step;
                    bool frame = n.x == 0 || n.y == 0 || n.x == W - 1 || n.y == H - 1;
                    if ((walls.Contains(n) && !gap.Contains(n)) || (frame && !doorCells.Contains(n) && !gap.Contains(n))) closed++;
                }
                if (closed >= 3) { UnityEngine.Object.DestroyImmediate(level); return null; }
            }

            // Every queued shape broken into jellies, mostly single cells, and scattered over the free floor.
            var free = new List<Vector2Int>();
            for (int x = 1; x < W - 1; x++)
            for (int y = 1; y < H - 1; y++)
            {
                var c = new Vector2Int(x, y);
                if (!walls.Contains(c)) free.Add(c);
            }
            var taken = new HashSet<Vector2Int>();
            var pieces = new List<(JellyColour colour, List<Vector2Int> cells)>();
            foreach (var stage in level.Doors.SelectMany(d => d.Queue))
                foreach (var part in Break(rng, stage.Cells))
                    pieces.Add((stage.Colour, part));
            foreach (var (colour, cells) in pieces.OrderByDescending(p => p.cells.Count))
            {
                bool placed = false;
                foreach (var spot in free.OrderBy(_ => rng.Next()))
                {
                    var offset = spot - cells[0];
                    var moved = cells.Select(c => c + offset).ToList();
                    if (moved.Any(c => c.x < 1 || c.y < 1 || c.x > W - 2 || c.y > H - 2 || walls.Contains(c) || taken.Contains(c))) continue;
                    foreach (var c in moved) taken.Add(c);
                    level.Shapes.Add(new JellyShapeCVO { Name = "Jelly", Colour = colour, Cells = moved });
                    placed = true;
                    break;
                }
                if (!placed) { UnityEngine.Object.DestroyImmediate(level); return null; }
            }

            // From level 53 on, a jelly or two is frozen; from 57 on, some move only one way. A normal level takes one
            // of the two at most, so the hard ones are where they meet.
            bool tough = Hard || SuperHard;
            bool frozenJellies = number >= 53 && (tough || rng.NextDouble() < .5);
            if (frozenJellies)
                foreach (var shape in level.Shapes.OrderBy(_ => rng.Next()).Take(rng.Next(1, tough ? 3 : 2)))
                {
                    shape.Frozen = true;
                    shape.ThawAfter = rng.Next(1, 4);
                }
            if (number >= 57 && (tough ? rng.NextDouble() < .4 : !frozenJellies && rng.NextDouble() < .3))
                foreach (var shape in level.Shapes.Where(s => !s.Frozen).OrderBy(_ => rng.Next()).Take(rng.Next(1, 4)))
                {
                    shape.ArrowBlock = true;
                    shape.ArrowAxis = rng.NextDouble() < .5 ? JellyAxis.Horizontal : JellyAxis.Vertical;
                }
            // After level 151 brings in the wooden crates, about half the boards have a few, on free floor and never
            // in front of a door, where one would close the way out before the first fuse.
            if (number > 151 && !Wide && rng.NextDouble() < (tough ? .7 : .5))
            {
                var mouths = new HashSet<Vector2Int>(level.Doors.SelectMany(d => level.DoorCells(d).Select(c => c - JellyGateCVOStep(d.Side))));
                foreach (var cell in free.Where(c => !taken.Contains(c) && !mouths.Contains(c)).OrderBy(_ => rng.Next()).Take(rng.Next(2, tough ? 6 : 5)))
                    level.Crates.Add(cell);
            }
            return level;
        }

        private static Vector2Int JellyGateCVOStep(JellySide side) => JellyGateCVO.DirectionOf(side);

        private static int Across(Vector2Int[] shape, JellySide side) => side is JellySide.Left or JellySide.Right
            ? shape.Max(c => c.y) - shape.Min(c => c.y) + 1
            : shape.Max(c => c.x) - shape.Min(c => c.x) + 1;

        /// <summary>A shape cut into joined parts of one to three cells, single cells the most; each keeps its place in the shape.</summary>
        private static List<List<Vector2Int>> Break(Random rng, List<Vector2Int> shape)
        {
            var left = new List<Vector2Int>(shape);
            var parts = new List<List<Vector2Int>>();
            while (left.Count > 0)
            {
                int want = rng.NextDouble() < .55 ? 1 : rng.NextDouble() < .75 ? 2 : 3;
                var part = new List<Vector2Int> { left[rng.Next(left.Count)] };
                left.Remove(part[0]);
                while (part.Count < want)
                {
                    var next = left.FirstOrDefault(c => part.Any(p => Mathf.Abs(p.x - c.x) + Mathf.Abs(p.y - c.y) == 1));
                    if (!left.Contains(next) || !part.Any(p => Mathf.Abs(p.x - next.x) + Mathf.Abs(p.y - next.y) == 1)) break;
                    part.Add(next);
                    left.Remove(next);
                }
                parts.Add(part);
            }
            return parts;
        }

        private static Vector2Int[][] BuildShapes()
        {
            string[] drawn =
            {
                "XX", "X|X", "XXX", "X|X|X", "XX|X.", "XX|.X", "X.|XX", ".X|XX",
                "XXXX", "X|X|X|X", "XX|XX", "XXX|X..", "XXX|..X", "X..|XXX", "..X|XXX",
                "XX|X.|X.", "XX|.X|.X", "X.|X.|XX", ".X|.X|XX", "XXX|.X.", ".X.|XXX", "X.|XX|X.", ".X|XX|.X",
                "XX.|.XX", ".XX|XX.", "X.|XX|.X", ".X|XX|X.",
            };
            return drawn.Select(text =>
            {
                var rows = text.Split('|');
                var cells = new List<Vector2Int>();
                for (int r = 0; r < rows.Length; r++)
                for (int x = 0; x < rows[r].Length; x++)
                    if (rows[r][x] == 'X') cells.Add(new Vector2Int(x, rows.Length - 1 - r));
                return cells.ToArray();
            }).ToArray();
        }
    }
}
#endif
