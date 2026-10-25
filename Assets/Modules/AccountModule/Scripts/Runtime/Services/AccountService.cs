using System;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Modules.AccountModule.Services.Sub;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Modules.AccountModule.Services
{
    internal sealed class AccountService : IAccountService
    {
        private const string GOOGLE_PLAY_GAMES = "google-play-games";

        private readonly GooglePlayGamesSubService _playGames = new();
        private Task<bool> _signingIn;

        public bool IsSignedIn => UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
        public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : string.Empty;
        public string PlayerName => IsSignedIn ? AuthenticationService.Instance.PlayerName ?? string.Empty : string.Empty;

        public bool IsLinkedToGooglePlayGames
        {
            get
            {
                if (!IsSignedIn || AuthenticationService.Instance.PlayerInfo?.Identities == null) return false;
                foreach (var identity in AuthenticationService.Instance.PlayerInfo.Identities)
                    if (identity.TypeId == GOOGLE_PLAY_GAMES) return true;
                return false;
            }
        }

        // Two callers asking at once share the one sign-in in flight, rather than racing two.
        public Task<bool> SignInAsync()
        {
            if (_signingIn == null || _signingIn.IsCompleted) _signingIn = SignIn();
            return _signingIn;
        }

        private async Task<bool> SignIn()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
                if (AuthenticationService.Instance.IsSignedIn) return true;

                // Play Games signs in silently on launch where the player has used it before. Its code
                // is what carries the account to a new phone or a reinstall.
                string code = await _playGames.GetCodeSilentlyAsync();

                if (code != null && !AuthenticationService.Instance.SessionTokenExists)
                {
                    await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(code);
                    FlowLogger.Log($"Signed in with Google Play Games as {AuthenticationService.Instance.PlayerId}.");
                    return true;
                }

                // Anonymous sign-in reuses the session token this device kept, so the account lasts
                // until the app's data is cleared.
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                FlowLogger.Log($"Signed in as {AuthenticationService.Instance.PlayerId}.");
                if (code != null) await LinkOrSwitch(code);
                return true;
            }
            catch (Exception exception) when (exception is AuthenticationException || exception is RequestFailedException)
            {
                FlowLogger.LogWarning($"AccountService - sign-in failed, playing offline: {exception.Message}");
                return false;
            }
        }

        public async Task<bool> LinkGooglePlayGamesAsync()
        {
            if (!IsSignedIn) return false;
            if (IsLinkedToGooglePlayGames) return true;

            string code = await _playGames.GetCodeInteractivelyAsync();
            if (code == null)
            {
                FlowLogger.LogWarning("AccountService - Google Play Games gave no sign-in code; nothing was linked.");
                return false;
            }

            try
            {
                await LinkOrSwitch(code);
                return true;
            }
            catch (Exception exception) when (exception is AuthenticationException || exception is RequestFailedException)
            {
                FlowLogger.LogWarning($"AccountService - linking Google Play Games failed: {exception.Message}");
                return false;
            }
        }

        // The Google account may already own a UGS account - this player on another phone. Then that
        // account is the player's, and this device's anonymous one is left behind.
        private async Task LinkOrSwitch(string code)
        {
            if (IsLinkedToGooglePlayGames) return;
            try
            {
                await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(code);
                FlowLogger.Log("Linked this account to Google Play Games.");
            }
            catch (AuthenticationException exception) when (exception.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                AuthenticationService.Instance.SignOut(true);
                // A code is single use, so the switch asks Play Games for a fresh one.
                string fresh = await _playGames.GetCodeSilentlyAsync();
                if (fresh == null)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    return;
                }
                await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(fresh);
                FlowLogger.Log($"This Google account already had a player; switched to {AuthenticationService.Instance.PlayerId}.");
            }
        }

        public async Task<bool> SetPlayerNameAsync(string playerName)
        {
            if (!IsSignedIn || string.IsNullOrWhiteSpace(playerName)) return false;
            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName.Trim());
                return true;
            }
            catch (Exception exception) when (exception is AuthenticationException || exception is RequestFailedException)
            {
                FlowLogger.LogWarning($"AccountService - the player name was not saved: {exception.Message}");
                return false;
            }
        }
    }
}
