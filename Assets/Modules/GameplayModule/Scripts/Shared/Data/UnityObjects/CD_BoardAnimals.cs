using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// Which profile icon brings which animal to the board, and where its frames are published. An icon not listed
    /// - or one whose animal cannot be fetched - keeps the board look's own cat, which ships in the build. Filed once,
    /// in GameplaySystemRoot's Shared Scriptables.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_BoardAnimals", menuName = "Jelly Fuse/Board Animals")]
    public sealed class CD_BoardAnimals : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("The profile icon's id, as CD_ProfileIcons lists it.")]
            public string IconId;
            [Tooltip("The addressable address of its CD_BoardAnimal.")]
            public string Address;
        }

        public List<Entry> Animals = new();
        [Min(1)] public float LoadTimeoutSeconds = 12f;

        /// <summary>The address of the icon's animal, or null when the icon has none.</summary>
        public string AddressOf(string iconId)
        {
            if (string.IsNullOrEmpty(iconId)) return null;
            foreach (var entry in Animals)
                if (entry != null && entry.IconId == iconId && !string.IsNullOrEmpty(entry.Address)) return entry.Address;
            return null;
        }
    }
}
