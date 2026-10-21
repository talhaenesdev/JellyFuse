using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.GameplayModule.Models
{
    /// <summary>The tutorial of the level being played: its steps, the one the player is on, and the jelly that step leads.</summary>
    public interface IJellyTutorialModel
    {
        /// <summary>A step is waiting to be done: the board is dark round its jelly and only that jelly moves, along its route.</summary>
        bool Active { get; }
        /// <summary>The step being done, or null.</summary>
        JellyTutorialStepCVO Current { get; }
        /// <summary>Its cells one grid step apart (JellyTutorialStepCVO.Route).</summary>
        IReadOnlyList<Vector2Int> Route { get; }
        /// <summary>The jelly the step leads, and how it lay when the step began: its lowest-leftmost corner and its size.</summary>
        int PieceId { get; }
        Vector2Int StartCorner { get; }
        int StartSize { get; }
        /// <summary>Where the cell taken hold of sits from the route's cell, for the drag going on.</summary>
        Vector2Int GrabOffset { get; set; }
        /// <summary>Starts a level's tutorial at its first step; none leaves it inactive.</summary>
        void Begin(IEnumerable<JellyTutorialStepCVO> steps);
        /// <summary>The step's jelly, as it lies now.</summary>
        void Lead(int pieceId, Vector2Int corner, int size);
        /// <summary>On to the next step; past the last, the tutorial is over.</summary>
        void Next();
        void End();
    }
}
