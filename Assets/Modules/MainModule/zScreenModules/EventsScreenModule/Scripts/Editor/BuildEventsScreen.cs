#if UNITY_EDITOR
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using Modules.MainModule.EventsScreenModule.ViewsMediators;

namespace Modules.MainModule.EventsScreenModule.Editor
{
    internal static class BuildEventsScreen
    {
        private const string PREFAB = "Assets/Modules/MainModule/zScreenModules/EventsScreenModule/Prefabs/EventsScreen.prefab";
        private const string MAIN_PREFAB = "Assets/Modules/MainModule/zScreenModules/MainScreenModule/Prefabs/MainScreen.prefab";
        private const string ART = "Assets/Art/Generated/UI/EventScreen/";
        private const string ATLAS = "Assets/Art/Atlases/Atlas_UI_Main.spriteatlasv2";
        private const string FONT = "Assets/Modules/UiWidgetsModule/Fonts/Baloo2-ExtraBold SDF.asset";
        private const string TITLE_FONT = "Assets/Modules/UiWidgetsModule/Fonts/LuckiestGuy SDF.asset";

        // The sunny palette the popups use: purple panels with a lilac rim, gold titles, sky-blue actions.
        private static readonly Color32 PANEL = new(0x6E, 0x22, 0xD6, 0xF5);
        private static readonly Color32 HEADER = new(0x9B, 0x3C, 0xF5, 0xFF);
        private static readonly Color32 WELL = new(0x43, 0x10, 0xA0, 0xE0);
        private static readonly Color32 BADGE = new(0x2A, 0x08, 0x70, 0xFF);
        private static readonly Color32 RIM = new(0xD9, 0xB8, 0xFF, 0xFF);
        private static readonly Color32 SHADE = new(0x3A, 0x0C, 0x8A, 0xC8);
        private static readonly Color32 GOLD = new(0xFF, 0xD2, 0x3F, 0xFF);
        private static readonly Color32 SKY = new(0x1E, 0xB4, 0xF2, 0xFF);
        private static readonly Color32 PINK = new(0xFF, 0x5E, 0x8A, 0xFF);
        private static readonly Color32 LILAC_TEXT = new(0xE2, 0xD4, 0xFF, 0xFF);
        private const string BACKGROUND = "Assets/Art/Textures/Main_Menu/T_bg_MainMenu.jpeg";
        private const string ROUNDED = "Assets/Modules/GameplayModule/zScreenModules/GameplayScreenModule/Sprites/UI/SPR_UI_Rounded.png";

        [MenuItem("Tools/Wobble Fuse/Rebuild Events Screen")]
        public static void Build()
        {
            ImportSprites();
            AddSpritesToAtlas();
            RegisterAddressable();
            CleanupOldMainPage();

            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
            try
            {
                while (root.transform.childCount > 0)
                    Object.DestroyImmediate(root.transform.GetChild(0).gameObject);

                RectTransform rootRect = (RectTransform)root.transform;
                Stretch(rootRect, 0, 0, 0, 0);
                root.layer = LayerMask.NameToLayer("UI");

                EventsScreenView view = root.GetComponent<EventsScreenView>();
                if (view == null) view = root.AddComponent<EventsScreenView>();

                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);
                TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TITLE_FONT);
                if (titleFont == null) titleFont = font;
                Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>(ROUNDED);

                Image background = Ui("Img_Background", root.transform, typeof(Image)).GetComponent<Image>();
                Stretch(background.rectTransform, 0, 0, 0, 0);
                background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BACKGROUND);
                background.preserveAspect = false;
                background.color = Color.white;

                Image wash = Ui("Img_SkyWash", root.transform, typeof(Image)).GetComponent<Image>();
                Stretch(wash.rectTransform, 0, 0, 0, 0);
                wash.color = new Color32(0x1A, 0x0B, 0x3A, 0x40);
                wash.raycastTarget = false;

                GameObject header = Ui("Pnl_Header", root.transform, typeof(Image), typeof(Shadow), typeof(Outline));
                Anchor(header.GetComponent<RectTransform>(), new Vector2(.05f, .86f), new Vector2(.95f, .97f));
                Image headerImage = header.GetComponent<Image>();
                headerImage.sprite = rounded;
                headerImage.type = Image.Type.Sliced;
                headerImage.color = HEADER;
                Rim(header);

                TMP_Text title = Text("Txt_Title", header.transform, titleFont, "EVENTS", 52);
                Stretch(title.rectTransform, 100, 35, 8, 8);
                title.alignment = TextAlignmentOptions.Center;
                title.color = GOLD;

                GameObject back = Ui("Btn_Back", header.transform, typeof(Image), typeof(Button), typeof(Shadow));
                RectTransform backRect = back.GetComponent<RectTransform>();
                backRect.anchorMin = backRect.anchorMax = new Vector2(0, .5f);
                backRect.pivot = new Vector2(0, .5f);
                backRect.anchoredPosition = new Vector2(18, 0);
                backRect.sizeDelta = new Vector2(82, 72);
                Image backImage = back.GetComponent<Image>();
                backImage.sprite = rounded;
                backImage.type = Image.Type.Sliced;
                backImage.color = PINK;
                back.GetComponent<Button>().targetGraphic = backImage;
                back.GetComponent<Shadow>().effectColor = SHADE;
                back.GetComponent<Shadow>().effectDistance = new Vector2(0, -5);
                TMP_Text arrow = Text("Txt_Arrow", back.transform, font, "‹", 58);
                Stretch(arrow.rectTransform, 0, 0, 2, 8);
                arrow.alignment = TextAlignmentOptions.Center;
                arrow.color = Color.white;

                TMP_Text subtitle = Text("Txt_Subtitle", root.transform, font,
                    "Complete limited-time challenges and collect rewards!", 24);
                Anchor(subtitle.rectTransform, new Vector2(.08f, .815f), new Vector2(.92f, .86f));
                subtitle.alignment = TextAlignmentOptions.Center;
                subtitle.color = Color.white;
                subtitle.outlineColor = SHADE;
                subtitle.outlineWidth = .22f;

                GameObject listPanel = Ui("Pnl_EventList", root.transform, typeof(Image), typeof(Shadow), typeof(Outline));
                Anchor(listPanel.GetComponent<RectTransform>(), new Vector2(.045f, .055f), new Vector2(.955f, .81f));
                Image listImage = listPanel.GetComponent<Image>();
                listImage.sprite = rounded;
                listImage.type = Image.Type.Sliced;
                listImage.color = PANEL;
                Rim(listPanel);

                GameObject viewport = Ui("Viewport", listPanel.transform, typeof(Image), typeof(Mask));
                Stretch(viewport.GetComponent<RectTransform>(), 18, 18, 18, 18);
                viewport.GetComponent<Image>().color = new Color(1, 1, 1, .001f);
                viewport.GetComponent<Mask>().showMaskGraphic = false;

                GameObject content = Ui("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                RectTransform contentRect = content.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(.5f, 1);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                VerticalLayoutGroup group = content.GetComponent<VerticalLayoutGroup>();
                group.padding = new RectOffset(10, 10, 12, 28);
                group.spacing = 20;
                group.childAlignment = TextAnchor.UpperCenter;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                ScrollRect scroll = listPanel.AddComponent<ScrollRect>();
                scroll.viewport = viewport.GetComponent<RectTransform>();
                scroll.content = contentRect;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Elastic;
                scroll.elasticity = .12f;
                scroll.decelerationRate = .12f;
                scroll.scrollSensitivity = 28;

                CreateCard(content.transform, font, titleFont, rounded, "Star Rush", "Win levels and fill the golden star meter.",
                    "2d 14h", "500", "SPR_UI_Event_StarRush.png", GOLD);
                CreateCard(content.transform, font, titleFont, rounded, "Color Carnival", "Collect colorful jellies to unlock the grand chest.",
                    "4d 08h", "3 Chests", "SPR_UI_Event_ColorCarnival.png", PINK);
                CreateCard(content.transform, font, titleFont, rounded, "Speed Run", "Complete five levels before the timer runs out.",
                    "01h 45m", "250", "SPR_UI_Event_SpeedRun.png", SKY);

                SerializedObject serializedView = new(view);
                serializedView.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
                serializedView.FindProperty("_eventList").objectReferenceValue = scroll;
                serializedView.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
                AssetDatabase.SaveAssets();
                Debug.Log("EventsScreen rebuilt as a separate scrollable screen.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CleanupOldMainPage()
        {
            GameObject main = PrefabUtility.LoadPrefabContents(MAIN_PREFAB);
            try
            {
                Transform oldPage = main.transform.Find("Pnl_Layout/Pnl_EventsPage");
                if (oldPage == null) return;
                Object.DestroyImmediate(oldPage.gameObject);
                PrefabUtility.SaveAsPrefabAsset(main, MAIN_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(main); }
        }

        private static void CreateCard(Transform parent, TMP_FontAsset font, TMP_FontAsset titleFont, Sprite rounded, string eventName,
            string description, string time, string reward, string iconName, Color accent)
        {
            GameObject card = Ui("Pnl_Event_" + eventName.Replace(" ", string.Empty), parent,
                typeof(Image), typeof(LayoutElement), typeof(Shadow));
            card.GetComponent<LayoutElement>().preferredHeight = 245;
            Image cardImage = card.GetComponent<Image>();
            cardImage.sprite = rounded;
            cardImage.type = Image.Type.Sliced;
            cardImage.color = WELL;
            card.GetComponent<Shadow>().effectColor = SHADE;
            card.GetComponent<Shadow>().effectDistance = new Vector2(0, -6);

            Image accentImage = Ui("Img_Accent", card.transform, typeof(Image)).GetComponent<Image>();
            RectTransform accentRect = accentImage.rectTransform;
            accentRect.anchorMin = new Vector2(0, 0);
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, .5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(16, -22);
            accentImage.sprite = rounded;
            accentImage.type = Image.Type.Sliced;
            accentImage.color = accent;

            Image icon = Ui("Img_Icon", card.transform, typeof(Image)).GetComponent<Image>();
            Edge(icon.rectTransform, 0, new Vector2(30, 4), new Vector2(190, 190));
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART + iconName);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TMP_Text name = Text("Txt_Name", card.transform, titleFont, eventName, 34);
            Span(name.rectTransform, 250, 40, 72, 48);
            name.alignment = TextAlignmentOptions.Left;
            name.color = GOLD;

            TMP_Text detail = Text("Txt_Description", card.transform, font, description, 21);
            Span(detail.rectTransform, 250, 40, 14, 64);
            detail.alignment = TextAlignmentOptions.TopLeft;
            detail.color = LILAC_TEXT;

            CreateBadge(card.transform, font, rounded, "Time", time, new Vector2(250, -72), BADGE);
            CreateBadge(card.transform, font, rounded, "Reward", reward, new Vector2(410, -72), accent);

            GameObject play = Ui("Btn_PlayEvent", card.transform, typeof(Image), typeof(Button), typeof(Shadow));
            Edge(play.GetComponent<RectTransform>(), 1, new Vector2(-30, -72), new Vector2(150, 62));
            Image playImage = play.GetComponent<Image>();
            playImage.sprite = rounded;
            playImage.type = Image.Type.Sliced;
            playImage.color = SKY;
            play.GetComponent<Button>().targetGraphic = playImage;
            play.GetComponent<Shadow>().effectColor = SHADE;
            play.GetComponent<Shadow>().effectDistance = new Vector2(0, -5);
            TMP_Text playText = Text("Txt_Play", play.transform, font, "PLAY", 22);
            Stretch(playText.rectTransform, 2, 2, 2, 2);
            playText.alignment = TextAlignmentOptions.Center;
            playText.color = Color.white;
        }

        private static void CreateBadge(Transform parent, TMP_FontAsset font, Sprite rounded, string name,
            string value, Vector2 position, Color color)
        {
            Image badge = Ui("Pnl_" + name, parent, typeof(Image)).GetComponent<Image>();
            Edge(badge.rectTransform, 0, position, new Vector2(148, 52));
            badge.sprite = rounded;
            badge.type = Image.Type.Sliced;
            badge.color = color;
            TMP_Text label = Text("Txt_" + name, badge.transform, font, value, 20);
            Stretch(label.rectTransform, 4, 4, 2, 2);
            label.alignment = TextAlignmentOptions.Center;
            // Gold reads with dark text; every other tone with white.
            bool light = color.r > .9f && color.g > .7f;
            label.color = light ? (Color)new Color32(0x3A, 0x0C, 0x8A, 0xFF) : Color.white;
        }

        /// <summary>The lilac rim and soft purple drop the popup panels have.</summary>
        private static void Rim(GameObject panel)
        {
            Outline rim = panel.GetComponent<Outline>();
            rim.effectColor = RIM;
            rim.effectDistance = new Vector2(5, -5);
            Shadow drop = panel.GetComponents<Shadow>()[0];
            if (drop == rim) drop = panel.GetComponents<Shadow>()[1];
            drop.effectColor = SHADE;
            drop.effectDistance = new Vector2(0, -8);
        }

        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size)
        {
            TMP_Text text = Ui(name, parent, typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject Ui(string name, Transform parent, params System.Type[] components)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            foreach (System.Type component in components) go.AddComponent(component);
            return go;
        }

        /// <summary>Pinned to the card's left (side 0) or right (side 1) edge, so a wider card spreads its contents.</summary>
        private static void Edge(RectTransform rect, float side, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(side, .5f);
            rect.pivot = new Vector2(side, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>From <paramref name="left"/> to <paramref name="right"/> in from the card's sides, centred at <paramref name="y"/>.</summary>
        private static void Span(RectTransform rect, float left, float right, float y, float height)
        {
            rect.anchorMin = new Vector2(0, .5f);
            rect.anchorMax = new Vector2(1, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(left, y - height / 2);
            rect.offsetMax = new Vector2(-right, y + height / 2);
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void ImportSprites()
        {
            foreach (string name in new[] { "SPR_UI_Event_StarRush.png", "SPR_UI_Event_ColorCarnival.png", "SPR_UI_Event_SpeedRun.png" })
            {
                string path = ART + name;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        private static void AddSpritesToAtlas()
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(ATLAS);
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(ART.TrimEnd('/'));
            if (atlas == null || folder == null) return;
            SpriteAtlasExtensions.Add(atlas, new[] { folder });
            EditorUtility.SetDirty(atlas);
        }

        private static void RegisterAddressable()
        {
            Assembly flowEditor = System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(),
                item => item.GetName().Name == "FlowIoC.Editor");
            System.Type entriesType = flowEditor?.GetType("FlowIoC.Editor.Addressables.ScreenAddressableEntries");
            System.Type registryType = flowEditor?.GetType("FlowIoC.Editor.Addressables.ScreenAddressables");
            if (entriesType == null || registryType == null)
                throw new System.MissingMemberException("FlowIoC screen addressable registration types were not found.");

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            object entries = System.Activator.CreateInstance(entriesType, true);
            object entry = entriesType.GetMethod("For", flags)?.Invoke(entries, new object[] { "EventsScreen" });
            entry?.GetType().GetField("AssetPath", flags)?.SetValue(entry, PREFAB);
            object registry = System.Activator.CreateInstance(registryType, true);
            registryType.GetMethod("Register", flags)?.Invoke(registry, new[] { entry });
        }
    }
}
#endif
