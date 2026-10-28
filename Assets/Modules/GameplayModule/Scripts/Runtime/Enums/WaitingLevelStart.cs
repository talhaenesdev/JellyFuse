namespace Modules.GameplayModule.Enums
{
    /// <summary>A level start held back for want of a life, to run once one is bought.</summary>
    public enum WaitingLevelStart
    {
        None = 0,
        /// <summary>The level played again: Start.</summary>
        Replay = 1,
        /// <summary>The level after it: NextLevel.</summary>
        Next = 2,
    }
}
