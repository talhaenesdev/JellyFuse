#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.Editor
{
    /// <summary>
    /// Draws the feeding pipes' tile set: every piece a tube of one profile, centred on its cell and meeting each edge it
    /// joins as a straight tube square to that edge, so any two pieces join without a seam. Pieces painted by hand never
    /// lined up; these are worked out from the tube's centre line, so they cannot be off.
    /// Written to SPR_UI_PipeTiles_Generated.png, 4 x 4 cells, in the order JellySpriteBoardView.Feeding reads them.
    /// </summary>
    internal static class BuildPipeTiles
    {
        private const string PATH = "Assets/Art/Generated/CatFeeding/SPR_UI_PipeTiles_Generated.png";
        private const int TILE = 128;
        /// <summary>Half the tube's width, as a share of the tile: a tube 0.375 of a cell wide.</summary>
        private const float RADIUS = TILE * .1875f;
        private const float OUTLINE = 3.2f;
        private const float LIP_RADIUS = RADIUS * 1.3f;
        private const float LIP_HEIGHT = TILE * .13f;

        private static readonly Color Body = new(.95f, .95f, .98f);
        private static readonly Color Shade = new(.66f, .64f, .78f);
        private static readonly Color Line = new(.22f, .19f, .32f);
        private static readonly Vector3 Light = new Vector3(-.45f, .6f, .66f).normalized;

        private enum Kind { Segment, Arc }
        private struct Part { public Kind Kind; public Vector2 A, B, Centre; public float Radius, Tube, From, To; }

        private const int N = 1, E = 2, S = 4, W = 8;

        [MenuItem("Tools/Wobble Fuse/Rebuild Pipe Tiles")]
        public static void Build()
        {
            // The order JellySpriteBoardView.Feeding reads them in.
            var pieces = new (int Mask, bool Lip, bool Cap)[]
            {
                (E | W, false, false), (N | S, false, false), (W | S, false, false), (E | S, false, false),
                (W | N, false, false), (E | N, false, false), (W | E | S, false, false), (W | E | N, false, false),
                (N | S | W, false, false), (N | S | E, false, false), (N | E | S | W, false, false), (W, false, true),
                (W | S, true, false), (W | E | S, true, false),
            };
            var sheet = new Texture2D(TILE * 4, TILE * 4, TextureFormat.RGBA32, false);
            var clear = new Color32[sheet.width * sheet.height];
            sheet.SetPixels32(clear);
            for (int i = 0; i < pieces.Length; i++)
            {
                var parts = Parts(pieces[i].Mask, pieces[i].Lip, pieces[i].Cap);
                int ox = i % 4 * TILE, oy = (3 - i / 4) * TILE;
                for (int y = 0; y < TILE; y++)
                for (int x = 0; x < TILE; x++)
                    sheet.SetPixel(ox + x, oy + y, Shading(new Vector2(x + .5f, y + .5f), parts));
            }
            sheet.Apply();
            System.IO.File.WriteAllBytes(PATH, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            AssetDatabase.ImportAsset(PATH);
            Slice(pieces.Length);
            Debug.Log("Pipe tiles drawn: " + PATH);
        }

        private static List<Part> Parts(int mask, bool lip, bool cap)
        {
            var parts = new List<Part>();
            var centre = new Vector2(TILE * .5f, TILE * .5f);
            Vector2 Edge(int side) => side switch
            {
                N => new Vector2(TILE * .5f, TILE + 2),
                E => new Vector2(TILE + 2, TILE * .5f),
                S => new Vector2(TILE * .5f, -2),
                _ => new Vector2(-2, TILE * .5f),
            };
            int count = 0;
            foreach (int side in new[] { N, E, S, W }) if ((mask & side) != 0) count++;
            bool vertical = (mask & (N | S)) != 0, horizontal = (mask & (E | W)) != 0;
            // Two sides at right angles: a quarter bend round the corner they share, radius half a tile.
            if (count == 2 && vertical && horizontal && !lip)
            {
                var corner = new Vector2((mask & E) != 0 ? TILE : 0, (mask & N) != 0 ? TILE : 0);
                float a = Mathf.Atan2(centre.y - corner.y, centre.x - corner.x);
                parts.Add(new Part { Kind = Kind.Arc, Centre = corner, Radius = TILE * .5f, Tube = RADIUS, From = a - Mathf.PI * .25f, To = a + Mathf.PI * .25f });
                return parts;
            }
            foreach (int side in new[] { N, E, S, W })
                // From the middle to the edge; a lone side ends in the middle, rounded, which is the cap.
                if ((mask & side) != 0) parts.Add(new Part { Kind = Kind.Segment, A = centre, B = Edge(side), Tube = RADIUS });
            if (lip)
            {
                // The spout's mouth: a wider ring where the tube leaves the bottom of the tile.
                parts.Add(new Part { Kind = Kind.Segment, A = new Vector2(TILE * .5f, LIP_HEIGHT), B = new Vector2(TILE * .5f, 1), Tube = LIP_RADIUS });
            }
            return parts;
        }

        private static Color Shading(Vector2 p, List<Part> parts)
        {
            float best = float.MaxValue, bestDistance = 0, bestTube = 1;
            Vector2 normal = Vector2.up;
            foreach (var part in parts)
            {
                Vector2 closest;
                if (part.Kind == Kind.Segment)
                {
                    var ab = part.B - part.A;
                    float t = Mathf.Clamp01(Vector2.Dot(p - part.A, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                    closest = part.A + ab * t;
                }
                else
                {
                    float angle = Mathf.Atan2(p.y - part.Centre.y, p.x - part.Centre.x);
                    angle = Mathf.Clamp(angle, part.From, part.To);
                    closest = part.Centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * part.Radius;
                }
                float distance = Vector2.Distance(p, closest);
                float share = distance / part.Tube;
                if (share < best)
                {
                    best = share; bestDistance = distance; bestTube = part.Tube;
                    normal = distance > 1e-4f ? (p - closest) / distance : Vector2.up;
                }
            }
            float alpha = Mathf.Clamp01(bestTube - bestDistance + .5f);
            if (alpha <= 0) return new Color(0, 0, 0, 0);
            float across = Mathf.Clamp01(bestDistance / bestTube);
            // A cylinder lit from the top left: bright on top, darker to its sides, one soft white streak.
            var surface = new Vector3(normal.x * across, normal.y * across, Mathf.Sqrt(Mathf.Max(0, 1 - across * across)));
            float diffuse = Mathf.Clamp01(Vector3.Dot(surface, Light));
            var half = (Light + Vector3.forward).normalized;
            float spec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(surface, half)), 28f);
            var colour = Color.Lerp(Shade, Body, .35f + .65f * diffuse);
            colour = Color.Lerp(colour, Color.white, spec * .85f);
            // The dark outline along both sides.
            float edge = Mathf.Clamp01((bestDistance - (bestTube - OUTLINE)) / 1.2f);
            colour = Color.Lerp(colour, Line, edge);
            colour.a = alpha;
            return colour;
        }

        private static void Slice(int count)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(PATH);
            var sprites = new List<SpriteMetaData>();
            for (int i = 0; i < count; i++)
                sprites.Add(new SpriteMetaData
                {
                    name = "SPR_UI_PipeTile_" + i.ToString("00"),
                    rect = new Rect(i % 4 * TILE, (3 - i / 4) * TILE, TILE, TILE),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(.5f, .5f),
                });
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable 618
            importer.spritesheet = sprites.ToArray();
#pragma warning restore 618
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }
}
#endif
