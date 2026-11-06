#if UNITY_EDITOR
using System.Reflection;
using Modules.MainModule.LeaderboardScreenModule.ViewsMediators;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.LeaderboardScreenModule.Editor
{
    /// <summary>
    /// Lays the leaderboard screen out in the popups' purple: a header with back, two tabs, the list and the
    /// player's own row. Also puts the podium button on the main menu's home page, under the slot machine.
    /// </summary>
    internal static class BuildLeaderboardScreen
    {
        private const string PREFAB = "Assets/Modules/MainModule/zScreenModules/LeaderboardScreenModule/Prefabs/LeaderboardScreen.prefab";
        private const string MAIN_PREFAB = "Assets/Modules/MainModule/zScreenModules/MainScreenModule/Prefabs/MainScreen.prefab";
        private const string FONT = "Assets/Modules/UiWidgetsModule/Fonts/Baloo2-ExtraBold SDF.asset";
        private const string TITLE_FONT = "Assets/Modules/UiWidgetsModule/Fonts/LuckiestGuy SDF.asset";
        private const string BACKGROUND = "Assets/Art/Textures/Main_Menu/T_bg_MainMenu.jpeg";
        private const string ROUNDED = "Assets/Modules/GameplayModule/zScreenModules/GameplayScreenModule/Sprites/UI/SPR_UI_Rounded.png";
        private const string PODIUM = "Assets/Art/Hyper_Casual_UI/Sprites/Icons/leaderboard.png";

        private static readonly Color32 PANEL = new(0x6E, 0x22, 0xD6, 0xF5);
        private static readonly Color32 HEADER = new(0x9B, 0x3C, 0xF5, 0xFF);
        private static readonly Color32 ROW = new(0x43, 0x10, 0xA0, 0xE0);
        private static readonly Color32 RIM = new(0xD9, 0xB8, 0xFF, 0xFF);
        private static readonly Color32 SHADE = new(0x3A, 0x0C, 0x8A, 0xC8);
        private static readonly Color32 GOLD = new(0xFF, 0xD2, 0x3F, 0xFF);
        private static readonly Color32 SKY = new(0x1E, 0xB4, 0xF2, 0xFF);
        private static readonly Color32 PINK = new(0xFF, 0x5E, 0x8A, 0xFF);
        private static readonly Color32 DARK_TEXT = new(0x3A, 0x0C, 0x8A, 0xFF);

        [MenuItem("Tools/Wobble Fuse/Rebuild Leaderboard Screen")]
        public static void Build()
        {
            RegisterAddressable();
            BuildScreen();
            AddMainMenuButton();
            AssetDatabase.SaveAssets();
            Debug.Log("LeaderboardScreen rebuilt and the main menu button placed.");
        }

        private static void BuildScreen()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
            try
            {
                while (root.transform.childCount > 0) Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                Stretch((RectTransform)root.transform, 0, 0, 0, 0);
                root.layer = LayerMask.NameToLayer("UI");
                var view = root.GetComponent<LeaderboardScreenView>();

                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);
                var titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TITLE_FONT) ?? font;
                var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(ROUNDED);

                var background = Ui("Img_Background", root.transform, typeof(Image)).GetComponent<Image>();
                Stretch(background.rectTransform, 0, 0, 0, 0);
                background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BACKGROUND);

                var wash = Ui("Img_SkyWash", root.transform, typeof(Image)).GetComponent<Image>();
                Stretch(wash.rectTransform, 0, 0, 0, 0);
                wash.color = new Color32(0x1A, 0x0B, 0x3A, 0x40);
                wash.raycastTarget = false;

                var header = Panel("Pnl_Header", root.transform, rounded, HEADER);
                Anchor((RectTransform)header.transform, new Vector2(.05f, .86f), new Vector2(.95f, .97f));
                var title = Text("Txt_Title", header.transform, titleFont, "LEADERBOARD", 52, GOLD);
                Stretch(title.rectTransform, 100, 35, 8, 8);

                var back = Button("Btn_Back", header.transform, rounded, PINK);
                var backRect = (RectTransform)back.transform;
                backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0, .5f);
                backRect.anchoredPosition = new Vector2(18, 0);
                backRect.sizeDelta = new Vector2(82, 72);
                Stretch(Text("Txt_Arrow", back.transform, font, "‹", 58, Color.white).rectTransform, 0, 0, 2, 8);

                var allTime = Button("Btn_Tab_AllTime", root.transform, rounded, GOLD);
                Anchor((RectTransform)allTime.transform, new Vector2(.06f, .785f), new Vector2(.49f, .845f));
                var allTimeLabel = Text("Txt_Label", allTime.transform, font, "All Time", 28, DARK_TEXT);
                Stretch(allTimeLabel.rectTransform, 4, 4, 2, 2);
                var weekly = Button("Btn_Tab_Weekly", root.transform, rounded, HEADER);
                Anchor((RectTransform)weekly.transform, new Vector2(.51f, .785f), new Vector2(.94f, .845f));
                var weeklyLabel = Text("Txt_Label", weekly.transform, font, "Weekly", 28, DARK_TEXT);
                Stretch(weeklyLabel.rectTransform, 4, 4, 2, 2);

                var list = Panel("Pnl_List", root.transform, rounded, PANEL);
                Anchor((RectTransform)list.transform, new Vector2(.045f, .16f), new Vector2(.955f, .77f));

                var viewport = Ui("Viewport", list.transform, typeof(Image), typeof(Mask));
                Stretch((RectTransform)viewport.transform, 16, 16, 16, 16);
                viewport.GetComponent<Image>().color = new Color(1, 1, 1, .001f);
                viewport.GetComponent<Mask>().showMaskGraphic = false;

                var content = Ui("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                var contentRect = (RectTransform)content.transform;
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(.5f, 1);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                var group = content.GetComponent<VerticalLayoutGroup>();
                group.padding = new RectOffset(6, 6, 6, 16);
                group.spacing = 10;
                group.childControlWidth = group.childControlHeight = true;
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var scroll = list.AddComponent<ScrollRect>();
                scroll.viewport = (RectTransform)viewport.transform;
                scroll.content = contentRect;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Elastic;
                scroll.scrollSensitivity = 28;

                var template = Row("Row_Template", content.transform, font, rounded, ROW);
                template.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
                template.gameObject.SetActive(false);

                var status = Text("Txt_Status", list.transform, font, "Loading...", 28, Color.white);
                Anchor(status.rectTransform, new Vector2(.08f, .4f), new Vector2(.92f, .6f));
                status.textWrappingMode = TextWrappingModes.Normal;

                var me = Row("Pnl_MyRow", root.transform, font, rounded, SKY);
                Anchor(me, new Vector2(.045f, .055f), new Vector2(.955f, .14f));
                me.gameObject.AddComponent<Shadow>().effectColor = SHADE;

                var serialized = new SerializedObject(view);
                serialized.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
                serialized.FindProperty("_allTimeTab").objectReferenceValue = allTime.GetComponent<Button>();
                serialized.FindProperty("_weeklyTab").objectReferenceValue = weekly.GetComponent<Button>();
                serialized.FindProperty("_title").objectReferenceValue = title;
                serialized.FindProperty("_allTimeLabel").objectReferenceValue = allTimeLabel;
                serialized.FindProperty("_weeklyLabel").objectReferenceValue = weeklyLabel;
                serialized.FindProperty("_status").objectReferenceValue = status;
                serialized.FindProperty("_scroll").objectReferenceValue = scroll;
                serialized.FindProperty("_content").objectReferenceValue = contentRect;
                serialized.FindProperty("_rowTemplate").objectReferenceValue = template;
                serialized.FindProperty("_meRow").objectReferenceValue = me;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>The podium button under the slot machine on the home page, wired to the main screen's Leaderboard action.</summary>
        private static void AddMainMenuButton()
        {
            GameObject main = PrefabUtility.LoadPrefabContents(MAIN_PREFAB);
            try
            {
                var page = main.transform.Find("Pnl_Layout/Pnl_HomePage");
                var slot = page.Find("Btn_Slot");
                var old = page.Find("Btn_Leaderboard");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var button = Object.Instantiate(slot.gameObject, page);
                button.name = "Btn_Leaderboard";
                button.transform.SetSiblingIndex(slot.GetSiblingIndex() + 1);
                var rect = (RectTransform)button.transform;
                rect.anchoredPosition = new Vector2(-285, -60);
                button.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PODIUM);
                button.GetComponent<Image>().preserveAspect = true;
                // A leaderboard has nothing waiting to be collected: no count dot.
                var dot = button.transform.Find("Img_Dot");
                if (dot != null) Object.DestroyImmediate(dot.gameObject);

                var view = main.GetComponent("MainScreenView");
                var serialized = new SerializedObject(view);
                serialized.FindProperty("_leaderboardButton").objectReferenceValue = button.GetComponent<Button>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(main, MAIN_PREFAB);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(main);
            }
        }

        private static RectTransform Row(string name, Transform parent, TMP_FontAsset font, Sprite rounded, Color colour)
        {
            var row = Ui(name, parent, typeof(Image));
            var image = row.GetComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = colour;
            var rank = Text("Txt_Rank", row.transform, font, "1", 30, GOLD);
            Side(rank.rectTransform, 0, 20, 70);
            var nameText = Text("Txt_Name", row.transform, font, "Player", 28, Color.white);
            nameText.rectTransform.anchorMin = new Vector2(0, 0);
            nameText.rectTransform.anchorMax = new Vector2(1, 1);
            nameText.rectTransform.offsetMin = new Vector2(100, 0);
            nameText.rectTransform.offsetMax = new Vector2(-190, 0);
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            var score = Text("Txt_Score", row.transform, font, "0", 28, GOLD);
            Side(score.rectTransform, 1, -20, 170);
            score.alignment = TextAlignmentOptions.Right;
            return (RectTransform)row.transform;
        }

        private static void Side(RectTransform rect, float side, float x, float width)
        {
            rect.anchorMin = new Vector2(side, 0);
            rect.anchorMax = new Vector2(side, 1);
            rect.pivot = new Vector2(side, .5f);
            rect.anchoredPosition = new Vector2(x, 0);
            rect.sizeDelta = new Vector2(width, 0);
        }

        private static GameObject Panel(string name, Transform parent, Sprite rounded, Color colour)
        {
            var panel = Ui(name, parent, typeof(Image), typeof(Shadow), typeof(Outline));
            var image = panel.GetComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = colour;
            var rim = panel.GetComponent<Outline>();
            rim.effectColor = RIM;
            rim.effectDistance = new Vector2(5, -5);
            foreach (var shadow in panel.GetComponents<Shadow>())
                if (shadow != rim) { shadow.effectColor = SHADE; shadow.effectDistance = new Vector2(0, -8); }
            return panel;
        }

        private static GameObject Button(string name, Transform parent, Sprite rounded, Color colour)
        {
            var button = Ui(name, parent, typeof(Image), typeof(Button), typeof(Shadow));
            var image = button.GetComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = colour;
            button.GetComponent<Button>().targetGraphic = image;
            button.GetComponent<Shadow>().effectColor = SHADE;
            button.GetComponent<Shadow>().effectDistance = new Vector2(0, -5);
            return button;
        }

        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size, Color colour)
        {
            var text = Ui(name, parent, typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = colour;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject Ui(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            foreach (var component in components) go.AddComponent(component);
            return go;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void RegisterAddressable()
        {
            var flowEditor = System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(), item => item.GetName().Name == "FlowIoC.Editor");
            var entriesType = flowEditor?.GetType("FlowIoC.Editor.Addressables.ScreenAddressableEntries");
            var registryType = flowEditor?.GetType("FlowIoC.Editor.Addressables.ScreenAddressables");
            if (entriesType == null || registryType == null)
                throw new System.MissingMemberException("FlowIoC screen addressable registration types were not found.");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            object entries = System.Activator.CreateInstance(entriesType, true);
            object entry = entriesType.GetMethod("For", flags)?.Invoke(entries, new object[] { "LeaderboardScreen" });
            entry?.GetType().GetField("AssetPath", flags)?.SetValue(entry, PREFAB);
            object registry = System.Activator.CreateInstance(registryType, true);
            registryType.GetMethod("Register", flags)?.Invoke(registry, new[] { entry });
        }
    }
}
#endif
