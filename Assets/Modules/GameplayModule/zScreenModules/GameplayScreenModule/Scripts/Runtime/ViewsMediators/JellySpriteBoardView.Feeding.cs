using System.Collections.Generic;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// Feeding the board's cat: every door has a pipe, laid round the outside of the board to the fed cat's bowl.
    /// A jelly going into a door sends a blob of food of its colour down that door's pipe; at the spout it drops
    /// into the bowl as a few pieces, and the cat comes to eat. When the level is won the cat sits back, full.
    /// Only for the look, like the cats themselves: nothing in the game hears of it.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private sealed class Pipe
        {
            /// <summary>The way the food goes, in the pipe layer's space: from the door to the nozzle's mouth.</summary>
            public readonly List<Vector2> Path = new();
            public float Length;
        }

        private sealed class FoodBlob
        {
            public Image Image;
            public Pipe Pipe;
            public float Travelled;
            public int Pieces;
            public Color Colour;
        }

        private sealed class FoodPiece
        {
            public Image Image;
            public Vector2 Position, Velocity;
            public float Delay, Spin;
            public bool Puff;
            public float PuffTime;
            public Color Colour;
        }

        private const float PIECE_GRAVITY = 22f;

        private RectTransform _pipeLayer;
        private readonly List<Image> _pipeImages = new();
        private int _pipeImagesUsed;
        private readonly Dictionary<int, Pipe> _pipes = new();
        private readonly List<FoodBlob> _foodBlobs = new();
        private readonly List<FoodPiece> _foodPieces = new();
        private readonly List<Image> _freeFoodImages = new();
        private Image _bowl, _bowlPile, _bowlFront;
        private Color _bowlColour = Color.white;
        private Cat _feeder;
        private Vector2 _bowlAt, _nozzle;
        private bool _bowlTop;
        private int _bowlFood;
        private float _eatClock;
        private bool _fedWin;

        // The pipes' tiles in the order BuildPipeTiles draws them.
        private const int TILE_H = 0, TILE_V = 1, TILE_LB = 2, TILE_RB = 3, TILE_LT = 4, TILE_RT = 5, TILE_T_DOWN = 6, TILE_T_UP = 7,
            TILE_T_LEFT = 8, TILE_T_RIGHT = 9, TILE_CROSS = 10, TILE_CAP = 11, TILE_SPOUT = 12, TILE_SPOUT_TEE = 13;
        private const int PIPE_N = 1, PIPE_E = 2, PIPE_S = 4, PIPE_W = 8;

        private bool FeedingDrawn => Look.CatEatFrames != null && Look.CatEatFrames.Length >= CAT_FRAMES * 3
                                     && Look.PipeTiles != null && Look.PipeTiles.Length > TILE_SPOUT_TEE
                                     && Look.BowlFrames != null && Look.BowlFrames.Length >= 4;

        private void BuildFeeding()
        {
            foreach (var blob in _foodBlobs) Free(blob.Image);
            foreach (var piece in _foodPieces) Free(piece.Image);
            _foodBlobs.Clear(); _foodPieces.Clear();
            _pipes.Clear();
            _pipeImagesUsed = 0;
            _feeder = null; _bowlFood = 0; _fedWin = false; _eatClock = 0; _bowlColour = Color.white;
            if (_bowlPile != null) _bowlPile.gameObject.SetActive(false);
            if (_bowlFront != null) _bowlFront.gameObject.SetActive(false);
            if (_bowl != null) _bowl.gameObject.SetActive(false);
            if (FeedingDrawn && _cats.Count > 0 && _level != null)
            {
                if (_pipeLayer == null)
                {
                    _pipeLayer = Layer("PipeLayer");
                    _pipeLayer.gameObject.AddComponent<Canvas>();
                }
                // Behind everything: the pipes run outside the board and tuck under the doors they leave.
                _pipeLayer.SetAsFirstSibling();
                LayPipes();
            }
            for (int i = _pipeImagesUsed; i < _pipeImages.Count; i++) _pipeImages[i].gameObject.SetActive(false);
        }


        /// <summary>The board's cell in the pipe layer's space: the pipes keep to the board's own grid.</summary>
        private Vector2 PipeAt(Vector2 cell)
        {
            var p = Position(cell);
            return new Vector2(p.x, p.y * Look.SpriteTilt);
        }

        /// <summary>
        /// The cat on the top wall is fed if there is one; its bowl stands in the middle of its run. Every door's pipe
        /// goes out from the door to a box round the board, along the box the shorter way to the bowl's column, and into
        /// the spout over the bowl. The pipes are laid on the board's own cells, so where two share a cell they are one
        /// pipe, and each cell takes the one tile that joins the sides its pipes leave by - a straight, a bend, a T or a
        /// cross. No tile is stretched and no pipe lies over another.
        /// </summary>
        private void LayPipes()
        {
            foreach (var cat in _cats) if (cat.Top) { _feeder = cat; break; }
            _feeder ??= _cats[0];
            _bowlTop = _feeder.Top;
            float cell = _cell, tilt = Look.SpriteTilt;

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (int y = 0; y < _level.Height; y++) for (int x = 0; x < _level.Width; x++)
            {
                if (!IsFloor(new Vector2Int(x, y))) continue;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
            if (minX > maxX) return;
            // The box, in cells: two out from the floor - past the frame - and wide enough for every door's first cell.
            int boxMinX = minX - 2, boxMaxX = maxX + 2, boxMinY = minY - 2, boxMaxY = maxY + 2;
            var starts = new Dictionary<int, (Vector2Int Cell, Vector2Int Direction)>();
            for (int g = 0; g < _level.Gates.Count; g++)
            {
                var gate = _level.Gates[g];
                var mid = Vector2.zero;
                foreach (var c in gate.Cells) mid += (Vector2)c;
                mid /= Mathf.Max(1, gate.Cells.Count);
                var start = Vector2Int.RoundToInt(mid) + gate.Direction;
                starts[g] = (start, gate.Direction);
                boxMinX = Mathf.Min(boxMinX, start.x); boxMaxX = Mathf.Max(boxMaxX, start.x);
                boxMinY = Mathf.Min(boxMinY, start.y); boxMaxY = Mathf.Max(boxMaxY, start.y);
            }
            // On a board that does not slide, the pipes keep on screen: where the box would leave the board's rect it is
            // drawn in, and the pipe runs behind a door's picture there.
            if (!CanPan && _rect != null)
            {
                var bounds = _rect.rect;
                int left = Mathf.CeilToInt(bounds.xMin / cell + _centre.x + .5f), right = Mathf.FloorToInt(bounds.xMax / cell + _centre.x - .5f);
                int bottom = Mathf.CeilToInt(bounds.yMin / (cell * tilt) + _centre.y + .5f);
                if (left <= minX - 1) boxMinX = Mathf.Max(boxMinX, left);
                if (right >= maxX + 1) boxMaxX = Mathf.Min(boxMaxX, right);
                if (bottom <= minY - 1) boxMinY = Mathf.Max(boxMinY, bottom);
            }
            // The bowl in the middle of the fed cat's run, on a column of the grid so the spout drops right into it.
            int bowlColumn = Mathf.RoundToInt(((_feeder.MinX + _feeder.MaxX) * .5f) / cell + _centre.x);
            _bowlAt = new Vector2(PipeAt(new Vector2(bowlColumn, 0)).x, _feeder.FeetY);
            // Over the cat's head on the top wall - its picture stands about a cell and a half high on this grid - or
            // under its feet on the front wall.
            float headRows = (Look.CatSize * cell) / (cell * tilt);
            int spoutRow;
            if (_bowlTop)
            {
                spoutRow = Mathf.CeilToInt(_feeder.FeetY / (cell * tilt) + _centre.y + headRows + .5f);
                boxMaxY = Mathf.Max(boxMaxY, spoutRow);
                // Never up into the screen's top bar: the board's rect is the ceiling, even if the spout then hangs a
                // little lower over the cat.
                if (_rect != null)
                {
                    float top = _rect.rect.yMax;
                    // The buttons over the board, in the pipe layer's space, a little under them.
                    if (Ceiling.HasValue && _pipeLayer != null) top = Mathf.Min(top, _pipeLayer.InverseTransformPoint(new Vector3(0, Ceiling.Value, 0)).y - cell * .35f * tilt);
                    int ceiling = Mathf.FloorToInt(top / (cell * tilt) + _centre.y - .5f);
                    // Never lower than just over the frame: there the pipe runs behind the cat rather than into the wall.
                    boxMaxY = Mathf.Min(boxMaxY, Mathf.Max(ceiling, maxY + 2));
                }
                spoutRow = boxMaxY;
            }
            else
            {
                spoutRow = Mathf.FloorToInt(_feeder.FeetY / (cell * tilt) + _centre.y - .8f);
                boxMinY = Mathf.Min(boxMinY, spoutRow);
                spoutRow = boxMinY;
            }
            var spoutCell = new Vector2Int(bowlColumn, spoutRow);
            int nozzleSide = _bowlTop ? PIPE_S : PIPE_N;
            _nozzle = PipeAt(new Vector2(bowlColumn, spoutRow + (_bowlTop ? -.5f : .5f)));

            if (_bowl == null)
            {
                _bowl = new GameObject("Img_CatBowl", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                _bowl.gameObject.layer = gameObject.layer;
                _bowl.rectTransform.SetParent(_catLayer, false);
                _bowl.raycastTarget = false;
                _bowl.preserveAspect = true;
                _bowl.rectTransform.anchorMin = _bowl.rectTransform.anchorMax = new Vector2(.5f, .5f);
                _bowl.rectTransform.pivot = new Vector2(.5f, .27f);
            }
            _bowl.gameObject.SetActive(true);
            // In front of the wall, behind the cats.
            _bowl.rectTransform.SetAsFirstSibling();
            _bowl.rectTransform.sizeDelta = Vector2.one * cell * .9f;
            _bowl.rectTransform.anchoredPosition = _bowlAt;
            _bowl.sprite = Look.BowlFrames[0];

            var sides = new Dictionary<Vector2Int, int>();
            void Join(Vector2Int a, Vector2Int b)
            {
                var step = b - a;
                int from = step.x > 0 ? PIPE_E : step.x < 0 ? PIPE_W : step.y > 0 ? PIPE_N : PIPE_S;
                int to = step.x > 0 ? PIPE_W : step.x < 0 ? PIPE_E : step.y > 0 ? PIPE_S : PIPE_N;
                sides[a] = (sides.TryGetValue(a, out int s) ? s : 0) | from;
                sides[b] = (sides.TryGetValue(b, out int t) ? t : 0) | to;
            }
            foreach (var pair in starts)
            {
                var (start, d) = pair.Value;
                // A door whose first cell is past a box drawn in for the screen starts inside it, under its picture,
                // rather than poking a stub out the far side.
                start = new Vector2Int(Mathf.Clamp(start.x, boxMinX, boxMaxX), Mathf.Clamp(start.y, boxMinY, boxMaxY));
                var corners = new List<Vector2> { start };
                var onEdge = d.x > 0 ? new Vector2Int(boxMaxX, start.y) : d.x < 0 ? new Vector2Int(boxMinX, start.y) : d.y > 0 ? new Vector2Int(start.x, boxMaxY) : new Vector2Int(start.x, boxMinY);
                // A door whose first cell is outside a box drawn in for the screen comes back in to it.
                onEdge = new Vector2Int(Mathf.Clamp(onEdge.x, boxMinX, boxMaxX), Mathf.Clamp(onEdge.y, boxMinY, boxMaxY));
                corners.Add(onEdge);
                AroundBox(onEdge, spoutCell, boxMinX, boxMaxX, boxMinY, boxMaxY, corners);
                corners.Add(spoutCell);
                // Walk it cell by cell, joining each to the next.
                var cells = new List<Vector2Int> { start };
                // The pipe begins under the door's picture.
                sides[start] = (sides.TryGetValue(start, out int first) ? first : 0) | (d.x > 0 ? PIPE_W : d.x < 0 ? PIPE_E : d.y > 0 ? PIPE_S : PIPE_N);
                var at = start;
                for (int i = 1; i < corners.Count; i++)
                {
                    var target = Vector2Int.RoundToInt(corners[i]);
                    // Straight lines only: across first, then up or down, if a corner is not lined up with the last.
                    while (at != target)
                    {
                        var next = at.x != target.x ? at + new Vector2Int(System.Math.Sign(target.x - at.x), 0) : at + new Vector2Int(0, System.Math.Sign(target.y - at.y));
                        Join(at, next);
                        cells.Add(next);
                        at = next;
                    }
                }
                var pipe = new Pipe();
                foreach (var c in cells) pipe.Path.Add(PipeAt(c));
                pipe.Path.Add(_nozzle);
                for (int i = 1; i < pipe.Path.Count; i++) pipe.Length += Vector2.Distance(pipe.Path[i - 1], pipe.Path[i]);
                _pipes[pair.Key] = pipe;
            }
            if (sides.Count == 0) return;
            sides[spoutCell] = (sides.TryGetValue(spoutCell, out int spout) ? spout : 0) | nozzleSide;

            var size = new Vector2(cell, cell * tilt);
            foreach (var pair in sides)
            {
                if (pair.Key == spoutCell) continue;
                PipeTile(pair.Key, pair.Value, size);
            }
            // The spout last, over the pipes that meet it.
            SpoutTile(spoutCell, sides[spoutCell], size);
            LayCouplings(sides, size);
            foreach (var pair in starts)
            {
                // The plate where the pipe leaves its door, on its first cell outside the door's picture.
                var start = new Vector2Int(Mathf.Clamp(pair.Value.Cell.x, boxMinX, boxMaxX), Mathf.Clamp(pair.Value.Cell.y, boxMinY, boxMaxY));
                if (Look.PipeFlange != null && start == pair.Value.Cell) PipeImage(Look.PipeFlange, PipeAt(start), size, Vector2.one);
            }
        }

        /// <summary>
        /// A coupling over each joint where a straight meets a bend, tee, cross or the spout, and every few cells along a
        /// straight run, so the pipe reads as pieces put together without crowding it.
        /// </summary>
        private void LayCouplings(Dictionary<Vector2Int, int> sides, Vector2 size)
        {
            if (Look.PipeCouplingAcross == null || Look.PipeCouplingUp == null) return;
            bool Straight(int s) => s == (PIPE_E | PIPE_W) || s == (PIPE_N | PIPE_S);
            int every = Mathf.Max(1, Look.PipeCouplingEvery);
            foreach (var pair in sides)
            {
                // Each joint once: from a cell to its right and to its top neighbour.
                foreach (var (side, step, sprite) in new[] { (PIPE_E, Vector2Int.right, Look.PipeCouplingAcross), (PIPE_N, Vector2Int.up, Look.PipeCouplingUp) })
                {
                    if ((pair.Value & side) == 0 || !sides.TryGetValue(pair.Key + step, out int other)) continue;
                    bool changes = !Straight(pair.Value) || !Straight(other);
                    var joint = pair.Key + step;
                    bool spaced = ((side == PIPE_E ? joint.x : joint.y) % every + every) % every == 0;
                    if (!changes && !spaced) continue;
                    PipeImage(sprite, (PipeAt(pair.Key) + PipeAt(joint)) * .5f, size, Vector2.one);
                }
            }
        }

        /// <summary>The tile that joins exactly these sides; a cell left with one side is capped.</summary>
        private void PipeTile(Vector2Int cell, int sides, Vector2 size)
        {
            int tile; var flip = Vector2.one;
            switch (sides)
            {
                case PIPE_E | PIPE_W: tile = TILE_H; break;
                case PIPE_N | PIPE_S: tile = TILE_V; break;
                case PIPE_W | PIPE_S: tile = TILE_LB; break;
                case PIPE_E | PIPE_S: tile = TILE_RB; break;
                case PIPE_W | PIPE_N: tile = TILE_LT; break;
                case PIPE_E | PIPE_N: tile = TILE_RT; break;
                case PIPE_W | PIPE_E | PIPE_S: tile = TILE_T_DOWN; break;
                case PIPE_W | PIPE_E | PIPE_N: tile = TILE_T_UP; break;
                case PIPE_N | PIPE_S | PIPE_W: tile = TILE_T_LEFT; break;
                case PIPE_N | PIPE_S | PIPE_E: tile = TILE_T_RIGHT; break;
                case PIPE_N | PIPE_E | PIPE_S | PIPE_W: tile = TILE_CROSS; break;
                case PIPE_W: tile = TILE_CAP; break;
                case PIPE_E: tile = TILE_CAP; flip = new Vector2(-1, 1); break;
                // A cap facing up or down is not drawn; the pipe just ends straight.
                case PIPE_N:
                case PIPE_S: tile = TILE_V; break;
                default: return;
            }
            PipeImage(Look.PipeTiles[tile], PipeAt(cell), size, flip);
        }

        /// <summary>
        /// The spout: from one side it bends into the nozzle, from both sides it is a tee into it. Mirrored for a pipe
        /// from the right, upside down under a bowl on the front wall.
        /// </summary>
        private void SpoutTile(Vector2Int cell, int sides, Vector2 size)
        {
            bool left = (sides & PIPE_W) != 0, right = (sides & PIPE_E) != 0;
            var tile = left && right ? TILE_SPOUT_TEE : TILE_SPOUT;
            var flip = new Vector2(!left && right ? -1 : 1, _bowlTop ? 1 : -1);
            PipeImage(Look.PipeTiles[tile], PipeAt(cell), size, flip);
            // A pipe that comes into the spout from behind it - from above it on the top wall - gets its own piece over it.
            int back = _bowlTop ? PIPE_N : PIPE_S;
            if ((sides & back) != 0) PipeImage(Look.PipeTiles[TILE_V], PipeAt(cell) + new Vector2(0, (_bowlTop ? .25f : -.25f) * size.y), new Vector2(size.x, size.y * .5f), Vector2.one);
        }

        /// <summary>Adds the box's corners met going the shorter way round from one point on its edge to another.</summary>
        private static void AroundBox(Vector2 from, Vector2 to, float xMin, float xMax, float yMin, float yMax, List<Vector2> path)
        {
            float w = xMax - xMin, h = yMax - yMin, length = 2 * (w + h);
            float At(Vector2 p)
            {
                if (Mathf.Abs(p.y - yMax) < .01f) return Mathf.Clamp(p.x - xMin, 0, w);
                if (Mathf.Abs(p.x - xMax) < .01f) return w + Mathf.Clamp(yMax - p.y, 0, h);
                if (Mathf.Abs(p.y - yMin) < .01f) return w + h + Mathf.Clamp(xMax - p.x, 0, w);
                return 2 * w + h + Mathf.Clamp(p.y - yMin, 0, h);
            }
            var corners = new[] { (w, new Vector2(xMax, yMax)), (w + h, new Vector2(xMax, yMin)), (2 * w + h, new Vector2(xMin, yMin)), (length, new Vector2(xMin, yMax)) };
            float a = At(from), b = At(to);
            float clockwise = (b - a + length) % length;
            bool cw = clockwise <= length - clockwise;
            float distance = cw ? clockwise : length - clockwise;
            var met = new List<(float, Vector2)>();
            foreach (var (at, point) in corners)
            {
                float along = cw ? (at - a + length) % length : (a - at + length) % length;
                if (along > .01f && along < distance - .01f) met.Add((along, point));
            }
            met.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            foreach (var (_, point) in met) path.Add(point);
        }

        private void PipeImage(Sprite sprite, Vector2 at, Vector2 size, Vector2 scale)
        {
            Image image;
            if (_pipeImagesUsed < _pipeImages.Count) image = _pipeImages[_pipeImagesUsed];
            else
            {
                image = new GameObject("Img_Pipe", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.gameObject.layer = gameObject.layer;
                image.rectTransform.SetParent(_pipeLayer, false);
                image.raycastTarget = false;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(.5f, .5f);
                _pipeImages.Add(image);
            }
            _pipeImagesUsed++;
            image.gameObject.SetActive(true);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Look.PipeTint;
            image.rectTransform.anchoredPosition = at;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.localScale = new Vector3(scale.x, scale.y, 1);
            image.rectTransform.SetAsLastSibling();
        }

        // ---------- food on its way ----------

        /// <summary>A jelly went into this door: a blob of its colour sets off down the door's pipe.</summary>
        private void FeedFromDoor(int door, Color colour)
        {
            if (!_pipes.TryGetValue(door, out var pipe) || pipe.Length <= 0) return;
            var image = FoodImage(_pipeLayer, Look.FoodBlob);
            // The blob is painted as food already: tinted over, a jelly's colour turned it muddy. A touch of the colour stays.
            // The food art is white: it takes the colour of the jelly that went into the door.
            image.color = colour;
            image.rectTransform.sizeDelta = Vector2.one * _cell * .42f;
            image.rectTransform.anchoredPosition = pipe.Path[0];
            _foodBlobs.Add(new FoodBlob { Image = image, Pipe = pipe, Pieces = Look.FoodPerJelly, Colour = colour });
        }

        private Image FoodImage(RectTransform layer, Sprite sprite)
        {
            Image image;
            if (_freeFoodImages.Count > 0) { image = _freeFoodImages[^1]; _freeFoodImages.RemoveAt(_freeFoodImages.Count - 1); }
            else
            {
                image = new GameObject("Img_Food", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.gameObject.layer = gameObject.layer;
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(.5f, .5f);
            }
            image.rectTransform.SetParent(layer, false);
            image.rectTransform.SetAsLastSibling();
            image.rectTransform.localRotation = Quaternion.identity;
            image.rectTransform.localScale = Vector3.one;
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.color = Color.white;
            image.gameObject.SetActive(true);
            return image;
        }

        private void Free(Image image)
        {
            if (image == null) return;
            image.gameObject.SetActive(false);
            _freeFoodImages.Add(image);
        }

        private void StepFeeding(float dt)
        {
            if (_feeder == null) return;
            float cell = _cell;
            for (int i = _foodBlobs.Count - 1; i >= 0; i--)
            {
                var blob = _foodBlobs[i];
                blob.Travelled += Look.FoodSpeed * cell * dt;
                if (blob.Travelled >= blob.Pipe.Length)
                {
                    Pour(blob.Pieces, blob.Colour);
                    Free(blob.Image);
                    _foodBlobs.RemoveAt(i);
                    continue;
                }
                blob.Image.rectTransform.anchoredPosition = AlongPipe(blob.Pipe, blob.Travelled);
                // A little squash as it squeezes along.
                float wobble = 1 + Mathf.Sin(blob.Travelled / cell * 9f) * .08f;
                blob.Image.rectTransform.localScale = new Vector3(wobble, 2 - wobble, 1);
            }
            float bowlTop = _bowlAt.y + cell * .3f;
            for (int i = _foodPieces.Count - 1; i >= 0; i--)
            {
                var piece = _foodPieces[i];
                if (piece.Delay > 0) { piece.Delay -= dt; piece.Image.enabled = false; continue; }
                if (piece.Puff)
                {
                    piece.PuffTime += dt;
                    float k = piece.PuffTime / .35f;
                    piece.Image.rectTransform.localScale = Vector3.one * Mathf.Lerp(.6f, 1.2f, k);
                    piece.Image.color = new Color(piece.Colour.r, piece.Colour.g, piece.Colour.b, 1 - k);
                    if (k >= 1) { Free(piece.Image); _foodPieces.RemoveAt(i); }
                    continue;
                }
                piece.Image.enabled = true;
                piece.Velocity.y -= PIECE_GRAVITY * cell * dt;
                piece.Position += piece.Velocity * dt;
                piece.Image.rectTransform.anchoredPosition = piece.Position;
                piece.Image.rectTransform.localRotation = Quaternion.Euler(0, 0, piece.Image.rectTransform.localEulerAngles.z + piece.Spin * dt);
                if (piece.Velocity.y < 0 && piece.Position.y <= bowlTop)
                {
                    _bowlFood++;
                    // The bowl fills with the colour of what fell in last.
                    _bowlColour = piece.Colour;
                    ShowBowl();
                    // Lands with a puff where it fell.
                    if (Look.FoodPuff != null)
                    {
                        piece.Puff = true; piece.PuffTime = 0;
                        piece.Image.sprite = Look.FoodPuff;
                        piece.Image.rectTransform.localRotation = Quaternion.identity;
                        piece.Image.rectTransform.anchoredPosition = new Vector2(piece.Position.x, bowlTop);
                    }
                    else { Free(piece.Image); _foodPieces.RemoveAt(i); }
                    CallFeeder();
                }
            }
            StepEating(dt);
        }

        private Vector2 AlongPipe(Pipe pipe, float distance)
        {
            for (int i = 1; i < pipe.Path.Count; i++)
            {
                float step = Vector2.Distance(pipe.Path[i - 1], pipe.Path[i]);
                if (distance <= step) return Vector2.Lerp(pipe.Path[i - 1], pipe.Path[i], step > 0 ? distance / step : 1);
                distance -= step;
            }
            return pipe.Path[^1];
        }

        /// <summary>The blob reaches the spout: it comes out as a few pieces that fall into the bowl.</summary>
        private void Pour(int pieces, Color colour)
        {
            float cell = _cell;
            for (int i = 0; i < pieces; i++)
            {
                var sprite = Look.FoodPieces != null && Look.FoodPieces.Length > 0 ? Look.FoodPieces[Random.Range(0, Look.FoodPieces.Length)] : null;
                var image = FoodImage(_catLayer, sprite);
                // Over the bowl, under the cats.
                image.rectTransform.SetSiblingIndex(_bowl != null ? _bowl.rectTransform.GetSiblingIndex() + 1 : 0);
                image.rectTransform.sizeDelta = Vector2.one * cell * .3f;
                image.color = colour;
                image.rectTransform.anchoredPosition = _nozzle;
                _foodPieces.Add(new FoodPiece
                {
                    Image = image, Colour = colour, Position = _nozzle, Delay = i * .09f, Spin = Random.Range(-360f, 360f),
                    // Out of a spout above, it drops; out of one below the bowl it is tossed up into it.
                    Velocity = new Vector2(Random.Range(-1.2f, 1.2f) * cell, _bowlTop ? Random.Range(0, cell) : Random.Range(7f, 8.5f) * cell),
                });
            }
        }

        private void ShowBowl()
        {
            if (_bowl == null) return;
            int stage = _bowlFood <= 0 ? 0 : _bowlFood <= 2 ? 1 : _bowlFood <= 5 ? 2 : 3;
            // With white piles to hand, the bowl stays empty and the food in it is a pile of the jelly's colour; without
            // them the bowl's own painted fill shows.
            bool piles = Look.FoodPiles != null && Look.FoodPiles.Length >= 3;
            _bowl.sprite = Look.BowlFrames[piles ? 0 : stage];
            if (!piles) return;
            if (_bowlPile == null)
            {
                _bowlPile = new GameObject("Img_CatBowlFood", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                _bowlPile.gameObject.layer = gameObject.layer;
                _bowlPile.raycastTarget = false;
                _bowlPile.preserveAspect = true;
                _bowlPile.rectTransform.anchorMin = _bowlPile.rectTransform.anchorMax = new Vector2(.5f, .5f);
                _bowlPile.rectTransform.pivot = new Vector2(.5f, .5f);
            }
            _bowlPile.rectTransform.SetParent(_bowl.rectTransform.parent, false);
            bool front = Look.BowlFront != null;
            // With the bowl's front to hand the heap is drawn in the bowl, the front wall over its foot; without it the
            // heap peeks out from behind the bowl.
            _bowlPile.rectTransform.SetSiblingIndex(_bowl.rectTransform.GetSiblingIndex() + (front ? 1 : 0));
            _bowlPile.gameObject.SetActive(stage > 0);
            if (front) ShowBowlFront(stage > 0);
            if (stage == 0) return;
            _bowlPile.sprite = Look.FoodPiles[stage - 1];
            _bowlPile.color = _bowlColour;
            if (front)
            {
                // Heaped on the bowl's floor, its foot behind the front rim, rising over the rim as it fills.
                float heap = _cell * (.58f + .04f * stage);
                _bowlPile.rectTransform.sizeDelta = Vector2.one * heap;
                _bowlPile.rectTransform.anchoredPosition = _bowlAt + new Vector2(0, _cell * (.3f + .015f * stage));
                return;
            }
            // Heaped in the bowl's mouth, rising over its rim as it fills.
            float size = _cell * (.5f + .1f * stage);
            _bowlPile.rectTransform.sizeDelta = Vector2.one * size;
            _bowlPile.rectTransform.anchoredPosition = _bowlAt + new Vector2(0, _cell * (.2f + .05f * stage));
        }

        private void ShowBowlFront(bool shown)
        {
            if (_bowlFront == null)
            {
                _bowlFront = new GameObject("Img_CatBowlFront", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                _bowlFront.gameObject.layer = gameObject.layer;
                _bowlFront.raycastTarget = false;
                _bowlFront.preserveAspect = true;
            }
            var rect = _bowlFront.rectTransform;
            rect.SetParent(_bowl.rectTransform.parent, false);
            // Laid exactly over the bowl.
            var bowl = _bowl.rectTransform;
            rect.anchorMin = bowl.anchorMin; rect.anchorMax = bowl.anchorMax; rect.pivot = bowl.pivot;
            rect.sizeDelta = bowl.sizeDelta;
            rect.anchoredPosition = bowl.anchoredPosition;
            rect.SetSiblingIndex(_bowlPile.rectTransform.GetSiblingIndex() + 1);
            _bowlFront.sprite = Look.BowlFront;
            _bowlFront.gameObject.SetActive(shown);
        }

        /// <summary>Food in the bowl: the cat stops what it was doing - unless a finger is on it - and comes to eat.</summary>
        private void CallFeeder()
        {
            var cat = _feeder;
            if (cat == null || cat.Held || cat.Act is CatAct.Notice or CatAct.GoEat or CatAct.Eat or CatAct.Full) return;
            if (cat.Act is CatAct.Petted or CatAct.Rub or CatAct.Reach or CatAct.Belly or CatAct.Hop) return;
            StartCatAct(cat, CatAct.Notice, 0);
        }

        /// <summary>Where the cat stands to eat: beside the bowl on its own side, facing it.</summary>
        private void StartGoEat(Cat cat)
        {
            float side = cat.X <= _bowlAt.x ? -1 : 1;
            cat.TargetX = Mathf.Clamp(_bowlAt.x + side * _cell * .55f, cat.MinX - _cell, cat.MaxX + _cell);
            if (Mathf.Abs(cat.TargetX - cat.X) > .5f) cat.FacingLeft = cat.TargetX < cat.X;
            StartCatAct(cat, CatAct.GoEat, 0);
        }

        private void StepEating(float dt)
        {
            var cat = _feeder;
            bool won = _snapshot != null && _snapshot.Status == PuzzleStatus.Won;
            if (cat.Act == CatAct.Eat)
            {
                // Eating, it faces the bowl.
                cat.FacingLeft = _bowlAt.x < cat.X;
                _eatClock += dt;
                if (_eatClock >= Look.FoodEatSeconds)
                {
                    _eatClock = 0;
                    _bowlFood = Mathf.Max(0, _bowlFood - 1);
                    ShowBowl();
                }
                if (_bowlFood <= 0 && _foodPieces.Count == 0 && _foodBlobs.Count == 0)
                {
                    if (won && !_fedWin) { _fedWin = true; StartCatAct(cat, CatAct.Full, 0); }
                    else StartCatAct(cat, CatAct.Blink, Random.Range(1.2f, 2f));
                }
                return;
            }
            // Won with nothing left to eat: it sits back, full, once.
            if (won && !_fedWin && _bowlFood <= 0 && _foodBlobs.Count == 0 && _foodPieces.Count == 0 && !cat.Held
                && cat.Act is not (CatAct.Notice or CatAct.GoEat or CatAct.Full))
            {
                _fedWin = true;
                StartCatAct(cat, CatAct.Full, 0);
            }
            // Food left in the bowl and nothing better to do: back to it.
            else if (_bowlFood > 0 && !cat.Held && cat.Act is CatAct.Walk or CatAct.Come or CatAct.Sit or CatAct.Lick or CatAct.Blink or CatAct.Curious or CatAct.Sleep)
                StartCatAct(cat, CatAct.Notice, 0);
        }

        private Sprite Eat(int row, int index) => Look.CatEatFrames[row * CAT_FRAMES + index];
    }
}
