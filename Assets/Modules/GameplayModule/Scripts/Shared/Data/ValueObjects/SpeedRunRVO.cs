namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>Where a Speed Run stands for the screen: whether one is on, the level in play (from 1) of how many, and whether it is the last.</summary>
    public sealed class SpeedRunRVO
    {
        public bool Active;
        public int Step;
        public int Total;
        public bool Final => Active && Step >= Total;
    }
}
