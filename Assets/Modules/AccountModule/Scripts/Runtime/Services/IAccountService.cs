using System.Threading.Tasks;

namespace Modules.AccountModule.Services
{
    /// <summary>
    /// The player's Unity Gaming Services account. The first sign-in makes an anonymous account and
    /// later launches sign back into the same one, so a player never sees a form. Every UGS feature -
    /// leaderboards, friends, chat, cloud save - knows the player by <see cref="PlayerId"/>.
    /// </summary>
    public partial interface IAccountService
    {
        /// <summary>Whether a player is signed in right now. False offline until a sign-in succeeds.</summary>
        bool IsSignedIn { get; }

        /// <summary>The signed-in player's id, or empty before a sign-in.</summary>
        string PlayerId { get; }

        /// <summary>The name other players see, as UGS keeps it (with its #1234 suffix), or empty.</summary>
        string PlayerName { get; }

        /// <summary>
        /// Starts the services and signs in: into the account this device used last, or a new
        /// anonymous one. True when a player is signed in afterwards. Never throws - offline is an
        /// ordinary answer here, logged as a warning and returned as false.
        /// </summary>
        Task<bool> SignInAsync();

        /// <summary>Whether this account can be signed into from Google Play Games on another phone.</summary>
        bool IsLinkedToGooglePlayGames { get; }

        /// <summary>
        /// Links the signed-in account to the player's Google Play Games account, asking Play Games
        /// to sign in if it has not. When that Google account already has a player, this device
        /// switches to it. True when the account is linked afterwards. Never throws.
        /// </summary>
        Task<bool> LinkGooglePlayGamesAsync();

        /// <summary>Sets the name other players see. False when not signed in or the call failed.</summary>
        Task<bool> SetPlayerNameAsync(string playerName);

        /// <summary>The steps this service ships for other modules to bind.</summary>
        public static partial class Commands
        {
        }
    }
}
