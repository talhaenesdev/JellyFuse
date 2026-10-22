namespace Modules.AudioModule.Enums
{
    /// <summary>
    /// What a sound effect means, not what it sounds like: a game asks for a cue and CD_Audio says
    /// which clips play for it. Generic on purpose, so any game can map its moments onto these.
    /// </summary>
    public enum AudioCue
    {
        Tap = 0,
        Grab = 1,
        Merge = 2,
        Deliver = 3,
        Win = 4,
        Lose = 5,
        Coin = 6,
        Rocket = 7,
        Freeze = 8,
        Ufo = 9,
        Undo = 10,
        Denied = 11,
    }
}
