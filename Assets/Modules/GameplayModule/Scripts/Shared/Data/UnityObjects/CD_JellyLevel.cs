using System;
using System.Collections.Generic;
using System.Linq;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    [CreateAssetMenu(fileName="CD_JellyLevel",menuName="Jelly Fuse/Level")]
    public sealed class CD_JellyLevel : ScriptableObject
    {
        public const int MAX_DOOR = 4;
        public string LevelName = "First fusion";
        [TextArea] public string Hint;
        [Range(3,20)] public int Width = 7;
        [Range(3,20)] public int Height = 7;
        [Min(1)] public float Seconds = 120;
        [Tooltip("Hard or Super Hard is shown under the timer while the level is played. Normal: nothing is shown.")]
        public LevelDifficulty Difficulty;
        [Tooltip("Coins the player gets for winning this level. -1 uses CD_JellyLevels' Default Coin Reward.")]
        [Min(-1)] public int OwnCoinReward = -1;
        [Tooltip("On: an arrow-block jelly becomes an ordinary jelly after it successfully fuses. Off: the fused jelly keeps the arrow movement restriction.")]
        public bool FreeArrowAfterFusion;
        public List<JellyColourCVO> ExtraColours = new();
        [SerializeField, HideInInspector] private int _nextColourId = 3;
        public JellyColourCVO AddColour()
        {
            _nextColourId = Math.Max(_nextColourId, ExtraColours.Select(c => c.Id).DefaultIfEmpty(2).Max() + 1);
            var colour = new JellyColourCVO { Id = _nextColourId, Name = "Colour " + _nextColourId };
            _nextColourId++;
            ExtraColours.Add(colour);
            return colour;
        }
        public List<Vector2Int> Walls = new();
        public List<Vector2Int> Holes = new();
        public List<JellyShapeCVO> Shapes = new();
        public List<JellyDoorCVO> Doors = new();
        [Tooltip("The moves this level's tutorial makes the player do, in order. Empty: no tutorial.")]
        public List<JellyTutorialStepCVO> Tutorial = new();
        [Tooltip("Exits from before doors. ConvertOldExits turns them into Doors; until then the game converts them as it loads.")]
        [HideInInspector] public List<JellyTargetCVO> Targets = new();

        /// <summary>How many cells long a side is: the width for the top and bottom, the height for the left and right.</summary>
        public int SideLength(JellySide side) => side is JellySide.Bottom or JellySide.Top ? Width : Height;
        public Vector2Int[] DoorCells(JellyDoorCVO door) => JellyGateCVO.EdgeCells(door.Side, door.Start, door.Size, Width, Height);
        /// <summary>
        /// Whether the cell is in the wall round the board: the outermost row or column. Doors open in it,
        /// and everything else of it is wall, whether it was painted or not.
        /// </summary>
        public bool IsFrame(Vector2Int c) => c.x == 0 || c.y == 0 || c.x == Width - 1 || c.y == Height - 1;
        /// <summary>The cells of the wall round the board that no door opens.</summary>
        public IEnumerable<Vector2Int> FrameWalls()
        {
            var doors = new HashSet<Vector2Int>(PlayedDoors().SelectMany(DoorCells));
            for (int x = 0; x < Width; x++) for (int y = 0; y < Height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (IsFrame(cell) && !doors.Contains(cell)) yield return cell;
            }
        }

        // ---------- old exits ----------

        public bool HasOldExits => Targets.Count > 0;
        /// <summary>
        /// Turns every old exit into a door on the same side, covering the rows (or columns) its
        /// jellies spanned, and taking the same jellies in the same order. Exits that shared rows on
        /// one side become one door. Old exits are then removed.
        /// </summary>
        public void ConvertOldExits()
        {
            Doors.AddRange(ConvertedExits());
            Targets.Clear();
        }
        private List<JellyDoorCVO> ConvertedExits()
        {
            // Old exits on one side could share rows; a door cannot, so those become one door taking
            // the first exit's jellies, then the next's.
            var doors = new List<JellyDoorCVO>();
            foreach (var door in ExitsAsDoors())
            {
                var shared = doors.FirstOrDefault(d => d.Side == door.Side && door.Start < d.Start + d.Size && d.Start < door.Start + door.Size
                    && Mathf.Max(d.Start + d.Size, door.Start + door.Size) - Mathf.Min(d.Start, door.Start) <= MAX_DOOR);
                if (shared == null) { doors.Add(door); continue; }
                int end = Mathf.Max(shared.Start + shared.Size, door.Start + door.Size);
                shared.Start = Mathf.Min(shared.Start, door.Start); shared.Size = end - shared.Start;
                shared.Queue.AddRange(door.Queue);
            }
            return doors;
        }
        private IEnumerable<JellyDoorCVO> ExitsAsDoors()
        {
            foreach (var target in Targets)
            {
                var direction = ExitDirection(target.Cells);
                if (direction == Vector2Int.zero) continue;
                var side = direction == Vector2Int.down ? JellySide.Bottom : direction == Vector2Int.up ? JellySide.Top
                    : direction == Vector2Int.left ? JellySide.Left : JellySide.Right;
                // The door spans every row (or column) any of its jellies did: queued shapes sat
                // relative to the first one's bottom-left.
                bool sideways = direction.x != 0;
                var origin = new Vector2Int(target.Cells.Min(c => c.x), target.Cells.Min(c => c.y));
                var lanes = target.Cells.Concat(target.NextGates.SelectMany(n => n.Cells.Select(c => c + origin))).Select(c => sideways ? c.y : c.x).ToList();
                int first = lanes.Min(), size = Mathf.Clamp(lanes.Max() - first + 1, 1, MAX_DOOR);
                var door = new JellyDoorCVO { Name = target.Name, Side = side, Start = Mathf.Clamp(first, 1, Mathf.Max(1, SideLength(side) - 1 - size)), Size = size };
                door.Queue.Add(new JellyShapeCVO { Name = target.Name, Colour = target.Colour, Cells = Local(target.Cells) });
                foreach (var next in target.NextGates)
                    door.Queue.Add(new JellyShapeCVO { Name = next.Name, Colour = next.Colour, Cells = Local(next.Cells) });
                yield return door;
            }
        }
        private Vector2Int ExitDirection(List<Vector2Int> cells)
        {
            if (cells.Count == 0) return Vector2Int.zero;
            if (cells.All(c => c.y >= 0 && c.y < Height))
            {
                if (cells.All(c => c.x < 0)) return Vector2Int.left;
                if (cells.All(c => c.x >= Width)) return Vector2Int.right;
            }
            if (cells.All(c => c.x >= 0 && c.x < Width))
            {
                if (cells.All(c => c.y < 0)) return Vector2Int.down;
                if (cells.All(c => c.y >= Height)) return Vector2Int.up;
            }
            return Vector2Int.zero;
        }
        /// <summary>The cells moved so their bottom-left is (0, 0).</summary>
        public static List<Vector2Int> Local(IEnumerable<Vector2Int> cells)
        {
            var list = cells.ToList();
            if (list.Count == 0) return list;
            var origin = new Vector2Int(list.Min(c => c.x), list.Min(c => c.y));
            return list.Select(c => c - origin).ToList();
        }
        /// <summary>The doors the game plays: the authored ones, plus old exits not converted yet.</summary>
        private List<JellyDoorCVO> PlayedDoors() => HasOldExits ? Doors.Concat(ConvertedExits()).ToList() : Doors;

        /// <summary>The playable snapshot. The palette's colours come first; this level's Extra colours after.</summary>
        public JellyLevelCVO CreateLevel(CD_JellyPalette palette = null)
        {
            var errors=Validate();
            if(errors.Count>0)throw new InvalidOperationException(name+": "+string.Join("; ",errors));
            var gates = PlayedDoors().Select(d => new JellyGateCVO(d.Side, d.Start, d.Size, Width, Height,
                d.Queue.Select(s => new JellyPieceVO(0, s.Colour, Local(s.Cells))), d.Name, d.FitToJelly, d.OwnPullReach, d.Frozen ? Mathf.Max(1, d.ThawAfter) : 0));
            // The wall round the board is played as wall even where nobody painted it.
            var walls = Walls.Concat(FrameWalls()).Where(c => !Holes.Contains(c)).Distinct();
            return new JellyLevelCVO(Width,Height,Seconds,walls,
                Shapes.Select((p,i)=>new JellyPieceVO(i+1,p.Colour,p.Cells)),
                gates,Holes,LevelName,Hint,(palette != null ? palette.AsLevelColours() : Enumerable.Empty<JellyColourCVO>()).Concat(ExtraColours),
                Shapes.Select((p, i) => (Id: i + 1, Shape: p)).Where(e => e.Shape.Frozen).ToDictionary(e => e.Id, e => Mathf.Max(1, e.Shape.ThawAfter)),
                Shapes.Select((p, i) => (Id: i + 1, Shape: p)).Where(e => e.Shape.ArrowBlock).ToDictionary(e => e.Id, e => e.Shape.ArrowAxis),
                FreeArrowAfterFusion, Difficulty);
        }
        /// <summary>One thing wrong with the level: an English text with {0}-style places, and what goes in them.</summary>
        public readonly struct Issue
        {
            public readonly string Format;
            public readonly object[] Values;
            public Issue(string format, params object[] values) { Format = format; Values = values; }
            public override string ToString() => string.Format(Format, Values);
        }
        /// <summary>The issue a colour whose jellies and doors hold different cell counts reports; the editor shows it in its balance rows.</summary>
        public const string BALANCE_ISSUE = "{0}: the jellies on the board have {1} cells, but its doors take {2}. Every cell must leave by a door.";

        /// <summary>Everything that keeps the level from being played, in plain English.</summary>
        public List<string> Validate() => Issues().Select(i => i.ToString()).Distinct().ToList();
        /// <summary>The same as Validate, kept as text and values so an editor can show it translated.</summary>
        public List<Issue> Issues()
        {
            var errors=new List<Issue>();
            void Add(string format, params object[] values) => errors.Add(new Issue(format, values));
            // Cells are counted from 1 at the bottom left, the way the editor's grid reads.
            string At(Vector2Int c) => (c.x + 1) + ", " + (c.y + 1);
            var doors=PlayedDoors();
            bool Inside(Vector2Int c)=>c.x>=0&&c.x<Width&&c.y>=0&&c.y<Height;
            if(Width<3||Width>20||Height<3||Height>20)Add("The board must be 3 to 20 cells wide and tall.");
            if(Seconds<=0||float.IsNaN(Seconds)||float.IsInfinity(Seconds))Add("The time limit must be more than 0 seconds.");
            if(Shapes.Count==0)Add("There is no jelly on the board. Place at least one.");
            if(doors.Count==0)Add("There is no door. Place at least one in the wall round the board.");
            if (ExtraColours.Any(c => c.Id < 3 || c.Id >= 100 || string.IsNullOrWhiteSpace(c.Name)) || ExtraColours.Select(c => c.Id).Distinct().Count() != ExtraColours.Count)
                Add("An extra colour has no name, or two extra colours share an ID.");
            var stages = doors.SelectMany(d => d.Queue).ToList();
            foreach (var colour in Shapes.Select(p => p.Colour).Concat(stages.Select(s => s.Colour)).Distinct())
                if (!Enum.IsDefined(typeof(JellyColour), colour) && !ExtraColours.Any(c => c.Id == (int)colour)) Add("Colour number {0} is used but not defined. Pick another colour for its jellies.", (int)colour);
            var occupied=new HashSet<Vector2Int>();
            foreach(var c in Walls.Concat(Holes))if(!Inside(c)||!occupied.Add(c))Add("A wall or empty cell at {0} is off the board or painted twice. Erase it.", At(c));
            var frame=new HashSet<Vector2Int>(FrameWalls());
            foreach(var p in Shapes)
            {
                if(!Connected(p.Cells))Add("{0}: its cells are not all joined side by side. Every jelly must be one piece.", p.Name);
                if(p.Cells.Any(frame.Contains))Add("{0}: it sits in the wall round the board. Move it inside.", p.Name);
                foreach(var c in p.Cells)if(!Inside(c)||!occupied.Add(c))Add("{0}: cell {1} is off the board or already taken by a wall or another jelly.", p.Name, At(c));
            }
            var doorCells=new HashSet<(JellySide, Vector2Int)>();
            foreach(var door in doors)
            {
                if(door.Size<1||door.Size>MAX_DOOR){Add("{0}: a door is 1 to {1} cells wide.", door.Name, MAX_DOOR);continue;}
                if(door.Start<1||door.Start+door.Size>SideLength(door.Side)-1){Add("{0}: it runs into a corner. Move it along its side.", door.Name);continue;}
                foreach(var c in DoorCells(door))
                {
                    if(!doorCells.Add((door.Side,c)))Add("{0}: it overlaps another door on the same side. Move one of them.", door.Name);
                    if(Walls.Contains(c))Add("{0}: a wall stands in the door at {1}. Erase the wall or move the door.", door.Name, At(c));
                    if(Holes.Contains(c))Add("{0}: an empty cell is in the door at {1}. Erase it or move the door.", door.Name, At(c));
                }
                if(door.Queue.Count==0)Add("{0}: it waits for no jelly. Add one to its queue.", door.Name);
                bool sideways=door.Side is JellySide.Left or JellySide.Right;
                foreach(var stage in door.Queue)
                {
                    if(!Connected(stage.Cells)){Add("{0}: {1} in its queue is not one joined piece.", door.Name, stage.Name);continue;}
                    int across=sideways?stage.Cells.Max(c=>c.y)-stage.Cells.Min(c=>c.y)+1:stage.Cells.Max(c=>c.x)-stage.Cells.Min(c=>c.x)+1;
                    if(across>door.Size)Add("{0}: {1} in its queue is {2} cells wide and cannot pass a {3}-wide door. Widen the door.", door.Name, stage.Name, across, door.Size);
                }
            }
            foreach(var colour in Shapes.Select(p=>p.Colour).Concat(stages.Select(s=>s.Colour)).Distinct())
            {
                int start=Shapes.Where(p=>p.Colour==colour).Sum(p=>p.Cells.Count), goal=stages.Where(s=>s.Colour==colour).Sum(s=>s.Cells.Count);
                if(start!=goal)Add(BALANCE_ISSUE, colour, start, goal);
            }
            return errors;
        }
        /// <summary>Walls standing where a door opens. A door clears the wall in front of it, so the editor takes these away.</summary>
        public List<Vector2Int> WallsInDoors()
        {
            var doors = new HashSet<Vector2Int>(PlayedDoors().SelectMany(DoorCells));
            return Walls.Where(doors.Contains).ToList();
        }
        private static bool Connected(List<Vector2Int> cells)
        {
            if(cells.Count==0||cells.Distinct().Count()!=cells.Count)return false;
            var seen=new HashSet<Vector2Int>{cells[0]};var queue=new Queue<Vector2Int>();queue.Enqueue(cells[0]);
            while(queue.Count>0){var cell=queue.Dequeue();foreach(var d in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})if(cells.Contains(cell+d)&&seen.Add(cell+d))queue.Enqueue(cell+d);}
            return seen.Count==cells.Count;
        }
    }
}
