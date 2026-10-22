using Modules.LocalizationModule.Shared.Constants;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects
{
    /// <summary>
    /// The gameplay screen's behaviour around the board: where the map sits, when the timer warns, and
    /// how long the result panels wait. The look of the HUD itself - colours, sizes, positions - is in
    /// the GameplayScreen prefab.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_GameplayHud", menuName = "Jelly Fuse/Gameplay HUD")]
    public sealed class CD_GameplayHud : ScriptableObject
    {
        [Section("Map placement")]
        [Tooltip("On: the map, doors and their counters included, reaches from the left edge of the undo button to the right edge of the settings button. Off: it keeps Map Side Margin from each side of the screen.")]
        public bool MapAlignToButtons = true;
        [Tooltip("On: the map reaches the screen's left and right edges, Map Edge Pixels in from each; above and below it keeps its placement. A map too tall for its room still shrinks to fit.")]
        public bool MapFullWidth = true;
        [Tooltip("With Map Full Width: screen pixels left free between the map and each side of the screen.")]
        [Range(0, 40)] public float MapEdgePixels = 20;
        [Tooltip("With Map Align To Buttons: moves the map's left edge right (positive) or left (negative) from the undo button's left edge, in layout units.")]
        [Range(-80, 120)] public float MapInsetLeft;
        [Tooltip("With Map Align To Buttons: moves the map's right edge left (positive) or right (negative) from the settings button's right edge, in layout units.")]
        [Range(-80, 120)] public float MapInsetRight;
        [Tooltip("With Map Align To Buttons: the map comes as close to the buttons above and the boosters below as the buttons are to the screen's side, plus this many layout units (negative lets it closer). A map taller than that space shrinks to fit.")]
        [Range(-60, 120)] public float MapVerticalGapExtra;
        [Tooltip("Without Map Align To Buttons - on: the map always fills the width it is given, and a tall map reaches into the space above and below. Off: a tall map shrinks to fit the height, leaving room at the sides.")]
        public bool MapFillWidth = true;
        [Tooltip("Without Map Align To Buttons: space between the map and each side of the screen, as a share of the screen's width.")]
        [Range(0, .25f)] public float MapSideMargin = .03f;
        [Tooltip("Room kept for the HUD above the map, in layout units (the screen is 760 units wide).")]
        [Range(120, 400)] public float MapTopSpace = 236;
        [Tooltip("Room kept for the restart button and the warning line below the map, in layout units. Lower moves the map down and lets it grow.")]
        [Range(100, 400)] public float MapBottomSpace = 222;

        [Section("Timer")]
        [Tooltip("The colour the timer turns to near the end. Its usual colour is Txt_Time's in the prefab.")]
        public Color TimeWarningColour = new Color32(255, 128, 140, 255);
        [Tooltip("The timer turns to the warning colour at this many seconds left.")]
        [Min(0)] public float TimeWarningSeconds = 20;
        [Tooltip("The label under the timer on a Hard level.")]
        public Color HardColour = new Color32(255, 170, 60, 255);
        [Tooltip("The label under the timer on a Super Hard level.")]
        public Color SuperHardColour = new Color32(255, 70, 90, 255);
        [Tooltip("The difficulty label's text size.")]
        [Range(12, 80)] public float DifficultyTextSize = 34;
        [Tooltip("How far below the timer the label sits, in layout units.")]
        public float DifficultyOffset = 8;

        [Section("Win and lose panels")]
        [Tooltip("Seconds after the win before the panel shows, so the last jelly is seen leaving.")]
        [Range(0, 3)] public float WinPanelDelay = .8f;
        [Range(0, 3)] public float LosePanelDelay = .3f;

        [Section("Coin flight")]
        [Tooltip("At most this many coins fly from the win panel to the count; a smaller win sends one per coin.")]
        [Range(1, 30)] public int CoinFlyCount = 10;
        [Tooltip("Seconds one coin takes to fly.")]
        [Range(.1f, 3)] public float CoinFlySeconds = .7f;
        [Tooltip("Seconds between one coin leaving and the next.")]
        [Range(0, .5f)] public float CoinFlyStagger = .08f;
        [Tooltip("A flying coin's size, as a share of the count's coin icon.")]
        [Range(.3f, 3)] public float CoinFlyScale = 1.2f;
        [Tooltip("How high the coins arc, as a share of the distance they fly.")]
        [Range(0, .6f)] public float CoinFlyArc = .15f;

        [Section("Boosters")]
        [Tooltip("While a rocket or a UFO waits for its target: how see-through the things it cannot pick become (0 hidden, 1 unchanged).")]
        [Range(0, 1)] public float BoosterDimAlpha = .3f;
        [Tooltip("While a rocket or a UFO waits for its target: the things it can pick pulse this many times a second.")]
        [Range(0, 4)] public float BoosterPulseSpeed = 1.6f;
        [Tooltip("While a rocket waits for its door: the soft glow pulsing round each door it can take.")]
        public Color BoosterDoorGlow = new Color(.8f, .95f, 1f, .9f);
        [Tooltip("While a rocket waits for its door: the veil over a frozen door, which it cannot take.")]
        public Color BoosterFrozenDoorVeil = new Color(.02f, .02f, .08f, .78f);
        [Tooltip("The tint of a booster button that is still locked, or that cannot be paid for.")]
        public Color BoosterLockedColour = new Color(.45f, .45f, .5f, 1);
        [Tooltip("The tint of the booster button waiting for its target.")]
        public Color BoosterArmedColour = new Color(1, .9f, .45f, 1);
        [Tooltip("The tile drawn behind each booster button. Empty draws none.")]
        public Sprite BoosterSlotSprite;
        [Tooltip("The tile's size, as a share of the booster button's.")]
        [Range(.5f, 2)] public float BoosterSlotScale = 1.15f;
        [Tooltip("The tile's tint while its booster is usable.")]
        public Color BoosterSlotColour = Color.white;
        [Tooltip("The tile's tint while its booster is locked, or cannot be paid for.")]
        public Color BoosterSlotLockedColour = new Color(.55f, .5f, .65f, 1);
        [Tooltip("The frost laid over the clock while it is frozen. Without a sprite, the clock's own background is used.")]
        public Sprite FrostSprite;
        public Color FrostColour = new Color(.6f, .9f, 1, .45f);
        [Tooltip("The clock's text colour while it is frozen.")]
        public Color FrostTimeColour = new Color(.75f, .95f, 1, 1);
        [Tooltip("The picture that flies over the board when a UFO is used.")]
        public Sprite UfoSprite;
        [Tooltip("Its size, in layout units (the screen is 760 wide).")]
        public Vector2 UfoSize = new(220, 140);
        [Tooltip("Seconds it takes to cross the screen.")]
        [Range(.3f, 4)] public float UfoFlightSeconds = 1.4f;

    }
}
