using System.Collections;
using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The electric combo on the board: the jellies a door is about to take crackle and rise, the crackle draws in to
    /// one point, bursts, and flies to the door, which answers with a ring. The board itself takes the jellies away
    /// when the next snapshot arrives; this only draws over it.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private readonly HashSet<int> _charged = new();
        private readonly List<Image> _chargeImages = new();
        private int _chargeUsed;
        private Coroutine _charge;
        private float _chargeLift;

        public void Charge(IReadOnlyList<int> ids, int door, float seconds, CD_ElectricCombo fx)
        {
            if (fx == null || _snapshot == null || ids == null || ids.Count == 0 || !isActiveAndEnabled) return;
            StopCharge();
            EnsureFxLayer();
            var centres = new List<Vector2>();
            var chargedIds = new List<int>();
            var sizes = new List<Vector2>();
            foreach (var id in ids)
            {
                JellyPieceVO piece = null;
                foreach (var candidate in _snapshot.Pieces) if (candidate.Id == id) { piece = candidate; break; }
                if (piece == null) continue;
                _charged.Add(id);
                Vector2Int min = piece.Cells[0], max = piece.Cells[0];
                foreach (var cell in piece.Cells) { min = Vector2Int.Min(min, cell); max = Vector2Int.Max(max, cell); }
                centres.Add(BoxCentre(piece.Cells));
                chargedIds.Add(id);
                sizes.Add(((Vector2)(max - min) + Vector2.one * (1 + fx.AuraPadding)) * _cell);
            }
            if (centres.Count == 0) return;
            _chargeLift = Feel.GrabLift * fx.LiftScale;
            foreach (var pair in _blobs) if (_charged.Contains(pair.Key)) pair.Value.LiftTarget = _chargeLift;
            _dirty = true;
            Vector2 target = _doorFrames.TryGetValue(door, out var frame) ? frame.Centre : centres[0] + Vector2.up * _cell * 3;
            _charge = StartCoroutine(RunCharge(chargedIds, centres, sizes, target, seconds, fx));
        }

        private IEnumerator RunCharge(List<int> ids, List<Vector2> centres, List<Vector2> sizes, Vector2 door, float seconds, CD_ElectricCombo fx)
        {
            Vector2 middle = Vector2.zero;
            foreach (var centre in centres) middle += centre;
            middle /= centres.Count;
            var sparks = new List<(Image Image, float Born)>();
            float gatherFrom = seconds * (1 - fx.GatherShare);
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                _chargeUsed = 0;
                int frame = (int)(t / fx.FrameSeconds);
                float gather = t < gatherFrom ? 0 : Mathf.SmoothStep(0, 1, (t - gatherFrom) / Mathf.Max(.01f, seconds - gatherFrom));
                // Each aura sits where its jelly is drawn now - risen with it - not on its cells.
                for (int i = 0; i < centres.Count; i++) centres[i] = DrawnCentre(ids[i], centres[i]);
                for (int i = 0; i < centres.Count; i++)
                {
                    Vector2 at = Vector2.Lerp(centres[i], middle, gather);
                    Vector2 size = Vector2.Lerp(sizes[i], Vector2.one * _cell, gather);
                    float pulse = .85f + .15f * Mathf.Sin((t + i) * 30);
                    if (fx.OuterAura > 0) Draw(Pick(fx.Aura, frame + i + 2), at, size * pulse * 1.35f, fx.OuterAura * (.6f + .4f * Mathf.Sin((t + i) * 22)), 90);
                    Draw(Pick(fx.Aura, frame + i), at, size * pulse, 1, 0);
                    // Stray arcs crackle out of the jelly in every direction, a new set every frame step.
                    if (fx.Arcs.Length > 0)
                    {
                        var stray = new System.Random(frame * 131 + i * 17);
                        for (int a = 0; a < fx.StrayArcs; a++)
                        {
                            float turn = (float)stray.NextDouble() * 360;
                            float reach = _cell * fx.StrayArcLength * (.6f + (float)stray.NextDouble() * .7f) * (1 - gather * .7f);
                            Vector2 dir = new Vector2(Mathf.Cos(turn * Mathf.Deg2Rad), Mathf.Sin(turn * Mathf.Deg2Rad));
                            Vector2 from = at + Vector2.Scale(dir, size * .4f);
                            Draw(Pick(fx.Arcs, frame + a + i), from + dir * reach * .5f, new Vector2(reach, _cell * .45f), .9f, turn);
                        }
                    }
                    // An arc to the next charged jelly, so they read as one charge.
                    if (centres.Count > 1 && fx.Arcs.Length > 0)
                    {
                        Vector2 next = Vector2.Lerp(centres[(i + 1) % centres.Count], middle, gather);
                        Vector2 span = next - at;
                        if (span.magnitude > _cell * .5f && (i < centres.Count - 1 || centres.Count > 2))
                            Draw(Pick(fx.Arcs, frame + i * 2), (at + next) * .5f, new Vector2(span.magnitude, _cell * .55f), .95f, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg);
                    }
                }
                // Sparks pop round the charge and fade.
                if (fx.Sparks.Length > 0 && sparks.Count < fx.SparksAtOnce && Random.value < .5f)
                {
                    int which = Random.Range(0, centres.Count);
                    var spark = NewSpark(fx.Sparks[Random.Range(0, fx.Sparks.Length)]);
                    spark.rectTransform.anchoredPosition = Vector2.Lerp(centres[which], middle, gather) + Random.insideUnitCircle * sizes[which].magnitude * .5f;
                    spark.rectTransform.sizeDelta = Vector2.one * _cell * fx.SparkSize * Random.Range(.6f, 1.3f);
                    spark.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
                    sparks.Add((spark, t));
                }
                for (int i = sparks.Count - 1; i >= 0; i--)
                {
                    float age = (t - sparks[i].Born) / .3f;
                    if (age >= 1) { Destroy(sparks[i].Image.gameObject); sparks.RemoveAt(i); continue; }
                    sparks[i].Image.color = new Color(1, 1, 1, 1 - age);
                }
                HideUnused();
                yield return null;
            }
            foreach (var spark in sparks) Destroy(spark.Image.gameObject);
            ReleaseCharge();
            // They fuse: a burst where the charge drew in.
            float burstTime = Mathf.Max(.3f, fx.ShakeSeconds);
            var shaken = (RectTransform)transform;
            Vector2 rest = shaken.anchoredPosition;
            for (float t = 0; t < burstTime; t += Time.unscaledDeltaTime)
            {
                _chargeUsed = 0;
                float k = t / burstTime;
                if (fx.FlashSize > 0 && fx.GlowDot != null) Draw(fx.GlowDot, middle, Vector2.one * _cell * fx.FlashSize * Mathf.Lerp(.5f, 1, k), (1 - k) * .9f, 0);
                Draw(Pick(fx.Burst, (int)(k * fx.Burst.Length)), middle, Vector2.one * _cell * fx.BurstSize * Mathf.Lerp(.6f, 1.2f, k), 1 - k * .5f, 0);
                Draw(Pick(fx.Burst, (int)(k * fx.Burst.Length) + 1), middle, Vector2.one * _cell * fx.BurstSize * Mathf.Lerp(.4f, .9f, k), 1 - k * .5f, 45);
                Draw(Pick(fx.Ring, (int)(k * fx.Ring.Length)), middle, Vector2.one * _cell * fx.RingSize * Mathf.Lerp(.4f, 1.6f, k), 1 - k, 0);
                float shake = t < fx.ShakeSeconds ? fx.Shake * _cell * (1 - t / Mathf.Max(.01f, fx.ShakeSeconds)) : 0;
                shaken.anchoredPosition = rest + Random.insideUnitCircle * shake;
                HideUnused();
                yield return null;
            }
            shaken.anchoredPosition = rest;
            // The charge flies to its door with its trail behind it.
            Vector2 way = door - middle;
            float angle = Mathf.Atan2(way.y, way.x) * Mathf.Rad2Deg;
            for (float t = 0; t < fx.FlightSeconds; t += Time.unscaledDeltaTime)
            {
                _chargeUsed = 0;
                float k = Mathf.SmoothStep(0, 1, t / fx.FlightSeconds);
                Vector2 at = Vector2.Lerp(middle, door, k);
                float length = Mathf.Max(_cell, way.magnitude * Mathf.Min(k, .45f));
                // The trail's bright head is its left end: turned to face back along the flight.
                Draw(fx.Trail, at - way.normalized * length * .5f, new Vector2(length, _cell * .7f), 1, angle + 180);
                Draw(fx.GlowDot, at, Vector2.one * _cell * fx.OrbSize * (1 + .15f * Mathf.Sin(t * 60)), 1, 0);
                Draw(Pick(fx.Burst, (int)(t / fx.FrameSeconds)), at, Vector2.one * _cell * fx.OrbSize * .9f, .8f, t * 900);
                if (fx.Arcs.Length > 0) Draw(Pick(fx.Arcs, (int)(t / fx.FrameSeconds)), at, new Vector2(_cell * 1.4f, _cell * .5f), .9f, Random.Range(0, 360f));
                HideUnused();
                yield return null;
            }
            // The door answers.
            float ringTime = .4f;
            for (float t = 0; t < ringTime; t += Time.unscaledDeltaTime)
            {
                _chargeUsed = 0;
                float k = t / ringTime;
                Draw(Pick(fx.Burst, (int)(k * fx.Burst.Length)), door, Vector2.one * _cell * fx.BurstSize * .8f * Mathf.Lerp(.5f, 1, k), 1 - k, 0);
                Draw(Pick(fx.Ring, (int)(k * fx.Ring.Length)), door, Vector2.one * _cell * fx.RingSize * Mathf.Lerp(.5f, 1.3f, k), 1 - k, 0);
                Draw(Pick(fx.Ring, (int)(k * fx.Ring.Length) + 1), door, Vector2.one * _cell * fx.RingSize * Mathf.Lerp(.2f, .9f, k), 1 - k, 30);
                HideUnused();
                yield return null;
            }
            _chargeUsed = 0;
            HideUnused();
            _charge = null;
        }

        /// <summary>The middle of a jelly's top faces as drawn, on the fx layer; its cells' middle when it is gone.</summary>
        private Vector2 DrawnCentre(int id, Vector2 fallback)
        {
            if (!_blobs.TryGetValue(id, out var blob) || blob.Parts.Count == 0) return fallback;
            Vector3 sum = Vector3.zero; int count = 0;
            foreach (var part in blob.Parts)
            {
                if (part.Item?.Body == null) continue;
                sum += part.Item.Body.TransformPoint(part.Item.Body.rect.center); count++;
            }
            return count == 0 ? fallback : (Vector2)_fxLayer.InverseTransformPoint(sum / count);
        }

        private static Sprite Pick(Sprite[] frames, int index) => frames == null || frames.Length == 0 ? null : frames[Mathf.Clamp(index, 0, int.MaxValue) % frames.Length];

        private void Draw(Sprite sprite, Vector2 position, Vector2 size, float alpha, float angle)
        {
            if (sprite == null) return;
            if (_chargeUsed == _chargeImages.Count)
            {
                var made = new GameObject("Img_Electric", typeof(RectTransform)).AddComponent<Image>();
                made.gameObject.layer = gameObject.layer;
                made.rectTransform.SetParent(_fxLayer, false);
                made.rectTransform.anchorMin = made.rectTransform.anchorMax = new Vector2(.5f, .5f);
                made.raycastTarget = false;
                _chargeImages.Add(made);
            }
            var image = _chargeImages[_chargeUsed++];
            if (!image.gameObject.activeSelf) image.gameObject.SetActive(true);
            image.rectTransform.SetAsLastSibling();
            image.sprite = sprite;
            image.color = new Color(1, 1, 1, Mathf.Clamp01(alpha));
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private Image NewSpark(Sprite sprite)
        {
            var image = new GameObject("Img_Spark", typeof(RectTransform)).AddComponent<Image>();
            image.gameObject.layer = gameObject.layer;
            image.rectTransform.SetParent(_fxLayer, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = sprite;
            return image;
        }

        private void HideUnused()
        {
            for (int i = _chargeUsed; i < _chargeImages.Count; i++)
                if (_chargeImages[i].gameObject.activeSelf) _chargeImages[i].gameObject.SetActive(false);
        }

        /// <summary>The charged jellies come down again; whatever is left of them is the board's.</summary>
        private void ReleaseCharge()
        {
            foreach (var pair in _blobs) if (_charged.Contains(pair.Key)) pair.Value.LiftTarget = 0;
            _charged.Clear();
            _dirty = true;
        }

        private void StopCharge()
        {
            if (_charge != null) { StopCoroutine(_charge); _charge = null; }
            ReleaseCharge();
            _chargeUsed = 0;
            HideUnused();
        }
    }
}
