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
        }

        private sealed class FoodPiece
        {
            public Image Image;
            public Vector2 Position, Velocity;
            public float Delay, Spin;
            public bool Puff;
            public float PuffTime;
        }

        private const float SPOUT_NOZZLE = .21f;
        private const float PIECE_GRAVITY = 22f;

        private RectTransform _pipeLayer;
        private readonly List<Image> _pipeImages = new();
        private int _pipeImagesUsed;
        private readonly Dictionary<int, Pipe> _pipes = new();
        private readonly List<FoodBlob> _foodBlobs = new();
        private readonly List<FoodPiece> _foodPieces = new();
        private readonly List<Image> _freeFoodImages = new();
        private Image _bowl;
        private Cat _feeder;
        private Vector2 _bowlAt, _nozzle;
        private bool _bowlTop;
        private int _bowlFood;
        private float _eatClock;
        private bool _fedWin;

        private bool FeedingDrawn => Look.CatEatFrames != null && Look.CatEatFrames.Length >= CAT_FRAMES * 3
                                     && Look.PipeStraightX != null && Look.PipeStraightY != null && Look.PipeSpout != null
                                     && Look.PipeCornerLB != null && Look.PipeCornerRB != null && Look.PipeCornerLT != null && Look.PipeCornerRT != null
                                     && Look.BowlFrames != null && Look.BowlFrames.Length >= 4;

        private void BuildFeeding()
        {
            foreach (var blob in _foodBlobs) Free(blob.Image);
            foreach (var piece in _foodPieces) Free(piece.Image);
            _foodBlobs.Clear(); _foodPieces.Clear();
            _pipes.Clear();
            _pipeImagesUsed = 0;
            _feeder = null; _bowlFood = 0; _fedWin = false; _eatClock = 0;
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

        /// <summary>The cat on the top wall is fed if there is one; its bowl stands in the middle of its run.</summary>
        private void LayPipes()
        {
            foreach (var cat in _cats) if (cat.Top) { _feeder = cat; break; }
            _feeder ??= _cats[0];
            _bowlTop = _feeder.Top;
            float cell = _cell, tilt = Look.SpriteTilt;
            _bowlAt = new Vector2((_feeder.MinX + _feeder.MaxX) * .5f, _feeder.FeetY);
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

            // The box the pipes run round: the floor and its frame, every door on it, and the fed cat's head.
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (int y = 0; y < _level.Height; y++) for (int x = 0; x < _level.Width; x++)
            {
                if (!IsFloor(new Vector2Int(x, y))) continue;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
            if (minX > maxX) return;
            float margin = Look.PipeMargin;
            Vector2 S(Vector2 board) { var p = Position(board); return new Vector2(p.x, p.y * tilt); }
            var low = S(new Vector2(minX - .5f - Look.MapFrameLeft - margin, minY - .5f - Look.MapFrameMargin - Look.SpriteBoardDepth - margin));
            var high = S(new Vector2(maxX + .5f + Look.MapFrameRight + margin, maxY + .5f + Look.MapFrameMargin + margin));
            float xMin = low.x, xMax = high.x, yMin = low.y, yMax = high.y;
            var doorStarts = new Dictionary<int, (Vector2 Start, Vector2Int Direction)>();
            for (int g = 0; g < _level.Gates.Count; g++)
            {
                var gate = _level.Gates[g];
                var mid = Vector2.zero;
                foreach (var c in gate.Cells) mid += (Vector2)c;
                mid /= Mathf.Max(1, gate.Cells.Count);
                var start = S(mid + (Vector2)gate.Direction * .5f);
                doorStarts[g] = (start, gate.Direction);
                // A door's picture reaches past the frame: the pipe still runs outside it.
                var outer = S(mid + (Vector2)gate.Direction * 1.1f);
                xMin = Mathf.Min(xMin, outer.x - margin * cell); xMax = Mathf.Max(xMax, outer.x + margin * cell);
                yMin = Mathf.Min(yMin, outer.y - margin * cell); yMax = Mathf.Max(yMax, outer.y + margin * cell);
            }
            // On a board that does not slide, the pipes stay on screen: kept inside the board's rect, and where that is
            // tight they run behind a door's picture rather than off the edge.
            if (!CanPan && _rect != null)
            {
                var bounds = _rect.rect;
                xMin = Mathf.Max(xMin, bounds.xMin + cell * .3f);
                xMax = Mathf.Min(xMax, bounds.xMax - cell * .3f);
                yMin = Mathf.Max(yMin, bounds.yMin + cell * .3f);
            }
            // The pipe passes over the fed cat's head, or under its feet on the front wall.
            if (_bowlTop) yMax = Mathf.Max(yMax, _bowlAt.y + Look.CatSize * cell + cell * .45f);
            else yMin = Mathf.Min(yMin, _bowlAt.y - cell * .7f);
            float edgeY = _bowlTop ? yMax : yMin;
            _nozzle = new Vector2(_bowlAt.x, edgeY + (_bowlTop ? -.5f : .5f) * cell);

            foreach (var pair in doorStarts)
            {
                var (start, d) = pair.Value;
                var onEdge = d.x > 0 ? new Vector2(xMax, start.y) : d.x < 0 ? new Vector2(xMin, start.y) : d.y > 0 ? new Vector2(start.x, yMax) : new Vector2(start.x, yMin);
                var target = new Vector2(_bowlAt.x, edgeY);
                var path = new List<Vector2> { start, onEdge };
                AroundBox(onEdge, target, xMin, xMax, yMin, yMax, path);
                // The last stretch runs along the bowl's edge of the box into the spout, which bends down (or up) to the bowl.
                var before = path[^1];
                float side = before.x <= _bowlAt.x ? 1 : -1;
                var spout = new Vector2(_bowlAt.x - SPOUT_NOZZLE * cell * side, edgeY);
                var entry = new Vector2(spout.x - .5f * cell * side, edgeY);
                if (Mathf.Abs(before.y - edgeY) > .5f) path.Add(new Vector2(before.x, edgeY));
                path.Add(entry);
                Simplify(path);
                var pipe = new Pipe();
                DrawPipe(path, cell);
                DrawSpout(spout, side, cell);
                pipe.Path.AddRange(path);
                pipe.Path.Add(spout + new Vector2(SPOUT_NOZZLE * cell * side, 0));
                pipe.Path.Add(_nozzle);
                for (int i = 1; i < pipe.Path.Count; i++) pipe.Length += Vector2.Distance(pipe.Path[i - 1], pipe.Path[i]);
                _pipes[pair.Key] = pipe;
            }
        }

        /// <summary>Adds the box's corners met going the shorter way round from one point on its edge to another.</summary>
        private static void AroundBox(Vector2 from, Vector2 to, float xMin, float xMax, float yMin, float yMax, List<Vector2> path)
        {
            float w = xMax - xMin, h = yMax - yMin, length = 2 * (w + h);
            float At(Vector2 p)
            {
                if (Mathf.Abs(p.y - yMax) < .5f) return Mathf.Clamp(p.x - xMin, 0, w);
                if (Mathf.Abs(p.x - xMax) < .5f) return w + Mathf.Clamp(yMax - p.y, 0, h);
                if (Mathf.Abs(p.y - yMin) < .5f) return w + h + Mathf.Clamp(xMax - p.x, 0, w);
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
                if (along > .5f && along < distance - .5f) met.Add((along, point));
            }
            met.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            foreach (var (_, point) in met) path.Add(point);
        }

        /// <summary>Drops points on a straight line between their neighbours, and points on top of one another.</summary>
        private static void Simplify(List<Vector2> path)
        {
            for (int i = path.Count - 2; i >= 1; i--)
            {
                var a = path[i - 1]; var b = path[i]; var c = path[i + 1];
                bool same = (b - a).sqrMagnitude < 1 || (c - b).sqrMagnitude < 1;
                bool straight = Mathf.Abs(a.x - b.x) < .5f && Mathf.Abs(b.x - c.x) < .5f || Mathf.Abs(a.y - b.y) < .5f && Mathf.Abs(b.y - c.y) < .5f;
                if (same || straight) path.RemoveAt(i);
            }
        }

        private void DrawPipe(List<Vector2> path, float cell)
        {
            float half = cell * .5f;
            for (int i = 0; i + 1 < path.Count; i++)
            {
                var a = path[i]; var b = path[i + 1];
                var direction = (b - a).normalized;
                // Stops short of a bend by half a cell: the corner piece fills it.
                var from = i > 0 ? a + direction * half : a;
                var to = i + 2 < path.Count ? b - direction * half : b;
                float length = Vector2.Distance(from, to);
                if (length < 1) continue;
                bool across = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);
                PipeImage(across ? Look.PipeStraightX : Look.PipeStraightY, (from + to) * .5f, across ? new Vector2(length + 2, cell) : new Vector2(cell, length + 2), Vector2.one);
            }
            for (int i = 1; i + 1 < path.Count; i++)
            {
                var into = (path[i] - path[i - 1]).normalized; var outOf = (path[i + 1] - path[i]).normalized;
                // The piece joins the side the pipe comes in from to the side it leaves by.
                bool left = into.x > .5f || outOf.x < -.5f, right = into.x < -.5f || outOf.x > .5f;
                bool bottom = into.y > .5f || outOf.y < -.5f;
                Sprite corner = left ? (bottom ? Look.PipeCornerLB : Look.PipeCornerLT) : right ? (bottom ? Look.PipeCornerRB : Look.PipeCornerRT) : null;
                if (corner != null) PipeImage(corner, path[i], Vector2.one * cell, Vector2.one);
            }
        }

        /// <summary>The spout comes in from the left and bends down; mirrored for a pipe from the right, and upside down under a bowl on the front wall.</summary>
        private void DrawSpout(Vector2 at, float side, float cell) =>
            PipeImage(Look.PipeSpout, at, Vector2.one * cell, new Vector2(side, _bowlTop ? 1 : -1));

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
            image.color = colour;
            image.rectTransform.sizeDelta = Vector2.one * _cell * .42f;
            image.rectTransform.anchoredPosition = pipe.Path[0];
            _foodBlobs.Add(new FoodBlob { Image = image, Pipe = pipe, Pieces = Look.FoodPerJelly });
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
                    Pour(blob.Pieces);
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
                    piece.Image.color = new Color(1, 1, 1, 1 - k);
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
        private void Pour(int pieces)
        {
            float cell = _cell;
            for (int i = 0; i < pieces; i++)
            {
                var sprite = Look.FoodPieces != null && Look.FoodPieces.Length > 0 ? Look.FoodPieces[Random.Range(0, Look.FoodPieces.Length)] : null;
                var image = FoodImage(_catLayer, sprite);
                // Over the bowl, under the cats.
                image.rectTransform.SetSiblingIndex(_bowl != null ? _bowl.rectTransform.GetSiblingIndex() + 1 : 0);
                image.rectTransform.sizeDelta = Vector2.one * cell * .3f;
                image.rectTransform.anchoredPosition = _nozzle;
                _foodPieces.Add(new FoodPiece
                {
                    Image = image, Position = _nozzle, Delay = i * .09f, Spin = Random.Range(-360f, 360f),
                    // Out of a spout above, it drops; out of one below the bowl it is tossed up into it.
                    Velocity = new Vector2(Random.Range(-1.2f, 1.2f) * cell, _bowlTop ? Random.Range(0, cell) : Random.Range(7f, 8.5f) * cell),
                });
            }
        }

        private void ShowBowl()
        {
            if (_bowl == null) return;
            int stage = _bowlFood <= 0 ? 0 : _bowlFood <= 2 ? 1 : _bowlFood <= 5 ? 2 : 3;
            _bowl.sprite = Look.BowlFrames[stage];
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
            else if (_bowlFood > 0 && !cat.Held && cat.Act is CatAct.Walk or CatAct.Sit or CatAct.Lick or CatAct.Blink or CatAct.Curious or CatAct.Sleep)
                StartCatAct(cat, CatAct.Notice, 0);
        }

        private Sprite Eat(int row, int index) => Look.CatEatFrames[row * CAT_FRAMES + index];
    }
}
