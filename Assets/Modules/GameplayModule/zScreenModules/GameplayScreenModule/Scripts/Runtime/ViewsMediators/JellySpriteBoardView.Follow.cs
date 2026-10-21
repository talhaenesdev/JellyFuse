using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The held jelly trails the finger between cells. Which way it may lead is the model's call
    /// (the snapshot's free sides); this only turns the finger's position into a lead and a pull.
    /// </summary>
    public sealed partial class JellySpriteBoardView
    {
        private bool _following;
        private Vector2 _grabPoint, _grabCentre, _dragDelta;

        /// <summary>The finger went down on a jelly: remember where, so the jelly can trail it between cells.</summary>
        public void BeginFollow(Vector2 localPoint, Vector2 rectSize)
        {
            _following = Pick(localPoint, rectSize, out _, out int id);
            if (!_following) return;
            _grabPoint = BoardPoint(localPoint, rectSize);
            _grabCentre = CentreOf(PieceById(_snapshot, id).Cells);
            _dragDelta = Vector2.zero;
        }
        public void Follow(Vector2 localPoint, Vector2 rectSize)
        {
            if (!_following) return;
            _dragDelta = BoardPoint(localPoint, rectSize) - _grabPoint;
            UpdateFollow();
        }
        /// <summary>
        /// The finger let go. A fast drag leaves the jelly drawn behind its cell, still walking the cells the finger
        /// passed; it drops that walk and slides straight to where it now is, instead of retracing the finger's path.
        /// </summary>
        public void EndFollow()
        {
            _following = false;
            foreach (var blob in _blobs.Values) blob.Route.Clear();
            UpdateFollow();
        }

        /// <summary>
        /// Leads the held jelly ahead of its cell toward the finger, along one axis and by at most
        /// DragFollow of a cell, so the grid move in between reads as one smooth slide.
        /// </summary>
        private void UpdateFollow()
        {
            _dirty = true;
            foreach (var blob in _blobs.Values) blob.Follow = blob.Pull = Vector2.zero;
            if (!_following || _snapshot == null || Feel.GridDrag || !_blobs.TryGetValue(_held, out var held)) return;
            var piece = PieceById(_snapshot, _held);
            if (piece == null) return;
            var lead = _grabCentre + _dragDelta - CentreOf(piece.Cells);
            lead = Mathf.Abs(lead.x) >= Mathf.Abs(lead.y) ? new Vector2(lead.x, 0) : new Vector2(0, lead.y);
            if (lead.sqrMagnitude < .0001f) return;
            // Only lead the way the model says the piece can step: a wall, a hole, the board's edge
            // or another jelly keeps it in its own cell.
            if (!_snapshot.CanHeldStep(new Vector2Int(System.Math.Sign(lead.x), System.Math.Sign(lead.y)))) return;
            held.Follow = Vector2.ClampMagnitude(lead, Feel.DragFollow) * _cell;
            // What the finger asks beyond that pulls on the jelly, and it stretches toward the finger.
            held.Pull = lead.normalized * Mathf.Clamp01(lead.magnitude - Feel.DragFollow);
        }
    }
}
