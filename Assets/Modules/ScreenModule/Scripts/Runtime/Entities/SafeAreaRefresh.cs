using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Modules.ScreenModule.Entities
{
    /// <summary>
    /// Keeps the screen manager's safe-area layers on the safe area for as long as the game runs.
    /// FlowIoC's ScreenSafeArea fits them once, in Awake, from that moment's Screen size and safe
    /// area. In the Editor the first frame reports the Simulator or Game window's own size against
    /// the device's safe area, so the layers come out larger than the screen and every screen shifts
    /// up and to the right. This fits them again whenever the size or the safe area changes, clamped
    /// to the screen. On a device the first frame is already right and this changes nothing.
    /// Sits beside ScreenSafeArea on the ScreenManager.
    /// </summary>
    public sealed class SafeAreaRefresh : MonoBehaviour
    {
        private readonly List<RectTransform> _layers = new();
        private Vector2Int _size;
        private Rect _safeArea;
        private Rect[] _cutouts = System.Array.Empty<Rect>();

        private void Awake()
        {
            // FlowIoC's ScreenLayer is internal: the layers that follow the safe area are found by its public flag.
            foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour.GetType().Name != "ScreenLayer") continue;
                PropertyInfo flag = behaviour.GetType().GetProperty("IsSafeAreaExists", BindingFlags.Instance | BindingFlags.Public);
                if (flag != null && (bool)flag.GetValue(behaviour)) _layers.Add((RectTransform)behaviour.transform);
            }
        }

        private void LateUpdate()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            Rect safeArea = Screen.safeArea;
            Rect[] cutouts = Screen.cutouts;
            if (size == _size && safeArea == _safeArea && SameCutouts(cutouts)) return;
            _size = size;
            _safeArea = safeArea;
            _cutouts = cutouts ?? System.Array.Empty<Rect>();
            Fit();
        }

        private void Fit()
        {
            if (_size.x <= 0 || _size.y <= 0) return;
            Vector2 min = Vector2.Max(_safeArea.min, Vector2.zero), max = Vector2.Min(_safeArea.max, _size);
            ApplyCutouts(ref min, ref max);
            if (max.x <= min.x || max.y <= min.y) { min = Vector2.zero; max = _size; }
            var anchorMin = new Vector2(min.x / _size.x, min.y / _size.y);
            var anchorMax = new Vector2(max.x / _size.x, max.y / _size.y);
            foreach (RectTransform layer in _layers)
            {
                if (layer == null) continue;
                layer.anchorMin = anchorMin;
                layer.anchorMax = anchorMax;
            }
        }

        private bool SameCutouts(Rect[] cutouts)
        {
            cutouts ??= System.Array.Empty<Rect>();
            if (_cutouts.Length != cutouts.Length) return false;
            for (int i = 0; i < cutouts.Length; i++)
                if (_cutouts[i] != cutouts[i]) return false;
            return true;
        }

        private void ApplyCutouts(ref Vector2 min, ref Vector2 max)
        {
            foreach (Rect cutout in _cutouts)
            {
                if (cutout.width <= 0 || cutout.height <= 0) continue;

                float left = cutout.xMin;
                float right = _size.x - cutout.xMax;
                float bottom = cutout.yMin;
                float top = _size.y - cutout.yMax;
                float nearest = Mathf.Min(left, right, bottom, top);

                if (nearest == top) max.y = Mathf.Min(max.y, cutout.yMin);
                else if (nearest == bottom) min.y = Mathf.Max(min.y, cutout.yMax);
                else if (nearest == left) min.x = Mathf.Max(min.x, cutout.xMax);
                else max.x = Mathf.Min(max.x, cutout.xMin);
            }
        }
    }
}
