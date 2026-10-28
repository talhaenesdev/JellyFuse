#if UNITY_EDITOR
using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using Modules.GameplayModule.GameplayScreenModule.ViewsMediators;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace Modules.GameplayModule.GameplayScreenModule.Editor
{
    internal static class BuildPopupPanelTheme
    {
        private const string SESSION_KEY = "WobbleFuse.PopupPanelThemeBuilt.v12";
        private const string ART = "Assets/Art/Generated/UI/PopupPanels/";
        private const string ATLAS = "Assets/Art/Atlases/Atlas_UI_Common.spriteatlasv2";
        private const string GAMEPLAY_PREFAB = "Assets/Modules/GameplayModule/zScreenModules/GameplayScreenModule/Prefabs/GameplayScreen.prefab";
        private const string PAUSE_PREFAB = "Assets/Modules/GameplayModule/zScreenModules/PauseScreenModule/Prefabs/PauseScreen.prefab";
        private const string SETTINGS_PREFAB = "Assets/Modules/GameplayModule/zScreenModules/SettingsScreenModule/Prefabs/SettingsScreen.prefab";
        private const string PROFILE_PREFAB = "Assets/Modules/ProfileModule/zScreenModules/ProfileSelectionScreenModule/Prefabs/ProfileSelectionScreen.prefab";
        private const string PURCHASE_PREFAB = "Assets/Modules/PurchaseModule/zScreenModules/PurchaseScreenModule/Prefabs/PurchaseScreen.prefab";
        private const string MAIN_PREFAB = "Assets/Modules/MainModule/zScreenModules/MainScreenModule/Prefabs/MainScreen.prefab";
        private const string EVENTS_PREFAB = "Assets/Modules/MainModule/zScreenModules/EventsScreenModule/Prefabs/EventsScreen.prefab";
        private const string CLOSE_PREFAB = "Assets/Modules/UiWidgetsModule/Prefabs/Btn_Close.prefab";
        private const string INFO_ASSET = "Assets/Modules/GameplayModule/zScreenModules/GameplayScreenModule/Scriptables/CD_InfoPopups.asset";

        // Every popup's close button: big enough to read as the way out and to hit with a thumb.
        private static readonly Vector2 CLOSE_SIZE = new(92, 92);
        private static readonly Color32 PANEL_INK = new(0x4A, 0x13, 0x9E, 0xFF);
        private static readonly Color32 PANEL_GOLD = new(0xFF, 0xD2, 0x3F, 0xFF);

        private static readonly (string Path, string[] Fields)[] CloseConsumers =
        {
            (GAMEPLAY_PREFAB, new[] { "_infoClose", "_loseClose", "_retryClose" }),
            (PAUSE_PREFAB, new[] { "_closeButton" }),
            ("Assets/Modules/GameplayModule/zScreenModules/SettingsScreenModule/Prefabs/SettingsScreen.prefab", new[] { "_closeButton" }),
            ("Assets/Modules/PurchaseModule/zScreenModules/PurchaseScreenModule/Prefabs/PurchaseScreen.prefab", new[] { "_closeButton" }),
            ("Assets/Modules/ProfileModule/zScreenModules/ProfileSelectionScreenModule/Prefabs/ProfileSelectionScreen.prefab", new[] { "_closeButton" }),
            ("Assets/Modules/DevToolsModule/zScreenModules/DevToolsScreenModule/Prefabs/DevToolsScreen.prefab", new[] { "_closeButton" }),
        };

        [InitializeOnLoadMethod]
        private static void ScheduleBuild()
        {
            if (SessionState.GetBool(SESSION_KEY, false)) return;
            EditorApplication.delayCall += BuildOnce;
        }

        private static void BuildOnce()
        {
            if (SessionState.GetBool(SESSION_KEY, false)) return;
            Build();
            SessionState.SetBool(SESSION_KEY, true);
        }

        [MenuItem("Tools/Wobble Fuse/Rebuild Popup Panel Theme")]
        public static void Build()
        {
            ImportSprites();
            AddToAtlas();

            Sprite rocket = Sprite("SPR_UI_Panel_BoosterRocket.png");
            Sprite freeze = Sprite("SPR_UI_Panel_BoosterFreezeTime.png");
            Sprite ufo = Sprite("SPR_UI_Panel_BoosterUfo.png");
            Sprite pause = Sprite("SPR_UI_Panel_Pause.png");
            Sprite pauseResume = Sprite("SPR_UI_Button_PauseResume.png");
            Sprite pauseQuit = Sprite("SPR_UI_Button_PauseQuit.png");
            Sprite win = Sprite("SPR_UI_Panel_Win.png");
            Sprite lose = Sprite("SPR_UI_Panel_Lose.png");
            Sprite settings = Sprite("SPR_UI_Panel_Settings.png");
            Sprite retry = Sprite("SPR_UI_Panel_Retry.png");
            Sprite profile = Sprite("SPR_UI_Panel_Profile.png");
            Sprite moreLives = Sprite("SPR_UI_Panel_MoreLives.png");
            Sprite shopEmpty = Sprite("SPR_UI_Screen_ShopEmpty.png");
            Sprite events = Sprite("SPR_UI_Screen_Events.png");
            Sprite close = Sprite("SPR_UI_Button_ClosePopup.png");

            AssignInfoPageSprites(rocket, freeze, ufo);
            BuildGameplayInfoPanel();
            BuildPausePanel(pause, pauseResume, pauseQuit);
            BuildResultPanels(win, lose, retry);
            BuildSettingsPanel(settings);
            BuildProfilePanel(profile);
            BuildMoreLivesPanel(moreLives);
            BuildEmptyShopPage(shopEmpty);
            BuildEventsPage(events);
            BuildSharedCloseButton(close);
            foreach ((string path, string[] fields) in CloseConsumers) AssignCloseButtons(path, fields, close);
            AssignEveryCloseButton(close);

            AssetDatabase.SaveAssets();
            Debug.Log("Popup panel theme rebuilt: boosters, pause, win, lose, retry, settings, profile, more lives, shop, events and shared buttons.");
        }

        private static void AssignInfoPageSprites(Sprite rocket, Sprite freeze, Sprite ufo)
        {
            CD_InfoPopups pages = AssetDatabase.LoadAssetAtPath<CD_InfoPopups>(INFO_ASSET);
            if (pages == null) throw new MissingReferenceException("CD_InfoPopups was not found.");
            foreach (var page in pages.Pages)
            {
                if (page == null) continue;
                page.PanelSprite = page.Key switch
                {
                    "booster.rocket" => rocket,
                    "booster.freezetime" => freeze,
                    "booster.ufo" => ufo,
                    _ => page.PanelSprite,
                };
            }
            EditorUtility.SetDirty(pages);
        }

        private static void BuildGameplayInfoPanel()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GAMEPLAY_PREFAB);
            try
            {
                GameplayScreenView view = root.GetComponent<GameplayScreenView>();
                SerializedObject serialized = new(view);
                GameObject panel = serialized.FindProperty("_infoPanel").objectReferenceValue as GameObject;
                TMP_Text title = serialized.FindProperty("_infoTitle").objectReferenceValue as TMP_Text;
                if (panel == null || title == null) throw new MissingReferenceException("Gameplay info panel references are incomplete.");

                Transform card = TopChildUnder(title.transform, panel.transform);
                Image theme = CreateThemeImage(card, "Img_InfoPanelTheme", new Vector2(-66, -78), new Vector2(66, 200));
                Image old = card.GetComponent<Image>();
                if (old != null && old != theme) old.color = new Color(old.color.r, old.color.g, old.color.b, 0);

                serialized.FindProperty("_infoPanelTheme").objectReferenceValue = theme;
                Transform background = card.Find("Pnl_Background");
                if (background != null) serialized.FindProperty("_infoPanelBackground").objectReferenceValue = background.gameObject;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GAMEPLAY_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildPausePanel(Sprite pause, Sprite resumeSprite, Sprite quitSprite)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PAUSE_PREFAB);
            try
            {
                MonoBehaviour view = FindBehaviourWithProperty(root, "_resumeButton");
                if (view == null) throw new MissingReferenceException("PauseScreenView was not found.");
                SerializedObject serialized = new(view);
                TMP_Text title = serialized.FindProperty("_title").objectReferenceValue as TMP_Text;
                Button resume = serialized.FindProperty("_resumeButton").objectReferenceValue as Button;
                Button quit = serialized.FindProperty("_quitButton").objectReferenceValue as Button;
                if (title == null || resume == null || quit == null) throw new MissingReferenceException("Pause panel references are incomplete.");

                Transform card = CommonAncestor(title.transform, resume.transform, quit.transform);
                Image theme = CreateThemeImage(card, "Img_PausePanelTheme", new Vector2(-45, -50), new Vector2(45, 50));
                theme.sprite = pause;

                Image old = card.GetComponent<Image>();
                if (old != null && old != theme) old.color = new Color(old.color.r, old.color.g, old.color.b, 0);
                // The old drawn card sits above the art; the art is the card now.
                Transform background = card.Find("Pnl_Background");
                if (background != null) background.gameObject.SetActive(false);
                // The art's inside is light, so labels on it turn dark; text inside buttons and tracks stays.
                foreach (TMP_Text text in card.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text == title) { text.color = PANEL_GOLD; text.outlineColor = PANEL_INK; text.outlineWidth = .2f; continue; }
                    if (text.GetComponentInParent<Selectable>(true) != null) continue;
                    text.color = PANEL_INK;
                }
                Transform divider = card.Find("Img_Divider");
                if (divider != null && divider.TryGetComponent(out Image line)) line.color = new Color32(0x6E, 0x22, 0xD6, 0x55);
                AssignButtonSprite(resume, resumeSprite);
                AssignButtonSprite(quit, quitSprite);
                PrefabUtility.SaveAsPrefabAsset(root, PAUSE_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildSharedCloseButton(Sprite close)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CLOSE_PREFAB);
            try
            {
                Image image = root.GetComponent<Image>();
                image.sprite = close;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                ((RectTransform)root.transform).sizeDelta = CLOSE_SIZE;
                PrefabUtility.SaveAsPrefabAsset(root, CLOSE_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildProfilePanel(Sprite profile)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PROFILE_PREFAB);
            try
            {
                MonoBehaviour view = FindBehaviourWithProperty(root, "_dialog");
                if (view == null) throw new MissingReferenceException("ProfileSelectionScreenView was not found.");
                SerializedObject serialized = new(view);
                RectTransform dialog = serialized.FindProperty("_dialog").objectReferenceValue as RectTransform;
                if (dialog == null) throw new MissingReferenceException("Profile dialog was not found.");
                // The dialog is far taller than the art's shape, so the art is sliced: crest, corners and edges
                // keep their drawing and only the plain middle grows. 732 wide stays inside the 32 the screen
                // keeps at each side, and the borders drawn at 1/1.6 leave a light inside wider than the 537 content.
                profile = SliceSprite("SPR_UI_Panel_Profile.png", new Vector4(110, 190, 110, 380));
                Image theme = ThemeCard(dialog, "Img_ProfilePanelTheme", profile, new Vector2(-16, -101), new Vector2(16, 222));
                theme.type = Image.Type.Sliced;
                theme.pixelsPerUnitMultiplier = 1.6f;
                TMP_Text title = FindRecursive(dialog, "TMP_title")?.GetComponent<TMP_Text>();
                if (title != null) InkTitle(title);
                PrefabUtility.SaveAsPrefabAsset(root, PROFILE_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildMoreLivesPanel(Sprite moreLives)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PURCHASE_PREFAB);
            try
            {
                MonoBehaviour view = FindBehaviourWithProperty(root, "_content");
                if (view == null) throw new MissingReferenceException("PurchaseScreenView was not found.");
                SerializedObject serialized = new(view);
                RectTransform content = serialized.FindProperty("_content").objectReferenceValue as RectTransform;
                if (content == null) throw new MissingReferenceException("Purchase content panel was not found.");
                // The card is the popup inside the content, which carries the fitter and the animation; the
                // art goes there so it grows, fits and animates with it.
                Transform stale = content.Find("Img_MoreLivesPanelTheme");
                if (stale != null) Object.DestroyImmediate(stale.gameObject);
                Transform card = content.Find("Pnl_PopupPurchase");
                if (card == null) throw new MissingReferenceException("Pnl_PopupPurchase was not found.");
                ThemeCard(card, "Img_MoreLivesPanelTheme", moreLives, new Vector2(-104, -134), new Vector2(104, 226));
                // The screen already scales the content to its share of the screen, so the card's own fitter
                // keeps no margin of its own: the art fills the content and the content keeps the margin.
                foreach (MonoBehaviour behaviour in card.GetComponents<MonoBehaviour>())
                {
                    if (behaviour == null || behaviour.GetType().Name != "PopupFitter") continue;
                    SerializedObject fitter = new(behaviour);
                    fitter.FindProperty("_horizontalMargin").floatValue = 0;
                    fitter.FindProperty("_maxHeightShare").floatValue = 1;
                    fitter.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, PURCHASE_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildEmptyShopPage(Sprite shopEmpty)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MAIN_PREFAB);
            try
            {
                Transform page = FindRecursive(root.transform, "Pnl_ShopPage");
                if (page == null) throw new MissingReferenceException("Pnl_ShopPage was not found.");

                // The art frames the shop's products. The page is 760 by 1000 and fills the screen's width, so
                // the art is 740 by 1140, its awning above the page's top; its light inside is 606 by 753.
                foreach (Transform child in page) child.gameObject.SetActive(true);
                ThemeCard(page, "Img_ShopEmptyPanelTheme", shopEmpty, Vector2.zero, Vector2.zero);
                RectTransform art = (RectTransform)page.Find("Img_ShopEmptyPanelTheme");
                art.anchorMin = art.anchorMax = art.pivot = new Vector2(.5f, .5f);
                art.sizeDelta = new Vector2(740, 1140);
                art.anchoredPosition = new Vector2(0, 70);
                Transform title = page.Find("Pnl_Title");
                if (title != null) title.gameObject.SetActive(false);
                // The offer and the four products, 650 by 895 between them, go in one box scaled into the light inside.
                Transform items = page.Find("Pnl_ShopItems");
                if (items == null)
                {
                    var box = new GameObject("Pnl_ShopItems", typeof(RectTransform));
                    box.layer = page.gameObject.layer;
                    items = box.transform;
                    items.SetParent(page, false);
                    RectTransform boxRect = (RectTransform)items;
                    boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(.5f, .5f);
                    boxRect.sizeDelta = new Vector2(650, 895);
                    var moved = new List<RectTransform>();
                    foreach (Transform child in page)
                        if (child.name == "Pnl_DailyOffer" || child.name.StartsWith("Pnl_Product_")) moved.Add((RectTransform)child);
                    foreach (RectTransform child in moved)
                    {
                        Vector2 position = child.anchoredPosition - new Vector2(0, 42);
                        child.SetParent(items, false);
                        child.anchoredPosition = position;
                    }
                }
                ((RectTransform)items).anchoredPosition = new Vector2(0, -32);
                items.localScale = Vector3.one * .8f;
                // The shop tab opens the shop; events stay closed until they have a flow of their own.
                foreach ((string tabName, bool open) in new[] { ("Btn_Nav_Shop", true), ("Btn_Nav_Events", false) })
                {
                    Transform tab = FindRecursive(root.transform, tabName);
                    if (tab != null && tab.TryGetComponent(out Button navTab)) navTab.interactable = open;
                }
                // Nothing is sold yet: every price reads Coming soon and every price panel, the offer's too,
                // shows dimmed. The page is fitted between the top bar and the nav bar by the view.
                MonoBehaviour mainView = FindBehaviourWithProperty(root, "_shopPriceLabels");
                if (mainView != null)
                {
                    var prices = new List<TMP_Text>();
                    var panels = new List<Graphic>();
                    foreach (TMP_Text text in items.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (text.name != "Txt_Price") continue;
                        prices.Add(text);
                        if (text.transform.parent.TryGetComponent(out Image panel)) panels.Add(panel);
                    }
                    SerializedObject viewObject = new(mainView);
                    SetArray(viewObject.FindProperty("_shopPriceLabels"), prices);
                    SetArray(viewObject.FindProperty("_shopBuyPanels"), panels);
                    viewObject.FindProperty("_shopFrame").objectReferenceValue = art;
                    viewObject.FindProperty("_topBar").objectReferenceValue = FindRecursive(root.transform, "Top_Panel");
                    viewObject.FindProperty("_navBar").objectReferenceValue = FindRecursive(root.transform, "Pnl_NavBar");
                    viewObject.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, MAIN_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildEventsPage(Sprite events)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EVENTS_PREFAB);
            try
            {
                Transform list = FindRecursive(root.transform, "Pnl_EventList");
                Transform viewport = list != null ? list.Find("Viewport") : null;
                if (list == null || viewport == null) throw new MissingReferenceException("Events list or its viewport was not found.");

                // The illustrated frame is the page shell. Event cards remain independent children of the
                // ScrollRect, so any number of events can still be laid out and scrolled normally.
                Image theme = ThemeCard(list, "Img_EventsPanelTheme", events, new Vector2(-16, -18), new Vector2(16, 156));
                theme.preserveAspect = false;

                // Keep the cards inside the light centre and clear of the calendar/trophy crest.
                RectTransform viewportRect = (RectTransform)viewport;
                viewportRect.offsetMin = new Vector2(58, 52);
                viewportRect.offsetMax = new Vector2(-58, -132);
                // The crest stands 156 above the list, so the list starts lower and the crest clears the subtitle.
                RectTransform listRect = (RectTransform)list;
                listRect.anchorMax = new Vector2(listRect.anchorMax.x, .74f);

                Transform header = FindRecursive(root.transform, "Pnl_Header");
                if (header != null)
                {
                    if (header.TryGetComponent(out Image plate))
                        plate.color = new Color(1, 1, 1, 0);
                    Transform title = FindRecursive(header, "Txt_Title");
                    if (title != null) title.gameObject.SetActive(false);
                    RectTransform headerRect = (RectTransform)header;
                    headerRect.anchorMin = new Vector2(.05f, .86f);
                    headerRect.anchorMax = new Vector2(.95f, .97f);
                    headerRect.anchoredPosition = Vector2.zero;
                    headerRect.sizeDelta = Vector2.zero;
                }
                Transform subtitle = FindRecursive(root.transform, "Txt_Subtitle");
                if (subtitle != null) subtitle.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, EVENTS_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildResultPanels(Sprite win, Sprite lose, Sprite retry)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GAMEPLAY_PREFAB);
            try
            {
                GameplayScreenView view = root.GetComponent<GameplayScreenView>();
                SerializedObject serialized = new(view);
                RectTransform winPanel = serialized.FindProperty("_winPanel").objectReferenceValue as RectTransform;
                RectTransform losePanel = serialized.FindProperty("_losePanel").objectReferenceValue as RectTransform;
                RectTransform retryPanel = serialized.FindProperty("_retryPanel").objectReferenceValue as RectTransform;
                if (winPanel == null || losePanel == null || retryPanel == null) throw new MissingReferenceException("Win, lose or retry panel was not found.");
                // The art's frame and crest sit outside the card, so the card's content lands on its light inside.
                ThemeCard(winPanel, "Img_WinPanelTheme", win, new Vector2(-104, -134), new Vector2(104, 226));
                ThemeCard(losePanel, "Img_LosePanelTheme", lose, new Vector2(-104, -134), new Vector2(104, 226));
                ThemeCard(retryPanel, "Img_RetryPanelTheme", retry, new Vector2(-104, -134), new Vector2(104, 226));
                PrefabUtility.SaveAsPrefabAsset(root, GAMEPLAY_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildSettingsPanel(Sprite settings)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SETTINGS_PREFAB);
            try
            {
                Transform panel = FindRecursive(root.transform, "Img_SettingsBox");
                if (panel == null) throw new MissingReferenceException("Img_SettingsBox was not found.");
                // One piece of art behind the title bar and the box together: the gear crest above the title.
                // The bar keeps to the dialog's top while the box keeps to its middle, so the gap between them
                // is the phone's. The title and close move onto the box, above its rows, and the art frames the
                // box alone: its crest above the title, its light inside behind title and rows.
                Transform old = panel.parent.Find("Img_SettingsPanelTheme");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                ThemeCard(panel, "Img_SettingsPanelTheme", settings, new Vector2(-56, -130), new Vector2(56, 287));
                Transform bar = FindRecursive(root.transform, "Top_Bar");
                if (bar != null)
                {
                    Transform barImage = bar.Find("Img_Top");
                    if (barImage != null && barImage.TryGetComponent(out Image barFill)) barFill.color = new Color(barFill.color.r, barFill.color.g, barFill.color.b, 0);
                    MoveOntoBox(bar.Find("Txt_Title"), panel, new Vector2(.5f, 1), new Vector2(0, 58));
                    MoveOntoBox(bar.Find("Btn_Close"), panel, new Vector2(1, 1), new Vector2(-8, 58));
                }
                // The screen is fitted by its design width; the art is 562 wide, so the width takes it in with room to spare.
                MonoBehaviour layout = FindBehaviourWithProperty(root, "_config");
                if (layout != null && new SerializedObject(layout).FindProperty("_config").objectReferenceValue is ScriptableObject config)
                {
                    SerializedObject configObject = new(config);
                    configObject.FindProperty("DesignWidth").floatValue = 600;
                    configObject.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(config);
                }
                Transform movedTitle = panel.Find("Txt_Title");
                if (movedTitle != null && movedTitle.TryGetComponent(out TMP_Text titleText)) InkTitle(titleText);
                PrefabUtility.SaveAsPrefabAsset(root, SETTINGS_PREFAB);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>
        /// Lays panel art behind a card: the card's own fill and old drawn background go, its labels turn
        /// dark for the art's light inside, and the dark wells in it turn purple.
        /// </summary>
        private static Image ThemeCard(Transform card, string name, Sprite sprite, Vector2 min, Vector2 max) =>
            ThemeCard(card, card, name, sprite, min, max);

        private static Image ThemeCard(Transform card, Transform holder, string name, Sprite sprite, Vector2 min, Vector2 max)
        {
            Image theme = CreateThemeImage(holder, name, min, max);
            theme.sprite = sprite;
            Image fill = card.GetComponent<Image>();
            if (fill != null && fill != theme) { fill.sprite = null; fill.color = new Color(1, 1, 1, 0); }
            Transform background = card.Find("Pnl_Background");
            if (background != null) background.gameObject.SetActive(false);
            foreach (Transform child in card)
            {
                if (child == theme.transform) continue;
                if (child.TryGetComponent(out TMP_Text text)) { if (child.name == "Txt_Title") InkTitle(text); else text.color = PANEL_INK; }
                else if (child.GetComponent<Selectable>() == null && child.TryGetComponent(out Image well) && well.sprite != null && well.color.r < .3f && well.color.g < .3f)
                    well.color = new Color32(0x6E, 0x22, 0xD6, 0xE6);
            }
            return theme;
        }

        private static void MoveOntoBox(Transform item, Transform box, Vector2 anchor, Vector2 position)
        {
            if (item == null) return;
            item.SetParent(box, false);
            RectTransform rect = (RectTransform)item;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
        }

        private static void InkTitle(TMP_Text title)
        {
            title.color = new Color32(0x9B, 0x3C, 0xF5, 0xFF);
            title.outlineWidth = 0;
        }

        private static void StyleTopTitle(Transform header, string caption, Vector2 position, Vector2 size)
        {
            RectTransform rect = (RectTransform)header;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            if (header.TryGetComponent(out Image plate))
            {
                plate.color = new Color32(0x83, 0x32, 0xE8, 0xFF);
                plate.raycastTarget = false;
            }

            TMP_Text label = FindRecursive(header, "Txt_Title")?.GetComponent<TMP_Text>();
            if (label == null) label = header.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            label.text = caption;
            label.color = PANEL_GOLD;
            label.fontSize = 52;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.outlineColor = PANEL_INK;
            label.outlineWidth = .2f;
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            bool hasButton = header.GetComponentInChildren<Button>(true) != null;
            labelRect.offsetMin = new Vector2(hasButton ? 108 : 28, 8);
            labelRect.offsetMax = new Vector2(-28, -8);
        }

        private static void AssignPanelSprite(Transform panel, Sprite sprite)
        {
            Image image = panel.GetComponent<Image>();
            if (image == null) image = panel.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Every close button in every module's prefabs, wherever no view names it: a button whose name
        /// says close, or one still drawn with the old cross. Popups nested in other prefabs are done first,
        /// so the screens that hold them inherit the new sprite.
        /// </summary>
        private static void AssignEveryCloseButton(Sprite close)
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Modules" })) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort((a, b) => (a.Contains("/Popups/") || a.Contains("Pnl_") ? 0 : 1).CompareTo(b.Contains("/Popups/") || b.Contains("Pnl_") ? 0 : 1));
            foreach (string path in paths)
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || !HasCloseToFix(asset, close)) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (Button button in root.GetComponentsInChildren<Button>(true))
                    {
                        if (!IsClose(button, close)) continue;
                        AssignButtonSprite(button, close);
                        ((RectTransform)button.transform).sizeDelta = CLOSE_SIZE;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        private static bool HasCloseToFix(GameObject root, Sprite close)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                if (IsClose(button, close) && (((Image)button.targetGraphic).sprite != close || ((RectTransform)button.transform).sizeDelta != CLOSE_SIZE))
                    return true;
            return false;
        }

        private static bool IsClose(Button button, Sprite close)
        {
            if (button.targetGraphic is not Image image) return false;
            if (image.sprite == close) return true;
            string sprite = image.sprite != null ? image.sprite.name.ToLowerInvariant() : "";
            return button.name.ToLowerInvariant().Contains("close") || sprite.Contains("cross");
        }

        private static void AssignCloseButtons(string path, IReadOnlyList<string> fields, Sprite close)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    SerializedObject serialized = new(behaviour);
                    foreach (string field in fields)
                    {
                        SerializedProperty property = serialized.FindProperty(field);
                        if (property?.objectReferenceValue is not Button button) continue;
                        Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
                        if (image == null) continue;
                        image.sprite = close;
                        image.type = Image.Type.Simple;
                        image.preserveAspect = true;
                        image.color = Color.white;
                        changed = true;
                    }
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Image CreateThemeImage(Transform parent, string name, Vector2 min, Vector2 max)
        {
            Transform old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = false;
            // A card that lays out its children would lay the art out as one of them.
            if (parent.GetComponent<LayoutGroup>() != null) go.AddComponent<LayoutElement>().ignoreLayout = true;
            // The art reaches past the card, so the card's fitter measures both and keeps the art on screen.
            foreach (MonoBehaviour behaviour in parent.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour.GetType().Name != "PopupFitter") continue;
                SerializedObject fitter = new(behaviour);
                fitter.FindProperty("_frame").objectReferenceValue = rect;
                fitter.ApplyModifiedPropertiesWithoutUndo();
            }
            return image;
        }

        private static Transform TopChildUnder(Transform child, Transform ancestor)
        {
            Transform current = child;
            while (current.parent != null && current.parent != ancestor) current = current.parent;
            return current;
        }

        private static Transform CommonAncestor(params Transform[] transforms)
        {
            for (Transform candidate = transforms[0]; candidate != null; candidate = candidate.parent)
            {
                bool containsAll = true;
                for (int i = 1; i < transforms.Length; i++)
                    if (!transforms[i].IsChildOf(candidate)) { containsAll = false; break; }
                if (containsAll) return candidate;
            }
            throw new MissingReferenceException("A common popup panel ancestor was not found.");
        }

        private static void AssignButtonSprite(Button button, Sprite sprite)
        {
            if (button?.targetGraphic is not Image image) return;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        private static MonoBehaviour FindBehaviourWithProperty(GameObject root, string property)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null && new SerializedObject(behaviour).FindProperty(property) != null)
                    return behaviour;
            return null;
        }

        private static void SetArray<T>(SerializedProperty array, IReadOnlyList<T> values) where T : Object
        {
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        /// <summary>Gives the sprite its nine-slice border, in pixels: left, bottom, right, top.</summary>
        private static Sprite SliceSprite(string name, Vector4 border)
        {
            if (AssetImporter.GetAtPath(ART + name) is TextureImporter importer && importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
            return Sprite(name);
        }

        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ART + name);

        private static void ImportSprites()
        {
            foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] { ART }))
            {
                string asset = AssetDatabase.GUIDToAssetPath(path);
                if (AssetImporter.GetAtPath(asset) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        private static void AddToAtlas()
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(ATLAS);
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(ART.TrimEnd('/'));
            if (atlas == null || folder == null) throw new MissingReferenceException("Popup atlas or art folder was not found.");
            SpriteAtlasExtensions.Add(atlas, new[] { folder });
            EditorUtility.SetDirty(atlas);
        }
    }
}
#endif
