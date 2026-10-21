using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.GameplayModule.Models
{
    public sealed class JellyTutorialModel : IJellyTutorialModel
    {
        private readonly List<JellyTutorialStepCVO> _steps = new();
        private int _step;
        private List<Vector2Int> _route = new();

        public bool Active => _step < _steps.Count;
        public JellyTutorialStepCVO Current => Active ? _steps[_step] : null;
        public IReadOnlyList<Vector2Int> Route => _route;
        public int PieceId { get; private set; } = -1;
        public Vector2Int StartCorner { get; private set; }
        public int StartSize { get; private set; }
        public Vector2Int GrabOffset { get; set; }

        public void Begin(IEnumerable<JellyTutorialStepCVO> steps)
        {
            _steps.Clear();
            // A step still being authored - no cell to lead the jelly to - is left out.
            foreach (var step in steps) if (step != null && step.Path.Count >= 2) _steps.Add(step);
            _step = 0;
            Refresh();
        }
        public void Lead(int pieceId, Vector2Int corner, int size) { PieceId = pieceId; StartCorner = corner; StartSize = size; }
        public void Next() { _step++; Refresh(); }
        public void End() { _step = _steps.Count; Refresh(); }
        private void Refresh()
        {
            PieceId = -1;
            _route = Active ? _steps[_step].Route() : new List<Vector2Int>();
        }
    }
}
