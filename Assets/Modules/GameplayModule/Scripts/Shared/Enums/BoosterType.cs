namespace Modules.GameplayModule.Shared.Enums
{
    /// <summary>The boosters the player can use in a level.</summary>
    public enum BoosterType
    {
        /// <summary>Pick a door: the jellies its waiting jelly needs and that request go.</summary>
        Rocket = 0,
        /// <summary>The clock stops for CD_Boosters' Freeze Seconds.</summary>
        FreezeTime = 1,
        /// <summary>Pick a jelly: every jelly of its colour, and every door request for that colour, go.</summary>
        Ufo = 2,
    }
}
