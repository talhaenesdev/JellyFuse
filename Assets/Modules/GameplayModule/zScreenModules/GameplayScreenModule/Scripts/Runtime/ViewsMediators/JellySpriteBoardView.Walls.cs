using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using TMPro;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UI;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The walls: the wall round the board follows the floor's edge - the board's outermost cells and
    /// any wall painted in from them - the doors sit on it, and walls standing alone inside are joined
    /// by their neighbours.
    /// Each piece takes its sprite from CD_JellyWallSkin by where it sits; an empty slot is drawn as a
    /// generated block. A door shows the jelly it waits for and, at its corner, how many come after.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private readonly List<TMP_Text> _labels = new();
        private readonly List<TMP_Text> _freeLabels = new();
        private CD_JellyWallSkin Skin => Look.WallSkin;
        private void BuildWalls()
        {
            foreach (var label in _labels) { label.gameObject.SetActive(false); _freeLabels.Add(label); }
            _labels.Clear();
            int width = _level.Width, height = _level.Height;
            bool painted = Skin != null && Skin.Straight(JellySide.Top) != null;
            // Back rows first, so a nearer wall covers the one behind it.
            for (int y = height - 1; y >= 0; y--) for (int x = 0; x < width; x++)
            {
                var cell = new Vector2Int(x, y);
                if (Contains(_level.Walls, cell))
                {
                    // A wall joined to the outside is part of the wall round the board: the outline
                    // drawn along the floor shows it. Without painted art it is drawn as a block.
                    if (!painted || !_outside.Contains(cell))
                        Wall(cell, Skin != null && !_outside.Contains(cell) ? Skin.InnerPiece(Neighbours(cell)) : null, !_outside.Contains(cell), Vector2.zero);
                    continue;
                }
                if (painted && !_outside.Contains(cell)) Outline(cell);
            }
            for (int door = 0; door < _level.Gates.Count; door++) BuildDoor(door);
            // A frozen jelly shows how many jellies through doors thaw it. It cannot move, so its number stays put.
            foreach (var piece in _snapshot.Pieces)
            {
                int frozen = _snapshot.JellyFrozen(piece.Id);
                if (frozen <= 0) continue;
                // Ice over each of its cells, seated where the jelly's picture is.
                var seat = Look.JellySpriteOffset * _cell;
                // A whole-shape picture of ice is part of the jelly itself, drawn in its row (see Present).
                if (!Look.IceTiles(piece.Cells, _iceTiles))
                    foreach (var cell in piece.Cells)
                    Flat(_doorLayer, Position(cell) + seat, Vector2.one * Look.JellyWidth * _cell, Pick(Look.FrozenJellySprite, _sprites.Rounded), Look.FrozenJellyOverlay, 0);
                Label(frozen.ToString(), Position(CentreOf(piece.Cells)) + seat, Look.FrozenTextSize * _cell, 0, Look.FrozenTextColour);
            }
        }

        // ---------- the wall round the board, following the floor's edge ----------

        private readonly HashSet<Vector2Int> _outside = new();
        private readonly Queue<Vector2Int> _outsideQueue = new();
        private readonly Vector2Int[] _fourSides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        /// <summary>
        /// The cells the wall round the board takes: everything off the board, the doors, the empty
        /// cells, and every wall joined to those. The floor is what is left, and the outline runs along
        /// its edge - so walls a designer painted in from the edge bend the outline round them.
        /// </summary>
        private void FindOutside()
        {
            _outside.Clear(); _outsideQueue.Clear();
            int width = _level.Width, height = _level.Height;
            void Seed(Vector2Int cell) { if (_outside.Add(cell)) _outsideQueue.Enqueue(cell); }
            for (int x = -1; x <= width; x++) { Seed(new Vector2Int(x, -1)); Seed(new Vector2Int(x, height)); }
            for (int y = 0; y < height; y++) { Seed(new Vector2Int(-1, y)); Seed(new Vector2Int(width, y)); }
            foreach (var hole in _level.Holes) Seed(hole);
            foreach (var gate in _level.Gates) foreach (var cell in gate.Cells) Seed(cell);
            while (_outsideQueue.Count > 0)
            {
                var cell = _outsideQueue.Dequeue();
                foreach (var side in _fourSides)
                {
                    var next = cell + side;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height || _outside.Contains(next)) continue;
                    if (Contains(_level.Walls, next) || Contains(_level.Holes, next)) Seed(next);
                }
            }
        }
        private bool Out(Vector2Int cell) => _outside.Contains(cell);

        /// <summary>
        /// The wall pieces round one floor cell: a straight piece on each side facing the outside, a
        /// corner piece where two of them meet. The art has its wall on the outer edge of its square, so a
        /// piece drawn on the floor cell sits on the floor's edge; FrameInset below 1 pushes it out a little.
        /// A straight piece reaches on into a corner pulled out that way, so no gap opens beside it.
        /// </summary>
        private void Outline(Vector2Int cell)
        {
            bool up = Out(cell + Vector2Int.up), down = Out(cell + Vector2Int.down), left = Out(cell + Vector2Int.left), right = Out(cell + Vector2Int.right);
            if (Skin.PixelsPerCell > 0) { NativeOutline(cell, up, down, left, right); return; }
            if (!up && !down && !left && !right) return;
            float push = 1 - Skin.FrameInset;
            if (up && left) Piece(cell, Skin.Corner(true, false), new Vector2(-push, push) + Skin.CornerOffset(true, false) - Skin.WallOffset(JellySide.Top), Vector2.one, JellySide.Top);
            if (up && right) Piece(cell, Skin.Corner(true, true), new Vector2(push, push) + Skin.CornerOffset(true, true) - Skin.WallOffset(JellySide.Top), Vector2.one, JellySide.Top);
            if (down && left) Piece(cell, Skin.Corner(false, false), new Vector2(-push, -push) + Skin.CornerOffset(false, false) - Skin.WallOffset(JellySide.Bottom), Vector2.one, JellySide.Bottom);
            if (down && right) Piece(cell, Skin.Corner(false, true), new Vector2(push, -push) + Skin.CornerOffset(false, true) - Skin.WallOffset(JellySide.Bottom), Vector2.one, JellySide.Bottom);
            if (up && !left && !right) Straight(cell, JellySide.Top, push);
            if (down && !left && !right) Straight(cell, JellySide.Bottom, push);
            if (left && !up && !down) Straight(cell, JellySide.Left, push);
            if (right && !up && !down) Straight(cell, JellySide.Right, push);
        }
        /// <summary>
        /// The pieces round one floor cell at their own size: each sprite's pivot sits on the floor's edge
        /// (straights) or on the floor's corner (corners, and concave corners where the floor turns inward).
        /// Corners are drawn over the straights they join.
        /// </summary>
        private void NativeOutline(Vector2Int cell, bool up, bool down, bool left, bool right)
        {
            if (up) NativeStraight(cell, JellySide.Top, Skin.WallOffsetTop);
            if (down) NativeStraight(cell, JellySide.Bottom, Skin.WallOffsetBottom);
            if (left) NativeStraight(cell, JellySide.Left, Skin.WallOffsetLeft);
            if (right) NativeStraight(cell, JellySide.Right, Skin.WallOffsetRight);
            for (int i = 0; i < 4; i++)
            {
                bool top = i < 2, east = i % 2 == 1;
                bool vertical = top ? up : down, horizontal = east ? right : left;
                var corner = new Vector2(east ? .5f : -.5f, top ? .5f : -.5f);
                var side = top ? JellySide.Top : JellySide.Bottom;
                if (vertical && horizontal) Native(cell, corner, Skin.Corner(top, east), side, true, Skin.CornerOffset(top, east));
                else if (!vertical && !horizontal && Out(cell + new Vector2Int(east ? 1 : -1, top ? 1 : -1)))
                    Native(cell, corner, Skin.Concave(top, east), side, true, Skin.ConcaveOffset(top, east));
            }
        }
        /// <summary>
        /// A straight piece along one side of a floor cell, starting where the corner picture at each
        /// end stops: it fills only the part of the side no corner covers, stretched along the wall.
        /// </summary>
        private void NativeStraight(Vector2Int cell, JellySide side, Vector2 offset)
        {
            var art = Skin.Straight(side);
            if (art == null) return;
            var outward = Outward(side);
            var along = side is JellySide.Top or JellySide.Bottom ? Vector2Int.right : Vector2Int.up;
            float before = CornerReach(cell, outward, -along), after = CornerReach(cell, outward, along);
            float length = 1 - before - after;
            if (length <= .001f) return;
            var size = art.rect.size / Skin.PixelsPerCell;
            var pivot = art.pivot / art.rect.size;
            // One straight sprite serves both sides of its pair: the bottom and right walls use the
            // top and left walls' pivot mirrored, so the same art meets the floor from the other side.
            if (side == JellySide.Bottom && art == Skin.Straight(JellySide.Top)) pivot.y = 1 - pivot.y;
            if (side == JellySide.Right && art == Skin.Straight(JellySide.Left)) pivot.x = 1 - pivot.x;
            // Along the wall the piece is centred on what is left of the side; across it the pivot holds.
            if (along.x != 0) { size.x = length; pivot.x = .5f; } else { size.y = length; pivot.y = .5f; }
            var point = (Vector2)outward * .5f + (Vector2)along * ((before - after) * .5f);
            Add(Position((Vector2)cell + point + Vector2.Scale(new Vector2(.5f, .5f) - pivot, size) + Skin.PieceOffset + offset), size, art, side, false);
        }
        /// <summary>
        /// How far into this cell's side, in cells, the corner picture at one end of it reaches: the
        /// outer corner where the wall turns away, or the inner corner where the floor turns in.
        /// </summary>
        private float CornerReach(Vector2Int cell, Vector2Int outward, Vector2Int end)
        {
            var corner = outward + end;
            var beside = cell + end;
            if (!Out(cell + outward)) return 0;
            Sprite art;
            if (Out(beside)) art = Skin.Corner(corner.y > 0, corner.x > 0);
            else if (!Out(cell + corner))
                // The inner corner is drawn by the floor cell beside this one, toward the outside.
                art = outward.x != 0 ? Skin.Concave(end.y < 0, outward.x > 0) : Skin.Concave(outward.y > 0, end.x < 0);
            else return 0;
            if (art == null) return 0;
            // The picture's pivot sits on the corner point; measure from it back toward this cell.
            float reach = end.x != 0
                ? (end.x > 0 ? art.pivot.x : art.rect.width - art.pivot.x)
                : (end.y > 0 ? art.pivot.y : art.rect.height - art.pivot.y);
            return Mathf.Clamp01(reach / Skin.PixelsPerCell);
        }
        private void Native(Vector2Int cell, Vector2 point, Sprite art, JellySide side, bool corner, Vector2 offset)
        {
            if (art == null) return;
            var size = art.rect.size / Skin.PixelsPerCell;
            var pivot = art.pivot / art.rect.size;
            Add(Position((Vector2)cell + point + Vector2.Scale(new Vector2(.5f, .5f) - pivot, size) + Skin.PieceOffset + offset), size, art, side, corner);
        }
        private void Add(Vector2 position, Vector2 size, Sprite art, JellySide side, bool corner)
        {
            var item = Place(Take(_jellyLayer), Role.Wall, position, size * _cell, Color.white, 0, 0, -1, art);
            // Corners come after the straights they join, among walls drawn at the same depth.
            if (side == JellySide.Top) item.SortY = corner ? 1e30f : float.MaxValue;
            else if (side == JellySide.Bottom) { item.Front = true; item.SortY = corner ? 1e29f : 1e30f; }
            _boardItems.Add(item);
        }

        /// <summary>Whether a floor cell is a corner of the outline on this side: the outside on the side and on the end given.</summary>
        private bool CornerAt(Vector2Int cell, Vector2Int side, Vector2Int end) =>
            !Out(cell) && !Contains(_level.Walls, cell) && Out(cell + side) && Out(cell + end);
        private void Straight(Vector2Int cell, JellySide side, float push)
        {
            var outward = Outward(side);
            var along = side is JellySide.Top or JellySide.Bottom ? Vector2Int.right : Vector2Int.up;
            // Reach on by the push into a neighbouring corner, which is pulled away from this cell.
            float before = CornerAt(cell - along, outward, -along) ? push : 0;
            float after = CornerAt(cell + along, outward, along) ? push : 0;
            float length = 1 + before + after;
            var shift = (Vector2)outward * push + (Vector2)along * ((after - before) * .5f);
            var size = along.x != 0 ? new Vector2(length, 1) : new Vector2(1, length);
            Piece(cell, Skin.Straight(side), shift, size, side);
        }
        private static Vector2Int Outward(JellySide side) => side switch
        {
            JellySide.Top => Vector2Int.up, JellySide.Bottom => Vector2Int.down, JellySide.Left => Vector2Int.left, _ => Vector2Int.right,
        };
        private void Piece(Vector2Int cell, Sprite art, Vector2 shift, Vector2 cells, JellySide side)
        {
            if (art == null) return;
            var size = Vector2.Scale(cells, Skin.PieceSize);
            var item = Place(Take(_jellyLayer), Role.Wall, Position(cell) + (shift + Skin.PieceOffset + Skin.WallOffset(side)) * _cell, size * _cell, Color.white, 0, 0, -1, art);
            // Seen from above at an angle: a wall with the floor below it is behind every jelly, a wall
            // with the floor above it in front of every jelly, held or not; side walls sort by their row.
            if (side == JellySide.Top) item.SortY = float.MaxValue;
            else if (side == JellySide.Bottom) item.Front = true;
            _boardItems.Add(item);
        }

        private bool IsDoorCell(Vector2Int cell)
        {
            for (int i = 0; i < _level.Gates.Count; i++) if (Contains(_level.Gates[i].Cells, cell)) return true;
            return false;
        }
        /// <summary>Which of the four cells beside an inner wall are inner walls too, as CD_JellyWallSkin's UP, RIGHT, DOWN and LEFT added up.</summary>
        private int Neighbours(Vector2Int cell) =>
            (InnerWall(cell + Vector2Int.up) ? CD_JellyWallSkin.UP : 0) | (InnerWall(cell + Vector2Int.right) ? CD_JellyWallSkin.RIGHT : 0) |
            (InnerWall(cell + Vector2Int.down) ? CD_JellyWallSkin.DOWN : 0) | (InnerWall(cell + Vector2Int.left) ? CD_JellyWallSkin.LEFT : 0);
        private bool InnerWall(Vector2Int cell) => !_outside.Contains(cell) && Contains(_level.Walls, cell);

        /// <summary>A painted piece is drawn as it is; without one, a raised block in the wall colour.</summary>
        private Item Wall(Vector2Int cell, Sprite painted, bool inner, Vector2 inset)
        {
            Item item;
            // A wall stands up like a jelly does, so it is sorted with them in the jelly layer.
            if (painted != null)
                item = Place(Take(_jellyLayer), Role.Wall, Position(cell) + (Skin.PieceOffset + inset) * _cell, Skin.PieceSize * _cell, Color.white, 0, 0, -1, painted);
            else
                item = Place(Take(_jellyLayer), Role.Wall, Position(cell), Vector2.one * _cell * (inner ? Look.SpriteWallSize : 1),
                    Look.WallColour, Look.SpriteWallDepth);
            _boardItems.Add(item);
            return item;
        }

        private void BuildDoor(int index)
        {
            var gate = _level.Gates[index];
            var stage = _snapshot.WaitingStage(index);
            // A door that has taken its whole queue is gone, and the wall shows. Only while its last
            // jelly is going in is it drawn, then faded out.
            bool closing = _closingDoors.Contains(index);
            if (stage == null && !closing) return;
            bool sideways = gate.Direction.x != 0;
            // The door is a picture laid over the wall round the board, where its gap is: DoorInset pulls
            // it in onto the wall, DoorThickness and DoorOverhang size it to cover the wall under it.
            float inset = Skin != null ? Skin.DoorInset : 0, across = Skin != null ? Skin.DoorThickness : 1, overhang = Skin != null ? Skin.DoorOverhang : 0;
            // A door that fits its jelly is drawn only as wide as it is open for the jelly it waits for.
            var open = gate.OpenCells(_snapshot.DoorProgress[index]);
            var centre = Position((Vector2)(open[0] + open[^1]) * .5f - (Vector2)gate.Direction * inset);
            var cells = sideways ? new Vector2(across, open.Count + overhang * 2) : new Vector2(open.Count + overhang * 2, across);
            var last = gate.Stages.Count > 0 ? gate.Stages[^1] : null;
            var colour = stage != null ? Tint((int)stage.Colour) : last != null ? Tint((int)last.Colour) : Look.WallColour;
            var painted = Skin != null ? Skin.Door(gate.Side, open.Count) : null;
            if (painted != null && Skin.PixelsPerCell > 0)
            {
                // At its own size, its pivot on the floor's edge in the middle of the door.
                cells = painted.rect.size / Skin.DoorPixels;
                var edge = (Vector2)(open[0] + open[^1]) * .5f - (Vector2)gate.Direction * .5f;
                centre = Position(edge + Vector2.Scale(new Vector2(.5f, .5f) - painted.pivot / painted.rect.size, cells));
            }
            // The door's picture, its jelly and its counter all move with the side's door offset.
            if (Skin != null) centre += Skin.DoorOffset(gate.Side) * _cell;
            const float depth = 0;
            // A door in the top wall stands behind the jellies, like the wall it sits in, so a jelly in
            // the row under it is drawn over its bottom edge; every other door is drawn over the board.
            bool behind = painted != null && gate.Side == JellySide.Top;
            var door = painted != null
                ? Place(Take(behind ? _jellyLayer : _doorLayer), Role.Wall, centre + Skin.PieceOffset * _cell, cells * _cell, Color.white, 0, 0, -1, painted)
                : Place(Take(_doorLayer), Role.Door, centre, cells * _cell - Vector2.one * _cell * .06f, Color.Lerp(colour, Look.BoardColour, .55f));
            if (behind) door.SortY = 1e29f; // after the top wall and its corners, before every jelly
            if (stage != null) _doorFrames[index] = (centre, cells * _cell, _snapshot.DoorFrozen.Count > index && _snapshot.DoorFrozen[index] > 0);
            _boardItems.Add(door);
            if (closing) _closingItems.Add(door);
            AddGate(index, gate.Side, open.Count, door, painted);
            if (stage == null) return;

            // Frozen under painted ice, the door's screen is not seen: no jelly is drawn on it.
            int frozenFor = _snapshot.DoorFrozen.Count > index ? _snapshot.DoorFrozen[index] : 0;
            var ice = frozenFor > 0 && Skin != null && Skin.DoorPixels > 0 ? Skin.DoorIce(gate.Side, open.Count) : null;
            // Under ice that lets the door show through, its screen and the jelly on it show too.
            bool screenHidden = ice != null && Look.FrozenDoorIceColour.a >= .99f;
            // A top door's ice is drawn with the door, behind the jellies: the jelly on its screen goes there too,
            // between the door and its ice, so the ice covers it.
            bool underIce = ice != null && behind;
            var screenLayer = underIce ? _jellyLayer : _doorLayer;
            void OnScreen(Item item) { if (underIce) { item.SortY = door.SortY; item.SortX = door.SortX + .5f; } }
            // The jelly it waits for, small, on the door's face.
            Span(stage.Cells, out float spanX, out float spanY);
            // Every door draws its jelly at the same cell size; only a shape bigger than its door shrinks.
            float mini = Mathf.Min(Skin != null ? Skin.IconCellSize : .22f, cells.x * .9f / spanX, cells.y * .9f / spanY);
            var iconCentre = centre + (Skin != null ? Skin.IconOffset * _cell : Vector2.zero);
            // On a door with a measured screen, the jelly sits on the screen's grid, a square a cell, drawn in its own
            // picture so its colour is the jelly's own.
            var look = ColourSprite((int)stage.Colour);
            bool onGrid = !screenHidden && painted != null && DrawOnScreen(gate.Side, open.Count, painted, centre, stage, look, colour, depth, screenLayer, OnScreen);
            // Otherwise a painted picture of its shape when there is one, turned to match and redrawn in its colour.
            int quarters = 0; bool mirrored = false;
            var shape = Skin != null && !onGrid ? Skin.ShapeIcon(stage.Cells, out quarters, out mirrored) : null;
            if (shape != null && !screenHidden)
            {
                var icon = Place(Take(screenLayer), Role.Marker, iconCentre, shape.rect.size * (mini * _cell / Skin.IconCellPixels), colour, 0, depth, -1, shape);
                OnScreen(icon);
                icon.BodyImage.color = colour;
                icon.BodyImage.material = Skin.IconTint;
                icon.Root.localRotation = Quaternion.Euler(0, 0, 90 * quarters);
                icon.Root.localScale = new Vector3(mirrored ? -1 : 1, 1, 1);
                _boardItems.Add(icon);
            }
            if (shape == null && !screenHidden && !onGrid)
                foreach (var cell in stage.Cells)
                {
                    var position = iconCentre + ((Vector2)cell - new Vector2(spanX - 1, spanY - 1) * .5f) * mini * _cell;
                    OnScreen(Flat(screenLayer, position, Vector2.one * mini * _cell * .92f, look != null ? look : Pick(Look.JellySprite, _sprites.Rounded), look != null ? Color.white : colour, depth));
                }

            // A frozen door: ice over it, and how many jellies through the other doors thaw it.
            int frozen = _snapshot.DoorFrozen.Count > index ? _snapshot.DoorFrozen[index] : 0;
            if (frozen > 0)
            {
                // Painted ice for this door, at its own size over the door's middle, drawn with the door: a top door's
                // stands behind the row of jellies under it, like the door itself.
                if (ice != null)
                {
                    var cover = Flat(behind ? _jellyLayer : _doorLayer, centre + Skin.PieceOffset * _cell, ice.rect.size / Skin.DoorPixels * _cell, ice, Look.FrozenDoorIceColour, depth);
                    cover.SortY = door.SortY; cover.SortX = door.SortX + 1;
                }
                else Flat(_doorLayer, centre, cells * _cell, Pick(Look.FrozenDoorSprite, _sprites.Rounded), Look.FrozenDoorOverlay, depth);
                Label(frozen.ToString(), centre, Look.FrozenTextSize * _cell, depth, Look.FrozenTextColour);
            }

            // How many more it takes after this one.
            int after = _snapshot.QueuedAfter(index);
            if (after <= 0) return;
            float badge = (Skin != null ? Skin.CounterSize : .34f) * _cell;
            var anchor = Skin != null ? Skin.CounterAnchor(gate.Side) : new Vector2(0, .5f);
            var corner = centre + Vector2.Scale(anchor - new Vector2(.5f, .5f), cells) * _cell + (Skin != null ? Skin.CounterOffset : Vector2.zero) * _cell;
            if (Skin != null && Skin.CounterSprite != null) Flat(_doorLayer, corner, Vector2.one * badge, Skin.CounterSprite, Color.white, depth);
            else
            {
                // A rounded square: the outline drawn first, the fill inset over it.
                float border = Skin != null ? Skin.CounterBorderWidth : .1f;
                if (border > 0) Flat(_doorLayer, corner, Vector2.one * badge, _sprites.Rounded, Skin != null ? Skin.CounterBorder : Color.gray, depth);
                Flat(_doorLayer, corner, Vector2.one * badge * (1 - border * 2), _sprites.Rounded, Skin != null ? Skin.CounterBadge : Color.black, depth);
            }
            Label((Skin != null && Skin.CounterPlus ? "+" : "") + after, corner, badge, depth, Skin != null ? Skin.CounterColour : Color.white);
        }

        /// <summary>A flat picture on a door, in the door layer over the walls and jellies.</summary>
        /// <summary>
        /// The jelly a door waits for, on the grid of the door's screen: centred on the screen, each cell on a grid
        /// square. False when the door has no measured screen or the jelly does not fit its grid.
        /// </summary>
        private bool DrawOnScreen(JellySide side, int size, Sprite painted, Vector2 centre, JellyPieceVO stage, Sprite look, Color colour, float depth,
            RectTransform layer, System.Action<Item> onScreen)
        {
            var screen = Skin != null && Skin.DoorPixels > 0 ? Skin.DoorScreen(side, size) : null;
            if (screen == null) return false;
            float pitch = screen.Pitch;
            // The first grid line inside the screen on each axis, and how many whole squares follow it.
            float firstX = screen.GridOrigin.x + Mathf.Ceil((screen.Screen.xMin - screen.GridOrigin.x) / pitch) * pitch;
            float firstY = screen.GridOrigin.y + Mathf.Ceil((screen.Screen.yMin - screen.GridOrigin.y) / pitch) * pitch;
            int across = Mathf.FloorToInt((screen.Screen.xMax - firstX) / pitch + .01f), up = Mathf.FloorToInt((screen.Screen.yMax - firstY) / pitch + .01f);
            Span(stage.Cells, out float spanX, out float spanY);
            int wide = Mathf.RoundToInt(spanX), tall = Mathf.RoundToInt(spanY);
            if (wide > across || tall > up) return false;
            var min = stage.Cells[0];
            foreach (var c in stage.Cells) min = Vector2Int.Min(min, c);
            // The squares nearest the middle of the screen that hold it.
            int fromX = Mathf.RoundToInt((screen.Screen.center.x - firstX) / pitch - wide * .5f), fromY = Mathf.RoundToInt((screen.Screen.center.y - firstY) / pitch - tall * .5f);
            fromX = Mathf.Clamp(fromX, 0, across - wide); fromY = Mathf.Clamp(fromY, 0, up - tall);
            float scale = _cell / Skin.DoorPixels;
            var picture = painted.rect.size;
            var origin = centre + Skin.PieceOffset * _cell;
            var square = Vector2.one * pitch * scale * Skin.ScreenCellFill;
            foreach (var cell in stage.Cells)
            {
                var local = cell - min;
                var pixel = new Vector2(firstX + (fromX + local.x + .5f) * pitch, firstY + (fromY + local.y + .5f) * pitch);
                var position = origin + (pixel - picture * .5f) * scale;
                onScreen(Flat(layer, position, square, look != null ? look : Pick(Look.JellySprite, _sprites.Rounded), look != null ? Color.white : colour, depth));
            }
            return true;
        }
        private Item Flat(RectTransform layer, Vector2 position, Vector2 size, Sprite sprite, Color colour, float lift)
        {
            var item = Place(Take(layer), Role.Marker, position, size, colour, 0, lift, -1, sprite);
            item.BodyImage.color = colour;
            _boardItems.Add(item);
            return item;
        }

        private void Label(string text, Vector2 position, float size, float lift, Color colour)
        {
            TMP_Text label;
            if (_freeLabels.Count > 0) { label = _freeLabels[^1]; _freeLabels.RemoveAt(_freeLabels.Count - 1); }
            else
            {
                var rect = new GameObject("Counter", typeof(RectTransform)).GetComponent<RectTransform>();
                rect.gameObject.layer = gameObject.layer;
                rect.SetParent(_doorLayer, false);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                label = rect.gameObject.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Overflow;
            }
            label.gameObject.SetActive(true);
            label.transform.SetAsLastSibling();
            var root = (RectTransform)label.transform;
            // The door layer is squashed by the tilt; the text is not.
            root.localScale = new Vector3(1, 1 / Look.SpriteTilt, 1);
            root.anchoredPosition = position + new Vector2(0, lift * _cell);
            root.sizeDelta = new Vector2(size * 2, size);
            // The look's font, or TextMeshPro's default (TMP Settings) when it names none.
            var font = Look.FrozenTextFont != null ? Look.FrozenTextFont : TMP_Settings.defaultFontAsset;
            if (label.font != font) label.font = font;
            label.fontStyle = Look.FrozenTextStyle;
            label.fontSize = Mathf.Max(8, size * .62f);
            label.color = colour; label.text = text;
            _labels.Add(label);
        }

        private void ClearWalls()
        {
            _doorFrames.Clear();
            foreach (var label in _labels) { label.gameObject.SetActive(false); _freeLabels.Add(label); }
            _labels.Clear();
        }
    }
}
