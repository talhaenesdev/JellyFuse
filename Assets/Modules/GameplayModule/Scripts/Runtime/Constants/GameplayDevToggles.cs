namespace Modules.GameplayModule.Constants
{
    /// <summary>The developer switches (CD_DevTools) the gameplay module reads.</summary>
    internal static class GameplayDevToggles
    {
        /// <summary>On: a held jelly finds its way round what is in the way. Off: it heads straight for the finger and stops against what blocks it.</summary>
        public const string PATH_FINDING = "gameplay.path_finding";
        /// <summary>
        /// On: a door's jelly is built on the biggest jelly on the board that fits it - small ones may not fuse
        /// into it while a bigger fitting one is there, so they are left for the later doors. Off: any fuse that
        /// fits a waiting door is allowed.
        /// </summary>
        public const string BIGGEST_FUSE_FIRST = "gameplay.biggest_fuse_first";
    }
}
