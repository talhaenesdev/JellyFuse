using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// Gates: a door in the top wall shows its mouth, closed by two leaves. When the jelly the door waits
    /// for comes up in front of it - or goes in - the leaves slide apart, left and right, and whatever of
    /// them passes the mouth's edges is cut away. Gates come from a pool and ride on their door's picture.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private sealed class Gate
        {
            public RectTransform Mask, Left, Right;
            public RectMask2D Clip;
            public Image LeftImage, RightImage;
            public Item Door;
            public int Index, Size;
            public float Open, Width;
            /// <summary>Whether it was opening last frame: the dust puffs out as it starts to.</summary>
            public bool Opening;
            /// <summary>Just built for a level being opened: it takes its place at once, with no dust.</summary>
            public bool Fresh;
            public Vector2 LeftSize, RightSize;
        }
        private readonly List<Gate> _gates = new();
        private readonly List<Gate> _freeGates = new();
        /// <summary>How far each door's gate was open, kept over a rebuild so a door moving on does not snap it shut.</summary>
        private readonly Dictionary<int, float> _gateOpen = new();

        /// <summary>Puts every gate back in the pool, remembering how open it was. Called before the doors are built again.</summary>
        private void ReleaseGates()
        {
            _gateOpen.Clear();
            foreach (var gate in _gates)
            {
                _gateOpen[gate.Index] = gate.Open;
                gate.Mask.SetParent(transform, false);
                gate.Mask.gameObject.SetActive(false);
                _freeGates.Add(gate);
            }
            _gates.Clear();
        }

        /// <summary>The two leaves over a door's mouth, laid on its picture. Only a top door with leaves for its width has them.</summary>
        private void AddGate(int index, JellySide side, int size, Item door, Sprite painted)
        {
            if (Skin == null || side != JellySide.Top || painted == null) return;
            var left = Skin.GateLeft(size); var right = Skin.GateRight(size);
            if (left == null || right == null) return;
            Gate gate;
            if (_freeGates.Count > 0) { gate = _freeGates[^1]; _freeGates.RemoveAt(_freeGates.Count - 1); }
            else
            {
                gate = new Gate();
                gate.Mask = new GameObject("DoorGate", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                gate.Clip = gate.Mask.GetComponent<RectMask2D>();
                gate.Mask.gameObject.layer = gameObject.layer;
                gate.Left = Leaf("Img_GateLeft", gate.Mask, out gate.LeftImage);
                gate.Right = Leaf("Img_GateRight", gate.Mask, out gate.RightImage);
            }
            gate.Index = index; gate.Door = door; gate.Size = size;
            gate.Fresh = !_gateOpen.TryGetValue(index, out var open);
            gate.Open = gate.Fresh ? 0 : open;
            gate.LeftImage.sprite = left; gate.RightImage.sprite = right;
            // The picture is drawn at DoorPixels sprite pixels to a cell: the leaves keep that scale.
            float scale = _cell / Skin.DoorPixels;
            gate.LeftSize = left.rect.size * scale; gate.RightSize = right.rect.size * scale;
            gate.Width = Mathf.Max(gate.LeftSize.x, gate.RightSize.x);
            gate.Mask.SetParent(door.Root, false);
            gate.Mask.gameObject.SetActive(true);
            gate.Mask.SetAsLastSibling();
            gate.Mask.localScale = Vector3.one; gate.Mask.localRotation = Quaternion.identity;
            gate.Mask.anchorMin = gate.Mask.anchorMax = new Vector2(.5f, 0); gate.Mask.pivot = new Vector2(.5f, 0);
            Seat(gate.Left, new Vector2(1, 0), gate.LeftSize);
            Seat(gate.Right, new Vector2(0, 0), gate.RightSize);
            _gates.Add(gate);
            PlaceLeaves(gate);
        }
        private RectTransform Leaf(string name, Transform parent, out Image image)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = gameObject.layer;
            rect.SetParent(parent, false);
            image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return rect;
        }
        private static void Seat(RectTransform leaf, Vector2 pivot, Vector2 size)
        {
            leaf.anchorMin = leaf.anchorMax = new Vector2(.5f, 0);
            leaf.pivot = pivot; leaf.sizeDelta = size;
        }
        /// <summary>
        /// The mask where Gate Mask Offset and Size put it on the door's picture, and the leaves where Gate Offset
        /// Pixels and how open the gate is put them - read every time, so a change to the skin shows at once.
        /// </summary>
        private void PlaceLeaves(Gate gate)
        {
            float scale = _cell / Skin.DoorPixels;
            var size = Skin.GateMaskSizeFor(gate.Size) * scale;
            if (size.x <= 0) size.x = gate.LeftSize.x + gate.RightSize.x;
            if (size.y <= 0) size.y = Mathf.Max(gate.LeftSize.y, gate.RightSize.y);
            var maskAt = Skin.GateMaskOffsetFor(gate.Size) * scale;
            gate.Mask.anchoredPosition = maskAt;
            gate.Mask.sizeDelta = size;
            var softness = Vector2Int.RoundToInt(Skin.GateMaskSoftnessFor(gate.Size) * scale);
            if (gate.Clip.softness != softness) gate.Clip.softness = softness;
            // The leaves stay put on the door when the mask moves: placed from the mask's bottom-middle.
            var leavesAt = Skin.GateOffsetPixels * scale - maskAt;
            float slide = gate.Open * gate.Width;
            gate.Left.anchoredPosition = leavesAt + new Vector2(-slide, 0);
            gate.Right.anchoredPosition = leavesAt + new Vector2(slide, 0);
        }

        /// <summary>Opens each gate toward its jelly coming up or going in, and closes it otherwise.</summary>
        private void UpdateGates(float dt)
        {
            if (Skin != null) StepDust(dt);
            if (_gates.Count == 0 || _snapshot == null || Skin == null) return;
            foreach (var gate in _gates)
            {
                float target = GateWanted(gate.Index) ? 1 : 0;
                bool opening = target > 0;
                if (gate.Fresh)
                {
                    // A door a jelly already stands at when the level opens is open from the start, without a puff.
                    gate.Fresh = false; gate.Opening = opening; gate.Open = target;
                    PlaceLeaves(gate);
                    continue;
                }
                if (opening && !gate.Opening && gate.Open < .5f) PuffDust(gate);
                gate.Opening = opening;
                float before = gate.Open;
                gate.Open = Mathf.MoveTowards(gate.Open, target, dt * Skin.GateOpenSpeed);
                if (!Mathf.Approximately(before, gate.Open) || Application.isEditor) PlaceLeaves(gate);
                // A closing door fades out, and its leaves with it.
                float alpha = gate.Door.BodyImage.color.a;
                if (!Mathf.Approximately(gate.LeftImage.color.a, alpha)) gate.LeftImage.color = gate.RightImage.color = new Color(1, 1, 1, alpha);
            }
        }
        private sealed class Puff
        {
            public Image Image;
            public Vector2 From, To;
            public float Age, Life, Size;
        }
        private readonly List<Puff> _puffs = new();
        private readonly List<Puff> _freePuffs = new();

        /// <summary>Dust thrown out of a gate's mouth as it starts to open, spreading sideways from its middle.</summary>
        private void PuffDust(Gate gate)
        {
            if (Skin.GateDustCount <= 0 || Skin.GateDustSize <= 0 || Skin.GateDustColour.a <= 0) return;
            // The mouth, in the layer the bits are drawn on.
            var rect = gate.Mask.rect;
            Vector2 left = _bitLayer.InverseTransformPoint(gate.Mask.TransformPoint(new Vector2(rect.xMin, rect.center.y)));
            Vector2 right = _bitLayer.InverseTransformPoint(gate.Mask.TransformPoint(new Vector2(rect.xMax, rect.center.y)));
            for (int i = 0; i < Skin.GateDustCount; i++)
            {
                Puff puff;
                if (_freePuffs.Count > 0) { puff = _freePuffs[^1]; _freePuffs.RemoveAt(_freePuffs.Count - 1); }
                else
                {
                    puff = new Puff();
                    puff.Image = new GameObject("Img_GateDust", typeof(RectTransform)).AddComponent<Image>();
                    puff.Image.gameObject.layer = gameObject.layer;
                    puff.Image.rectTransform.SetParent(_bitLayer, false);
                    puff.Image.rectTransform.anchorMin = puff.Image.rectTransform.anchorMax = new Vector2(.5f, .5f);
                    puff.Image.raycastTarget = false;
                }
                puff.Image.gameObject.SetActive(true);
                puff.Image.sprite = _sprites.Highlight;
                // Along the mouth, pushed outward from its middle the way the leaves slide, and up a little.
                float along = Random.value;
                var at = Vector2.Lerp(left, right, along);
                float side = (along - .5f) * 2;
                puff.From = at;
                puff.To = at + new Vector2((side + Random.Range(-.3f, .3f)) * Skin.GateDustSpread, Skin.GateDustRise * Random.Range(.5f, 1.5f)) * _cell;
                puff.Life = Skin.GateDustTime * Random.Range(.7f, 1.3f);
                puff.Size = Skin.GateDustSize * _cell * Random.Range(.6f, 1.4f);
                puff.Age = 0;
                _puffs.Add(puff);
            }
        }
        /// <summary>The puffs drift out, swell and fade.</summary>
        private void StepDust(float dt)
        {
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                var puff = _puffs[i];
                puff.Age += dt;
                float k = puff.Age / Mathf.Max(.01f, puff.Life);
                if (k >= 1) { puff.Image.gameObject.SetActive(false); _freePuffs.Add(puff); _puffs.RemoveAt(i); continue; }
                float eased = 1 - (1 - k) * (1 - k);
                puff.Image.rectTransform.anchoredPosition = Vector2.Lerp(puff.From, puff.To, eased);
                puff.Image.rectTransform.sizeDelta = Vector2.one * puff.Size * Mathf.Lerp(.4f, 1, eased);
                var colour = Skin.GateDustColour; colour.a *= (1 - k) * Mathf.Clamp01(k * 6);
                puff.Image.color = colour;
            }
        }
        private void ClearDust()
        {
            foreach (var puff in _puffs) { puff.Image.gameObject.SetActive(false); _freePuffs.Add(puff); }
            _puffs.Clear();
        }
        private bool GateWanted(int door)
        {
            // A jelly is going in through it.
            foreach (var mouth in _mouths) if (mouth.Door == door) return true;
            var stage = _snapshot.WaitingStage(door);
            if (stage == null || door >= _level.Gates.Count) return false;
            var gate = _level.Gates[door];
            var open = gate.OpenCells(_snapshot.DoorProgress[door]);
            int fromX = int.MaxValue, toX = int.MinValue;
            foreach (var cell in open) { fromX = Mathf.Min(fromX, cell.x); toX = Mathf.Max(toX, cell.x); }
            // The jelly it waits for, within its columns and no more than Gate Open Distance rows below it.
            foreach (var piece in _snapshot.Pieces)
            {
                if (piece.Colour != stage.Colour || !SameShape(piece.Cells, stage.Cells)) continue;
                bool within = true; int top = int.MinValue;
                foreach (var cell in piece.Cells) { within &= cell.x >= fromX && cell.x <= toX; top = Mathf.Max(top, cell.y); }
                if (!within) continue;
                int gap = open[0].y - 1 - top;
                if (gap >= 0 && gap <= Skin.GateOpenDistance) return true;
            }
            return false;
        }
        /// <summary>Whether the jelly is now exactly the shape and colour a door waits for.</summary>
        private bool FormsWaitingShape(JellyPieceVO piece)
        {
            if (_snapshot == null) return false;
            for (int door = 0; door < _snapshot.Level.Gates.Count; door++)
            {
                var stage = _snapshot.WaitingStage(door);
                if (stage != null && stage.Colour == piece.Colour && SameShape(piece.Cells, stage.Cells)) return true;
            }
            return false;
        }
        private static bool SameShape(IReadOnlyList<Vector2Int> cells, IReadOnlyList<Vector2Int> shape)
        {
            if (cells.Count != shape.Count) return false;
            Vector2Int min = cells[0], shapeMin = shape[0];
            for (int i = 1; i < cells.Count; i++) { min = Vector2Int.Min(min, cells[i]); shapeMin = Vector2Int.Min(shapeMin, shape[i]); }
            foreach (var cell in cells)
            {
                bool found = false;
                foreach (var s in shape) if (s - shapeMin == cell - min) { found = true; break; }
                if (!found) return false;
            }
            return true;
        }
    }
}
