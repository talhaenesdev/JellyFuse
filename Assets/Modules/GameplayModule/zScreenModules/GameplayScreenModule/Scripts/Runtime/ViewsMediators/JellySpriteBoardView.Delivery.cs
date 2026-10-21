using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UI;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// Delivery: a jelly a door takes slides into the door in one piece, and whatever has passed the
    /// floor's edge at the door is cut away, so it is eaten by the door as it goes in. Bits of jelly
    /// splash back from the door's mouth while it crosses. It is drawn over the doors and walls. It
    /// plays whatever AnimateJellies says: it is the move that ends the jelly, not its feel. Cells,
    /// cut-outs and bits come from pools.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        /// <summary>One cell of a delivered jelly, sliding from where it stood into its door.</summary>
        private sealed class Entering
        {
            public Item Item;
            /// <summary>Where it is drawn, where it lines up in front of the door, and where it ends inside it.</summary>
            public Vector2 From, Front, To;
            /// <summary>The share of the slide spent lining up before it goes in.</summary>
            public float Time, Align;
        }
        /// <summary>
        /// The door's mouth for one delivered jelly: a cut-out that shows only the board's side of the
        /// floor's edge, and the stretch of that edge the jelly crosses, where bits splash from.
        /// </summary>
        private sealed class Mouth
        {
            public RectTransform Mask, Content;
            public Vector2 Direction, EdgeFrom, EdgeTo;
            public Color Colour;
            public float Time, Owed, Align;
            public int Cells;
            /// <summary>The door it opens into.</summary>
            public int Door;
            /// <summary>Each row (or column) of the jelly that crosses the edge, and when along the slide it does.</summary>
            public readonly List<Lane> Lanes = new();
            public readonly List<Lane> Crossing = new();
        }
        /// <summary>
        /// One row (or column) of a delivered jelly: the stretch of the edge it covers, and the part of
        /// the slide, 0 to 1 of its eased progress, in which it is crossing the edge.
        /// </summary>
        private struct Lane
        {
            public Vector2 EdgeFrom, EdgeTo;
            public float CrossFrom, CrossTo;
        }
        /// <summary>A bit of jelly splashing from a door's mouth.</summary>
        private sealed class Bit
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position, Velocity;
            public Color Colour;
            public float Age, Life, Size;
        }
        private readonly List<Mouth> _mouths = new();
        private readonly List<Mouth> _freeMouths = new();
        private readonly List<Bit> _bits = new();
        private readonly List<Bit> _freeBits = new();
        private RectTransform _deliveryLayer, _bitLayer;
        private const float FAR = 100000;
        private readonly List<Entering> _entering = new();
        private readonly List<Entering> _freeEntering = new();
        private readonly HashSet<int> _delivered = new();
        /// <summary>Doors whose last jelly is going in now: drawn until it is in, then faded out, leaving the wall.</summary>
        private readonly HashSet<int> _closingDoors = new();
        private readonly List<Item> _closingItems = new();
        private float _closingTime;
        private const float DOOR_FADE = .25f;

        private bool DeliveryRunning => _entering.Count > 0 || _closingItems.Count > 0 || _bits.Count > 0;

        /// <summary>Finds the doors this snapshot finished. Called before the board is built, which draws them one last time.</summary>
        private void FindClosingDoors(PuzzleSnapshotRVO previous, PuzzleSnapshotRVO snapshot)
        {
            _closingDoors.Clear();
            _closingItems.Clear();
            _closingTime = 0;
            if (previous == null) return;
            for (int door = 0; door < snapshot.Level.Gates.Count; door++)
                if (snapshot.WaitingStage(door) == null && previous.WaitingStage(door) != null) _closingDoors.Add(door);
        }

        /// <summary>Starts the slide into the door for every jelly a door took since the last snapshot.</summary>
        private void StartDelivery(PuzzleSnapshotRVO previous, PuzzleSnapshotRVO snapshot, IReadOnlyList<PuzzleSnapshotRVO> route, float width, float depth)
        {
            if (previous == null) return;
            _delivered.Clear();
            try { StartDeliveries(previous, snapshot, route, width, depth); }
            finally { if (_delivered.Count > 0) DeliveryStarted?.Invoke(Feel.DoorEnterTime); }
        }
        /// <summary>A jelly started going into its door: how many seconds the slide in takes.</summary>
        public event System.Action<float> DeliveryStarted;
        private void StartDeliveries(PuzzleSnapshotRVO previous, PuzzleSnapshotRVO snapshot, IReadOnlyList<PuzzleSnapshotRVO> route, float width, float depth)
        {
            for (int door = 0; door < _level.Gates.Count; door++)
            for (int stage = previous.DoorProgress[door]; stage < snapshot.DoorProgress[door]; stage++)
            {
                var gate = _level.Gates[door];
                var piece = Delivered(previous, snapshot, route, door, gate.Stages[stage]);
                if (piece == null) continue;
                _delivered.Add(piece.Id);
                var colour = Tint((int)piece.Colour);
                var d = gate.Direction;
                // Where the jelly is drawn now, off its cells while it was still sliding there.
                // A jelly that fused on the way in is not the shape its blob was drawn as, so it starts on its cells.
                var before = PieceById(previous, piece.Id);
                var drawn = _blobs.TryGetValue(piece.Id, out var blob) && before != null && before.Cells.Count == piece.Cells.Count
                    ? blob.Centre + blob.Offset - Position(CentreOf(piece.Cells)) : Vector2.zero;
                // From where it is drawn, it moves in one piece to where its back cell has passed the floor's edge, and on a
                // little more for the side hanging under it.
                int furthest = 0, laneMin = int.MaxValue, laneMax = int.MinValue;
                foreach (var cell in piece.Cells)
                {
                    furthest = Mathf.Max(furthest, d.x > 0 ? _level.Width - 1 - cell.x : d.x < 0 ? cell.x : d.y > 0 ? _level.Height - 1 - cell.y : cell.y);
                    int lane = d.x != 0 ? cell.y : cell.x;
                    laneMin = Mathf.Min(laneMin, lane); laneMax = Mathf.Max(laneMax, lane);
                }
                var travel = (Vector2)d * ((furthest + .5f) * _cell);
                var mouth = OpenMouth(gate, laneMin, laneMax, width, colour, piece.Cells.Count);
                mouth.Door = door;
                // Drawn off its door's rows (or columns) - still catching up round a corner - it first lines up in
                // front of the door, then goes straight in: never across the corner into it.
                var along = (Vector2)d * Vector2.Dot(drawn, d);
                var sideways = drawn - along;
                float alongDistance = Mathf.Max(0, Vector2.Dot(travel - drawn, d));
                mouth.Align = sideways.magnitude < _cell * .05f ? 0 : Mathf.Clamp(sideways.magnitude / (sideways.magnitude + alongDistance), .15f, .45f);
                // For each of its rows (or columns): when, along the slide, its front reaches the edge and
                // its back has passed it - so the bits come only from the part that is going in.
                float edgeAt = Vector2.Dot(Position((Vector2)gate.Cells[0] - (Vector2)d * .5f), d);
                float path = Mathf.Max(1, Vector2.Dot(Position(piece.Cells[0]) + travel - (Position(piece.Cells[0]) + drawn), d));
                var across = new Vector2(Mathf.Abs(d.y), Mathf.Abs(d.x));
                for (int lane = laneMin; lane <= laneMax; lane++)
                {
                    float front = float.MinValue, back = float.MaxValue;
                    foreach (var cell in piece.Cells)
                    {
                        if ((d.x != 0 ? cell.y : cell.x) != lane) continue;
                        float at = Vector2.Dot(Position(cell) + drawn, d);
                        front = Mathf.Max(front, at + width * .5f); back = Mathf.Min(back, at - width * .5f);
                    }
                    if (front == float.MinValue) continue;
                    var middle = Vector2.Scale(Position(d.x != 0 ? new Vector2(0, lane) : new Vector2(lane, 0)), across) + Vector2.Scale(mouth.EdgeFrom, Vector2.one - across);
                    mouth.Lanes.Add(new Lane
                    {
                        EdgeFrom = middle - across * (width * .5f), EdgeTo = middle + across * (width * .5f),
                        CrossFrom = Mathf.Clamp01((edgeAt - front) / path), CrossTo = Mathf.Clamp01((edgeAt - back) / path),
                    });
                }
                foreach (var cell in piece.Cells)
                {
                    var from = Position(cell) + drawn;
                    Entering entering;
                    if (_freeEntering.Count > 0) { entering = _freeEntering[^1]; _freeEntering.RemoveAt(_freeEntering.Count - 1); }
                    else entering = new Entering();
                    entering.Item = Place(Take(mouth.Content), Role.Jelly, from, Vector2.one * width, colour, depth, 0, (int)piece.Colour);
                    entering.From = from; entering.Front = Position(cell) + along; entering.To = Position(cell) + travel;
                    entering.Time = 0; entering.Align = mouth.Align;
                    _entering.Add(entering);
                }
            }
        }
        /// <summary>
        /// A cut-out over everything that shows only the board's side of the floor's edge at this door,
        /// so a jelly passing that edge is cut away there.
        /// </summary>
        private Mouth OpenMouth(JellyGateCVO gate, int laneMin, int laneMax, float width, Color colour, int cells)
        {
            Mouth mouth;
            if (_freeMouths.Count > 0) { mouth = _freeMouths[^1]; _freeMouths.RemoveAt(_freeMouths.Count - 1); }
            else
            {
                mouth = new Mouth();
                mouth.Mask = new GameObject("DoorMouth", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                mouth.Mask.gameObject.layer = gameObject.layer;
                mouth.Mask.SetParent(_deliveryLayer, false);
                mouth.Content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
                mouth.Content.gameObject.layer = gameObject.layer;
                mouth.Content.SetParent(mouth.Mask, false);
            }
            mouth.Mask.gameObject.SetActive(true);
            var d = gate.Direction;
            // The floor's edge at the door: half a cell in from the door's own cells.
            var edge = Position((Vector2)gate.Cells[0] - (Vector2)d * .5f);
            // Everything on the board's side of the edge, as far as anything is drawn.
            var min = new Vector2(-FAR, -FAR); var max = new Vector2(FAR, FAR);
            if (d.x > 0) max.x = edge.x; else if (d.x < 0) min.x = edge.x; else if (d.y > 0) max.y = edge.y; else min.y = edge.y;
            var centre = (min + max) * .5f;
            mouth.Mask.anchorMin = mouth.Mask.anchorMax = mouth.Mask.pivot = new Vector2(.5f, .5f);
            mouth.Mask.anchoredPosition = centre; mouth.Mask.sizeDelta = max - min;
            mouth.Content.anchorMin = mouth.Content.anchorMax = mouth.Content.pivot = new Vector2(.5f, .5f);
            mouth.Content.anchoredPosition = -centre; mouth.Content.sizeDelta = Vector2.zero;
            // The stretch of the edge the jelly crosses.
            float half = width * .5f;
            mouth.EdgeFrom = d.x != 0 ? new Vector2(edge.x, Position(new Vector2(0, laneMin)).y - half) : new Vector2(Position(new Vector2(laneMin, 0)).x - half, edge.y);
            mouth.EdgeTo = d.x != 0 ? new Vector2(edge.x, Position(new Vector2(0, laneMax)).y + half) : new Vector2(Position(new Vector2(laneMax, 0)).x + half, edge.y);
            mouth.Direction = d; mouth.Colour = colour; mouth.Time = 0; mouth.Owed = 0; mouth.Cells = cells;
            mouth.Lanes.Clear(); mouth.Crossing.Clear();
            _mouths.Add(mouth);
            return mouth;
        }

        /// <summary>The jelly that left through the door, as it last stood on the board: the latest frame of the turn that still holds it.</summary>
        private JellyPieceVO Delivered(PuzzleSnapshotRVO previous, PuzzleSnapshotRVO snapshot, IReadOnlyList<PuzzleSnapshotRVO> route, int door, JellyPieceVO stage)
        {
            if (route != null)
                for (int f = route.Count - 1; f >= 0; f--)
                {
                    var piece = Leaving(route[f], snapshot, door, stage);
                    if (piece != null) return piece;
                }
            return Leaving(previous, snapshot, door, stage);
        }
        private JellyPieceVO Leaving(PuzzleSnapshotRVO frame, PuzzleSnapshotRVO snapshot, int door, JellyPieceVO stage)
        {
            var gate = _level.Gates[door];
            foreach (var piece in frame.Pieces)
            {
                if (_delivered.Contains(piece.Id) || PieceById(snapshot, piece.Id) != null) continue;
                if (piece.Colour != stage.Colour || piece.Cells.Count != stage.Cells.Count) continue;
                bool inside = true;
                for (int i = 0; i < piece.Cells.Count && inside; i++) inside = gate.Spans(piece.Cells[i]);
                if (inside) return piece;
            }
            return null;
        }
        private void StepDelivery(float h)
        {
            foreach (var entering in _entering) entering.Time += h;
            if (_closingItems.Count > 0) _closingTime += h;
            float duration = Mathf.Max(.05f, Feel.DoorEnterTime);
            // Bits splash from each mouth while its jelly is crossing the edge.
            foreach (var mouth in _mouths)
            {
                mouth.Time += h;
                float move = Going(mouth.Time / duration, mouth.Align);
                mouth.Crossing.Clear();
                foreach (var lane in mouth.Lanes) if (move >= lane.CrossFrom && move <= lane.CrossTo) mouth.Crossing.Add(lane);
                if (mouth.Crossing.Count == 0) continue;
                // As many bits as rows going in right now: an L's single row sheds a little, its long side a lot.
                mouth.Owed += Feel.DoorBits * mouth.Crossing.Count * h;
                for (; mouth.Owed >= 1; mouth.Owed--) SpawnBit(mouth, mouth.Crossing[Random.Range(0, mouth.Crossing.Count)]);
            }
            for (int i = _bits.Count - 1; i >= 0; i--)
            {
                var bit = _bits[i];
                bit.Age += h;
                if (bit.Age >= bit.Life) { bit.Rect.gameObject.SetActive(false); _freeBits.Add(bit); _bits.RemoveAt(i); continue; }
                bit.Position += bit.Velocity * h;
                bit.Velocity *= Mathf.Exp(-3 * h);
            }
        }
        /// <summary>The slide's progress: it gathers speed as the door draws it in.</summary>
        private static float Eased(float t) { t = Mathf.Clamp01(t); return t * t * (2 - t); }
        /// <summary>How far along the slide into the door it is, after the share <paramref name="align"/> spent lining up.</summary>
        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        private static float Going(float t, float align) => t <= align ? 0 : Eased((t - align) / Mathf.Max(.01f, 1 - align));
        /// <summary>Bits burst out of every cell of a jelly that has just fused, in its colour.</summary>
        private void BurstFuse(IReadOnlyList<Vector2Int> cells, Color colour)
        {
            if (Feel.FuseBitsPerCell <= 0) return;
            float speed = Feel.FuseBitSpeed * _cell;
            foreach (var cell in cells)
                for (int i = 0; i < Feel.FuseBitsPerCell; i++)
                {
                    var bit = TakeBit();
                    var direction = Random.insideUnitCircle.normalized;
                    bit.Position = Position(cell) + direction * (_cell * Random.Range(.1f, .4f));
                    bit.Velocity = direction * speed * Random.Range(.5f, 1.3f);
                    bit.Life = Feel.FuseBitLife * Random.Range(.6f, 1.4f);
                    bit.Size = Feel.FuseBitSize * _cell * Random.Range(.5f, 1.5f);
                    bit.Colour = Color.Lerp(colour, Color.white, Random.Range(0f, .45f));
                }
        }
        /// <summary>A jelly a booster took away bursts apart where it is drawn, in its colour.</summary>
        private void BurstVanish(IReadOnlyList<Vector2Int> cells, Color colour, Vector2 drawnOffset)
        {
            if (Feel.VanishBitsPerCell <= 0) return;
            float speed = Feel.VanishBitSpeed * _cell;
            foreach (var cell in cells)
                for (int i = 0; i < Feel.VanishBitsPerCell; i++)
                {
                    var bit = TakeBit();
                    var direction = Random.insideUnitCircle.normalized;
                    bit.Position = Position(cell) + drawnOffset + direction * (_cell * Random.Range(0f, .45f));
                    bit.Velocity = direction * speed * Random.Range(.4f, 1.3f);
                    bit.Life = Feel.VanishBitLife * Random.Range(.6f, 1.4f);
                    bit.Size = Feel.VanishBitSize * _cell * Random.Range(.5f, 1.5f);
                    bit.Colour = Color.Lerp(colour, Color.white, Random.Range(0f, .5f));
                }
        }
        private void SpawnBit(Mouth mouth, Lane lane)
        {
            var bit = TakeBit();
            var across = new Vector2(-mouth.Direction.y, mouth.Direction.x);
            bit.Position = Vector2.Lerp(lane.EdgeFrom, lane.EdgeTo, Random.value);
            // Mostly back out of the mouth and to the sides, as if the door tears it apart.
            float speed = Feel.DoorBitSpeed * _cell;
            bit.Velocity = (-mouth.Direction * Random.Range(.2f, 1f) + across * Random.Range(-1f, 1f) * Feel.DoorBitSpread) * speed * Random.Range(.5f, 1.3f);
            bit.Life = Feel.DoorBitLife * Random.Range(.6f, 1.4f);
            bit.Size = Feel.DoorBitSize * _cell * Random.Range(.5f, 1.5f);
            bit.Colour = Color.Lerp(mouth.Colour, Color.white, Random.Range(0f, .35f));
        }
        /// <summary>A bit from the pool, shown and live; the caller sets where it flies.</summary>
        private Bit TakeBit()
        {
            Bit bit;
            if (_freeBits.Count > 0) { bit = _freeBits[^1]; _freeBits.RemoveAt(_freeBits.Count - 1); }
            else
            {
                bit = new Bit();
                bit.Rect = new GameObject("Bit", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                bit.Rect.gameObject.layer = gameObject.layer;
                bit.Rect.SetParent(_bitLayer, false);
                bit.Rect.anchorMin = bit.Rect.anchorMax = bit.Rect.pivot = new Vector2(.5f, .5f);
                bit.Image = bit.Rect.GetComponent<Image>();
                bit.Image.raycastTarget = false;
            }
            bit.Rect.gameObject.SetActive(true);
            bit.Image.sprite = _sprites.Circle;
            bit.Age = 0;
            _bits.Add(bit);
            return bit;
        }
        private void ApplyDelivery()
        {
            float duration = Mathf.Max(.05f, Feel.DoorEnterTime);
            for (int i = _entering.Count - 1; i >= 0; i--)
            {
                var entering = _entering[i];
                float t = entering.Time / duration;
                if (t >= 1) { Give(entering.Item); _freeEntering.Add(entering); _entering.RemoveAt(i); continue; }
                entering.Item.Root.anchoredPosition = t < entering.Align
                    ? Vector2.Lerp(entering.From, entering.Front, Smooth(t / entering.Align))
                    : Vector2.Lerp(entering.Front, entering.To, Going(t, entering.Align));
            }
            for (int i = _mouths.Count - 1; i >= 0; i--)
                if (_mouths[i].Time >= duration) { _mouths[i].Mask.gameObject.SetActive(false); _freeMouths.Add(_mouths[i]); _mouths.RemoveAt(i); }
            foreach (var bit in _bits)
            {
                float k = bit.Age / bit.Life;
                bit.Rect.anchoredPosition = bit.Position;
                bit.Rect.sizeDelta = Vector2.one * (bit.Size * (1 - k * k));
                bit.Image.color = WithAlpha(bit.Colour, 1 - k * k * k);
            }
            FadeClosingDoors();
        }
        private void FadeClosingDoors()
        {
            if (_closingItems.Count == 0) return;
            float wait = Mathf.Max(.05f, Feel.DoorEnterTime);
            float alpha = 1 - Mathf.Clamp01((_closingTime - wait) / DOOR_FADE);
            foreach (var item in _closingItems)
            {
                item.BodyImage.color = WithAlpha(item.BodyImage.color, alpha);
                if (alpha <= 0) item.Root.gameObject.SetActive(false);
            }
            if (alpha <= 0) _closingItems.Clear();
        }
        private static Color WithAlpha(Color colour, float alpha) => new Color(colour.r, colour.g, colour.b, alpha);
        private void ClearDelivery()
        {
            foreach (var entering in _entering) { Give(entering.Item); _freeEntering.Add(entering); }
            _entering.Clear();
            foreach (var mouth in _mouths) { mouth.Mask.gameObject.SetActive(false); _freeMouths.Add(mouth); }
            _mouths.Clear();
            foreach (var bit in _bits) { bit.Rect.gameObject.SetActive(false); _freeBits.Add(bit); }
            _bits.Clear();
            _closingItems.Clear();
        }
    }
}
