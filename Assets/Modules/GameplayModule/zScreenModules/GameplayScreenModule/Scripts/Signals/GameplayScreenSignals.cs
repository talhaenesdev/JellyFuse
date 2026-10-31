using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.Signals
{
    public class GameplayScreenSignals : ISignalHolder
    {
        public GameplayScreenSignalsIncoming Incoming = new();
        public GameplayScreenSignalsOutgoing Outgoing = new();
    }

    public class GameplayScreenSignalsIncoming
    {
        public Signal OpenGameplayScreen = new();
        public Signal<PuzzleTurnVO> ApplyBoard = new(hideCommandLog: true);
        public Signal<float> ApplyTime = new(hideCommandLog: true);
        public Signal<int, bool> ApplyLevel = new();
        /// <summary>The coin count shown top left.</summary>
        public Signal<int> ApplyCoins = new();
        /// <summary>The coins the level just won paid, shown on the win panel.</summary>
        public Signal<int> ApplyReward = new();
        /// <summary>The rewarded ads the result panels offer now.</summary>
        public Signal<AdOffersRVO> ApplyAdOffers = new();
        /// <summary>How each booster stands, in the order of the booster buttons.</summary>
        public Signal<IReadOnlyList<BoosterStateRVO>> ApplyBoosters = new();
        /// <summary>A booster was used on its target: the UFO flies over.</summary>
        public Signal<BoosterType, int> ApplyBoosterUsed = new();
        /// <summary>Seconds the clock stands still for; 0 when it runs again.</summary>
        public Signal<float> ApplyClockFrozen = new(hideCommandLog: true);
        /// <summary>The lives the player holds, and when the next comes back.</summary>
        public Signal<LivesStateRVO> ApplyLives = new();
        /// <summary>A level could not start for want of a life: seconds until the next comes back.</summary>
        public Signal<float> ApplyOutOfLives = new();
        /// <summary>Send the values of a developer tuning page, by its name (Outgoing.TuningSliders).</summary>
        public Signal<string> SendTuningSliders = new();
        /// <summary>A tuned value: its key and the new value.</summary>
        public Signal<string, float> SetTuned = new();
        /// <summary>A developer tuning page's values back to their defaults, by its name; they are sent again (Outgoing.TuningSliders).</summary>
        public Signal<string> ResetTuning = new();
        /// <summary>Show (true) or hide the bar behind the HUD at the top.</summary>
        public Signal<bool> ShowTopBar = new();
        /// <summary>Show (true) or hide the bar behind the boosters at the bottom.</summary>
        public Signal<bool> ShowBottomBar = new();
        /// <summary>The level's tutorial: the board dark but for the cells to use, and a hand showing the move.</summary>
        public Signal<JellyTutorialRVO> ApplyTutorial = new();
        /// <summary>Show the info panel's page with this key (CD_InfoPopups).</summary>
        public Signal<string> ShowInfo = new();
        /// <summary>The game's language changed: the code of the new one. The screen redraws its texts.</summary>
        public Signal<string> ApplyLanguage = new();
        /// <summary>The player quit the level from the pause panel: the screen closes and the main menu opens.</summary>
        public Signal Quit = new();
    }

    public class GameplayScreenSignalsOutgoing
    {
        public Signal Start = new();
        public Signal<Vector2Int, Vector2Int, bool> Drag = new(hideCommandLog: true);
        public Signal Undo = new();
        public Signal Closed = new();
        public Signal NextLevel = new();
        /// <summary>The screen has closed itself and the player goes back to the main menu.</summary>
        public Signal HomeClicked = new();
        /// <summary>The settings button top right was pressed: the pause panel opens over the level.</summary>
        public Signal PauseClicked = new();
        /// <summary>Developer move: the next (+1) or previous (-1) level without winning this one (the Editor's N / B keys).</summary>
        public Signal<int> SkipLevel = new();
        /// <summary>A booster and its target: the door for a rocket, the colour for a UFO, 0 for a freeze.</summary>
        public Signal<BoosterType, int> UseBooster = new();
        /// <summary>The time to the next life ran out on the HUD: the lives should be brought up to date.</summary>
        public Signal LivesRefreshRequested = new();
        /// <summary>The win panel's ad button: the win's coins again for an ad.</summary>
        public Signal DoubleRewardClicked = new();
        /// <summary>The lose panel's ad button: more time for an ad.</summary>
        public Signal ExtraTimeClicked = new();
        /// <summary>A booster with no use left was pressed while today's free one is offered: its free use for an ad.</summary>
        public Signal<BoosterType> BoosterAdClicked = new();
        /// <summary>The level label was tapped three times quickly: the developer panel opens.</summary>
        public Signal DevToolsRequested = new();
        /// <summary>A developer tuning page's values: the page's name and its sliders.</summary>
        public Signal<string, IReadOnlyList<Modules.DevToolsModule.Data.ValueObjects.DevSliderVO>> TuningSliders = new();
#if UNITY_EDITOR
        public Signal<PuzzleStatus> ForceResult = new();
#endif
    }
}
