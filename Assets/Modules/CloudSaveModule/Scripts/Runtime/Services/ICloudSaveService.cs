using System.Collections.Generic;
using System.Threading.Tasks;

namespace Modules.CloudSaveModule.Services
{
    /// <summary>
    /// The signed-in player's records on Unity Gaming Services Cloud Save: string values under string
    /// keys, kept on the player's account rather than the device. It knows no game data - a caller
    /// hands it the strings it wants kept - and it signs nobody in: with no player signed in every
    /// call answers as a failure.
    /// </summary>
    public partial interface ICloudSaveService
    {
        /// <summary>
        /// Writes every pair, replacing what the keys held. True when the cloud took them. Never
        /// throws - offline or signed out is answered false and logged as a warning.
        /// </summary>
        Task<bool> SaveAsync(IReadOnlyDictionary<string, string> records);

        /// <summary>
        /// Reads the keys asked for. A key the player never saved is absent from the answer, so a new
        /// player gets an empty dictionary. Null when the cloud could not be asked - offline or
        /// signed out - which is not the same as "nothing saved".
        /// </summary>
        Task<IReadOnlyDictionary<string, string>> LoadAsync(IReadOnlyCollection<string> keys);

        /// <summary>The steps this service ships for other modules to bind.</summary>
        public static partial class Commands
        {
        }
    }
}
