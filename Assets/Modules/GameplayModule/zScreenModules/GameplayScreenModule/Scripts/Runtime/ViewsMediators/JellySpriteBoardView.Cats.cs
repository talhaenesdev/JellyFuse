using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// Cats strolling along the top of the wall frame: each takes a run of wall with floor under it and no
    /// door in it, walks up and down it, and now and then sits, washes, stretches or naps. They are only
    /// for the look - nothing touches them and they touch nothing - and they sit on a canvas of their own,
    /// so walking every frame rebuilds no other part of the board.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private enum CatAct { Walk = 0, SitDown = 1, Sit = 2, Sleep = 3, Stretch = 4, Lick = 5, StandUp = 6 }

        private sealed class Cat
        {
            public Image Image;
            public float MinX, MaxX, FeetY, X, TargetX, Clock, Left;
            public CatAct Act;
            public bool FacingLeft;
        }

        // Rows of the sheet, 8 frames each.
        private const int CAT_ROW_WALK = 0, CAT_ROW_SIT = 1, CAT_ROW_SIT_DOWN = 2, CAT_ROW_SLEEP = 3, CAT_ROW_STRETCH = 4, CAT_ROW_LICK = 5;
        private const int CAT_FRAMES = 8;

        private RectTransform _catLayer;
        private readonly List<Cat> _cats = new();
        private readonly List<Image> _catPool = new();
        private readonly List<(int Y, int MinX, int MaxX, bool Top)> _catRuns = new();

        private bool CatsDrawn => Look.CatFrames != null && Look.CatFrames.Length >= CAT_FRAMES * 6 && Look.CatCount > 0;

        private void BuildCats()
        {
            foreach (var cat in _cats) { cat.Image.gameObject.SetActive(false); _catPool.Add(cat.Image); }
            _cats.Clear();
            if (!CatsDrawn || _level == null) return;
            if (_catLayer == null)
            {
                _catLayer = Layer("CatLayer");
                _catLayer.gameObject.AddComponent<Canvas>();
            }
            _catLayer.SetAsLastSibling();

            FindCatRuns();
            float tilt = Look.SpriteTilt;
            for (int i = 0; i < _catRuns.Count && _cats.Count < Look.CatCount; i++)
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
                var cat = new Cat { Image = image, MinX = low.x, MaxX = Mathf.Max(low.x, high.x), FeetY = low.y * tilt };
                cat.X = Random.Range(cat.MinX, cat.MaxX);
                cat.Clock = Random.Range(0f, 1f);
                StartCatWalk(cat);
                _cats.Add(cat);
                PlaceCat(cat);
            }
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
                        if (x - start >= Look.CatMinRun) _catRuns.Add((y, start, x - 1, top));
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
            }
            image.gameObject.SetActive(true);
            return image;
        }

        private void StepCats(float dt)
        {
            if (_cats.Count == 0) return;
            foreach (var cat in _cats)
            {
                cat.Clock += dt;
                switch (cat.Act)
                {
                    case CatAct.Walk:
                        float step = Look.CatSpeed * _cell * dt;
                        float gap = cat.TargetX - cat.X;
                        if (Mathf.Abs(gap) <= step) { cat.X = cat.TargetX; NextCatAct(cat); }
                        else cat.X += Mathf.Sign(gap) * step;
                        break;
                    case CatAct.SitDown:
                    case CatAct.Stretch:
                    case CatAct.StandUp:
                        if (cat.Clock * Look.CatFps >= CAT_FRAMES) NextCatAct(cat);
                        break;
                    default:
                        cat.Left -= dt;
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
                case CatAct.SitDown: StartCatAct(cat, Random.value < .4f ? CatAct.Lick : CatAct.Sit, Random.Range(2.5f, 5f)); return;
                case CatAct.Sit:
                case CatAct.Lick: StartCatAct(cat, CatAct.StandUp, 0); return;
                case CatAct.Sleep: StartCatAct(cat, CatAct.Stretch, 0); return;
            }
            float roll = Random.value;
            if (cat.Act == CatAct.Walk && roll < .3f) StartCatAct(cat, CatAct.SitDown, 0);
            else if (cat.Act == CatAct.Walk && roll < .42f) StartCatAct(cat, CatAct.Sleep, Random.Range(5f, 9f));
            else if (cat.Act == CatAct.Walk && roll < .52f) StartCatAct(cat, CatAct.Stretch, 0);
            else StartCatWalk(cat);
        }

        private void StartCatWalk(Cat cat)
        {
            // Somewhere at least a cell and a half away along its run, when the run has that much room.
            float span = cat.MaxX - cat.MinX, least = Mathf.Min(span * .5f, _cell * 1.5f);
            float target = cat.X;
            for (int i = 0; i < 6 && Mathf.Abs(target - cat.X) < least; i++) target = Random.Range(cat.MinX, cat.MaxX);
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
            int row, index;
            switch (cat.Act)
            {
                case CatAct.SitDown: row = CAT_ROW_SIT_DOWN; index = Mathf.Min(frame, CAT_FRAMES - 1); break;
                // Standing up is sitting down played backwards.
                case CatAct.StandUp: row = CAT_ROW_SIT_DOWN; index = CAT_FRAMES - 1 - Mathf.Min(frame, CAT_FRAMES - 1); break;
                case CatAct.Stretch: row = CAT_ROW_STRETCH; index = Mathf.Min(frame, CAT_FRAMES - 1); break;
                case CatAct.Sit: row = CAT_ROW_SIT; index = frame % CAT_FRAMES; break;
                case CatAct.Lick: row = CAT_ROW_LICK; index = frame % CAT_FRAMES; break;
                // A sleeping cat breathes slowly.
                case CatAct.Sleep: row = CAT_ROW_SLEEP; index = (int)(cat.Clock * Look.CatFps * .5f) % CAT_FRAMES; break;
                default: row = CAT_ROW_WALK; index = frame % CAT_FRAMES; break;
            }
            var sprite = Look.CatFrames[row * CAT_FRAMES + index];
            if (cat.Image.sprite != sprite) cat.Image.sprite = sprite;
            var rect = cat.Image.rectTransform;
            rect.anchoredPosition = new Vector2(cat.X, cat.FeetY);
            rect.localScale = new Vector3(cat.FacingLeft ? -1 : 1, 1, 1);
        }
    }
}
