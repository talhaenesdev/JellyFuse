using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The boosters, in the order the HUD's booster buttons show them, and how each one plays.
    /// Filed in GameplaySystemRoot's Shared Scriptables.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_Boosters", menuName = "Jelly Fuse/Boosters")]
    public sealed class CD_Boosters : ScriptableObject
    {
        [Tooltip("In the order of the HUD's booster buttons: the first entry is the first button.")]
        public List<BoosterCVO> Boosters = new()
        {
            new() { Type = BoosterType.Rocket, UnlockLevel = 1, FreeUses = 3, CoinPrice = 100 },
            new() { Type = BoosterType.FreezeTime, UnlockLevel = 1, FreeUses = 3, CoinPrice = 80 },
            new() { Type = BoosterType.Ufo, UnlockLevel = 1, FreeUses = 3, CoinPrice = 150 },
        };

        [Header("Rocket")]
        [Tooltip("On: the jellies taken for the door's shape are chosen biggest first (pieces of many cells, then single ones). Off: smallest first.")]
        public bool RocketPreferBigPieces = true;
        [Tooltip("On: the door's request goes even when the board does not hold enough jellies of its colour to make its shape; whatever there is goes with it. Off: the rocket is not used then.")]
        public bool RocketWithoutEnoughJellies = true;

        [Header("Electric combo")]
        [Tooltip("Correct fuses or doors in a row, each quick after the last, that make a door finish itself with an electric charge. 0 turns it off.")]
        [Min(0)] public int ComboStrikeAt = 3;
        [Tooltip("Seconds the chosen jellies crackle and rise before they fuse and fly into their door.")]
        [Min(.2f)] public float ComboStrikeSeconds = 1.1f;

        [Header("Freeze Time")]
        [Tooltip("Seconds the clock stands still.")]
        [Min(1)] public float FreezeSeconds = 10;

        public BoosterCVO Get(BoosterType type) => Boosters.Find(b => b != null && b.Type == type);
    }
}
