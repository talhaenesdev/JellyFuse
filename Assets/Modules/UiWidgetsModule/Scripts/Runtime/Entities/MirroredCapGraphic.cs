using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// The two ends of a MirroredCapBackground: the cap sprite as it is on the left, and the same
    /// sprite mirrored on the right, each as wide as the cap is at this height. Driven by its
    /// MirroredCapBackground; it holds no settings of its own.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MirroredCapGraphic : MaskableGraphic
    {
        [SerializeField] private Sprite _cap;

        public override Texture mainTexture => _cap != null ? _cap.texture : s_WhiteTexture;

        /// <summary>Sets the cap sprite and redraws.</summary>
        public void SetCap(Sprite cap)
        {
            if (_cap == cap) return;
            _cap = cap;
            SetAllDirty();
        }

        /// <summary>The cap's width at a height, keeping the sprite's proportions.</summary>
        public float CapWidth(float height) =>
            _cap == null || _cap.rect.height <= 0 ? 0 : height * _cap.rect.width / _cap.rect.height;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_cap == null) return;
            var rect = GetPixelAdjustedRect();
            float width = Mathf.Min(CapWidth(rect.height), rect.width * .5f);
            var uv = DataUtility.GetOuterUV(_cap);
            // Left as painted; right with its U running backwards, which mirrors it.
            Quad(vh, rect.xMin, rect.xMin + width, rect.yMin, rect.yMax, uv.x, uv.z, uv.y, uv.w);
            Quad(vh, rect.xMax - width, rect.xMax, rect.yMin, rect.yMax, uv.z, uv.x, uv.y, uv.w);
        }

        private void Quad(VertexHelper vh, float x0, float x1, float y0, float y1, float u0, float u1, float v0, float v1)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), color, new Vector2(u0, v0));
            vh.AddVert(new Vector3(x0, y1), color, new Vector2(u0, v1));
            vh.AddVert(new Vector3(x1, y1), color, new Vector2(u1, v1));
            vh.AddVert(new Vector3(x1, y0), color, new Vector2(u1, v0));
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
