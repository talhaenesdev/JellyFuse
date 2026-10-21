using System.Collections.Generic;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Where a tutorial step's jelly is, where a drag may take it, and what the screen shows of it.</summary>
    internal static class JellyTutorialFunction
    {
        /// <summary>Finds the jelly the step leads: the one on its first cell. A step with no jelly there is skipped.</summary>
        public static void FindJelly(IJellyTutorialModel tutorial, PuzzleSnapshotRVO board)
        {
            while (tutorial.Active)
            {
                var piece = PieceAt(board, tutorial.Route[0]);
                if (piece != null) { tutorial.Lead(piece.Id, Corner(piece.Cells), piece.Cells.Count); return; }
                tutorial.Next();
            }
        }
        /// <summary>The step's jelly as it lies now, and where the route's cell has got to with it.</summary>
        public static JellyPieceVO Jelly(IJellyTutorialModel tutorial, PuzzleSnapshotRVO board, out Vector2Int at)
        {
            at = default;
            if (!tutorial.Active) return null;
            JellyPieceVO piece = null;
            foreach (var p in board.Pieces) if (p.Id == tutorial.PieceId) { piece = p; break; }
            if (piece == null) return null;
            at = tutorial.Route[0] + Corner(piece.Cells) - tutorial.StartCorner;
            return piece;
        }
        /// <summary>Done: the jelly fused, went out through a door, or reached the end of the route.</summary>
        public static bool StepDone(IJellyTutorialModel tutorial, PuzzleSnapshotRVO board)
        {
            var piece = Jelly(tutorial, board, out var at);
            return piece == null || piece.Cells.Count != tutorial.StartSize || at == tutorial.Route[tutorial.Route.Count - 1];
        }
        /// <summary>The finger's cell moved to the nearest one that keeps the jelly on its route.</summary>
        public static Vector2Int Constrain(IJellyTutorialModel tutorial, Vector2Int target)
        {
            var wanted = target - tutorial.GrabOffset;
            var best = tutorial.Route[0];
            float bestDistance = float.MaxValue;
            // Ties go to the cell further on, so a finger between two leads the jelly forward.
            foreach (var cell in tutorial.Route)
            {
                float distance = (cell - wanted).sqrMagnitude;
                if (distance <= bestDistance) { bestDistance = distance; best = cell; }
            }
            return best + tutorial.GrabOffset;
        }
        /// <summary>
        /// The jelly and every cell it covers on the rest of its way are lit, with any jelly it would touch
        /// at the end of it - the one it fuses with. The hand goes from where the jelly is to the end.
        /// </summary>
        public static JellyTutorialRVO Show(IJellyTutorialModel tutorial, PuzzleSnapshotRVO board)
        {
            var piece = Jelly(tutorial, board, out var at);
            if (piece == null) return JellyTutorialRVO.None;
            var route = tutorial.Route;
            int from = 0;
            for (int i = 0; i < route.Count; i++) if (route[i] == at) { from = i; break; }
            var lit = new HashSet<Vector2Int>();
            // The hand rides on the middle of the jelly, not on the cell the route was drawn from.
            var middle = Vector2.zero;
            foreach (var cell in piece.Cells) middle += cell;
            middle /= piece.Cells.Count;
            var hand = new List<Vector2>();
            for (int i = from; i < route.Count; i++)
            {
                hand.Add(middle + (route[i] - at));
                foreach (var cell in piece.Cells) lit.Add(cell + route[i] - at);
            }
            var end = new HashSet<Vector2Int>();
            foreach (var cell in piece.Cells) end.Add(cell + route[route.Count - 1] - at);
            foreach (var other in board.Pieces)
            {
                if (other.Id == piece.Id) continue;
                bool touches = false;
                foreach (var cell in other.Cells)
                    if (end.Contains(cell + Vector2Int.left) || end.Contains(cell + Vector2Int.right) || end.Contains(cell + Vector2Int.up) || end.Contains(cell + Vector2Int.down)) { touches = true; break; }
                if (touches) foreach (var cell in other.Cells) lit.Add(cell);
            }
            // The spot is marked only on the board; a door shows on its own where the jelly goes.
            var target = new List<Vector2Int>(end);
            foreach (var cell in end)
                if (cell.x < 1 || cell.y < 1 || cell.x > board.Level.Width - 2 || cell.y > board.Level.Height - 2) { target.Clear(); break; }
            var step = tutorial.Current;
            return new JellyTutorialRVO(new List<Vector2Int>(lit), hand, target, piece.Colour, step?.TextKey, step?.Text);
        }
        private static JellyPieceVO PieceAt(PuzzleSnapshotRVO board, Vector2Int cell)
        {
            foreach (var piece in board.Pieces) foreach (var c in piece.Cells) if (c == cell) return piece;
            return null;
        }
        private static Vector2Int Corner(IReadOnlyList<Vector2Int> cells)
        {
            var corner = cells[0];
            foreach (var c in cells) corner = Vector2Int.Min(corner, c);
            return corner;
        }
    }
}
