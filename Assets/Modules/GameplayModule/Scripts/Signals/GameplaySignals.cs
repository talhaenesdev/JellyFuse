using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.PurchaseModule.Shared.Data.ValueObjects;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.GameplayModule.Signals
{
    public class GameplaySignals : ISignalHolder
    {
        public GameplaySignalsIncoming Incoming = new();
        public GameplaySignalsOutgoing Outgoing = new();
    }

    public class GameplaySignalsIncoming
    {
        public Signal Start = new();
        public Signal<Vector2Int, Vector2Int, bool> Drag = new(hideCommandLog: true);
        public Signal Undo = new();
        public Signal Stop = new();
        /// <summary>The level was left for the main menu: the clock stops, and a level still being played costs a life.</summary>
        public Signal Quit = new();
        /// <summary>Carry on a level that was stopped for the pause panel; the clock runs again while it is still being played.</summary>
        public Signal Resume = new();
        public Signal NextLevel = new();
        /// <summary>Developer move: go to the next (+1) or previous (-1) level without winning this one.</summary>
        public Signal<int> SkipLevel = new();
        /// <summary>Use a booster on a target: the door for a rocket, the colour (JellyColour) for a UFO, nothing for a freeze.</summary>
        public Signal<BoosterType, int> UseBooster = new();
        /// <summary>Developer move: the player's boosters go back to what a new player gets.</summary>
        public Signal ResetBoosters = new();
        /// <summary>Bring the lives up to date: a refill may be due.</summary>
        public Signal RefreshLives = new();
        /// <summary>Developer move: lives back to full.</summary>
        public Signal ResetLives = new();
        /// <summary>Open the purchase popup on the lives offer.</summary>
        public Signal OpenLivesOffer = new();
        /// <summary>Send the lives offer again: its countdown ran out.</summary>
        public Signal RefreshLivesOffer = new();
        /// <summary>Show a rewarded ad and give CD_Lives' lives per ad once it is watched.</summary>
        public Signal WatchAdForLives = new();
        /// <summary>Buy lives with coins.</summary>
        public Signal BuyLives = new();
        /// <summary>A product was paid for with real money: its store id. What it gives is in CD_ShopProducts.</summary>
        public Signal<string> GrantShopProduct = new();
#if UNITY_EDITOR
        /// <summary>Editor-only cheat: end this level as won or lost at once.</summary>
        public Signal<PuzzleStatus> ForceResult = new();
#endif
    }

    public class GameplaySignalsOutgoing
    {
        public Signal<PuzzleTurnVO> BoardChanged = new(hideCommandLog: true);
        public Signal<float> TimeChanged = new(hideCommandLog: true);
        /// <summary>The level number that just started, and whether a next level follows it.</summary>
        public Signal<int, bool> LevelChanged = new();
        /// <summary>The coins the level just won paid.</summary>
        public Signal<int> CoinsEarned = new();
        /// <summary>The coins the player now holds.</summary>
        public Signal<int> CoinsChanged = new();
        /// <summary>The level Play starts now, counted from 1.</summary>
        public Signal<int> CurrentLevelChanged = new();
        /// <summary>How each booster stands now, in the order of the HUD's booster buttons.</summary>
        public Signal<IReadOnlyList<BoosterStateRVO>> BoostersChanged = new();
        /// <summary>A booster was used, on its target (see Incoming.UseBooster).</summary>
        public Signal<BoosterType, int> BoosterUsed = new();
        /// <summary>Seconds the clock stands still for; 0 when it runs again.</summary>
        public Signal<float> ClockFrozen = new(hideCommandLog: true);
        /// <summary>The lives the player holds, and when the next comes back.</summary>
        public Signal<LivesStateRVO> LivesChanged = new();
        /// <summary>A level could not start for want of a life: seconds until the next comes back.</summary>
        public Signal<float> OutOfLives = new();
        /// <summary>The lives offer, for the purchase popup to open on.</summary>
        public Signal<PurchaseOfferVO> LivesOfferOpened = new();
        /// <summary>The lives offer changed, for an open purchase popup to redraw.</summary>
        public Signal<PurchaseOfferVO> LivesOfferChanged = new();
        /// <summary>The lives offer did its job - a life bought for a level that waited - and can close.</summary>
        public Signal LivesOfferFinished = new();
        /// <summary>What the level's tutorial shows now; JellyTutorialRVO.None when there is none, or it is over.</summary>
        public Signal<JellyTutorialRVO> TutorialChanged = new();
        /// <summary>Something new the player should be told about - a booster that just opened: its info page's key.</summary>
        public Signal<string> InfoRequested = new();
    }
}
