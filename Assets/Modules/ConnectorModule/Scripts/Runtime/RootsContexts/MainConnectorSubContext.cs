using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using Modules.GameplayModule.DuelScreenModule.Signals;
using Modules.GameplayModule.GameplayScreenModule.Signals;
using Modules.GameplayModule.Signals;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.LocalizationModule.Signals;
using Modules.MainModule.ConsentScreenModule.Signals;
using Modules.MainModule.MainScreenModule.Signals;
using Modules.MainModule.EventsScreenModule.Signals;
using Modules.MainModule.LeaderboardScreenModule.Signals;
using Modules.MainModule.Signals;
using Modules.ProfileModule.Signals;
using Modules.ProfileModule.ProfileSelectionScreenModule.Signals;

namespace Modules.ConnectorModule.RootsContexts
{
    public class MainConnectorSubContext : Context
    {
        private const string LANGUAGE_GROUP = nameof(MainConnectorSubContext) + ".Language";
        private const string LIVES_GROUP = nameof(MainConnectorSubContext) + ".Lives";
        private const string CONSENT_GROUP = nameof(MainConnectorSubContext) + ".Consent";
        private const string DUEL_GROUP = nameof(MainConnectorSubContext) + ".Duel";
        /// <summary>The height the duel's two race bars take over the map, in the gameplay screen's units.</summary>
        private const float DUEL_BARS = 230f;
        private MainSignals _mainSignals;
        private MainScreenSignals _mainScreenSignals;
        private EventsScreenSignals _eventsScreenSignals;
        private LeaderboardScreenSignals _leaderboardScreenSignals;
        private GameplayScreenSignals _gameplayScreenSignals;
        private GameplaySignals _gameplaySignals;
        private LoadingScreenSignals _loadingScreenSignals;
        private LocalizationSignals _localizationSignals;
        private ProfileSignals _profileSignals;
        private ProfileSelectionScreenSignals _profileSelectionSignals;
        private ConsentScreenSignals _consentScreenSignals;
        private DuelScreenSignals _duelScreenSignals;

        public override void Setup()
        {
            base.Setup();

            _mainSignals = InjectionBinderCrossContext.GetInstance<MainSignals>();
            _mainScreenSignals = InjectionBinderCrossContext.GetInstance<MainScreenSignals>();
            _eventsScreenSignals = InjectionBinderCrossContext.GetInstance<EventsScreenSignals>();
            _leaderboardScreenSignals = InjectionBinderCrossContext.GetInstance<LeaderboardScreenSignals>();
            _gameplayScreenSignals = InjectionBinderCrossContext.GetInstance<GameplayScreenSignals>();
            _gameplaySignals = InjectionBinderCrossContext.GetInstance<GameplaySignals>();
            _loadingScreenSignals = InjectionBinderCrossContext.GetInstance<LoadingScreenSignals>();
            _localizationSignals = InjectionBinderCrossContext.GetInstance<LocalizationSignals>();
            _profileSignals = InjectionBinderCrossContext.GetInstance<ProfileSignals>();
            _profileSelectionSignals = InjectionBinderCrossContext.GetInstance<ProfileSelectionScreenSignals>();
            _consentScreenSignals = InjectionBinderCrossContext.GetInstance<ConsentScreenSignals>();
            _duelScreenSignals = InjectionBinderCrossContext.GetInstance<DuelScreenSignals>();

            IncomingSignals();
            OutgoingSignals();
        }

        private void IncomingSignals()
        {
            _mainSignals.Outgoing.Started.Connect(_mainScreenSignals.Incoming.OpenMainScreen);
            // The consent question is asked by its panel; the answer goes back to Main, which starts ads and analytics.
            _mainSignals.Outgoing.ConsentRequested.Connect(_consentScreenSignals.Incoming.OpenConsentScreen, CONSENT_GROUP);

            // The loading screen's retry button asks Main to boot again; the set it names is Main's boot.
            _loadingScreenSignals.Outgoing.RetryClicked.Connect(_ => _mainSignals.Incoming.RetryBoot.Dispatch());

            // The main menu redraws its texts when the game's language changes.
            _localizationSignals.Outgoing.LanguageChanged.Connect(_mainScreenSignals.Incoming.ApplyLanguage, LANGUAGE_GROUP);

            _profileSignals.Outgoing.IconChanged.Connect(_mainScreenSignals.Incoming.ApplyProfileIcon);
            _profileSignals.Outgoing.NameRewardPending.Connect(_mainScreenSignals.Incoming.ApplyProfileDot);
            _profileSignals.Outgoing.IconChanged.Connect(icon => _gameplayScreenSignals.Incoming.PrefetchBoardAnimal.Dispatch(icon.Id));
            _profileSignals.Outgoing.FrameChanged.Connect(_mainScreenSignals.Incoming.ApplyProfileFrame);
            _profileSignals.Outgoing.SelectionRequested.Connect(_profileSelectionSignals.Incoming.Open);
            _profileSignals.Outgoing.ProfileSaved.Connect(_profileSelectionSignals.Incoming.ProfileSaved);
            // A locked icon is paid for from Gameplay's purse; the answer comes back under the profile's own key.
            _profileSelectionSignals.Outgoing.BuyRequested.Connect(_profileSignals.Incoming.BuyIcon, DUEL_GROUP);
            _profileSignals.Outgoing.CoinsRequested.Connect(_gameplaySignals.Incoming.SpendCoins, DUEL_GROUP);
            _gameplaySignals.Outgoing.CoinsSpent.Connect(_profileSignals.Incoming.CoinsAnswered, DUEL_GROUP);
            _profileSignals.Outgoing.IconBought.Connect(_profileSelectionSignals.Incoming.IconBought, DUEL_GROUP);
            _profileSignals.Outgoing.IconNotBought.Connect(_profileSelectionSignals.Incoming.IconNotBought, DUEL_GROUP);
            // The Events panel sits over the menu: closing it puts the menu's bar back on Home, and any other tab closes it.
            _eventsScreenSignals.Outgoing.BackClicked.Connect(_mainScreenSignals.Incoming.ShowHomeTab);
            _mainScreenSignals.Outgoing.NavTabClicked.Connect(_ => _eventsScreenSignals.Incoming.CloseEventsScreen.Dispatch(), DUEL_GROUP);
            _mainScreenSignals.Outgoing.HomeClicked.Connect(_eventsScreenSignals.Incoming.CloseEventsScreen, DUEL_GROUP);
            _leaderboardScreenSignals.Outgoing.BackClicked.Connect(_mainScreenSignals.Incoming.OpenMainScreen);
            // A changed name or icon has reached the server: the board on show fetches it again.
            _profileSignals.Outgoing.ProfilePublished.Connect(_leaderboardScreenSignals.Incoming.ReloadBoard);
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
            _gameplaySignals.Outgoing.CoinsChanged.Connect(_profileSelectionSignals.Incoming.ApplyCoins, LIVES_GROUP);
            _mainScreenSignals.Outgoing.LivesPlusClicked.Connect(_gameplaySignals.Incoming.OpenLivesOffer, LIVES_GROUP);
            _mainScreenSignals.Outgoing.DailyChallengeClicked.Connect(_gameplaySignals.Incoming.StartDailyChallenge, LIVES_GROUP);
            _gameplaySignals.Outgoing.DailyChallengeChosen.Connect(_gameplayScreenSignals.Incoming.OpenGameplayScreen, LIVES_GROUP);
            // The shop asks which once-a-day products are sold out today; Gameplay keeps that day.
            _mainScreenSignals.Outgoing.ShopSoldOutRequested.Connect(_gameplaySignals.Incoming.RequestShopSoldOut, LIVES_GROUP);
            _gameplaySignals.Outgoing.ShopSoldOutChanged.Connect(_mainScreenSignals.Incoming.ApplyShopSoldOut, LIVES_GROUP);
            _gameplaySignals.Outgoing.CurrentLevelChanged.Connect(_mainScreenSignals.Incoming.ApplyNextLevel, LIVES_GROUP);
            _mainScreenSignals.Outgoing.ProfileIconRequested.Connect(_profileSignals.Incoming.RequestCurrentIcon);
            _mainScreenSignals.Outgoing.ProfileIconClicked.Connect(_profileSignals.Incoming.OpenSelection);
            _mainScreenSignals.Outgoing.EventsClicked.Connect(_eventsScreenSignals.Incoming.OpenEventsScreen);
            // Star Rush: the panel asks for the events, shows the meter, and pays a step reached.
            _eventsScreenSignals.Outgoing.EventsRequested.Connect(_gameplaySignals.Incoming.RequestEvents);
            _gameplaySignals.Outgoing.EventsChanged.Connect(_eventsScreenSignals.Incoming.ApplyEvents);
            _eventsScreenSignals.Outgoing.StarRushClaimClicked.Connect(_gameplaySignals.Incoming.ClaimStarRush);
            // Color Carnival: a chest reached opens, and what it gave shows on the card.
            _eventsScreenSignals.Outgoing.CarnivalOpenClicked.Connect(_gameplaySignals.Incoming.OpenCarnivalChest);
            _gameplaySignals.Outgoing.CarnivalChestOpened.Connect(_eventsScreenSignals.Incoming.ApplyCarnivalPrize);
            // Speed Run: Start sets a run up, which opens the board over the menu; an ad buys one more try.
            _eventsScreenSignals.Outgoing.SpeedRunStartClicked.Connect(_gameplaySignals.Incoming.StartSpeedRun);
            _eventsScreenSignals.Outgoing.SpeedRunAdClicked.Connect(_gameplaySignals.Incoming.WatchAdForSpeedRun);
            _gameplaySignals.Outgoing.SpeedRunChosen.Connect(_gameplayScreenSignals.Incoming.OpenGameplayScreen);
            _gameplaySignals.Outgoing.SpeedRunChosen.Connect(_eventsScreenSignals.Incoming.CloseEventsScreen);
            _gameplaySignals.Outgoing.SpeedRunChosen.Connect(_mainScreenSignals.Incoming.HideMainScreen);
            _gameplaySignals.Outgoing.SpeedRunChanged.Connect(_gameplayScreenSignals.Incoming.ApplySpeedRun);
            _mainScreenSignals.Outgoing.LeaderboardClicked.Connect(_leaderboardScreenSignals.Incoming.OpenLeaderboardScreen);
            _profileSelectionSignals.Outgoing.SaveRequested.Connect(_profileSignals.Incoming.SaveProfile);
            _consentScreenSignals.Outgoing.Answered.Connect(_mainSignals.Incoming.ConsentAnswered, CONSENT_GROUP);
            _leaderboardScreenSignals.Outgoing.EditProfileClicked.Connect(_profileSignals.Incoming.OpenSelection);
            // The first name a player picks pays coins into Gameplay's purse.
            _profileSignals.Outgoing.NameRewardEarned.Connect(_gameplaySignals.Incoming.GrantCoins);

            // The duel: the Events card asks Gameplay for an opponent while the duel screen searches; the race runs
            // on the gameplay screen with the duel screen over it.
            _eventsScreenSignals.Outgoing.DuelClicked.Connect(_duelScreenSignals.Incoming.OpenSearch, DUEL_GROUP);
            _eventsScreenSignals.Outgoing.DuelClicked.Connect(_gameplaySignals.Incoming.StartDuel, DUEL_GROUP);
            _gameplaySignals.Outgoing.DuelUnavailable.Connect(_duelScreenSignals.Incoming.ShowUnavailable, DUEL_GROUP);
            _eventsScreenSignals.Outgoing.DuelClicked.Connect(_mainScreenSignals.Incoming.HideMainScreen, DUEL_GROUP);
            _duelScreenSignals.Outgoing.SearchClosed.Connect(_mainScreenSignals.Incoming.OpenMainScreen, DUEL_GROUP);
            // Closed while an invite waits: the invite is withdrawn (nothing happens when none is waiting).
            _duelScreenSignals.Outgoing.SearchClosed.Connect(_gameplaySignals.Incoming.CancelInvite, DUEL_GROUP);
            _gameplaySignals.Outgoing.ChallengeWithdrawn.Connect(_duelScreenSignals.Incoming.WithdrawChallenge, DUEL_GROUP);
            _gameplaySignals.Outgoing.DuelChosen.Connect(_gameplayScreenSignals.Incoming.OpenGameplayScreen, DUEL_GROUP);
            _gameplaySignals.Outgoing.DuelStarted.Connect(_duelScreenSignals.Incoming.StartRace, DUEL_GROUP);
            _gameplaySignals.Outgoing.DuelProgress.Connect(_duelScreenSignals.Incoming.ApplyProgress, DUEL_GROUP);
            _gameplaySignals.Outgoing.DuelFinished.Connect(_duelScreenSignals.Incoming.ShowResult, DUEL_GROUP);
            _gameplaySignals.Outgoing.DuelEnded.Connect(_duelScreenSignals.Incoming.Close, DUEL_GROUP);
            // The map is laid out under the race bars while a duel is played.
            _gameplaySignals.Outgoing.DuelStarted.Connect(_ => _gameplayScreenSignals.Incoming.ReserveTop.Dispatch(DUEL_BARS), DUEL_GROUP);
            _gameplaySignals.Outgoing.DuelEnded.Connect(() => _gameplayScreenSignals.Incoming.ReserveTop.Dispatch(0f), DUEL_GROUP);
            // One result on show at a time: the duel's, and Continue on it leaves the level for the menu.
            _gameplaySignals.Outgoing.DuelStarted.Connect(_ => _gameplayScreenSignals.Incoming.DuelMode.Dispatch(true), DUEL_GROUP);
            _gameplaySignals.Outgoing.DuelEnded.Connect(() => _gameplayScreenSignals.Incoming.DuelMode.Dispatch(false), DUEL_GROUP);
            _duelScreenSignals.Outgoing.ResultContinued.Connect(_gameplayScreenSignals.Incoming.Quit, DUEL_GROUP);
            // Play again: Gameplay remembers the rival, the level is left, and the invite goes out from the Quit flow.
            _duelScreenSignals.Outgoing.RematchClicked.Connect(_gameplaySignals.Incoming.Rematch, DUEL_GROUP);
            _duelScreenSignals.Outgoing.RematchClicked.Connect(_gameplayScreenSignals.Incoming.Quit, DUEL_GROUP);
            // Leaving a level opens the menu; a duel that starts covers it again.
            _gameplaySignals.Outgoing.DuelChosen.Connect(_mainScreenSignals.Incoming.HideMainScreen, DUEL_GROUP);
            // Live duels: the invite waits on the other player; accepting shows the panel until the start comes.
            _gameplaySignals.Outgoing.LiveInviteSent.Connect(_duelScreenSignals.Incoming.ShowWaiting, DUEL_GROUP);
            _gameplaySignals.Outgoing.DuelOpponentProgress.Connect(_duelScreenSignals.Incoming.ApplyOpponentProgress, DUEL_GROUP);
            _duelScreenSignals.Outgoing.ChallengeAccepted.Connect(_ => _duelScreenSignals.Incoming.OpenSearch.Dispatch(), DUEL_GROUP);

            // Challenges: Duel on an active player's leaderboard row sets one; one received shows over whatever is up.
            _leaderboardScreenSignals.Outgoing.ChallengeClicked.Connect(_ => _duelScreenSignals.Incoming.OpenSearch.Dispatch(), DUEL_GROUP);
            _leaderboardScreenSignals.Outgoing.ChallengeClicked.Connect(_ => _mainScreenSignals.Incoming.HideMainScreen.Dispatch(), DUEL_GROUP);
            _leaderboardScreenSignals.Outgoing.ChallengeClicked.Connect(_gameplaySignals.Incoming.StartChallenge, DUEL_GROUP);
            _gameplaySignals.Outgoing.ChallengeReceived.Connect(_duelScreenSignals.Incoming.ShowChallenge, DUEL_GROUP);
            _gameplaySignals.Outgoing.ChallengeOutcome.Connect(_duelScreenSignals.Incoming.ShowOutcome, DUEL_GROUP);
            _duelScreenSignals.Outgoing.ChallengeAccepted.Connect(_ => _eventsScreenSignals.Incoming.CloseEventsScreen.Dispatch(), DUEL_GROUP);
            _duelScreenSignals.Outgoing.ChallengeAccepted.Connect(_ => _mainScreenSignals.Incoming.HideMainScreen.Dispatch(), DUEL_GROUP);
            _duelScreenSignals.Outgoing.ChallengeAccepted.Connect(_gameplaySignals.Incoming.AcceptChallenge, DUEL_GROUP);
            // Accepted over a finished duel's result: that level is left first (ignored when no level is open).
            _duelScreenSignals.Outgoing.ChallengeAccepted.Connect(_ => _gameplayScreenSignals.Incoming.Quit.Dispatch(), DUEL_GROUP);
            _duelScreenSignals.Outgoing.ChallengeDeclined.Connect(_gameplaySignals.Incoming.DeclineChallenge, DUEL_GROUP);
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
            _profileSignals.Outgoing.NameRewardPending.Disconnect();
            _profileSignals.Outgoing.FrameChanged.Disconnect();
            _profileSignals.Outgoing.SelectionRequested.Disconnect();
            _profileSignals.Outgoing.ProfileSaved.Disconnect();
            _eventsScreenSignals.Outgoing.BackClicked.Disconnect();
            _leaderboardScreenSignals.Outgoing.BackClicked.Disconnect();
            _profileSignals.Outgoing.ProfilePublished.Disconnect();
        }

        private void UnbindOutgoingSignals()
        {
            _mainScreenSignals.Outgoing.PlayClicked.Disconnect();
            _gameplayScreenSignals.Outgoing.HomeClicked.Disconnect();
            _mainScreenSignals.Outgoing.ProfileIconRequested.Disconnect();
            _mainScreenSignals.Outgoing.ProfileIconClicked.Disconnect();
            _mainScreenSignals.Outgoing.EventsClicked.Disconnect();
            _mainScreenSignals.Outgoing.LeaderboardClicked.Disconnect();
            _profileSelectionSignals.Outgoing.SaveRequested.Disconnect();
            _leaderboardScreenSignals.Outgoing.EditProfileClicked.Disconnect();
            // Its own group, so the gameplay screen keeps hearing about the lives.
            SignalConnector.DisconnectGroup(LIVES_GROUP);
            SignalConnector.DisconnectGroup(CONSENT_GROUP);
            SignalConnector.DisconnectGroup(DUEL_GROUP);
        }
    }
}
