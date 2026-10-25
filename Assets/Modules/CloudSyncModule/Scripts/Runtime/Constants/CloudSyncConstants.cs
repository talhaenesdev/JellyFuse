namespace Modules.CloudSyncModule.Constants
{
    internal static class CloudSyncConstants
    {
        /// <summary>The boot's loading step for the restore, as CD_LoadingSets lists it.</summary>
        public const string CLOUD_STEP = "Cloud";

        /// <summary>The saved assets kept in the cloud, by the name they are filed under - which is also their cloud key.</summary>
        public const string PROGRESS = "SD_JellyProgress";
        public const string PROFILE = "SD_PlayerProfile";

        /// <summary>
        /// How long the boot waits for the sign-in before playing on without the cloud. Long enough
        /// for a first anonymous sign-in on mobile data, short enough that an offline launch is not
        /// held back noticeably.
        /// </summary>
        public const float SIGN_IN_WAIT_SECONDS = 6f;
    }
}
