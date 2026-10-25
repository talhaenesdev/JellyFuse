using System.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

namespace Modules.AccountModule.Services.Sub
{
    /// <summary>
    /// The phone's side of a Google Play Games sign-in: asks Play Games for the player, then for the
    /// one-time code UGS exchanges for the account. Answers null wherever there is no code to give -
    /// in the Editor, on a phone without Play Games, when the player declined, or before Play Console
    /// and the web client id are set up.
    /// </summary>
    internal sealed class GooglePlayGamesSubService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private bool _activated;
#endif

        /// <summary>The silent sign-in Play Games does on launch; shows no UI.</summary>
        public Task<string> GetCodeSilentlyAsync() => GetCodeAsync(interactive: false);

        /// <summary>A sign-in the player asked for - may show Play Games' own account picker.</summary>
        public Task<string> GetCodeInteractivelyAsync() => GetCodeAsync(interactive: true);

        private Task<string> GetCodeAsync(bool interactive)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_activated)
            {
                PlayGamesPlatform.Activate();
                _activated = true;
            }

            var answer = new TaskCompletionSource<string>();
            void OnStatus(SignInStatus status)
            {
                if (status != SignInStatus.Success)
                {
                    answer.TrySetResult(null);
                    return;
                }
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code => answer.TrySetResult(string.IsNullOrEmpty(code) ? null : code));
            }

            if (interactive) PlayGamesPlatform.Instance.ManuallyAuthenticate(OnStatus);
            else PlayGamesPlatform.Instance.Authenticate(OnStatus);
            return answer.Task;
#else
            return Task.FromResult<string>(null);
#endif
        }
    }
}
