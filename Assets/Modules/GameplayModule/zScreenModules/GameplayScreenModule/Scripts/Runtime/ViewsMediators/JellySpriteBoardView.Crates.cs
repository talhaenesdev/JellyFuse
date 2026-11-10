using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;
using UnityEngine.UI;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The wooden crates: drawn among the jellies and sorted with them, so a crate's front covers the row in
    /// front of it. A crate the snapshot no longer has breaks apart - it swells and fades while its splinters
    /// fly out - and one with a hit taken is drawn cracked and jolts.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private sealed class CrateBreak
        {
            public Item Item;
            public Image Bits;
            public Vector2 Position;
            public float Age;
        }
        private readonly Dictionary<Vector2Int, Item> _crateItems = new();
        private readonly Dictionary<Vector2Int, int> _crateHits = new();
        private readonly List<CrateBreak> _crateBreaks = new();
        private readonly List<Image> _freeCrateBits = new();
        private readonly List<Vector2Int> _crateGone = new();
        private JellyLevelCVO _crateLevel;
        /// <summary>A cracked crate jolts for this long.</summary>
        private float _crateJolt;
        private Vector2Int _crateJoltCell;

        private static readonly Color CRATE_FALLBACK = new(.62f, .4f, .18f);

        private void PresentCrates(PuzzleSnapshotRVO snapshot)
        {
            if (_crateLevel != snapshot.Level) { ClearCrates(); _crateLevel = snapshot.Level; }
            _crateGone.Clear();
            foreach (var cell in _crateItems.Keys) if (!snapshot.Crates.ContainsKey(cell)) _crateGone.Add(cell);
            foreach (var cell in _crateGone)
            {
                BreakCrate(cell, _crateItems[cell]);
                _crateItems.Remove(cell); _crateHits.Remove(cell);
            }
            foreach (var pair in snapshot.Crates)
            {
                bool known = _crateHits.TryGetValue(pair.Key, out int hits);
                if (known && hits == pair.Value) continue;
                if (_crateItems.TryGetValue(pair.Key, out var old)) Give(old);
                _crateItems[pair.Key] = PlaceCrate(pair.Key, pair.Value);
                _crateHits[pair.Key] = pair.Value;
                if (known && pair.Value < hits) { _crateJolt = .25f; _crateJoltCell = pair.Key; }
            }
        }
        private Item PlaceCrate(Vector2Int cell, int hits)
        {
            var sprite = hits < Models_CrateHits && Look.CrateCrackedSprite != null ? Look.CrateCrackedSprite : Look.CrateSprite;
            float height = Look.CrateHeight;
            // The top face fills the cell and the front hangs below it, over the cell in front.
            var position = Position(cell) + new Vector2(0, -(height - 1) * .5f * _cell);
            var item = Place(Take(_jellyLayer), Role.Marker, position, new Vector2(_cell * 1.06f, _cell * height), sprite != null ? Color.white : CRATE_FALLBACK, 0, 0, -1, sprite);
            if (sprite != null) item.BodyImage.type = Image.Type.Simple;
            // Sorted by the cell, not by the picture's lowered centre: it stands in its row like a jelly.
            item.SortY = Position(cell).y;
            return item;
        }
        /// <summary>What the model starts every crate with; a crate with fewer is cracked.</summary>
        private const int Models_CrateHits = 2;

        private void BreakCrate(Vector2Int cell, Item item)
        {
            var bits = TakeCrateBits();
            bits.rectTransform.anchoredPosition = Position(cell);
            bits.sprite = Look.CrateBitsSprite;
            bits.enabled = Look.CrateBitsSprite != null;
            _crateBreaks.Add(new CrateBreak { Item = item, Bits = bits, Position = item.Root.anchoredPosition });
            _dirty = true;
        }
        private Image TakeCrateBits()
        {
            Image image;
            if (_freeCrateBits.Count > 0) { image = _freeCrateBits[^1]; _freeCrateBits.RemoveAt(_freeCrateBits.Count - 1); }
            else
            {
                var rect = new GameObject("CrateBits", typeof(RectTransform)).GetComponent<RectTransform>();
                rect.gameObject.layer = gameObject.layer;
                rect.SetParent(_bitLayer, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                image = rect.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
            }
            image.gameObject.SetActive(true);
            image.rectTransform.SetAsLastSibling();
            return image;
        }

        /// <summary>Advances the breaking crates and the jolt of a cracked one; true while any of it moves.</summary>
        private bool StepCrates(float dt)
        {
            bool moving = false;
            float life = Mathf.Max(.05f, Look.CrateBreakSeconds);
            for (int i = _crateBreaks.Count - 1; i >= 0; i--)
            {
                var crate = _crateBreaks[i];
                crate.Age += dt;
                float k = Mathf.Clamp01(crate.Age / life);
                if (k >= 1)
                {
                    Give(crate.Item);
                    crate.Item.Root.localScale = Vector3.one;
                    SetAlpha(crate.Item, 1);
                    crate.Bits.gameObject.SetActive(false); _freeCrateBits.Add(crate.Bits);
                    _crateBreaks.RemoveAt(i);
                    continue;
                }
                float ease = 1 - (1 - k) * (1 - k);
                crate.Item.Root.localScale = Vector3.one * (1 + .25f * ease);
                SetAlpha(crate.Item, 1 - k);
                crate.Bits.rectTransform.sizeDelta = Vector2.one * _cell * Mathf.Lerp(.8f, 2.1f, ease);
                crate.Bits.rectTransform.localEulerAngles = new Vector3(0, 0, 40 * ease);
                crate.Bits.color = new Color(1, 1, 1, 1 - k * k);
                moving = true;
            }
            if (_crateJolt > 0 && _crateItems.TryGetValue(_crateJoltCell, out var jolted))
            {
                _crateJolt = Mathf.Max(0, _crateJolt - dt);
                float shake = Mathf.Sin(_crateJolt * 70) * _crateJolt * 4 * .06f;
                jolted.Root.localScale = new Vector3(1 + shake, 1 - shake, 1);
                moving = true;
            }
            return moving;
        }
        private static void SetAlpha(Item item, float alpha)
        {
            var colour = item.BodyImage.color; colour.a = alpha; item.BodyImage.color = colour;
        }
        private void SortCrates()
        {
            foreach (var item in _crateItems.Values) _sortBuffer.Add(item);
        }
        private void ClearCrates()
        {
            foreach (var item in _crateItems.Values) Give(item);
            _crateItems.Clear(); _crateHits.Clear();
            foreach (var crate in _crateBreaks)
            {
                crate.Item.Root.localScale = Vector3.one; SetAlpha(crate.Item, 1); Give(crate.Item);
                crate.Bits.gameObject.SetActive(false); _freeCrateBits.Add(crate.Bits);
            }
            _crateBreaks.Clear();
            _crateJolt = 0;
            _crateLevel = null;
        }
    }
}
