using System;
using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// A level wider than the screen: everything the board draws sits on one content layer, slid left and right.
    /// A level that sets ViewColumns shows that many columns across; whatever is left over is reached by panning.
    /// A new level starts at its left end. While a jelly is held near a side of the board, the map slides that way
    /// by itself, and AutoPanned asks the screen to work the drag out again from the finger's unchanged point.
    /// </summary>
    public partial class JellySpriteBoardView
    {
        /// <summary>How much of the board, from each side, pulls the map along while a jelly is held there.</summary>
        private const float AUTO_PAN_EDGE = .18f;
        /// <summary>Cells a second the map slides at, with the finger right at the side.</summary>
        private const float AUTO_PAN_CELLS = 9f;

        private RectTransform _content;
        private float _pan, _panMax;
        private object _pannedLevel;
        private Vector2 _followPoint;

        /// <summary>The finger held a jelly near a side and the map moved under it.</summary>
        public event Action AutoPanned;

        /// <summary>The map is wider than the board and can be slid.</summary>
        public bool CanPan => _panMax > .5f;

        /// <summary>Slides the map by this many board units, as far as its ends allow.</summary>
        public void Pan(float deltaX) => SetPan(_pan + deltaX);

        private RectTransform Content()
        {
            if (_content != null) return _content;
            _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _content.gameObject.layer = gameObject.layer;
            _content.SetParent(transform, false);
            _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(.5f, .5f);
            _content.sizeDelta = Vector2.zero;
            return _content;
        }

        /// <summary>A point on the board's rect, where it lies on the slid map.</summary>
        private Vector2 Unpanned(Vector2 localPoint) => localPoint - new Vector2(_pan, 0);

        private void SetPan(float pan)
        {
            _pan = Mathf.Clamp(pan, -_panMax, _panMax);
            if (_content != null) _content.anchoredPosition = new Vector2(_pan, 0);
        }

        /// <summary>
        /// The cell size for a level that says how many columns it shows, or <paramref name="fitted"/> when it shows
        /// them all; and how far the map can then slide. A new level starts with its left end in view.
        /// </summary>
        private float FitPan(float fitted, float spanX, float spanY, Vector2 size)
        {
            int columns = _level.ViewColumns;
            float cell = columns > 0 && columns < spanX ? Mathf.Min(size.x / columns, size.y / (spanY * Look.SpriteTilt)) : fitted;
            _panMax = Mathf.Max(0, (spanX * cell - size.x) * .5f);
            if (!ReferenceEquals(_pannedLevel, _level)) { _pannedLevel = _level; SetPan(_panMax); }
            else SetPan(_pan);
            return cell;
        }

        /// <summary>Called each frame: a held jelly near a side of the board pulls the map that way.</summary>
        private void StepAutoPan(float dt)
        {
            if (!_following || !CanPan || _rect == null) return;
            var rect = _rect.rect;
            float zone = rect.width * AUTO_PAN_EDGE;
            float x = _followPoint.x, push = 0;
            if (x > rect.xMax - zone) push = (x - (rect.xMax - zone)) / zone;
            else if (x < rect.xMin + zone) push = (x - (rect.xMin + zone)) / zone;
            if (push == 0) return;
            float before = _pan;
            SetPan(_pan - Mathf.Clamp(push, -1, 1) * AUTO_PAN_CELLS * _cell * dt);
            if (!Mathf.Approximately(before, _pan)) AutoPanned?.Invoke();
        }
    }
}
