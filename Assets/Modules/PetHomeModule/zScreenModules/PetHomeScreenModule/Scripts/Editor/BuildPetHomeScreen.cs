#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Modules.PetHomeModule.PetHomeScreenModule.ViewsMediators;
using Modules.PetHomeModule.Shared.Data.UnityObjects;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Shared.Enums;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.PetHomeModule.PetHomeScreenModule.Editor
{
    /// <summary>
    /// Builds the pet home screen's prefab from the kit's cut sprites, fills CD_PetHome's shop with them and their
    /// prices, and makes the save asset. Run again after changing the layout here; prices are kept in the asset.
    /// </summary>
    internal static class BuildPetHomeScreen
    {
        private const string PREFAB = "Assets/Modules/PetHomeModule/zScreenModules/PetHomeScreenModule/Prefabs/PetHomeScreen.prefab";
        private const string ART = "Assets/Art/Generated/UI/PetHome/Sprites/";
        private const string ROOMS = "Assets/Art/Generated/UI/PetHome/Rooms/";
        private const string CONFIG = "Assets/Modules/PetHomeModule/Scriptables/CD_PetHome.asset";
        private const string SAVE = "Assets/Modules/PetHomeModule/Scriptables/SD_PetHome.asset";
        private const string FONT = "Assets/Modules/UiWidgetsModule/Fonts/Baloo2-ExtraBold SDF.asset";
        private const string TITLE_FONT = "Assets/Modules/UiWidgetsModule/Fonts/LuckiestGuy SDF.asset";
        private const string CLOSE = "Assets/Art/Generated/UI/Buttons/Kit/SPR_UI_Round_Close.png";
        private const string ROUNDED = "Assets/Modules/GameplayModule/zScreenModules/GameplayScreenModule/Sprites/UI/SPR_UI_Rounded.png";
        private const string CLOSE_SMALL = "Assets/Art/Generated/UI/Buttons/Kit/SPR_UI_Round_Close_Small.png";

        // id, category, price, height (share of the floor's height), spot on the floor
        private static readonly (string id, PetHomeCategory cat, int price, float height, Vector2 spot)[] ITEMS =
        {
            ("Room_Bedroom", PetHomeCategory.Room, 0, 1, Vector2.zero),
            ("Room_Garden", PetHomeCategory.Room, 1500, 1, Vector2.zero),
            ("Room_Candy", PetHomeCategory.Room, 2500, 1, Vector2.zero),

            ("Bowl_Ceramic", PetHomeCategory.Bowl, 0, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Paw", PetHomeCategory.Bowl, 120, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Fish", PetHomeCategory.Bowl, 180, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Wooden", PetHomeCategory.Bowl, 200, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Double", PetHomeCategory.Bowl, 250, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Bone", PetHomeCategory.Bowl, 300, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Flower", PetHomeCategory.Bowl, 350, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Candy", PetHomeCategory.Bowl, 400, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Star", PetHomeCategory.Bowl, 450, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Jelly", PetHomeCategory.Bowl, 500, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Golden", PetHomeCategory.Bowl, 800, .15f, new Vector2(.75f, .3f)),
            ("Bowl_Crown", PetHomeCategory.Bowl, 1200, .17f, new Vector2(.75f, .3f)),

            ("Bed_Cushion", PetHomeCategory.Bed, 0, .26f, new Vector2(.25f, .62f)),
            ("Bed_Basket", PetHomeCategory.Bed, 300, .3f, new Vector2(.25f, .62f)),
            ("Bed_Box", PetHomeCategory.Bed, 350, .34f, new Vector2(.25f, .62f)),
            ("Bed_Cloud", PetHomeCategory.Bed, 600, .3f, new Vector2(.25f, .62f)),
            ("Bed_Hut", PetHomeCategory.Bed, 700, .42f, new Vector2(.25f, .62f)),
            ("Bed_Mushroom", PetHomeCategory.Bed, 900, .44f, new Vector2(.25f, .62f)),
            ("Bed_Igloo", PetHomeCategory.Bed, 1000, .38f, new Vector2(.25f, .62f)),
            ("Bed_Jelly", PetHomeCategory.Bed, 1400, .4f, new Vector2(.25f, .62f)),
            ("Bed_Castle", PetHomeCategory.Bed, 2500, .52f, new Vector2(.25f, .62f)),

            ("Toy_Ball", PetHomeCategory.Toy, 80, .12f, new Vector2(.5f, .15f)),
            ("Toy_Yarn", PetHomeCategory.Toy, 80, .12f, new Vector2(.4f, .2f)),
            ("Toy_Bone", PetHomeCategory.Toy, 100, .1f, new Vector2(.6f, .18f)),
            ("Toy_Duck", PetHomeCategory.Toy, 120, .13f, new Vector2(.35f, .25f)),
            ("Toy_Carrot", PetHomeCategory.Toy, 120, .13f, new Vector2(.55f, .22f)),
            ("Toy_Mouse", PetHomeCategory.Toy, 150, .1f, new Vector2(.45f, .12f)),
            ("Toy_Frisbee", PetHomeCategory.Toy, 150, .09f, new Vector2(.6f, .1f)),
            ("Toy_Rope", PetHomeCategory.Toy, 180, .1f, new Vector2(.4f, .1f)),
            ("Toy_Feather", PetHomeCategory.Toy, 200, .17f, new Vector2(.65f, .25f)),
            ("Toy_Top", PetHomeCategory.Toy, 220, .14f, new Vector2(.5f, .25f)),
            ("Toy_Drum", PetHomeCategory.Toy, 260, .14f, new Vector2(.3f, .15f)),
            ("Toy_JellyCube", PetHomeCategory.Toy, 300, .13f, new Vector2(.7f, .15f)),
            ("Toy_Tennis", PetHomeCategory.Toy, 80, .11f, new Vector2(.45f, .18f)),
            ("Toy_PaperBag", PetHomeCategory.Toy, 90, .14f, new Vector2(.3f, .3f)),
            ("Toy_Soccer", PetHomeCategory.Toy, 100, .12f, new Vector2(.55f, .12f)),
            ("Toy_Catnip", PetHomeCategory.Toy, 120, .11f, new Vector2(.4f, .3f)),
            ("Toy_PlushFish", PetHomeCategory.Toy, 140, .12f, new Vector2(.6f, .3f)),
            ("Toy_PlushStar", PetHomeCategory.Toy, 140, .13f, new Vector2(.35f, .2f)),
            ("Toy_BellBall", PetHomeCategory.Toy, 160, .12f, new Vector2(.5f, .2f)),
            ("Toy_Donut", PetHomeCategory.Toy, 160, .11f, new Vector2(.65f, .18f)),
            ("Toy_JellyRing", PetHomeCategory.Toy, 180, .1f, new Vector2(.45f, .1f)),
            ("Toy_TreatBall", PetHomeCategory.Toy, 200, .12f, new Vector2(.55f, .28f)),
            ("Toy_Cloud", PetHomeCategory.Toy, 220, .13f, new Vector2(.3f, .25f)),
            ("Toy_Car", PetHomeCategory.Toy, 240, .12f, new Vector2(.7f, .22f)),
            ("Toy_Scratcher", PetHomeCategory.Toy, 260, .13f, new Vector2(.25f, .35f)),
            ("Toy_Butterfly", PetHomeCategory.Toy, 280, .2f, new Vector2(.6f, .4f)),
            ("Toy_Laser", PetHomeCategory.Toy, 300, .2f, new Vector2(.75f, .35f)),
            ("Toy_Puzzle", PetHomeCategory.Toy, 320, .1f, new Vector2(.4f, .4f)),
            ("Toy_JellyBear", PetHomeCategory.Toy, 350, .18f, new Vector2(.35f, .45f)),
            ("Toy_Tunnel", PetHomeCategory.Toy, 400, .16f, new Vector2(.65f, .45f)),

            ("Decor_Rug", PetHomeCategory.Decor, 200, .14f, new Vector2(.5f, .35f)),
            ("Decor_Cushion", PetHomeCategory.Decor, 150, .13f, new Vector2(.6f, .5f)),
            ("Decor_Plant", PetHomeCategory.Decor, 250, .32f, new Vector2(.9f, .85f)),
            ("Decor_Vase", PetHomeCategory.Decor, 280, .28f, new Vector2(.1f, .85f)),
            ("Decor_Lamp", PetHomeCategory.Decor, 350, .5f, new Vector2(.1f, .9f)),
            ("Decor_Table", PetHomeCategory.Decor, 400, .26f, new Vector2(.6f, .75f)),
            ("Decor_Chest", PetHomeCategory.Decor, 450, .26f, new Vector2(.85f, .7f)),
            ("Decor_Frame", PetHomeCategory.Decor, 300, .3f, new Vector2(.5f, .95f)),
            ("Decor_Lights", PetHomeCategory.Decor, 350, .2f, new Vector2(.5f, .98f)),
            ("Decor_ScratchPost", PetHomeCategory.Decor, 500, .45f, new Vector2(.85f, .45f)),
            ("Decor_Shelf", PetHomeCategory.Decor, 600, .48f, new Vector2(.15f, .95f)),
            ("Decor_Fountain", PetHomeCategory.Decor, 1200, .42f, new Vector2(.5f, .85f)),
        };

        [MenuItem("Tools/Wobble Fuse/Rebuild Pet Home")]
        public static void Build()
        {
            ImportSprites();
            FillConfig();
            MakeSave();
            BuildPrefab();
            RegisterAddressable();
            AssetDatabase.SaveAssets();
            Debug.Log("BuildPetHomeScreen - pet home built.");
        }

        /// <summary>A big scrollable room in Rooms/ wins over the small one cut from the kit sheet.</summary>
        private static Sprite Art(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(ROOMS + "SPR_PetHome_" + name + ".png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>(ART + "SPR_PetHome_" + name + ".png");

        private static void ImportSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ART.TrimEnd('/'), ROOMS.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                int size = path.StartsWith(ROOMS) ? 2048 : path.Contains("_Room_") ? 1024 : 512;
                if (importer.textureType == TextureImporterType.Sprite && importer.maxTextureSize == size) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = size;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        private static void FillConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CD_PetHome>(CONFIG);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CD_PetHome>();
                AssetDatabase.CreateAsset(config, CONFIG);
            }
            // What was tuned in the asset is kept: prices, and where each room's floor is.
            var prices = new Dictionary<string, int>();
            var floors = new Dictionary<string, Rect>();
            var shadows = new Dictionary<string, Color>();
            foreach (var item in config.Items)
            {
                if (item == null || item.Id == null) continue;
                prices[item.Id] = item.Price;
                floors[item.Id] = item.Floor;
                shadows[item.Id] = item.Shadow;
            }
            config.Items.Clear();
            foreach (var (id, cat, price, height, spot) in ITEMS)
            {
                config.Items.Add(new PetHomeItemCVO
                {
                    Id = id, Category = cat, Sprite = Art(id), Height = height, Spot = spot,
                    Price = prices.TryGetValue(id, out int kept) ? kept : price,
                    EmptySprite = cat == PetHomeCategory.Bowl ? Art(id + "_Empty") : null,
                    Floor = floors.TryGetValue(id, out var floor) ? floor : new Rect(.3f, .08f, .4f, .42f),
                    Shadow = shadows.TryGetValue(id, out var shadow) ? shadow : new Color(.22f, .1f, .32f, .3f),
                });
            }
            config.StarterItems = new List<string> { "Bowl_Ceramic", "Bed_Cushion" };
            config.StarterRoom = "Room_Bedroom";
            EditorUtility.SetDirty(config);
        }

        private static void MakeSave()
        {
            if (AssetDatabase.LoadAssetAtPath<SD_PetHome>(SAVE) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SD_PetHome>(), SAVE);
        }

        private static void BuildPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
            try
            {
                while (root.transform.childCount > 0) Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                root.layer = LayerMask.NameToLayer("UI");
                Stretch((RectTransform)root.transform);
                var view = root.GetComponent<PetHomeScreenView>() ?? root.AddComponent<PetHomeScreenView>();
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);
                var titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TITLE_FONT) ?? font;

                // Room: the background fills the screen keeping its shape; the floor is the open lower part of it.
                var backdrop = Ui("Img_Backdrop", root.transform, typeof(Image)).GetComponent<Image>();
                Stretch(backdrop.rectTransform);
                backdrop.color = new Color32(0xF6, 0xD9, 0xC4, 0xFF);
                // The room is bigger than the screen and slides under the finger; the view sizes it to its picture.
                var roomScroll = Ui("Scroll_Room", root.transform, typeof(Image), typeof(ScrollRect)).GetComponent<ScrollRect>();
                Stretch((RectTransform)roomScroll.transform);
                roomScroll.GetComponent<Image>().color = Color.clear;
                var room = Ui("Img_Room", roomScroll.transform, typeof(Image)).GetComponent<Image>();
                room.rectTransform.anchorMin = room.rectTransform.anchorMax = room.rectTransform.pivot = new Vector2(.5f, .5f);
                room.rectTransform.sizeDelta = new Vector2(2400, 2400);
                room.sprite = Art("Room_Bedroom");
                room.raycastTarget = false;
                roomScroll.content = room.rectTransform;
                roomScroll.viewport = (RectTransform)roomScroll.transform;
                roomScroll.horizontal = roomScroll.vertical = true;
                roomScroll.movementType = ScrollRect.MovementType.Clamped;
                roomScroll.inertia = true;
                roomScroll.decelerationRate = .1f;

                // The floor lives on the room picture, so it stays on the painted floor whatever the screen's shape;
                // each room says where its floor is (CD_PetHome, Floor).
                // Shadows lie on the floor under everything, so they sit in a layer of their own drawn before it.
                var shadows = (RectTransform)Ui("Pnl_Shadows", room.transform).transform;
                Anchor(shadows, new Vector2(.3f, .08f), new Vector2(.7f, .5f));
                Set(view, "_shadows", shadows);
                var floor = (RectTransform)Ui("Pnl_Floor", room.transform).transform;
                Anchor(floor, new Vector2(.3f, .08f), new Vector2(.7f, .5f));

                var mark = Ui("Img_FloorMark", floor, typeof(Image)).GetComponent<Image>();
                mark.raycastTarget = false;
                mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = Vector2.zero;
                mark.rectTransform.pivot = new Vector2(.5f, .5f);
                mark.color = new Color(1, 1, 1, .85f);
                mark.gameObject.SetActive(false);

                var itemGo = Ui("Item_Template", floor, typeof(Image), typeof(PetHomeItemView));
                var itemImage = itemGo.GetComponent<Image>();
                itemImage.preserveAspect = true;
                Set(itemGo.GetComponent<PetHomeItemView>(), "_image", itemImage);
                itemGo.SetActive(false);

                var petGo = Ui("Img_Pet", floor, typeof(Image), typeof(PetHomePetView));
                var petRect = (RectTransform)petGo.transform;
                petRect.anchorMin = petRect.anchorMax = Vector2.zero;
                petRect.pivot = new Vector2(.5f, 0);
                var petImage = petGo.GetComponent<Image>();
                petImage.enabled = false;
                var pet = petGo.GetComponent<PetHomePetView>();
                Set(pet, "_screen", view);
                Set(pet, "_image", petImage);

                // Top bar.
                var close = RoundButton("Btn_Close", root.transform, AssetDatabase.LoadAssetAtPath<Sprite>(CLOSE), new Vector2(0, 1), new Vector2(100, -150), 140);
                var coins = Ui("Pnl_Coins", root.transform, typeof(Image));
                var coinsRect = (RectTransform)coins.transform;
                coinsRect.anchorMin = coinsRect.anchorMax = coinsRect.pivot = new Vector2(.5f, 1);
                coinsRect.anchoredPosition = new Vector2(0, -95);
                coinsRect.sizeDelta = new Vector2(340, 110);
                coins.GetComponent<Image>().sprite = Art("Tag_Price");
                coins.GetComponent<Image>().raycastTarget = false;
                var coinsText = Text("Txt_Coins", coins.transform, font, 54, new Color32(0x6B, 0x3A, 0x1E, 0xFF), TextAlignmentOptions.Center);
                Anchor(coinsText.rectTransform, new Vector2(.3f, 0), new Vector2(.95f, 1));
                coinsText.text = "0";

                var edit = RoundButton("Btn_Edit", root.transform, Art("Btn_Move"), new Vector2(1, 1), new Vector2(-100, -150), 140);
                var done = RoundButton("Btn_Done", root.transform, Art("Btn_Confirm"), new Vector2(1, 1), new Vector2(-100, -150), 140);
                var shop = RoundButton("Btn_Shop", root.transform, Art("Btn_Store"), new Vector2(1, 0), new Vector2(-130, 170), 190);

                var hint = Text("Txt_Hint", root.transform, font, 40, Color.white, TextAlignmentOptions.Center);
                hint.rectTransform.anchorMin = new Vector2(.05f, 1);
                hint.rectTransform.anchorMax = new Vector2(.95f, 1);
                hint.rectTransform.pivot = new Vector2(.5f, 1);
                hint.rectTransform.anchoredPosition = new Vector2(0, -250);
                hint.rectTransform.sizeDelta = new Vector2(0, 110);
                Outline(hint);

                var toast = Text("Txt_Toast", root.transform, titleFont, 70, new Color32(0xFF, 0x5E, 0x8A, 0xFF), TextAlignmentOptions.Center);
                toast.rectTransform.anchorMin = new Vector2(.05f, .55f);
                toast.rectTransform.anchorMax = new Vector2(.95f, .62f);
                toast.rectTransform.offsetMin = toast.rectTransform.offsetMax = Vector2.zero;
                Outline(toast);

                var store = RoundButton("Btn_StoreItem", root.transform, Art("Btn_Remove"), new Vector2(.5f, .5f), Vector2.zero, 100);
                var storeRect = (RectTransform)store.transform;
                storeRect.anchorMin = storeRect.anchorMax = new Vector2(.5f, 0);
                storeRect.pivot = new Vector2(.5f, 0);
                store.gameObject.SetActive(false);

                // Shop sheet.
                // The kit's panel and card carry their own fill; the image under each only catches taps.
                var panel = Ui("Pnl_Shop", root.transform);
                Anchor((RectTransform)panel.transform, new Vector2(.02f, 0), new Vector2(.98f, .47f));
                var panelFill = Ui("Img_Fill", panel.transform, typeof(Image)).GetComponent<Image>();
                Anchor(panelFill.rectTransform, new Vector2(.04f, .03f), new Vector2(.96f, .8f));
                panelFill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ROUNDED);
                panelFill.type = Image.Type.Sliced;
                panelFill.color = new Color(1, 1, 1, 0);
                var panelFrame = Ui("Img_Frame", panel.transform, typeof(Image)).GetComponent<Image>();
                Stretch(panelFrame.rectTransform);
                panelFrame.sprite = Art("Panel_Shop");
                panelFrame.raycastTarget = false;

                var tabsRow = Ui("Row_Tabs", panel.transform, typeof(HorizontalLayoutGroup));
                Anchor((RectTransform)tabsRow.transform, new Vector2(.05f, .73f), new Vector2(.85f, .85f));
                var tabsLayout = tabsRow.GetComponent<HorizontalLayoutGroup>();
                tabsLayout.spacing = 10;
                tabsLayout.childAlignment = TextAnchor.MiddleCenter;
                tabsLayout.childControlWidth = tabsLayout.childControlHeight = true;
                tabsLayout.childForceExpandWidth = tabsLayout.childForceExpandHeight = true;
                var tabs = new List<Button>();
                foreach (string tab in new[] { "Tab_Bowl", "Tab_Bed", "Tab_Toy", "Tab_Decor", "Tab_Room" })
                {
                    var t = Ui("Btn_" + tab, tabsRow.transform, typeof(Image), typeof(Button));
                    var image = t.GetComponent<Image>();
                    image.sprite = Art(tab);
                    image.preserveAspect = true;
                    tabs.Add(t.GetComponent<Button>());
                }
                var shopClose = RoundButton("Btn_ShopClose", panel.transform, AssetDatabase.LoadAssetAtPath<Sprite>(CLOSE_SMALL), new Vector2(1, 1), new Vector2(-70, -150), 100);

                var scroll = Ui("Scroll_Cards", panel.transform, typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
                Anchor((RectTransform)scroll.transform, new Vector2(.05f, .05f), new Vector2(.95f, .71f));
                scroll.GetComponent<Image>().color = new Color(1, 1, 1, 0);
                var content = Ui("Content", scroll.transform, typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                var contentRect = (RectTransform)content.transform;
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(.5f, 1);
                contentRect.sizeDelta = Vector2.zero;
                var grid = content.GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(210, 240);
                grid.spacing = new Vector2(18, 18);
                grid.padding = new RectOffset(10, 10, 10, 10);
                grid.childAlignment = TextAnchor.UpperCenter;
                content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var scrollRect = scroll.GetComponent<ScrollRect>();
                scrollRect.content = contentRect;
                scrollRect.horizontal = false;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;

                var card = Ui("Card_Template", content.transform, typeof(Image), typeof(Button), typeof(PetHomeCardView));
                var cardFill = card.GetComponent<Image>();
                cardFill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ROUNDED);
                cardFill.type = Image.Type.Sliced;
                cardFill.color = new Color(1, 1, 1, 0);
                var cardFrame = Ui("Img_Frame", card.transform, typeof(Image)).GetComponent<Image>();
                Stretch(cardFrame.rectTransform);
                cardFrame.sprite = Art("Card_Item");
                cardFrame.raycastTarget = false;
                var icon = Ui("Img_Icon", card.transform, typeof(Image)).GetComponent<Image>();
                Anchor(icon.rectTransform, new Vector2(.12f, .3f), new Vector2(.88f, .9f));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var price = Ui("Pnl_Price", card.transform, typeof(Image));
                Anchor((RectTransform)price.transform, new Vector2(.04f, .02f), new Vector2(.96f, .27f));
                price.GetComponent<Image>().sprite = Art("Tag_Price");
                price.GetComponent<Image>().raycastTarget = false;
                var priceText = Text("Txt_Price", price.transform, font, 36, new Color32(0x6B, 0x3A, 0x1E, 0xFF), TextAlignmentOptions.Center);
                Anchor(priceText.rectTransform, new Vector2(.3f, 0), new Vector2(.98f, 1));
                var owned = Ui("Img_Owned", card.transform, typeof(Image));
                Corner((RectTransform)owned.transform, 70);
                owned.GetComponent<Image>().sprite = Art("Badge_Owned");
                owned.GetComponent<Image>().raycastTarget = false;
                var inUse = Ui("Img_InUse", card.transform, typeof(Image));
                Corner((RectTransform)inUse.transform, 70);
                inUse.GetComponent<Image>().sprite = Art("Badge_Equipped");
                inUse.GetComponent<Image>().raycastTarget = false;
                var cardView = card.GetComponent<PetHomeCardView>();
                Set(cardView, "_button", card.GetComponent<Button>());
                Set(cardView, "_icon", icon);
                Set(cardView, "_price", price);
                Set(cardView, "_priceText", priceText);
                Set(cardView, "_owned", owned);
                Set(cardView, "_inUse", inUse);
                card.SetActive(false);
                panel.SetActive(false);

                Set(view, "_room", room);
                Set(view, "_roomScroll", roomScroll);
                Set(view, "_floor", floor);
                Set(view, "_itemTemplate", itemGo.GetComponent<PetHomeItemView>());
                Set(view, "_pet", pet);
                Set(view, "_floorMark", mark);
                Set(view, "_validMark", Art("Floor_Valid"));
                Set(view, "_invalidMark", Art("Floor_Invalid"));
                Set(view, "_closeButton", close);
                Set(view, "_coinsText", coinsText);
                Set(view, "_shopButton", shop);
                Set(view, "_editButton", edit);
                Set(view, "_doneButton", done);
                Set(view, "_hintText", hint);
                Set(view, "_toastText", toast);
                Set(view, "_storeButton", store);
                Set(view, "_shopPanel", panel);
                Set(view, "_shopCloseButton", shopClose);
                SetArray(view, "_tabs", tabs.ToArray());
                Set(view, "_cardScroll", scrollRect);
                Set(view, "_cardTemplate", cardView);

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RegisterAddressable()
        {
            Assembly flowEditor = System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(), item => item.GetName().Name == "FlowIoC.Editor");
            System.Type entriesType = flowEditor?.GetType("FlowIoC.Editor.Addressables.ScreenAddressableEntries");
            System.Type registryType = flowEditor?.GetType("FlowIoC.Editor.Addressables.ScreenAddressables");
            if (entriesType == null || registryType == null) throw new System.MissingMemberException("FlowIoC screen addressable registration types were not found.");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            object entries = System.Activator.CreateInstance(entriesType, true);
            object entry = entriesType.GetMethod("For", flags)?.Invoke(entries, new object[] { "PetHomeScreen" });
            entry?.GetType().GetField("AssetPath", flags)?.SetValue(entry, PREFAB);
            object registry = System.Activator.CreateInstance(registryType, true);
            registryType.GetMethod("Register", flags)?.Invoke(registry, new[] { entry });
        }

        // ---- helpers ----

        private static GameObject Ui(string name, Transform parent, params System.Type[] components)
        {
            var list = new List<System.Type> { typeof(RectTransform) };
            list.AddRange(components);
            var go = new GameObject(name, list.ToArray());
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Button RoundButton(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, float size)
        {
            var go = Ui(name, parent, typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            return go.GetComponent<Button>();
        }

        private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var text = Ui(name, parent, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void Outline(TMP_Text text)
        {
            text.outlineWidth = .25f;
            text.outlineColor = new Color32(0x5A, 0x2A, 0x10, 0xFF);
        }

        private static void Corner(RectTransform rect, float size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(10, 10);
            rect.sizeDelta = new Vector2(size, size);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
