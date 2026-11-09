using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The board's colours: walls, doors, floor and grid. Filed in GameplaySystemRoot's Shared Scriptables and
    /// published to the content server as the addressable "BoardColours" (Remote_Levels group), so a change goes
    /// out with the catalog and no new build. At boot the published copy's colours replace the built-in ones; the
    /// gameplay screen paints the board with whatever is filed.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_BoardColours", menuName = "Jelly Fuse/Board Colours")]
    public sealed class CD_BoardColours : ScriptableObject
    {
        [Tooltip("The walls - the frame round the board, the walls on it and the frame round each door.")]
        public Color Wall = new Color(1f, .788f, .29f);
        [Tooltip("The doors and their gate leaves.")]
        public Color Door = new Color(.18f, .608f, 1f);
        [Tooltip("The floor tile.")]
        public Color Tile = new Color(.369f, .561f, .702f);
        [Tooltip("The second floor tile of the checkerboard.")]
        public Color TileAlt = new Color(.31f, .498f, .639f);
        [Tooltip("The grid corner pieces between the floor tiles.")]
        public Color GridCorner = new Color(.498f, .706f, .839f);

        /// <summary>Takes the colours of another copy - the one published to the content server.</summary>
        public void CopyFrom(CD_BoardColours other)
        {
            Wall = other.Wall; Door = other.Door; Tile = other.Tile; TileAlt = other.TileAlt; GridCorner = other.GridCorner;
        }
    }
}
