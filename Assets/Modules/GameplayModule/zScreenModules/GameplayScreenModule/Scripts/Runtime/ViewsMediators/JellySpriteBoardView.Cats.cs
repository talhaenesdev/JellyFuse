using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// Cats strolling along the top of the wall frame and the front wall: each takes a run of wall with floor
    /// beside it and no door in it, walks up and down it, and now and then sits, washes, stretches, naps, looks
    /// about, swats at nothing or blinks slowly. A finger on a cat is answered: on its body it is petted and leans
    /// in, just above its head it stands up and reaches for the finger, and quick taps make it hop. They are only
    /// for the look - nothing in the game hears of them - and they sit on a canvas of their own, so walking every
    /// frame rebuilds no other part of the board. Only the part of a cat over the wall can be touched, so a cat
    /// never stands between a finger and a jelly.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private enum CatAct
        {
            Walk = 0, SitDown = 1, Sit = 2, Sleep = 3, Stretch = 4, Lick = 5, StandUp = 6,
            Petted = 7, Rub = 8, Reach = 9, Belly = 10, Hop = 11, Blink = 12, Curious = 13, Swat = 14,
            Notice = 15, GoEat = 16, Eat = 17, Full = 18, Come = 19,
        }

        private sealed class Cat
        {
            public Image Image;
            public CatTouchArea Touch;
            /// <summary>The strip of wall the cat walks along: a tap there calls it over.</summary>
            public RectTransform Walkway;
            public float MinX, MaxX, FeetY, X, TargetX, Clock, Left;
            public CatAct Act;
            public bool FacingLeft, Held, Top;
            public int Taps;
            public float LastTap;
        }

        // Rows of the strolling sheet, 8 frames each.
        private const int CAT_ROW_WALK = 0, CAT_ROW_SIT = 1, CAT_ROW_SIT_DOWN = 2, CAT_ROW_SLEEP = 3, CAT_ROW_STRETCH = 4, CAT_ROW_LICK = 5;
        // Rows of the touch sheet.
        private const int TOUCH_ROW_PETTED = 0, TOUCH_ROW_RUB = 1, TOUCH_ROW_REACH = 2, TOUCH_ROW_BELLY = 3, TOUCH_ROW_HOP = 4,
            TOUCH_ROW_BLINK = 5, TOUCH_ROW_CURIOUS = 6, TOUCH_ROW_SWAT = 7;
        private const int CAT_FRAMES = 8;
        /// <summary>The share of a cat's touchable height, from the top, that counts as above its head.</summary>
        private const float CAT_HEAD_SHARE = .4f;
        private const int CAT_HOP_TAPS = 3;
        private const float CAT_TAP_GAP = .6f;

        private RectTransform _catLayer;
        private readonly List<Cat> _cats = new();
        private readonly List<Image> _catPool = new();
        private readonly List<RectTransform> _walkwayPool = new();
        private const float CAT_COME_SPEED = 1.8f;
        private readonly List<(int Y, int MinX, int MaxX, bool Top)> _catRuns = new();

        private bool CatsDrawn => Look.CatFrames != null && Look.CatFrames.Length >= CAT_FRAMES * 6 && Look.CatCount > 0;
        private bool CatTouchDrawn => Look.CatTouchFrames != null && Look.CatTouchFrames.Length >= CAT_FRAMES * 8;

        private void BuildCats()
        {
            foreach (var cat in _cats)
            {
                cat.Image.gameObject.SetActive(false);
                _catPool.Add(cat.Image);
                if (cat.Walkway == null) continue;
                cat.Walkway.gameObject.SetActive(false);
                _walkwayPool.Add(cat.Walkway);
            }
            _cats.Clear();
            if (!CatsDrawn || _level == null) { BuildFeeding(); return; }
            if (_catLayer == null)
            {
                _catLayer = Layer("CatLayer");
                _catLayer.gameObject.AddComponent<Canvas>();
                // A canvas of its own hears no finger without a raycaster of its own.
                _catLayer.gameObject.AddComponent<GraphicRaycaster>();
            }
            _catLayer.SetAsLastSibling();

            FindCatRuns();
            float tilt = Look.SpriteTilt;
            // With the feeding drawn the board keeps one cat, the one by the bowl: on the top wall when there is room
            // there, so the pipes have the shortest way to it.
            int count = Look.CatCount;
            if (FeedingDrawn)
            {
                count = Mathf.Min(count, 1);
                // The fed cat and its pipe are only ever on the top wall: a board with no room there has no cat.
                _catRuns.RemoveAll(run => !run.Top);
            }
            for (int i = 0; i < _catRuns.Count && _cats.Count < count; i++)
            {
                var run = _catRuns[i];
                var image = TakeCatImage();
                float size = Look.CatSize * _cell;
                image.rectTransform.sizeDelta = new Vector2(size, size);
                image.color = Look.CatTints != null && Look.CatTints.Length > 0 ? Look.CatTints[Random.Range(0, Look.CatTints.Length)] : Color.white;
                // The run's ends, pulled in by half a cat so it never hangs off the wall.
                float half = Look.CatSize * .35f;
                float feet = run.Top ? run.Y + .5f + Look.CatFeetHeight : run.Y - .5f - Look.CatBottomFeetDepth;
                var low = Position(new Vector2(run.MinX - .5f + half, feet));
                var high = Position(new Vector2(run.MaxX + .5f - half, feet));
                var cat = new Cat { Image = image, Touch = image.GetComponentInChildren<CatTouchArea>(true), MinX = low.x, MaxX = Mathf.Max(low.x, high.x), FeetY = low.y * tilt, Top = run.Top };
                // On the front wall the cat's head reaches over the last row of the floor: only what stands over the
                // wall can be touched there, so a drag on that row still takes the jelly.
                float reach = size;
                if (!run.Top) reach = Mathf.Clamp(Position(new Vector2(0, run.Y - .5f)).y * tilt - cat.FeetY, size * .35f, size);
                ShapeCatTouch(cat, size, reach);
                ShapeWalkway(cat, low.x - half * _cell, high.x + half * _cell);
                cat.X = Random.Range(cat.MinX, cat.MaxX);
                cat.FacingLeft = Random.value < .5f;
                // A level opens with its cats already settled, not on their way somewhere.
                StartCatAct(cat, CatAct.Sit, Random.Range(1.5f, 4f));
                cat.Clock = Random.Range(0f, 1f);
                _cats.Add(cat);
                PlaceCat(cat);
            }
            BuildFeeding();
        }

        /// <summary>
        /// Runs of floor with wall, not door, beside them on one side: above - the top of the wall frame and
        /// the tops of walls painted in from it - or below, the front wall. A door's cells are left alone, and the cat
        /// keeps half its width back from each end, so no cat stands over the jelly a door shows. The longest runs come first.
        /// </summary>
        private void FindCatRuns()
        {
            _catRuns.Clear();
            var doors = new HashSet<Vector2Int>();
            foreach (var gate in _level.Gates) foreach (var cell in gate.Cells) doors.Add(cell);
            FindCatRuns(doors, Vector2Int.up, true);
            FindCatRuns(doors, Vector2Int.down, false);
            _catRuns.Sort((a, b) => (b.MaxX - b.MinX).CompareTo(a.MaxX - a.MinX));
        }

        private void FindCatRuns(HashSet<Vector2Int> doors, Vector2Int side, bool top)
        {
            for (int y = 0; y < _level.Height; y++)
            {
                int start = -1;
                for (int x = 0; x <= _level.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var wall = cell + side;
                    bool edge = x < _level.Width && IsFloor(cell) && _outside.Contains(wall) && !doors.Contains(wall);
                    if (edge && start < 0) start = x;
                    if (!edge && start >= 0)
                    {
                        // The fed cat takes even one cell of top wall - a door can fill the rest of it - and mostly sits by its bowl.
                        int least = top && FeedingDrawn ? 1 : Look.CatMinRun;
                        if (x - start >= least) _catRuns.Add((y, start, x - 1, top));
                        start = -1;
                    }
                }
            }
        }

        private Image TakeCatImage()
        {
            Image image;
            if (_catPool.Count > 0) { image = _catPool[^1]; _catPool.RemoveAt(_catPool.Count - 1); }
            else
            {
                var go = new GameObject("Img_Cat", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = gameObject.layer;
                go.transform.SetParent(_catLayer, false);
                image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.rectTransform.pivot = new Vector2(.5f, 0);
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
                // The touchable part: an invisible box over the cat's body, from its feet up.
                var hit = new GameObject("Hit_Cat", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CatTouchArea));
                hit.layer = go.layer;
                var hitRect = (RectTransform)hit.transform;
                hitRect.SetParent(go.transform, false);
                hitRect.anchorMin = hitRect.anchorMax = new Vector2(.5f, 0);
                hitRect.pivot = new Vector2(.5f, 0);
                var hitImage = hit.GetComponent<Image>();
                hitImage.color = new Color(1, 1, 1, 0);
                hitImage.raycastTarget = true;
            }
            image.gameObject.SetActive(true);
            return image;
        }

        private void ShapeCatTouch(Cat cat, float size, float reach)
        {
            if (cat.Touch == null) return;
            ((RectTransform)cat.Touch.transform).sizeDelta = new Vector2(size * .7f, reach);
            // A cat without the touch art is only to look at.
            cat.Touch.GetComponent<Image>().raycastTarget = CatTouchDrawn;
            cat.Touch.Pressed = height => OnCatPressed(cat, height);
            cat.Touch.Held = height => OnCatHeld(cat, height);
            cat.Touch.Released = () => OnCatReleased(cat);
        }

        /// <summary>
        /// An invisible strip over the wall the cat walks on, behind the cats so a touch on the cat itself is the cat's.
        /// It only covers wall, never floor, so it takes no jelly's tap; the press still reaches the board for a pan.
        /// </summary>
        private void ShapeWalkway(Cat cat, float left, float right)
        {
            RectTransform strip;
            if (_walkwayPool.Count > 0) { strip = _walkwayPool[^1]; _walkwayPool.RemoveAt(_walkwayPool.Count - 1); }
            else
            {
                var go = new GameObject("Hit_CatWalkway", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CatTouchArea));
                go.layer = gameObject.layer;
                strip = (RectTransform)go.transform;
                strip.SetParent(_catLayer, false);
                strip.anchorMin = strip.anchorMax = new Vector2(.5f, .5f);
                strip.pivot = new Vector2(0, 0);
                var image = go.GetComponent<Image>();
                image.color = new Color(1, 1, 1, 0);
                image.raycastTarget = true;
            }
            strip.gameObject.SetActive(true);
            strip.SetAsFirstSibling();
            // From a little under the cat's feet to the top of the wall it stands on.
            float below = _cell * .25f, above = _cell * (cat.Top ? .45f : .35f);
            strip.anchoredPosition = new Vector2(left, cat.FeetY - below);
            strip.sizeDelta = new Vector2(Mathf.Max(right - left, _cell * .6f), below + above);
            var touch = strip.GetComponent<CatTouchArea>();
            touch.Pressed = null;
            touch.Held = null;
            touch.Released = null;
            touch.PressedAcross = across => CallCat(cat, Mathf.Lerp(left, right, across));
            cat.Walkway = strip;
        }

        /// <summary>A tap on its wall: the cat turns and comes to the finger, then looks up at it.</summary>
        private void CallCat(Cat cat, float x)
        {
            // Food comes first.
            if (cat.Act is CatAct.Notice or CatAct.GoEat or CatAct.Eat or CatAct.Full) return;
            float target = Mathf.Clamp(x, cat.MinX, cat.MaxX);
            if (Mathf.Abs(target - cat.X) < _cell * .3f)
            {
                cat.FacingLeft = x < cat.X;
                StartCatAct(cat, CatTouchDrawn ? CatAct.Curious : CatAct.Sit, Random.Range(1.5f, 2.5f));
                return;
            }
            cat.TargetX = target;
            cat.FacingLeft = target < cat.X;
            StartCatAct(cat, CatAct.Come, 0);
        }

        // ---------- a finger on a cat ----------

        private void OnCatPressed(Cat cat, float height)
        {
            if (!CatTouchDrawn) return;
            float now = Time.unscaledTime;
            cat.Taps = now - cat.LastTap <= CAT_TAP_GAP ? cat.Taps + 1 : 1;
            cat.LastTap = now;
            cat.Held = true;
            if (cat.Taps >= CAT_HOP_TAPS) { cat.Taps = 0; StartCatAct(cat, CatAct.Hop, 0); return; }
            if (height >= 1 - CAT_HEAD_SHARE) { StartCatAct(cat, CatAct.Reach, 1.2f); return; }
            float roll = Random.value;
            if (roll < .15f) StartCatAct(cat, CatAct.Belly, 0);
            else StartCatAct(cat, roll < .55f ? CatAct.Petted : roll < .85f ? CatAct.Rub : CatAct.Blink, Random.Range(2.5f, 4f));
        }

        private void OnCatHeld(Cat cat, float height)
        {
            if (!CatTouchDrawn || !cat.Held) return;
            // Stroked from the body up past the head: it stops leaning in and reaches for the finger instead.
            if (height >= 1 - CAT_HEAD_SHARE && cat.Act != CatAct.Reach && cat.Act != CatAct.Hop && cat.Act != CatAct.Belly) StartCatAct(cat, CatAct.Reach, 1.2f);
            if (cat.Act is CatAct.Reach or CatAct.Petted or CatAct.Rub) cat.Left = Mathf.Max(cat.Left, 1.2f);
        }

        private static void OnCatReleased(Cat cat) => cat.Held = false;

        // ---------- every frame ----------

        private void StepCats(float dt)
        {
            if (_cats.Count == 0) return;
            StepFeeding(dt);
            foreach (var cat in _cats)
            {
                cat.Clock += dt;
                switch (cat.Act)
                {
                    case CatAct.Walk:
                    case CatAct.GoEat:
                    case CatAct.Come:
                        // Hungry, it trots; called, it hurries.
                        float step = Look.CatSpeed * _cell * dt * (cat.Act == CatAct.GoEat ? 2.2f : cat.Act == CatAct.Come ? CAT_COME_SPEED : 1);
                        float gap = cat.TargetX - cat.X;
                        if (Mathf.Abs(gap) <= step) { cat.X = cat.TargetX; NextCatAct(cat); }
                        else cat.X += Mathf.Sign(gap) * step;
                        break;
                    case CatAct.Eat:
                        // The feeding decides when it has eaten its fill.
                        break;
                    case CatAct.SitDown:
                    case CatAct.Stretch:
                    case CatAct.StandUp:
                    case CatAct.Belly:
                    case CatAct.Hop:
                    case CatAct.Swat:
                    case CatAct.Notice:
                    case CatAct.Full:
                        if (cat.Clock * Look.CatFps >= CAT_FRAMES) NextCatAct(cat);
                        break;
                    default:
                        // A finger still on the cat keeps it there.
                        if (!cat.Held || cat.Act is not (CatAct.Reach or CatAct.Petted or CatAct.Rub)) cat.Left -= dt;
                        if (cat.Left <= 0) NextCatAct(cat);
                        break;
                }
                PlaceCat(cat);
            }
        }

        /// <summary>What a cat does when it has finished what it was doing.</summary>
        private void NextCatAct(Cat cat)
        {
            switch (cat.Act)
            {
                case CatAct.SitDown: StartCatAct(cat, Random.value < .4f ? CatAct.Lick : CatAct.Sit, Random.Range(4f, 8f)); return;
                case CatAct.Sit:
                case CatAct.Lick: StartCatAct(cat, CatAct.StandUp, 0); return;
                case CatAct.Sleep: StartCatAct(cat, CatAct.Stretch, 0); return;
                // Loved, it sits a moment and blinks before it wanders off.
                case CatAct.Petted:
                case CatAct.Rub:
                case CatAct.Belly:
                case CatAct.Hop: StartCatAct(cat, CatAct.Blink, Random.Range(1.2f, 2f)); return;
                // Food: it looks up, then trots to the bowl and eats.
                case CatAct.Notice: StartGoEat(cat); return;
                case CatAct.GoEat: StartCatAct(cat, CatAct.Eat, 0); return;
                case CatAct.Full: StartCatAct(cat, CatAct.Blink, Random.Range(3f, 5f)); return;
                // Called over: it looks up at the finger, then sits.
                case CatAct.Come: StartCatAct(cat, CatTouchDrawn ? CatAct.Curious : CatAct.SitDown, Random.Range(1.5f, 2.5f)); return;
            }
            // A cat mostly stays put: after a walk it always settles into something, and after that it walks on
            // only now and then - otherwise it finds something else to do where it is.
            bool walked = cat.Act == CatAct.Walk;
            if (!walked && Random.value < Look.CatWalkChance) { StartCatWalk(cat); return; }
            float roll = Random.value;
            bool touchArt = CatTouchDrawn;
            if (roll < .34f) StartCatAct(cat, CatAct.SitDown, 0);
            else if (roll < .50f) StartCatAct(cat, CatAct.Sleep, Random.Range(6f, 11f));
            else if (roll < .58f || !touchArt) StartCatAct(cat, CatAct.Stretch, 0);
            else if (roll < .76f) StartCatAct(cat, CatAct.Curious, Random.Range(3f, 5f));
            else if (roll < .84f) StartCatAct(cat, CatAct.Swat, 0);
            else StartCatAct(cat, CatAct.Blink, Random.Range(2.5f, 4f));
        }

        private void StartCatWalk(Cat cat)
        {
            // A short stroll: somewhere one to two and a half cells away along its run, within the run.
            float span = cat.MaxX - cat.MinX, least = Mathf.Min(span * .5f, _cell);
            float most = Mathf.Max(least, _cell * 2.5f);
            float target = cat.X;
            for (int i = 0; i < 8 && (Mathf.Abs(target - cat.X) < least || Mathf.Abs(target - cat.X) > most); i++) target = Random.Range(cat.MinX, cat.MaxX);
            cat.TargetX = target;
            if (Mathf.Abs(target - cat.X) > .5f) cat.FacingLeft = target < cat.X;
            StartCatAct(cat, CatAct.Walk, 0);
        }

        private static void StartCatAct(Cat cat, CatAct act, float seconds)
        {
            cat.Act = act;
            cat.Clock = 0;
            cat.Left = seconds;
        }

        private void PlaceCat(Cat cat)
        {
            int frame = (int)(cat.Clock * Look.CatFps);
            int once = Mathf.Min(frame, CAT_FRAMES - 1), loop = frame % CAT_FRAMES;
            Sprite sprite = cat.Act switch
            {
                CatAct.SitDown => Walk(CAT_ROW_SIT_DOWN, once),
                // Standing up is sitting down played backwards.
                CatAct.StandUp => Walk(CAT_ROW_SIT_DOWN, CAT_FRAMES - 1 - once),
                CatAct.Stretch => Walk(CAT_ROW_STRETCH, once),
                CatAct.Sit => Walk(CAT_ROW_SIT, loop),
                CatAct.Lick => Walk(CAT_ROW_LICK, loop),
                // A sleeping cat breathes slowly.
                CatAct.Sleep => Walk(CAT_ROW_SLEEP, (int)(cat.Clock * Look.CatFps * .5f) % CAT_FRAMES),
                CatAct.Petted => Touch(TOUCH_ROW_PETTED, loop),
                CatAct.Rub => Touch(TOUCH_ROW_RUB, loop),
                CatAct.Reach => Touch(TOUCH_ROW_REACH, loop),
                CatAct.Belly => Touch(TOUCH_ROW_BELLY, once),
                CatAct.Hop => Touch(TOUCH_ROW_HOP, once),
                CatAct.Blink => Touch(TOUCH_ROW_BLINK, loop),
                CatAct.Curious => Touch(TOUCH_ROW_CURIOUS, loop),
                CatAct.Swat => Touch(TOUCH_ROW_SWAT, once),
                CatAct.Notice => FeedingDrawn ? Eat(0, once) : Walk(CAT_ROW_SIT, loop),
                CatAct.Eat => FeedingDrawn ? Eat(1, loop) : Walk(CAT_ROW_SIT, loop),
                CatAct.Full => FeedingDrawn ? Eat(2, once) : Walk(CAT_ROW_SIT, loop),
                _ => Walk(CAT_ROW_WALK, loop),
            };
            if (cat.Image.sprite != sprite) cat.Image.sprite = sprite;
            var rect = cat.Image.rectTransform;
            rect.anchoredPosition = new Vector2(cat.X, cat.FeetY);
            rect.localScale = new Vector3(cat.FacingLeft ? -1 : 1, 1, 1);
        }

        private Sprite Walk(int row, int index) => Look.CatFrames[row * CAT_FRAMES + index];
        // Without the touch art a touch act never starts, but a cat already in one keeps the stroll's sitting frame.
        private Sprite Touch(int row, int index) => CatTouchDrawn ? Look.CatTouchFrames[row * CAT_FRAMES + index] : Walk(CAT_ROW_SIT, index);
    }
}
