namespace Modules.DevToolsModule.Constants
{
    /// <summary>The developer switches (CD_DevTools) the dev tools module itself owns.</summary>
    public static class DevToolKeys
    {
        /// <summary>On: a frames-per-second readout stays in a corner of the screen.</summary>
        public const string SHOW_FPS = "dev.show_fps";
        /// <summary>On: the game runs as fast as the device goes, instead of CD_FrameRate's target.</summary>
        public const string UNCAPPED_FPS = "dev.uncapped_fps";
    }
}
