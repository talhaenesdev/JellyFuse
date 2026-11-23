using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// Picks a level the player has won for an event to play: any above the duel floor for Star Rush, and for the Color
    /// Carnival the one, of a few drawn, holding the most cells of the day's colour. The level just played is passed over.
    /// </summary>
    public static class EventLevelFunction
    {
        private const int CARNIVAL_DRAWS = 24;

        public static int Pick(EventPlayKind kind, CD_JellyLevels levels, SD_JellyProgress progress, CD_Events events, int last)
        {
            if (levels == null || progress == null || events == null) return -1;
            int won = levels.StartIndex(progress);
            if (won <= events.DuelLevelFloor) return -1;
            if (kind != EventPlayKind.ColorCarnival) return Draw(levels, won, events.DuelLevelFloor, last);
            var colour = (JellyColour)progress.CarnivalColour;
            int best = -1, bestCells = -1;
            for (int i = 0; i < CARNIVAL_DRAWS; i++)
            {
                int index = Draw(levels, won, events.DuelLevelFloor, last);
                if (index < 0) continue;
                int cells = 0;
                foreach (var piece in levels.Get(index).CreateLevel(levels.Palette).Pieces)
                    if (piece.Colour == colour) cells += piece.Cells.Count;
                if (cells > bestCells) { best = index; bestCells = cells; }
            }
            return best;
        }

        private static int Draw(CD_JellyLevels levels, int won, int floor, int last)
        {
            int index = levels.RandomPlayableBelow(won, floor);
            for (int retry = 0; retry < 4 && index >= 0 && index == last; retry++) index = levels.RandomPlayableBelow(won, floor);
            return index;
        }
    }
}
