using System;
using Modules.UiWidgetsModule.Data.UnityObjects;
using Modules.UiWidgetsModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Lays a screen out from a CD_ScreenLayout. Sits on the screen's root: the screen's View fits its
    /// content with the asset's sizes, then this puts every listed object where the asset says. Edit
    /// the asset during Play and the screen follows; what is edited stays, because the asset is saved.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenLayout : MonoBehaviour
    {
        [SerializeField] private CD_ScreenLayout _config;

        private CD_ScreenLayout _listening;
        private bool _dirty;

        public CD_ScreenLayout Config => _config;

        /// <summary>Raised when the asset is edited, so the View fits its content again.</summary>
        public event Action Changed;

        private void OnEnable()
        {
            Listen();
            _dirty = true;
        }

        private void OnDisable()
        {
            if (_listening != null) _listening.Changed -= OnConfigChanged;
            _listening = null;
        }

        // After the View's fit and the layout groups' pass of this frame, so the asset has the last word.
        private void LateUpdate()
        {
            if (_listening != _config) Listen();
            if (!_dirty) return;
            _dirty = false;
            Apply();
        }

        /// <summary>Puts every listed object where the asset says. Called by the View after it fits its content.</summary>
        public void Apply()
        {
            if (_config == null) return;
            foreach (ScreenLayoutElementCVO element in _config.Elements)
            {
                if (element == null || string.IsNullOrEmpty(element.Path)) continue;
                RectTransform target = Find(element.Path);
                if (target == null) continue;
                if (element.OverrideActive && target.gameObject.activeSelf != element.Active) target.gameObject.SetActive(element.Active);
                if (element.OverrideAnchors)
                {
                    target.anchorMin = element.AnchorMin;
                    target.anchorMax = element.AnchorMax;
                    target.pivot = element.Pivot;
                }
                if (element.OverrideSize) target.sizeDelta = element.Size;
                if (element.OverridePosition) target.anchoredPosition = element.Position;
                if (element.OverrideScale) target.localScale = Vector3.one * element.Scale;
                // The asset gives the place as authored; a bleed then grows it to the screen's edge from there.
                if (target.TryGetComponent(out ScreenBleed bleed) && bleed.isActiveAndEnabled) bleed.Recapture();
            }
        }

        /// <summary>
        /// The object at a path under the root. Where two siblings share a name, the one shown is taken,
        /// so a hidden leftover of the same name never takes the place meant for the one on screen.
        /// </summary>
        public RectTransform Find(string path)
        {
            Transform current = transform;
            foreach (string name in path.Split('/'))
            {
                Transform found = null;
                foreach (Transform child in current)
                {
                    if (child.name != name) continue;
                    if (child.gameObject.activeSelf) { found = child; break; }
                    if (found == null) found = child;
                }
                if (found == null) return null;
                current = found;
            }
            return current as RectTransform;
        }

        /// <summary>Asks for the layout to be applied again at the end of this frame.</summary>
        public void SetDirty() => _dirty = true;

        private void Listen()
        {
            if (_listening != null) _listening.Changed -= OnConfigChanged;
            _listening = _config;
            if (_listening != null) _listening.Changed += OnConfigChanged;
        }

        private void OnConfigChanged()
        {
            _dirty = true;
            Changed?.Invoke();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Writes where the listed objects stand now into the asset - every part, ticked or not. Move
        /// them by hand (in Play too), then capture, and the asset keeps the result.
        /// </summary>
        [ContextMenu("Capture Current Layout Into Config")]
        private void Capture()
        {
            if (_config == null) return;
            UnityEditor.Undo.RecordObject(_config, "Capture Screen Layout");
            foreach (ScreenLayoutElementCVO element in _config.Elements)
            {
                RectTransform target = element == null || string.IsNullOrEmpty(element.Path) ? null : Find(element.Path);
                if (target == null) continue;
                // A bled object is read as authored, not as grown to the screen's edge.
                if (target.TryGetComponent(out ScreenBleed bleed) && bleed.TryGetAuthored(out var min, out var max, out var pivot, out var position, out var size))
                {
                    element.AnchorMin = min; element.AnchorMax = max; element.Pivot = pivot; element.Position = position; element.Size = size;
                }
                else
                {
                    element.AnchorMin = target.anchorMin;
                    element.AnchorMax = target.anchorMax;
                    element.Pivot = target.pivot;
                    element.Position = target.anchoredPosition;
                    element.Size = target.sizeDelta;
                }
                element.Scale = target.localScale.x;
                element.Active = target.gameObject.activeSelf;
            }
            UnityEditor.EditorUtility.SetDirty(_config);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(_config);
        }
#endif
    }
}
