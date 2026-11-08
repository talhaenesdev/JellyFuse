#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Editor
{
    /// <summary>
    /// Puts a soft breathing halo behind what should look lit - the Play button, the daily games, the leaderboard's
    /// crown - the cheap stand-in for a bloom. Each halo is an Img_Glow sibling placed just behind its target, drawn
    /// with UI/Glow, so it takes no touches and costs one quad. Makes the halo sprite and the material the first
    /// time; running it again only puts back a halo that is missing.
    /// </summary>
    internal static class AddUIGlows
    {
        private const string ART = "Assets/Modules/UiWidgetsModule/Art";
        private const string SPRITE = ART + "/SPR_UI_Glow.png";
        private const string MATERIAL = ART + "/MT_UIGlow.mat";
        private const string SHADER = "UI/Glow";
        private const string MAIN = "Assets/Modules/MainModule/zScreenModules/MainScreenModule/Prefabs/MainScreen.prefab";
        private const string LEADERBOARD = "Assets/Modules/MainModule/zScreenModules/LeaderboardScreenModule/Prefabs/LeaderboardScreen.prefab";

        private const float INTENSITY = 2.4f;
        private static readonly Color GOLD = new(1f, .82f, .3f, 1f);
        private static readonly Color VIOLET = new(.85f, .55f, 1f, 1f);
        private static readonly Color WARM = new(1f, .65f, .45f, 1f);

        [MenuItem("Tools/Wobble Fuse/Add UI Glows")]
        private static void Run()
        {
            var sprite = Sprite();
            var material = Material();
            Edit(MAIN, root =>
            {
                Glow(root, "Pnl_Layout/Pnl_HomePage/Btn_Play", sprite, material, VIOLET, 1.35f, 0f);
                Glow(root, "Pnl_Layout/Pnl_HomePage/Btn_Profile", sprite, material, GOLD, 1.45f, .2f);
                Glow(root, "Pnl_Layout/Pnl_HomePage/Btn_Wheel", sprite, material, WARM, 1.7f, .4f);
                Glow(root, "Pnl_Layout/Pnl_HomePage/Btn_Scratch", sprite, material, GOLD, 1.7f, .6f);
                Glow(root, "Pnl_Layout/Pnl_HomePage/Btn_Slot", sprite, material, WARM, 1.7f, .8f);
                Glow(root, "Pnl_Layout/Pnl_HomePage/Btn_Leaderboard", sprite, material, GOLD, 1.7f, .1f);
            });
            Edit(LEADERBOARD, root =>
            {
                var crown = Find(root.transform, "Img_Crown");
                if (crown != null) Glow(root, Path(root.transform, crown), sprite, material, GOLD, 2.2f, 0f);
            });
            AssetDatabase.SaveAssets();
            Debug.Log("AddUIGlows - halos are behind the Play button, the daily games and the crown.");
        }

        private static void Edit(string path, System.Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>An Img_Glow just behind the target, on its anchors and centre, the target's size times scale.</summary>
        private static void Glow(GameObject root, string path, Sprite sprite, Material material, Color tint, float scale, float phase)
        {
            var target = root.transform.Find(path) as RectTransform;
            if (target == null)
            {
                Debug.LogWarning("AddUIGlows - no " + path + " in " + root.name + "; skipped.");
                return;
            }
            string name = "Img_Glow_" + target.name;
            var parent = target.parent;
            var existing = parent.Find(name);
            var rect = existing != null ? (RectTransform)existing : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            // Out of the way first: a halo already before the target would otherwise land after it.
            rect.SetAsLastSibling();
            rect.SetSiblingIndex(target.GetSiblingIndex());
            // The target's own placement, grown by scale: right whether it is anchored to a point or stretched.
            rect.anchorMin = target.anchorMin;
            rect.anchorMax = target.anchorMax;
            rect.pivot = target.pivot;
            rect.anchoredPosition = target.anchoredPosition;
            rect.sizeDelta = target.sizeDelta;
            rect.localScale = target.localScale * scale;

            var image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = tint;
            image.raycastTarget = false;
            // One material each, so every halo keeps its own phase and they do not breathe in step.
            if (image.material != null && image.material.name == "MT_UIGlow_" + target.name)
            {
                image.material.SetFloat("_Phase", phase);
                image.material.SetFloat("_Intensity", INTENSITY);
                return;
            }
            var own = new Material(material) { name = "MT_UIGlow_" + target.name };
            own.SetFloat("_Phase", phase);
            own.SetFloat("_Intensity", INTENSITY);
            image.material = own;
            AssetDatabase.AddObjectToAsset(own, MATERIAL);
        }

        private static Transform Find(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static string Path(Transform root, Transform child)
        {
            string path = child.name;
            for (var t = child.parent; t != null && t != root; t = t.parent) path = t.name + "/" + path;
            return path;
        }

        /// <summary>A white radial falloff, made once: the glow's tint comes from the Image colour.</summary>
        private static Sprite Sprite()
        {
            if (!File.Exists(SPRITE))
            {
                const int size = 128;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f) / size * 2 - 1, dy = (y + .5f) / size * 2 - 1;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float a = Mathf.Pow(1 - d, 1.4f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, a));
                }
                File.WriteAllBytes(SPRITE, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SPRITE);
                var importer = (TextureImporter)AssetImporter.GetAtPath(SPRITE);
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE);
        }

        private static Material Material()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL);
            if (material != null) return material;
            material = new Material(Shader.Find(SHADER)) { name = "MT_UIGlow" };
            AssetDatabase.CreateAsset(material, MATERIAL);
            return material;
        }
    }
}
#endif
