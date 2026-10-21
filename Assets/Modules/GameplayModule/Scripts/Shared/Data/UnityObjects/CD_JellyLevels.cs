using System.Collections.Generic;
using UnityEngine;
namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// Every level the game plays, in order: the first entry is level 1, and Next opens the entry
    /// after the one just won. Filed once, in GameplaySystemRoot's Shared Scriptables.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_JellyLevels", menuName = "Jelly Fuse/Level List")]
    public sealed class CD_JellyLevels : ScriptableObject
    {
        [Tooltip("Level to start on when Play begins, counted from 1. Use it to test a later level without reordering.")]
        [Min(1)] public int StartLevel = 1;
        [Tooltip("The colours every level can use.")]
        public CD_JellyPalette Palette;
        [Tooltip("Drag to reorder. The position in this list is the level number.")]
        public List<CD_JellyLevel> Levels = new();
        [Tooltip("How many free cells a door draws its jelly across when the door has no pull reach of its own: a jelly it waits for, let go of in its rows (or columns) that close, slides in. 0: it has to be put right in front of the door.")]
        [Range(0, 10)] public int DefaultPullReach = 1;
        [Tooltip("A jelly let go of where it fuses with nothing, that passed a spot on the way where it would have fused into a shape a door takes, goes back there and fuses - if that spot is at most this many steps back. 0: it stays where it was let go of.")]
        [Range(0, 10)] public int ReleaseFuseLookBack = 0;
        [Tooltip("Coins for winning a level that has no coin reward of its own.")]
        [Min(0)] public int DefaultCoinReward = 20;

        public int Count => Levels.Count;
        public CD_JellyLevel Get(int index) => index >= 0 && index < Levels.Count ? Levels[index] : null;
        /// <summary>A level that is filled in and passes its checks; an empty or broken entry is skipped in play.</summary>
        public bool IsPlayable(int index) => Get(index) is { } level && level.Validate().Count == 0;
        /// <summary>The first playable entry at or after <paramref name="index"/>, or -1 when there is none.</summary>
        public int PlayableFrom(int index)
        {
            for (int i = Mathf.Max(0, index); i < Levels.Count; i++) if (IsPlayable(i)) return i;
            return -1;
        }
        public bool HasNext(int index) => PlayableFrom(index + 1) >= 0;
        /// <summary>The coins winning the level at <paramref name="index"/> pays: its own reward, or the default.</summary>
        public int CoinReward(int index) => Get(index) is { OwnCoinReward: >= 0 } level ? level.OwnCoinReward : DefaultCoinReward;
        /// <summary>
        /// The level a run starts on: the one the player was on last time (SD_JellyProgress), when it
        /// is still playable, or else the first playable one from StartLevel. -1 when there is none.
        /// </summary>
        public int StartIndex(SD_JellyProgress progress)
        {
            int saved = progress != null ? progress.LevelIndex : -1;
            return saved >= 0 && IsPlayable(saved) ? saved : PlayableFrom(StartLevel - 1);
        }
        /// <summary>The last playable entry at or before <paramref name="index"/>, or -1 when there is none.</summary>
        public int PlayableBefore(int index)
        {
            for (int i = Mathf.Min(index, Levels.Count - 1); i >= 0; i--) if (IsPlayable(i)) return i;
            return -1;
        }
    }
}
