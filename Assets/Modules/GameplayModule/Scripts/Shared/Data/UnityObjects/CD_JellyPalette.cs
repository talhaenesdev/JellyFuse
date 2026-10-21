using System.Collections.Generic;
using System.Linq;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The jelly colours every level can use, with their names and tints. The level editor offers
    /// them in this order, and the game draws them with these tints. A level's Extra colours come
    /// after them. Referenced by CD_JellyLevels.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_JellyPalette", menuName = "Jelly Fuse/Palette")]
    public sealed class CD_JellyPalette : ScriptableObject
    {
        public List<JellyPaletteCVO> Colours = new();

        public JellyPaletteCVO Find(JellyColour colour)
        {
            // A plain loop: the board asks this while it draws, and a lambda would leave garbage.
            for (int i = 0; i < Colours.Count; i++) if (Colours[i].Colour == colour) return Colours[i];
            return null;
        }

        /// <summary>The palette in the shape a level snapshot keeps its colours in.</summary>
        public IEnumerable<JellyColourCVO> AsLevelColours() =>
            Colours.Select(c => new JellyColourCVO { Id = (int)c.Colour, Name = c.Name, Tint = c.Tint });
    }
}
