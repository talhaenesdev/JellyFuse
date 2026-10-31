using System.Collections.Generic;
using FlowIoC.BaseModule.Signals;
using Modules.ProfileModule.Shared.Data.ValueObjects;

namespace Modules.MainModule.MainScreenModule.Signals
{
    public class MainScreenSignals : ISignalHolder
    {
        public MainScreenSignalsIncoming Incoming = new();
        public MainScreenSignalsOutgoing Outgoing = new();
    }

    public class MainScreenSignalsIncoming
    {
        public Signal OpenMainScreen = new();
        /// <summary>The game's language changed: the code of the new one. The screen redraws its texts.</summary>
        public Signal<string> ApplyLanguage = new();
        /// <summary>The coin count shown top left.</summary>
        public Signal<int> ApplyCoins = new();
        /// <summary>The level Play starts, counted from 1.</summary>
        public Signal<int> ApplyNextLevel = new();
        /// <summary>The lives the player holds, shown as lives/max.</summary>
        public Signal<Modules.GameplayModule.Shared.Data.ValueObjects.LivesStateRVO> ApplyLives = new();
        /// <summary>The profile icon currently selected by the player.</summary>
        public Signal<ProfileIconVO> ApplyProfileIcon = new();
        public Signal<ProfileFrameVO> ApplyProfileFrame = new();
        /// <summary>The store's localized price of each product, by product id.</summary>
        public Signal<IReadOnlyDictionary<string, string>> ApplyShopPrices = new();
        /// <summary>The products sold out today, by product id: their panels wait until tomorrow.</summary>
        public Signal<IReadOnlyList<string>> ApplyShopSoldOut = new();
    }

    public class MainScreenSignalsOutgoing
    {
        public Signal PlayClicked = new();
        /// <summary>The settings button, top right, was pressed.</summary>
        public Signal SettingsClicked = new();
        /// <summary>The Home button in the navigation bar was pressed.</summary>
        public Signal HomeClicked = new();
        /// <summary>A tab of the navigation bar was picked: its index, left to right (2 is Home).</summary>
        public Signal<int> NavTabClicked = new();
        /// <summary>The Events navigation item was pressed.</summary>
        public Signal EventsClicked = new();
        public Signal ProfileIconClicked = new();
        public Signal ProfileIconRequested = new();
        /// <summary>The + by the lives was pressed: more lives are offered.</summary>
        public Signal LivesPlusClicked = new();
        /// <summary>The coin icon was tapped three times quickly: the developer panel opens.</summary>
        public Signal DevToolsRequested = new();
        /// <summary>The screen opened and wants the store's prices.</summary>
        public Signal ShopPricesRequested = new();
        /// <summary>The screen opened and wants to know which products are sold out today.</summary>
        public Signal ShopSoldOutRequested = new();
        /// <summary>A priced shop panel was pressed: the product id to buy.</summary>
        public Signal<string> ShopBuyClicked = new();
    }
}
