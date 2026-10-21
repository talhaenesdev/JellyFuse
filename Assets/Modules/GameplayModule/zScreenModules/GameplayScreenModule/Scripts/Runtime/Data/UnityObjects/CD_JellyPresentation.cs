using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects
{
    /// <summary>
    /// The gameplay screen's settings, one asset per concern: how the jellies move (Feel), how the board
    /// looks (Look, which also holds the wall skin and the palette) and the screen around it (Hud).
    /// The screen holds this one asset; swapping one of the three swaps that concern only.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_JellyPresentation", menuName = "Jelly Fuse/Presentation")]
    public sealed class CD_JellyPresentation : ScriptableObject
    {
        public CD_JellyFeel Feel;
        public CD_JellyBoardLook Look;
        public CD_GameplayHud Hud;
    }
}
