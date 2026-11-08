using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The board's glow, over what the fuse and the doors already do: a flare where jellies fuse and where one
    /// goes through its door (with confetti), a halo round the held jelly and round every door waiting for its
    /// colour, a spark where a jelly runs into a wall, a light running round the walls on a win, and the walls
    /// glowing red as the clock runs out. Fuses and doors close together count as a combo: each flare grows, and
    /// ComboReached tells the screen. Every glow is a soft quad from one pool on a layer over the board; when the
    /// device has switched the UI glow off (_UIGlowStrength 0) none is drawn, and the bits and confetti stay.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private sealed class Flare
        {
            public Image Image;
            public Vector2 Position;
            public Color Colour;
            public float Delay, Age, Life, From, To, Alpha;
        }
        private const float COMBO_WINDOW = 2.5f;
        private static readonly int GLOW_STRENGTH = Shader.PropertyToID("_UIGlowStrength");

        private RectTransform _fxLayer, _fxBackLayer;
        private Sprite _glowSprite;
        private readonly List<Flare> _flares = new();
        private readonly List<Flare> _freeFlares = new();
        private readonly List<Image> _steady = new();
        private int _steadyUsed;
        private Image _heldHalo;
        private readonly List<(Vector2 Point, Vector2 Normal)> _edges = new();
        private JellyLevelCVO _edgesFor;
        private float _comboClock = 99;
        private int _combo;
        private float _urgency;
        private PuzzleStatus _lastStatus = PuzzleStatus.Playing;

        /// <summary>Fuses and doors close together: how many in a row, from 2 on.</summary>
        public event System.Action<int> ComboReached;

        private bool GlowOn => Shader.GetGlobalFloat(GLOW_STRENGTH) > .5f;

        /// <summary>0 while there is time, rising to 1 as the clock runs out: the walls glow red with it.</summary>
        public void SetUrgency(float urgency) => _urgency = Mathf.Clamp01(urgency);

        // ---------- what starts an effect ----------

        /// <summary>Jellies fused: a flare as big as the new jelly, bigger with the combo.</summary>
        private void FxFuse(IReadOnlyList<Vector2Int> cells, Color colour)
        {
            float boost = CountCombo();
            Span(cells, out float spanX, out float spanY);
            float size = Mathf.Max(spanX, spanY) * _cell;
            var centre = Position(CentreOf(cells));
            AddFlare(centre, size * .9f, size * (1.5f + .15f * boost), Color.Lerp(colour, Color.white, .25f), .4f, .5f + .05f * boost, 0);
        }

        /// <summary>A jelly went through its door: a flare at the door, and confetti thrown back out of it.</summary>
        private void FxDeliver(Vector2 mouthFrom, Vector2 mouthTo, Vector2 direction, Color colour, int cells)
        {
            float boost = CountCombo();
            var centre = (mouthFrom + mouthTo) * .5f;
            float width = Mathf.Max(_cell, Vector2.Distance(mouthFrom, mouthTo));
            AddFlare(centre, width * .8f, width * (1.4f + .15f * boost), Color.Lerp(colour, Color.white, .25f), .45f, .5f + .05f * boost, Feel.DoorEnterTime * .5f);
            int count = Mathf.Min(16, 4 + cells * 2 + (int)(boost * 2));
            for (int i = 0; i < count; i++)
            {
                var bit = TakeBit();
                bit.Position = Vector2.Lerp(mouthFrom, mouthTo, Random.value);
                var across = new Vector2(-direction.y, direction.x);
                bit.Velocity = (-direction * Random.Range(.6f, 1.4f) + across * Random.Range(-1f, 1f)) * _cell * Random.Range(1.5f, 3f);
                bit.Life = Random.Range(.5f, 1f);
                bit.Size = _cell * Random.Range(.06f, .11f);
                bit.Colour = Color.HSVToRGB(Random.value, .55f, 1f);
            }
        }

        /// <summary>
        /// Every jelly that slid into a wall gets a spark on its leading edge when it arrives there - the slide's
        /// length over its speed from now.
        /// </summary>
        private void FxBumps(PuzzleSnapshotRVO previous, PuzzleSnapshotRVO snapshot)
        {
            if (previous == null) return;
            foreach (var piece in snapshot.Pieces)
            {
                var old = PieceById(previous, piece.Id);
                if (old == null || old.Cells.Count != piece.Cells.Count) continue;
                var shift = CentreOf(piece.Cells) - CentreOf(old.Cells);
                if (shift.sqrMagnitude < .25f) continue;
                var direction = Mathf.Abs(shift.x) > Mathf.Abs(shift.y) ? new Vector2Int((int)Mathf.Sign(shift.x), 0) : new Vector2Int(0, (int)Mathf.Sign(shift.y));
                float delay = shift.magnitude / Mathf.Max(.1f, Feel.SlideSpeed);
                var colour = Color.Lerp(Tint((int)piece.Colour), Color.white, .6f);
                foreach (var cell in piece.Cells)
                {
                    var next = cell + direction;
                    if (Contains(piece.Cells, next) || IsFloor(next)) continue;
                    var point = Position(cell) + (Vector2)direction * (_cell * .5f);
                    AddFlare(point, _cell * .25f, _cell * .7f, colour, .3f, .4f, delay);
                }
            }
        }

        /// <summary>The level was won: a light runs once round the walls, the edge nearest the top first.</summary>
        private void FxWinWave()
        {
            FindEdges();
            if (_edges.Count == 0) return;
            var centre = Vector2.zero;
            foreach (var edge in _edges) centre += edge.Point;
            centre /= _edges.Count;
            foreach (var edge in _edges)
            {
                var from = edge.Point - centre;
                float angle = Mathf.Repeat(Mathf.Atan2(from.x, from.y) / (2 * Mathf.PI), 1);
                AddFlare(edge.Point, _cell * .4f, _cell * 1f, new Color(1f, .9f, .5f), .45f, .45f, angle * 1.1f);
            }
        }

        /// <summary>One more fuse or door: the combo grows when it comes within COMBO_WINDOW of the last. Returns how far past one it is.</summary>
        private float CountCombo()
        {
            _combo = _comboClock <= COMBO_WINDOW ? _combo + 1 : 1;
            _comboClock = 0;
            if (_combo >= 2) ComboReached?.Invoke(_combo);
            return Mathf.Min(4, _combo - 1);
        }

        /// <summary>Called by Present with the snapshot it now shows.</summary>
        private void FxPresented(PuzzleSnapshotRVO previous, PuzzleSnapshotRVO snapshot)
        {
            FxBumps(previous, snapshot);
            if (snapshot.Status == PuzzleStatus.Won && _lastStatus != PuzzleStatus.Won) FxWinWave();
            _lastStatus = snapshot.Status;
        }

        // ---------- every frame ----------

        /// <summary>Moves the flares on and draws the steady glows; true while anything was drawn.</summary>
        private bool StepFx(float dt)
        {
            _comboClock += dt;
            EnsureFxLayer();
            bool on = GlowOn;
            for (int i = _flares.Count - 1; i >= 0; i--)
            {
                var flare = _flares[i];
                if (flare.Delay > 0) { flare.Delay -= dt; flare.Image.enabled = false; continue; }
                flare.Age += dt;
                float k = flare.Age / flare.Life;
                if (k >= 1 || !on) { flare.Image.gameObject.SetActive(false); _freeFlares.Add(flare); _flares.RemoveAt(i); continue; }
                float grow = 1 - (1 - k) * (1 - k);
                flare.Image.enabled = true;
                flare.Image.rectTransform.anchoredPosition = flare.Position;
                flare.Image.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(flare.From, flare.To, grow);
                flare.Image.color = WithAlpha(flare.Colour, flare.Alpha * (1 - k) * (1 - k));
            }

            _steadyUsed = 0;
            bool heldShown = false;
            if (on && _snapshot != null && _snapshot.Status == PuzzleStatus.Playing)
            {
                float pulse = .5f + .5f * Mathf.Sin(_time * 5f);
                // The held jelly, and every door waiting for its colour.
                if (_held >= 0 && _blobs.TryGetValue(_held, out var blob) && PieceById(_snapshot, _held) is { } held)
                {
                    var colour = Tint((int)held.Colour);
                    float size = Mathf.Max(blob.Span.x, blob.Span.y) * _cell;
                    HeldHalo(blob.Centre + blob.Offset, size * (1.3f + .08f * pulse), colour, .3f + .15f * pulse);
                    heldShown = true;
                    for (int door = 0; door < _level.Gates.Count; door++)
                    {
                        var stage = _snapshot.WaitingStage(door);
                        if (stage == null || stage.Colour != held.Colour || !_doorFrames.TryGetValue(door, out var frame) || frame.Frozen) continue;
                        Steady(frame.Centre, Mathf.Max(frame.Size.x, frame.Size.y) * (1.15f + .1f * pulse), colour, .2f + .15f * pulse);
                    }
                }
                // The clock running out: the walls glow red, faster and stronger as it does.
                if (_urgency > 0)
                {
                    FindEdges();
                    float beat = .5f + .5f * Mathf.Sin(_time * (6f + 8f * _urgency));
                    var red = new Color(1f, .2f, .15f);
                    foreach (var edge in _edges)
                        Steady(edge.Point, _cell * (.9f + .2f * beat), red, (.08f + .17f * beat) * _urgency);
                }
            }
            if (!heldShown && _heldHalo != null && _heldHalo.gameObject.activeSelf) _heldHalo.gameObject.SetActive(false);
            for (int i = _steadyUsed; i < _steady.Count; i++) if (_steady[i].gameObject.activeSelf) _steady[i].gameObject.SetActive(false);
            return _flares.Count > 0 || _steadyUsed > 0 || heldShown;
        }

        // ---------- drawing ----------

        private void AddFlare(Vector2 position, float from, float to, Color colour, float life, float alpha, float delay)
        {
            if (!GlowOn) return;
            EnsureFxLayer();
            Flare flare;
            if (_freeFlares.Count > 0) { flare = _freeFlares[^1]; _freeFlares.RemoveAt(_freeFlares.Count - 1); }
            else flare = new Flare { Image = GlowImage("Img_Flare") };
            flare.Image.gameObject.SetActive(true);
            flare.Image.enabled = false;
            flare.Position = position; flare.From = from; flare.To = to; flare.Colour = colour;
            flare.Life = Mathf.Max(.05f, life); flare.Alpha = alpha; flare.Delay = delay; flare.Age = 0;
            _flares.Add(flare);
        }

        private void Steady(Vector2 position, float size, Color colour, float alpha)
        {
            if (_steadyUsed == _steady.Count) _steady.Add(GlowImage("Img_Glow"));
            var image = _steady[_steadyUsed++];
            if (!image.gameObject.activeSelf) image.gameObject.SetActive(true);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = Vector2.one * size;
            image.color = WithAlpha(colour, Mathf.Clamp01(alpha));
        }

        private void HeldHalo(Vector2 position, float size, Color colour, float alpha)
        {
            if (_heldHalo == null) _heldHalo = GlowImage("Img_HeldHalo", _fxBackLayer);
            if (!_heldHalo.gameObject.activeSelf) _heldHalo.gameObject.SetActive(true);
            _heldHalo.rectTransform.anchoredPosition = position;
            _heldHalo.rectTransform.sizeDelta = Vector2.one * size;
            _heldHalo.color = WithAlpha(colour, Mathf.Clamp01(alpha));
        }

        private Image GlowImage(string name, RectTransform layer = null)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.gameObject.layer = gameObject.layer;
            image.rectTransform.SetParent(layer != null ? layer : _fxLayer, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            image.raycastTarget = false;
            image.sprite = GlowSprite();
            return image;
        }

        /// <summary>
        /// Two layers. The flares, the doors' and the walls' glows draw over the board, under the bits, where they
        /// are seen. The held jelly's halo draws behind the jellies, on the floor round it: over it, it would cover
        /// the lift of the jelly about to fuse.
        /// </summary>
        private void EnsureFxLayer()
        {
            if (_fxLayer != null) return;
            _fxBackLayer = Layer("FxBackLayer");
            _fxBackLayer.SetSiblingIndex(_edgeCornerLayer.GetSiblingIndex() + 1);
            _fxLayer = Layer("FxLayer");
            _fxLayer.SetSiblingIndex(_bitLayer.GetSiblingIndex());
        }

        /// <summary>A soft white disc, made once: every glow is this, tinted.</summary>
        private Sprite GlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "TX_BoardGlow" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2 - 1, dy = (y + .5f) / size * 2 - 1;
                float a = Mathf.Pow(1 - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy)), 1.6f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
            return _glowSprite;
        }

        /// <summary>Where the floor meets a wall, the board's edge or a hole: one point per cell side, with the way out of the floor.</summary>
        private void FindEdges()
        {
            if (_edgesFor == _level && _edges.Count > 0) return;
            _edges.Clear();
            _edgesFor = _level;
            if (_level == null) return;
            var sides = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int y = 0; y < _level.Height; y++)
            for (int x = 0; x < _level.Width; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!IsFloor(cell)) continue;
                foreach (var side in sides)
                    if (!IsFloor(cell + side) && !IsDoorCell(cell + side))
                        _edges.Add((Position(cell) + (Vector2)side * (_cell * .5f), side));
            }
        }

        private void ClearFx()
        {
            foreach (var flare in _flares) { flare.Image.gameObject.SetActive(false); _freeFlares.Add(flare); }
            _flares.Clear();
            foreach (var image in _steady) image.gameObject.SetActive(false);
            if (_heldHalo != null) _heldHalo.gameObject.SetActive(false);
            _edges.Clear(); _edgesFor = null;
            _combo = 0; _comboClock = 99; _urgency = 0; _lastStatus = PuzzleStatus.Playing;
        }
    }
}
