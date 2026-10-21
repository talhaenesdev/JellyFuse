using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>The sprites the flat board draws with, generated once so the module ships no texture files.</summary>
    public sealed class JellySpriteLibrary
    {
        public Sprite Rounded { get; }
        public Sprite Circle { get; }
        public Sprite Highlight { get; }
        /// <summary>A double-headed arrow, left to right, and the same arrow standing up.</summary>
        public Sprite ArrowHorizontal { get; }
        public Sprite ArrowVertical { get; }
        /// <summary>A rounded box that fades out softly past its edge: sliced, its halo keeps its width at any size.</summary>
        public Sprite Glow { get; }
        /// <summary>A soft-edged ring, for the wave a fuse sends out.</summary>
        public Sprite Ring { get; }
        private readonly Texture2D[] _textures = new Texture2D[7];
        public JellySpriteLibrary()
        {
            Rounded = Make(0, 128, 34, (x, y) => RoundedAlpha(x, y, 128, 34));
            Circle = Make(1, 64, 0, (x, y) => Mathf.Clamp01(31.5f - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32))));
            Highlight = Make(2, 64, 0, (x, y) =>
            {
                var p = new Vector2((x + .5f - 32) / 30f, (y + .5f - 32) / 30f);
                return Mathf.Pow(Mathf.Clamp01(1 - p.sqrMagnitude), 1.6f);
            });
            ArrowHorizontal = Make(3, 128, 0, (x, y) => ArrowAlpha(x, y));
            ArrowVertical = Make(4, 128, 0, (x, y) => ArrowAlpha(y, x));
            Glow = Make(5, 128, 56, (x, y) => GlowAlpha(x, y, 128, 48, 20));
            Ring = Make(6, 128, 0, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(64, 64));
                return Mathf.Clamp01(1 - Mathf.Abs(d - 56) / 6f);
            });
        }
        /// <summary>A double-headed arrow across a 128 square, smoothed by sampling each pixel four times.</summary>
        private static float ArrowAlpha(int x, int y)
        {
            float inside = 0;
            for (int sx = 0; sx < 2; sx++) for (int sy = 0; sy < 2; sy++)
            {
                float px = x + .25f + sx * .5f, py = Mathf.Abs(y + .25f + sy * .5f - 64);
                float fromEnd = Mathf.Min(px - 6, 122 - px);
                bool head = fromEnd >= 0 && fromEnd < 40 && py <= fromEnd * 1.05f;
                bool shaft = fromEnd >= 0 && py <= 11;
                if (head || shaft) inside += .25f;
            }
            return inside;
        }
        /// <summary>Full inside a rounded box inset by <paramref name="halo"/>, fading to nothing over the halo outside it.</summary>
        private static float GlowAlpha(int x, int y, int size, float halo, float radius)
        {
            float inner = size * .5f - halo;
            var p = new Vector2(Mathf.Abs(x + .5f - size * .5f), Mathf.Abs(y + .5f - size * .5f));
            var q = p - new Vector2(inner - radius, inner - radius);
            float outside = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude - radius;
            float t = Mathf.Clamp01(1 - outside / halo);
            return t * t * (3 - 2 * t);
        }
        private static float RoundedAlpha(int x, int y, int size, float radius)
        {
            float dx = Mathf.Max(radius - (x + .5f), 0, x + .5f - (size - radius));
            float dy = Mathf.Max(radius - (y + .5f), 0, y + .5f - (size - radius));
            return Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
        }
        private Sprite Make(int slot, int size, float border, System.Func<int, int, float> alpha)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "JellySprite" + slot, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) pixels[y * size + x] = new Color(1, 1, 1, alpha(x, y));
            texture.SetPixels(pixels); texture.Apply(false, true);
            _textures[slot] = texture;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, Vector4.one * border);
        }
        public void Destroy()
        {
            foreach (var sprite in new[] { Rounded, Circle, Highlight, ArrowHorizontal, ArrowVertical }) if (sprite != null) Object.Destroy(sprite);
            foreach (var texture in _textures) if (texture != null) Object.Destroy(texture);
        }
    }
}
