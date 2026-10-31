using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.GameplayModule.Signals;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.LocalizationModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;
using Modules.MainModule.EventsScreenModule.Signals;
using Modules.MainModule.Signals;
using Modules.ProfileModule.Signals;
using Modules.ProfileModule.ProfileSelectionScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    public class MainConnectorSubContext : Context
    {
        private const string LANGUAGE_GROUP = nameof(MainConnectorSubContext) + ".Language";
        private const string LIVES_GROUP = nameof(MainConnectorSubContext) + ".Lives";
        private MainSignals _mainSignals;
        private MainScreenSignals _mainScreenSignals;
        private EventsScreenSignals _eventsScreenSignals;
        private GameplayScreenSignals _gameplayScreenSignals;
        private GameplaySignals _gameplaySignals;
        private LoadingScreenSignals _loadingScreenSignals;
        private LocalizationSignals _localizationSignals;
        private ProfileSignals _profileSignals;
        private ProfileSelectionScreenSignals _profileSelectionSignals;

        public override void Setup()
        {
            base.Setup();

            _mainSignals = InjectionBinderCrossContext.GetInstance<MainSignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            _eventsScreenSignals = InjectionBinderCrossContext.GetInstance<EventsScreenSignals>();
            _gameplayScreenSignals = InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _loadingScreenSignals = InjectionBinderCrossContext.GetInstance<LoadingScreenSignals>();
            _localizationSignals = InjectionBinderCrossContext.GetInstance<LocalizationSignals>();
            _profileSignals = InjectionBinderCrossContext.GetInstance<ProfileSignals>();
            _profileSelectionSignals = InjectionBinderCrossContext.GetInstance<ProfileSelectionScreenSignals>();

            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainSignals.Outgoing.Started.Connect(_mainScreenSignals.Incoming.OpenMainScreen);

            // The loading screen's retry button asks Main to boot again; the set it names is Main's boot.
            _loadingScreenSignals.Outgoing.RetryClicked.Connect(_ => _mainSignals.Incoming.RetryBoot.Dispatch());

            // The main menu redraws its texts when the game's language changes.
            _localizationSignals.Outgoing.LanguageChanged.Connect(_mainScreenSignals.Incoming.ApplyLanguage, LANGUAGE_GROUP);

            _profileSignals.Outgoing.IconChanged.Connect(_mainScreenSignals.Incoming.ApplyProfileIcon);
            _profileSignals.Outgoing.FrameChanged.Connect(_mainScreenSignals.Incoming.ApplyProfileFrame);
            _profileSignals.Outgoing.SelectionRequested.Connect(_profileSelectionSignals.Incoming.Open);
            _profileSignals.Outgoing.ProfileSaved.Connect(_profileSelectionSignals.Incoming.ProfileSaved);
            _eventsScreenSignals.Outgoing.BackClicked.Connect(_mainScreenSignals.Incoming.OpenMainScreen);
        }

        private void OutgoingSignals()
        {
            _mainScreenSignals.Outgoing.PlayClicked.Connect(_gameplayScreenSignals.Incoming.OpenGameplayScreen);
            // The gameplay screen's top-right button brings the player back to the main menu.
            _gameplayScreenSignals.Outgoing.HomeClicked.Connect(_mainScreenSignals.Incoming.OpenMainScreen);
            // The menu shows the lives; they are brought up to date whenever it opens.
            _gameplaySignals.Outgoing.LivesChanged.Connect(_mainScreenSignals.Incoming.ApplyLives, LIVES_GROUP);
            _mainSignals.Outgoing.Started.Connect(_gameplaySignals.Incoming.RefreshLives);
            _gameplayScreenSignals.Outgoing.HomeClicked.Connect(_gameplaySignals.Incoming.RefreshLives);
            _gameplaySignals.Outgoing.CoinsChanged.Connect(_mainScreenSignals.Incoming.ApplyCoins, LIVES_GROUP);
            _mainScreenSignals.Outgoing.LivesPlusClicked.Connect(_gameplaySignals.Incoming.OpenLivesOffer, LIVES_GROUP);
            // The shop asks which once-a-day products are sold out today; Gameplay keeps that day.
            _mainScreenSignals.Outgoing.ShopSoldOutRequested.Connect(_gameplaySignals.Incoming.RequestShopSoldOut, LIVES_GROUP);
            _gameplaySignals.Outgoing.ShopSoldOutChanged.Connect(_mainScreenSignals.Incoming.ApplyShopSoldOut, LIVES_GROUP);
            _gameplaySignals.Outgoing.CurrentLevelChanged.Connect(_mainScreenSignals.Incoming.ApplyNextLevel, LIVES_GROUP);
            _mainScreenSignals.Outgoing.ProfileIconRequested.Connect(_profileSignals.Incoming.RequestCurrentIcon);
            _mainScreenSignals.Outgoing.ProfileIconClicked.Connect(_profileSignals.Incoming.OpenSelection);
            _mainScreenSignals.Outgoing.EventsClicked.Connect(_eventsScreenSignals.Incoming.OpenEventsScreen);
            _profileSelectionSignals.Outgoing.SaveRequested.Connect(_profileSignals.Incoming.SaveProfile);
        }

        public override void DestroyContext()
        {
            UnbindIncomingSignals();
            UnbindOutgoingSignals();

            base.DestroyContext();
        }

        private void UnbindIncomingSignals()
        {
            _mainSignals.Outgoing.Started.Disconnect();
            _loadingScreenSignals.Outgoing.RetryClicked.Disconnect();
            // Its own group, so the other connectors listening to LanguageChanged keep theirs.
            SignalConnector.DisconnectGroup(LANGUAGE_GROUP);
            _profileSignals.Outgoing.IconChanged.Disconnect();
            _profileSignals.Outgoing.FrameChanged.Disconnect();
            _profileSignals.Outgoing.SelectionRequested.Disconnect();
            _profileSignals.Outgoing.ProfileSaved.Disconnect();
            _eventsScreenSignals.Outgoing.BackClicked.Disconnect();
        }

        private void UnbindOutgoingSignals()
        {
            _mainScreenSignals.Outgoing.PlayClicked.Disconnect();
            _gameplayScreenSignals.Outgoing.HomeClicked.Disconnect();
            _mainScreenSignals.Outgoing.ProfileIconRequested.Disconnect();
            _mainScreenSignals.Outgoing.ProfileIconClicked.Disconnect();
            _mainScreenSignals.Outgoing.EventsClicked.Disconnect();
            _profileSelectionSignals.Outgoing.SaveRequested.Disconnect();
            // Its own group, so the gameplay screen keeps hearing about the lives.
            SignalConnector.DisconnectGroup(LIVES_GROUP);
        }
    }
}
