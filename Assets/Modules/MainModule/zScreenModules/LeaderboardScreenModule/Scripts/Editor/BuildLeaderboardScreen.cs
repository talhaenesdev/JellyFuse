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
    /// Lays the leaderboard screen out from the art in Assets/Art/Generated/UI/Leaderboard: a header with back, two
    /// tabs, a podium for the first three, the list and the player's own row. Also puts the podium button on the
    /// main menu's home page, under the slot machine.
    /// </summary>
    internal static class BuildLeaderboardScreen
    {
        private const string PREFAB = "Assets/Modules/MainModule/zScreenModules/LeaderboardScreenModule/Prefabs/LeaderboardScreen.prefab";
        private const string MAIN_PREFAB = "Assets/Modules/MainModule/zScreenModules/MainScreenModule/Prefabs/MainScreen.prefab";
        private const string FONT = "Assets/Modules/UiWidgetsModule/Fonts/Baloo2-ExtraBold SDF.asset";
        private const string PODIUM_ICON = "Assets/Art/Hyper_Casual_UI/Sprites/Icons/leaderboard.png";
        private const string ART_FOLDER = "Assets/Art/Generated/UI/Leaderboard";
        private const string ART = ART_FOLDER + "/SPR_UI_Leaderboard_";

        private static readonly Color32 GOLD = new(0xFF, 0xD2, 0x3F, 0xFF);
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

        private static Sprite Art(string name, Vector4 border = default)
        {
            string path = ART + name + ".png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                bool changed = importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.spriteBorder = border;
                if (changed) importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void BuildScreen()
        {
            AssetDatabase.ImportAsset(ART_FOLDER, ImportAssetOptions.ImportRecursive);
            var backgroundArt = Art("Background");
            var headerArt = Art("Header");
            var backArt = Art("BackButton");
            var tabOn = Art("Tab_Selected", new Vector4(60, 40, 60, 40));
            var tabOff = Art("Tab_Unselected", new Vector4(60, 40, 60, 40));
            var podiumArt = Art("Podium");
            var crownArt = Art("Crown");
            var rowArt = Art("ListRow", new Vector4(64, 50, 64, 50));
            var meArt = Art("YourRankRow", new Vector4(70, 55, 70, 55));
            var starArt = Art("ScoreStar");
            var badges = new[] { Art("RankBadge_01"), Art("RankBadge_02"), Art("RankBadge_03"), Art("RankBadge_Other") };

            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
            try
            {
                while (root.transform.childCount > 0) Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                Stretch((RectTransform)root.transform, 0, 0, 0, 0);
                root.layer = LayerMask.NameToLayer("UI");
                var view = root.GetComponent<LeaderboardScreenView>();
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);

                var background = Ui("Img_Background", root.transform, typeof(Image)).GetComponent<Image>();
                Stretch(background.rectTransform, 0, 0, 0, 0);
                background.sprite = backgroundArt;

                // The header art carries the title, so no text sits on it.
                var header = Ui("Img_Header", root.transform, typeof(Image)).GetComponent<Image>();
                Anchor(header.rectTransform, new Vector2(.2f, .875f), new Vector2(.97f, .965f));
                header.sprite = headerArt;
                header.preserveAspect = true;
                header.raycastTarget = false;

                var back = Ui("Btn_Back", root.transform, typeof(Image), typeof(Button));
                var backRect = (RectTransform)back.transform;
                backRect.anchorMin = backRect.anchorMax = new Vector2(.03f, .92f);
                backRect.pivot = new Vector2(0, .5f);
                backRect.anchoredPosition = Vector2.zero;
                backRect.sizeDelta = new Vector2(150, 155);
                var backImage = back.GetComponent<Image>();
                backImage.sprite = backArt;
                backImage.preserveAspect = true;
                back.GetComponent<Button>().targetGraphic = backImage;

                var allTime = Tab("Btn_Tab_AllTime", root.transform, tabOn, new Vector2(.06f, .8f), new Vector2(.49f, .86f));
                var allTimeLabel = Text("Txt_Label", allTime.transform, font, "All Time", 40, DARK_TEXT);
                Stretch(allTimeLabel.rectTransform, 10, 10, 6, 6);
                var weekly = Tab("Btn_Tab_Weekly", root.transform, tabOff, new Vector2(.51f, .8f), new Vector2(.94f, .86f));
                var weeklyLabel = Text("Txt_Label", weekly.transform, font, "Weekly", 40, DARK_TEXT);
                Stretch(weeklyLabel.rectTransform, 10, 10, 6, 6);

                var podium = Podium(root.transform, font, podiumArt, crownArt, badges);

                var list = Ui("Pnl_List", root.transform, typeof(Image));
                Anchor((RectTransform)list.transform, new Vector2(.04f, .16f), new Vector2(.96f, .555f));
                list.GetComponent<Image>().color = new Color(1, 1, 1, .001f);

                var viewport = Ui("Viewport", list.transform, typeof(RectMask2D));
                Stretch((RectTransform)viewport.transform, 0, 0, 0, 0);

                var content = Ui("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                var contentRect = (RectTransform)content.transform;
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(.5f, 1);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                var group = content.GetComponent<VerticalLayoutGroup>();
                group.padding = new RectOffset(4, 4, 4, 16);
                group.spacing = 12;
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

                var template = Row("Row_Template", content.transform, font, rowArt, badges[3], starArt);
                template.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
                template.gameObject.SetActive(false);

                var status = Text("Txt_Status", list.transform, font, "Loading...", 36, Color.white);
                Anchor(status.rectTransform, new Vector2(.08f, .4f), new Vector2(.92f, .6f));
                status.textWrappingMode = TextWrappingModes.Normal;
                Outlined(status);

                var me = Row("Pnl_MyRow", root.transform, font, meArt, badges[3], starArt);
                Anchor(me, new Vector2(.03f, .05f), new Vector2(.97f, .145f));
                // The own row is the way to the profile: tapped, it opens the name and icon to change.
                var edit = me.gameObject.AddComponent<Button>();
                edit.targetGraphic = me.GetComponent<Image>();
                var editTag = Ui("Img_EditTag", me, typeof(Image)).GetComponent<Image>();
                editTag.sprite = tabOn;
                editTag.type = Image.Type.Sliced;
                editTag.raycastTarget = false;
                var tagRect = editTag.rectTransform;
                tagRect.anchorMin = tagRect.anchorMax = new Vector2(.5f, 1);
                tagRect.pivot = new Vector2(.5f, .5f);
                tagRect.anchoredPosition = new Vector2(0, 0);
                tagRect.sizeDelta = new Vector2(220, 64);
                var editLabel = Text("Txt_Edit", editTag.transform, font, "Edit", 30, DARK_TEXT);
                Stretch(editLabel.rectTransform, 8, 8, 4, 4);

                var serialized = new SerializedObject(view);
                serialized.FindProperty("_backButton").objectReferenceValue = back.GetComponent<Button>();
                serialized.FindProperty("_allTimeTab").objectReferenceValue = allTime.GetComponent<Button>();
                serialized.FindProperty("_weeklyTab").objectReferenceValue = weekly.GetComponent<Button>();
                serialized.FindProperty("_title").objectReferenceValue = null;
                serialized.FindProperty("_allTimeLabel").objectReferenceValue = allTimeLabel;
                serialized.FindProperty("_weeklyLabel").objectReferenceValue = weeklyLabel;
                serialized.FindProperty("_status").objectReferenceValue = status;
                serialized.FindProperty("_scroll").objectReferenceValue = scroll;
                serialized.FindProperty("_content").objectReferenceValue = contentRect;
                serialized.FindProperty("_rowTemplate").objectReferenceValue = template;
                serialized.FindProperty("_meRow").objectReferenceValue = me;
                serialized.FindProperty("_editProfileButton").objectReferenceValue = edit;
                serialized.FindProperty("_editProfileLabel").objectReferenceValue = editLabel;
                serialized.FindProperty("_rowColour").colorValue = Color.white;
                serialized.FindProperty("_myRowColour").colorValue = new Color(.7f, 1f, 1f, 1f);
                serialized.FindProperty("_tabOnSprite").objectReferenceValue = tabOn;
                serialized.FindProperty("_tabOffSprite").objectReferenceValue = tabOff;
                var slots = serialized.FindProperty("_podium");
                slots.arraySize = podium.Length;
                for (int i = 0; i < podium.Length; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = podium[i];
                var badgeList = serialized.FindProperty("_badges");
                badgeList.arraySize = badges.Length;
                for (int i = 0; i < badges.Length; i++) badgeList.GetArrayElementAtIndex(i).objectReferenceValue = badges[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// The podium art in a box that keeps its shape, and one slot per place over its block, in the order 1st,
        /// 2nd, 3rd. The fractions are measured on the art: where each block sits across and how high its top is.
        /// </summary>
        private static RectTransform[] Podium(Transform parent, TMP_FontAsset font, Sprite art, Sprite crown, Sprite[] badges)
        {
            var area = Ui("Pnl_PodiumArea", parent);
            Anchor((RectTransform)area.transform, new Vector2(.03f, .565f), new Vector2(.97f, .795f));
            var box = Ui("Pnl_Podium", area.transform, typeof(AspectRatioFitter));
            Stretch((RectTransform)box.transform, 0, 0, 0, 0);
            const float artShare = .7f;     // the art fills the lower part of the box; above it is room for the crowns
            var fitter = box.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 959f / (345f / artShare);

            var image = Ui("Img_Podium", box.transform, typeof(Image)).GetComponent<Image>();
            Anchor(image.rectTransform, Vector2.zero, new Vector2(1, artShare));
            image.sprite = art;
            image.raycastTarget = false;

            var places = new[]
            {
                (min: .325f, max: .665f, top: .95f),    // 1st, in the middle
                (min: 0f, max: .34f, top: .72f),        // 2nd, on the left
                (min: .65f, max: 1f, top: .58f),        // 3rd, on the right
            };
            var slots = new RectTransform[places.Length];
            for (int i = 0; i < places.Length; i++)
            {
                var place = places[i];
                float top = place.top * artShare;
                var slot = (RectTransform)Ui("Slot_" + (i + 1), box.transform).transform;
                Anchor(slot, new Vector2(place.min, 0), new Vector2(place.max, 1));

                var mark = Ui(i == 0 ? "Img_Crown" : "Img_Place", slot, typeof(Image)).GetComponent<Image>();
                mark.sprite = i == 0 ? crown : badges[i];
                mark.preserveAspect = true;
                mark.raycastTarget = false;
                Anchor(mark.rectTransform, new Vector2(.3f, top + .17f), new Vector2(.7f, Mathf.Min(1f, top + .35f)));

                var avatar = Ui("Img_Avatar", slot, typeof(Image)).GetComponent<Image>();
                avatar.preserveAspect = true;
                avatar.raycastTarget = false;
                Anchor(avatar.rectTransform, new Vector2(.22f, top - .03f), new Vector2(.78f, top + .2f));
                avatar.transform.SetSiblingIndex(mark.transform.GetSiblingIndex());
                var podiumFrame = Ui("Img_Frame", slot, typeof(Image)).GetComponent<Image>();
                podiumFrame.preserveAspect = true;
                podiumFrame.raycastTarget = false;
                Anchor(podiumFrame.rectTransform, new Vector2(.18f, top - .05f), new Vector2(.82f, top + .22f));
                podiumFrame.transform.SetSiblingIndex(avatar.transform.GetSiblingIndex() + 1);
                if (i > 0)
                {
                    var number = Text("Txt_Place", mark.transform, font, (i + 1).ToString(), 44, Color.white);
                    Stretch(number.rectTransform, 0, 0, 4, 0);
                    Outlined(number);
                }

                var name = Text("Txt_Name", slot, font, "Player", 34, Color.white);
                Anchor(name.rectTransform, new Vector2(.06f, top - .2f), new Vector2(.94f, top - .07f));
                name.overflowMode = TextOverflowModes.Ellipsis;
                name.enableAutoSizing = true;
                name.fontSizeMin = 20;
                name.fontSizeMax = 34;
                Outlined(name);

                var score = Text("Txt_Score", slot, font, "0", 32, GOLD);
                Anchor(score.rectTransform, new Vector2(.06f, top - .32f), new Vector2(.94f, top - .2f));
                Outlined(score);
                slots[i] = slot;
            }
            return slots;
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
                button.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PODIUM_ICON);
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

        private static RectTransform Row(string name, Transform parent, TMP_FontAsset font, Sprite art, Sprite badge, Sprite star)
        {
            var row = Ui(name, parent, typeof(Image));
            var image = row.GetComponent<Image>();
            image.sprite = art;
            image.type = Image.Type.Sliced;

            var badgeImage = Ui("Img_Badge", row.transform, typeof(Image)).GetComponent<Image>();
            badgeImage.sprite = badge;
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;
            Side(badgeImage.rectTransform, 0, 22, 84, 14);
            var rank = Text("Txt_Rank", badgeImage.transform, font, "4", 36, Color.white);
            Stretch(rank.rectTransform, 0, 0, 4, 0);
            rank.enableAutoSizing = true;
            rank.fontSizeMin = 18;
            rank.fontSizeMax = 36;
            Outlined(rank);

            var nameText = Text("Txt_Name", row.transform, font, "Player", 36, Color.white);
            nameText.rectTransform.anchorMin = Vector2.zero;
            nameText.rectTransform.anchorMax = Vector2.one;
            var avatar = Ui("Img_Avatar", row.transform, typeof(Image)).GetComponent<Image>();
            avatar.preserveAspect = true;
            avatar.raycastTarget = false;
            Side(avatar.rectTransform, 0, 116, 92, 12);
            var frameImage = Ui("Img_Frame", row.transform, typeof(Image)).GetComponent<Image>();
            frameImage.preserveAspect = true;
            frameImage.raycastTarget = false;
            Side(frameImage.rectTransform, 0, 108, 108, 4);

            nameText.rectTransform.offsetMin = new Vector2(222, 0);
            nameText.rectTransform.offsetMax = new Vector2(-250, 0);
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            Outlined(nameText);

            var starImage = Ui("Img_Star", row.transform, typeof(Image)).GetComponent<Image>();
            starImage.sprite = star;
            starImage.preserveAspect = true;
            starImage.raycastTarget = false;
            Side(starImage.rectTransform, 1, -26, 56, 30);
            var score = Text("Txt_Score", row.transform, font, "0", 36, Color.white);
            Side(score.rectTransform, 1, -90, 200, 0);
            score.alignment = TextAlignmentOptions.Right;
            Outlined(score);
            return (RectTransform)row.transform;
        }

        private static GameObject Tab(string name, Transform parent, Sprite sprite, Vector2 min, Vector2 max)
        {
            var tab = Ui(name, parent, typeof(Image), typeof(Button));
            Anchor((RectTransform)tab.transform, min, max);
            var image = tab.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            tab.GetComponent<Button>().targetGraphic = image;
            return tab;
        }

        private const string OUTLINE = "Assets/Modules/MainModule/zScreenModules/LeaderboardScreenModule/Art/MT_Baloo2_Outline.mat";
        private static Material _outline;

        /// <summary>A dark rim on the text, from a saved preset of the font's material so the prefab keeps it.</summary>
        private static void Outlined(TMP_Text text)
        {
            if (_outline == null)
            {
                _outline = AssetDatabase.LoadAssetAtPath<Material>(OUTLINE);
                if (_outline == null)
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(OUTLINE));
                    _outline = new Material(text.font.material);
                    AssetDatabase.CreateAsset(_outline, OUTLINE);
                }
                _outline.EnableKeyword("OUTLINE_ON");
                _outline.SetFloat("_OutlineWidth", .22f);
                _outline.SetColor("_OutlineColor", DARK_TEXT);
                _outline.SetFloat("_FaceDilate", .1f);
                EditorUtility.SetDirty(_outline);
            }
            text.fontSharedMaterial = _outline;
        }

        private static void Side(RectTransform rect, float side, float x, float width, float inset)
        {
            rect.anchorMin = new Vector2(side, 0);
            rect.anchorMax = new Vector2(side, 1);
            rect.pivot = new Vector2(side, .5f);
            rect.anchoredPosition = new Vector2(x, 0);
            rect.sizeDelta = new Vector2(width, -2 * inset);
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
