using UnityEngine;

namespace Modules.MainModule.Models
{
    /// <summary>
    /// The player's answer to the consent question - whether ads and analytics may use personal data. Kept in
    /// PlayerPrefs, apart from the save file, so a reset of the player's progress does not ask again.
    /// </summary>
    internal sealed class ConsentModel
    {
        private const string ANSWERED_KEY = "consent.answered";
        private const string ACCEPTED_KEY = "consent.accepted";

        public bool Answered => PlayerPrefs.GetInt(ANSWERED_KEY, 0) == 1;
        public bool Accepted => PlayerPrefs.GetInt(ACCEPTED_KEY, 0) == 1;

        public void Record(bool accepted)
        {
            PlayerPrefs.SetInt(ANSWERED_KEY, 1);
            PlayerPrefs.SetInt(ACCEPTED_KEY, accepted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
