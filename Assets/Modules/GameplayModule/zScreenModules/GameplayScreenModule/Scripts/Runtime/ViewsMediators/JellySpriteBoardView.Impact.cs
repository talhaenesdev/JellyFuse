using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The impact of a fuse: the fused jelly flashes white, a ring spreads from it in its colour, and the board
    /// gives a short shake. Each is set in CD_JellyFeel's fuse impact settings; at 0 it is left out.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private sealed class Ring
        {
            public Image Image;
            public Vector2 Centre;
            public Color Colour;
            public float Age, Size;
        }
        private readonly List<Ring> _rings = new();
        private readonly List<Ring> _freeRings = new();
        private float _shakeLeft;
        private readonly Dictionary<int, float> _flashes = new();
        private readonly List<int> _flashKeys = new();

        private bool ImpactRunning => _rings.Count > 0 || _shakeLeft > 0 || _flashes.Count > 0;

        /// <summary>A jelly just fused: its flash, its ring and the board's shake start.</summary>
        private void Impact(int id, IReadOnlyList<Vector2Int> cells, Color colour)
        {
            if (Feel.FuseFlash > 0 && Feel.FuseFlashTime > 0) _flashes[id] = 1;
            if (Feel.FuseShake > 0 && Feel.FuseShakeTime > 0) _shakeLeft = Feel.FuseShakeTime;
            SpreadRing(cells, colour);
        }
        /// <summary>A ring in the colour spreading from the cells, as Fuse Ring sets it.</summary>
        private void SpreadRing(IReadOnlyList<Vector2Int> cells, Color colour)
        {
            if (Feel.FuseRingSize <= 0 || Feel.FuseRingTime <= 0 || Feel.FuseRingAlpha <= 0) return;
            Ring ring;
            if (_freeRings.Count > 0) { ring = _freeRings[^1]; _freeRings.RemoveAt(_freeRings.Count - 1); }
            else
            {
                ring = new Ring();
                ring.Image = new GameObject("Img_FuseRing", typeof(RectTransform)).AddComponent<Image>();
                ring.Image.gameObject.layer = gameObject.layer;
                ring.Image.rectTransform.SetParent(_bitLayer, false);
                ring.Image.rectTransform.anchorMin = ring.Image.rectTransform.anchorMax = new Vector2(.5f, .5f);
                ring.Image.raycastTarget = false;
            }
            ring.Image.gameObject.SetActive(true);
            ring.Image.sprite = _sprites.Ring;
            ring.Centre = Position(CentreOf(cells));
            Span(cells, out float spanX, out float spanY);
            // It starts as big as the jelly and spreads Fuse Ring Size cells beyond it.
            ring.Size = Mathf.Max(spanX, spanY);
            ring.Colour = Color.Lerp(colour, Color.white, .45f);
            ring.Age = 0;
            _rings.Add(ring);
        }

        /// <summary>Moves the flashes, rings and shake on; true while any still runs.</summary>
        private bool StepImpact(float dt)
        {
            if (!ImpactRunning) return false;
            // Flashes fade out.
            _flashKeys.Clear(); _flashKeys.AddRange(_flashes.Keys);
            foreach (var id in _flashKeys)
            {
                float left = _flashes[id] - dt / Mathf.Max(.01f, Feel.FuseFlashTime);
                if (left <= 0 || !_blobs.ContainsKey(id)) _flashes.Remove(id); else _flashes[id] = left;
            }
            // Rings spread, thin out and fade.
            for (int i = _rings.Count - 1; i >= 0; i--)
            {
                var ring = _rings[i];
                ring.Age += dt;
                float k = ring.Age / Mathf.Max(.01f, Feel.FuseRingTime);
                if (k >= 1) { ring.Image.gameObject.SetActive(false); _freeRings.Add(ring); _rings.RemoveAt(i); continue; }
                float eased = 1 - (1 - k) * (1 - k);
                ring.Image.rectTransform.anchoredPosition = ring.Centre;
                ring.Image.rectTransform.sizeDelta = Vector2.one * (ring.Size + Feel.FuseRingSize * eased) * _cell;
                var colour = ring.Colour; colour.a = Feel.FuseRingAlpha * (1 - k);
                ring.Image.color = colour;
            }
            // The board shakes, dying away; every layer moves together.
            var offset = Vector2.zero;
            if (_shakeLeft > 0)
            {
                _shakeLeft -= dt;
                float strength = Mathf.Max(0, _shakeLeft / Feel.FuseShakeTime) * Feel.FuseShake * _cell;
                offset = new Vector2(Mathf.Sin(_time * 90), Mathf.Cos(_time * 70)) * strength;
            }
            foreach (RectTransform layer in transform) layer.anchoredPosition = offset;
            return true;
        }
        /// <summary>How much the jelly flashes white now, 0 to 1.</summary>
        private float FlashOf(int id) => _flashes.TryGetValue(id, out var left) ? left * left * Feel.FuseFlash : 0;

        private void ClearImpact()
        {
            foreach (var ring in _rings) { ring.Image.gameObject.SetActive(false); _freeRings.Add(ring); }
            _rings.Clear(); _flashes.Clear(); _shakeLeft = 0;
            foreach (RectTransform layer in transform) layer.anchoredPosition = Vector2.zero;
        }
    }
}
