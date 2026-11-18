#if UNITY_EDITOR
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Modules.GameplayModule.DuelScreenModule.ViewsMediators;

namespace Modules.GameplayModule.DuelScreenModule.Editor
{
    /// <summary>
    /// Lays the duel screen out once from the art in Assets/Art/Generated/UI/Duel, and puts the duel card at the top of the
    /// Events screen's list. Run again only to start the layout over: whatever was moved by hand in DuelScreen is lost,
    /// while the Events screen keeps everything but its duel card.
    /// </summary>
    internal static class BuildDuelScreen
    {
        private const string PREFAB = "Assets/Modules/GameplayModule/zScreenModules/DuelScreenModule/Prefabs/DuelScreen.prefab";
        private const string EVENTS_PREFAB = "Assets/Modules/MainModule/zScreenModules/EventsScreenModule/Prefabs/EventsScreen.prefab";
        private const string ART = "Assets/Art/Generated/UI/Duel/";
        private const string FONT = "Assets/Modules/UiWidgetsModule/Fonts/Baloo2-ExtraBold SDF.asset";
        private const string TITLE_FONT = "Assets/Modules/UiWidgetsModule/Fonts/LuckiestGuy SDF.asset";
        private const string ROUNDED = "Assets/Modules/GameplayModule/zScreenModules/GameplayScreenModule/Sprites/UI/SPR_UI_Rounded.png";

        private static readonly Color32 SHADE = new(0x3A, 0x0C, 0x8A, 0xC8);
        private static readonly Color32 GOLD = new(0xFF, 0xD2, 0x3F, 0xFF);
        private static readonly Color32 SKY = new(0x1E, 0xB4, 0xF2, 0xFF);
        private static readonly Color32 VIOLET = new(0x7B, 0x5C, 0xD6, 0xFF);

        [MenuItem("Tools/Wobble Fuse/Rebuild Duel Screen")]
        public static void Build()
        {
            ImportSprites();
            BuildScreen();
            AddEventsCard();
            RegisterAddressable();
            AssetDatabase.SaveAssets();
            Debug.Log("DuelScreen built and the duel card added to the Events screen.");
        }

        private static void BuildScreen()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
            try
            {
                while (root.transform.childCount > 0) Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                root.layer = LayerMask.NameToLayer("UI");
                Stretch((RectTransform)root.transform);
                DuelScreenView view = root.GetComponent<DuelScreenView>() ?? root.AddComponent<DuelScreenView>();
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);
                TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TITLE_FONT) ?? font;
                Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>(ROUNDED);

                // Under everything: keeps taps off the board while the search and the countdown show.
                Image blocker = Img("Img_Blocker", root.transform, null);
                Stretch(blocker.rectTransform);
                blocker.color = new Color(0.08f, 0.03f, 0.2f, 0.55f);
                blocker.raycastTarget = true;

                // The race bar: the opponent's row above the player's, high on the board.
                GameObject race = Ui("Pnl_Race", root.transform);
                RectTransform raceRect = (RectTransform)race.transform;
                // As wide as the track's own proportions allow at its height (1024x160), plus the avatar on its left.
                raceRect.anchorMin = raceRect.anchorMax = new Vector2(0.5f, 1);
                raceRect.pivot = new Vector2(0.5f, 1);
                raceRect.anchoredPosition = new Vector2(0, -300);
                raceRect.sizeDelta = new Vector2(100 + 91 * 1024f / 160f, 250);
                var opponentRow = Row(race.transform, "Pnl_OpponentRow", 0, "SPR_UI_Duel_RaceFill_Opponent", "SPR_UI_Duel_AvatarRing_Opponent", font);
                var playerRow = Row(race.transform, "Pnl_PlayerRow", -125, "SPR_UI_Duel_RaceFill_Player", "SPR_UI_Duel_AvatarRing_Player", font);

                // The search panel.
                GameObject search = Ui("Pnl_Search", root.transform);
                Place((RectTransform)search.transform, new Vector2(0, 60), new Vector2(900, 900));
                Image panel = Img("Img_Panel", search.transform, Sprite("SPR_UI_Duel_SearchPanel"));
                Stretch(panel.rectTransform);
                // The panel art draws both rings and the VS itself: the icons sit inside its rings.
                Image playerSearchIcon = Img("Img_PlayerIcon", search.transform, null);
                Place(playerSearchIcon.rectTransform, new Vector2(-183, -32), new Vector2(190, 190));
                Image opponentSearchIcon = Img("Img_OpponentIcon", search.transform, null);
                Place(opponentSearchIcon.rectTransform, new Vector2(170, -32), new Vector2(190, 190));
                playerSearchIcon.transform.SetSiblingIndex(0);
                opponentSearchIcon.transform.SetSiblingIndex(0);
                Image spinnerImage = Img("Img_Spinner", search.transform, Sprite("SPR_UI_Duel_Icon"));
                Place(spinnerImage.rectTransform, new Vector2(170, -32), new Vector2(130, 130));
                RectTransform spinner = spinnerImage.rectTransform;
                TMP_Text status = Text("Txt_Status", search.transform, font, "Finding an opponent...", 34);
                Place(status.rectTransform, new Vector2(0, -238), new Vector2(680, 80));
                status.alignment = TextAlignmentOptions.Center;
                status.textWrappingMode = TextWrappingModes.Normal;
                status.enableAutoSizing = true;
                status.fontSizeMin = 20;
                status.fontSizeMax = 34;
                status.color = Color.white;
                status.outlineColor = SHADE;
                status.outlineWidth = 0.2f;
                Button close = Button("Btn_Close", search.transform, rounded, font, "OK", new Vector2(0, -420), new Vector2(300, 100));

                // The countdown, over the board.
                Image countdown = Img("Img_Countdown", root.transform, Sprite("SPR_UI_Duel_Countdown_3"));
                Place(countdown.rectTransform, new Vector2(0, 80), new Vector2(460, 460));

                // The result: a tap anywhere puts it away and leaves the win or lose panel under it.
                GameObject result = Ui("Pnl_Result", root.transform, typeof(Image), typeof(Button));
                Stretch((RectTransform)result.transform);
                Image resultShade = result.GetComponent<Image>();
                resultShade.color = new Color(0, 0, 0, 0.35f);
                result.GetComponent<Button>().targetGraphic = resultShade;
                Image banner = Img("Img_Banner", result.transform, Sprite("SPR_UI_Duel_ResultWin"));
                Place(banner.rectTransform, new Vector2(0, 120), new Vector2(960, 720));
                TMP_Text title = Text("Txt_Title", banner.transform, titleFont, "You won!", 76);
                Place(title.rectTransform, new Vector2(0, 0), new Vector2(620, 120));
                title.alignment = TextAlignmentOptions.Center;
                title.color = Color.white;
                title.outlineColor = SHADE;
                title.outlineWidth = 0.25f;
                TMP_Text times = Text("Txt_Times", result.transform, font, "You 0.0s  ·  Rival 0.0s", 38);
                Place(times.rectTransform, new Vector2(0, -280), new Vector2(860, 70));
                times.alignment = TextAlignmentOptions.Center;
                times.color = Color.white;
                times.outlineColor = SHADE;
                times.outlineWidth = 0.22f;
                GameObject reward = Ui("Pnl_Reward", result.transform);
                Place((RectTransform)reward.transform, new Vector2(0, -400), new Vector2(420, 150));
                Image chest = Img("Img_Chest", reward.transform, Sprite("SPR_UI_Duel_RewardChest"));
                Place(chest.rectTransform, new Vector2(-90, 0), new Vector2(150, 150));
                TMP_Text coins = Text("Txt_Coins", reward.transform, titleFont, "+100", 64);
                Place(coins.rectTransform, new Vector2(80, 0), new Vector2(220, 100));
                coins.alignment = TextAlignmentOptions.Left;
                coins.color = GOLD;
                coins.outlineColor = SHADE;
                coins.outlineWidth = 0.25f;

                var so = new SerializedObject(view);
                Set(so, "_search", search);
                Set(so, "_searchPlayerIcon", playerSearchIcon);
                Set(so, "_searchOpponentIcon", opponentSearchIcon);
                Set(so, "_searchStatus", status);
                Set(so, "_searchClose", close);
                Set(so, "_searchSpinner", spinner);
                Set(so, "_blocker", blocker.gameObject);
                Set(so, "_countdown", countdown);
                var sprites = so.FindProperty("_countdownSprites");
                sprites.arraySize = 4;
                string[] names = { "SPR_UI_Duel_Countdown_3", "SPR_UI_Duel_Countdown_2", "SPR_UI_Duel_Countdown_1", "SPR_UI_Duel_Countdown_Go" };
                for (int i = 0; i < 4; i++) sprites.GetArrayElementAtIndex(i).objectReferenceValue = Sprite(names[i]);
                Set(so, "_race", race);
                Set(so, "_playerFill", playerRow.fill);
                Set(so, "_opponentFill", opponentRow.fill);
                Set(so, "_racePlayerIcon", playerRow.icon);
                Set(so, "_raceOpponentIcon", opponentRow.icon);
                Set(so, "_playerName", playerRow.name);
                Set(so, "_opponentName", opponentRow.name);
                Set(so, "_result", result);
                Set(so, "_resultClose", result.GetComponent<Button>());
                Set(so, "_resultBanner", banner);
                Set(so, "_winSprite", Sprite("SPR_UI_Duel_ResultWin"));
                Set(so, "_loseSprite", Sprite("SPR_UI_Duel_ResultLose"));
                Set(so, "_resultTitle", title);
                Set(so, "_resultTimes", times);
                Set(so, "_reward", reward);
                Set(so, "_rewardCoins", coins);
                so.ApplyModifiedPropertiesWithoutUndo();

                search.SetActive(false);
                countdown.gameObject.SetActive(false);
                race.SetActive(false);
                result.SetActive(false);
                blocker.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>One runner's row: their icon in its ring on the left, the track, the fill inside it, their name above.</summary>
        private static (Image fill, Image icon, TMP_Text name) Row(Transform parent, string name, float y, string fillSprite, string ringSprite, TMP_FontAsset font)
        {
            GameObject row = Ui(name, parent);
            RectTransform rect = (RectTransform)row.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(0, 125);

            Image track = Img("Img_Track", row.transform, Sprite("SPR_UI_Duel_RaceBar"));
            track.preserveAspect = false;
            track.rectTransform.anchorMin = new Vector2(0, 0);
            track.rectTransform.anchorMax = new Vector2(1, 1);
            track.rectTransform.offsetMin = new Vector2(100, 4);
            track.rectTransform.offsetMax = new Vector2(0, -30);
            track.type = Image.Type.Sliced;
            Image fill = Img("Img_Fill", track.transform, Sprite(fillSprite));
            fill.preserveAspect = false;
            fill.type = Image.Type.Sliced;
            fill.rectTransform.anchorMin = new Vector2(0.02f, 0.2f);
            fill.rectTransform.anchorMax = new Vector2(0.02f, 0.8f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.enabled = false;

            Image ring = Img("Img_Ring", row.transform, Sprite(ringSprite));
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0, 0.5f);
            ring.rectTransform.pivot = new Vector2(0, 0.5f);
            ring.rectTransform.anchoredPosition = new Vector2(0, -6);
            ring.rectTransform.sizeDelta = new Vector2(110, 110);
            Image icon = Img("Img_Icon", ring.transform, null);
            Place(icon.rectTransform, Vector2.zero, new Vector2(76, 76));
            icon.transform.SetAsFirstSibling();

            TMP_Text label = Text("Txt_Name", row.transform, font, "Name", 26);
            label.rectTransform.anchorMin = new Vector2(0, 1);
            label.rectTransform.anchorMax = new Vector2(1, 1);
            label.rectTransform.pivot = new Vector2(0, 1);
            label.rectTransform.offsetMin = new Vector2(120, -32);
            label.rectTransform.offsetMax = new Vector2(0, 4);
            label.alignment = TextAlignmentOptions.Left;
            label.color = Color.white;
            label.outlineColor = SHADE;
            label.outlineWidth = 0.25f;
            return (fill, icon, label);
        }

        private static Image Avatar(Transform parent, string who, Vector2 position, float size, string ringSprite)
        {
            Image icon = Img("Img_" + who + "Icon", parent, null);
            Place(icon.rectTransform, position, new Vector2(size * 0.68f, size * 0.68f));
            Image ring = Img("Img_" + who + "Ring", parent, Sprite(ringSprite));
            Place(ring.rectTransform, position, new Vector2(size, size));
            return icon;
        }

        /// <summary>The duel card at the top of the Events list; the card already there is replaced, nothing else is touched.</summary>
        private static void AddEventsCard()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EVENTS_PREFAB);
            try
            {
                Transform content = root.transform.Find("Pnl_EventList/Viewport/Content");
                if (content == null)
                {
                    Debug.LogError("BuildDuelScreen - the Events screen has no Pnl_EventList/Viewport/Content to put the duel card in.");
                    return;
                }
                Transform old = content.Find("Pnl_Event_Duel");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);
                TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TITLE_FONT) ?? font;
                Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>(ROUNDED);

                GameObject card = Ui("Pnl_Event_Duel", content, typeof(Image), typeof(LayoutElement));
                card.transform.SetAsFirstSibling();
                card.GetComponent<LayoutElement>().preferredHeight = 330;
                Image art = card.GetComponent<Image>();
                art.sprite = Sprite("SPR_UI_Duel_EventCard");
                art.preserveAspect = true;
                TMP_Text name = Text("Txt_Name", card.transform, titleFont, "DUEL", 46);
                name.rectTransform.anchorMin = new Vector2(0.52f, 0.56f);
                name.rectTransform.anchorMax = new Vector2(0.82f, 0.84f);
                name.enableAutoSizing = true;
                name.fontSizeMin = 24;
                name.fontSizeMax = 46;
                name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;
                name.alignment = TextAlignmentOptions.Center;
                name.color = GOLD;
                name.outlineColor = SHADE;
                name.outlineWidth = 0.25f;
                Button play = Button("Btn_PlayDuel", card.transform, rounded, font, "PLAY", Vector2.zero, new Vector2(200, 72));
                RectTransform playRect = (RectTransform)play.transform;
                playRect.anchorMin = playRect.anchorMax = new Vector2(0.67f, 0.36f);

                // Found by name: the duel screen references nothing of the Events screen's assembly.
                var view = root.GetComponent("EventsScreenView");
                var so = new SerializedObject(view);
                so.FindProperty("_duelButton").objectReferenceValue = play;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, EVENTS_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Button Button(string name, Transform parent, Sprite rounded, TMP_FontAsset font, string label, Vector2 position, Vector2 size)
        {
            GameObject go = Ui(name, parent, typeof(Image), typeof(UnityEngine.UI.Button), typeof(Shadow));
            Place((RectTransform)go.transform, position, size);
            Image image = go.GetComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = SKY;
            go.GetComponent<Shadow>().effectColor = SHADE;
            go.GetComponent<Shadow>().effectDistance = new Vector2(0, -5);
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            TMP_Text text = Text("Txt_Label", go.transform, font, label, 34);
            Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return button;
        }

        private static void Set(SerializedObject so, string field, Object value)
        {
            var property = so.FindProperty(field);
            if (property == null) Debug.LogError("BuildDuelScreen - DuelScreenView has no field " + field);
            else property.objectReferenceValue = value;
        }

        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ART + name + ".png");

        private static Image Img(string name, Transform parent, Sprite sprite)
        {
            Image image = Ui(name, parent, typeof(Image)).GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            if (sprite == null) image.enabled = false;
            return image;
        }

        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size)
        {
            TMP_Text text = Ui(name, parent, typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
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

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void ImportSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ART.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.maxTextureSize <= 1024 && !importer.mipmapEnabled) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            // The race bars stretch along the screen: only their ends keep their shape.
            Border("SPR_UI_Duel_RaceBar", new Vector4(80, 0, 170, 0));
            Border("SPR_UI_Duel_RaceFill_Player", new Vector4(30, 0, 30, 0));
            Border("SPR_UI_Duel_RaceFill_Opponent", new Vector4(30, 0, 30, 0));
        }

        private static void Border(string name, Vector4 border)
        {
            if (AssetImporter.GetAtPath(ART + name + ".png") is not TextureImporter importer || importer.spriteBorder == border) return;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static void RegisterAddressable()
        {
            Assembly flowEditor = System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(), item => item.GetName().Name == "FlowIoC.Editor");
            System.Type entriesType = flowEditor?.GetType("FlowIoC.Editor.Addressables.ScreenAddressableEntries");
            System.Type registryType = flowEditor?.GetType("FlowIoC.Editor.Addressables.ScreenAddressables");
            if (entriesType == null || registryType == null)
                throw new System.MissingMemberException("FlowIoC screen addressable registration types were not found.");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            object entries = System.Activator.CreateInstance(entriesType, true);
            object entry = entriesType.GetMethod("For", flags)?.Invoke(entries, new object[] { "DuelScreen" });
            entry?.GetType().GetField("AssetPath", flags)?.SetValue(entry, PREFAB);
            object registry = System.Activator.CreateInstance(registryType, true);
            registryType.GetMethod("Register", flags)?.Invoke(registry, new[] { entry });
        }
    }
}
#endif
