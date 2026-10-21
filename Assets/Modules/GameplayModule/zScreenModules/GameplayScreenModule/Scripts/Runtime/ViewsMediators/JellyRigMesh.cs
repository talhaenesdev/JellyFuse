using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// A jelly's rig, drawn in code: the picture's quad is cut into a grid and each point is moved
    /// by how far up the jelly it sits. The bottom row stays on the base; going up, the top bends
    /// toward <see cref="Bend"/>, presses down by <see cref="Squash"/> and its sides bulge out as it
    /// does. The board sets the values every frame it moves the jelly; the picture, its atlas and its
    /// fill stay the Image's own.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class JellyRigMesh : BaseMeshEffect
    {
        /// <summary>How far the top edge is bent, in the rect's own units (x right, y up).</summary>
        public Vector2 Bend;
        /// <summary>How much the top is pressed down, as a share of its height; negative stretches it up.</summary>
        public float Squash;
        /// <summary>How far the sides bulge out per unit of squash, as a share of the width.</summary>
        public float Bulge = .5f;
        /// <summary>How far the middle of the top edge dips per unit of squash, as a share of the height.</summary>
        public float TopDip = .3f;
        /// <summary>Points across and up the grid; more is smoother.</summary>
        public int Columns = 4, Rows = 4;
        /// <summary>How the bend grows from where it holds still out to the moving edge: above 1 the still part is wider.</summary>
        public float Curve = 1.6f;
        /// <summary>
        /// Off: the bottom row holds and the top edge moves. On: the middle holds and the top and the
        /// bottom edges both move, the bottom the same way the top does.
        /// </summary>
        public bool Mirror;
        /// <summary>Moves the whole mesh, in the rect's own units: how a picture under a mirrored one follows its bottom edge.</summary>
        public Vector2 Shift;

        private readonly List<UIVertex> _quad = new();

        public void Apply() { if (graphic != null) graphic.SetVerticesDirty(); }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount != 4) return;
            _quad.Clear();
            for (int i = 0; i < 4; i++) { var v = new UIVertex(); vh.PopulateUIVertex(ref v, i); _quad.Add(v); }
            Vector2 min = _quad[0].position, max = min, uvMin = _quad[0].uv0, uvMax = uvMin;
            foreach (var v in _quad)
            {
                min = Vector2.Min(min, v.position); max = Vector2.Max(max, v.position);
                uvMin = Vector2.Min(uvMin, v.uv0); uvMax = Vector2.Max(uvMax, v.uv0);
            }
            var size = max - min;
            if (size.x <= 0 || size.y <= 0) return;
            var colour = _quad[0].color;
            int columns = Mathf.Max(1, Columns), rows = Mathf.Max(1, Rows);
            vh.Clear();
            for (int r = 0; r <= rows; r++)
            {
                float t = (float)r / rows;
                for (int c = 0; c <= columns; c++)
                {
                    float s = (float)c / columns;
                    vh.AddVert(Deform(min, size, s, t), colour, new Vector4(Mathf.Lerp(uvMin.x, uvMax.x, s), Mathf.Lerp(uvMin.y, uvMax.y, t), 0, 0));
                }
            }
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                {
                    int a = r * (columns + 1) + c, b = a + 1, d = a + columns + 1, e = d + 1;
                    vh.AddTriangle(a, d, e);
                    vh.AddTriangle(e, b, a);
                }
        }

        /// <summary>The point <paramref name="s"/> across and <paramref name="t"/> up the rect, moved by the rig.</summary>
        private Vector3 Deform(Vector2 min, Vector2 size, float s, float t)
        {
            float across = s * 2 - 1;
            var p = new Vector2(min.x + s * size.x, min.y + t * size.y) + Shift;
            if (Mirror)
            {
                // The middle row holds; going up or down from it, the rig moves a point more. Both edges
                // bend the same way, and pressed, they close toward the middle - the top down, the bottom
                // up - each sagging in its middle and bulging at its sides.
                float side = t * 2 - 1;
                float w = Mathf.Pow(Mathf.Abs(side), Curve);
                // Sideways both edges lean the same way; up and down they move apart or together, so a jelly
                // moving up or down squashes and stretches instead of only shifting.
                p.x += Bend.x * w;
                p.y += Bend.y * w * Mathf.Sign(side);
                p.y -= Squash * size.y * .5f * side;
                p.y -= Mathf.Sign(side) * Squash * TopDip * size.y * .5f * w * (1 - across * across);
                p.x += Squash * Bulge * size.x * .5f * across * w;
                return p;
            }
            // The bottom row is held by the base; the higher a point, the more the rig moves it.
            float weight = Mathf.Pow(t, Curve);
            p += Bend * weight;
            // Pressed down, it gets lower, its middle sags at the top and its sides bulge, keeping its body.
            p.y -= Squash * size.y * t;
            p.y -= Squash * TopDip * size.y * weight * (1 - across * across);
            p.x += Squash * Bulge * size.x * .5f * across * Mathf.Sin(t * Mathf.PI);
            return p;
        }
    }
}
