using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.GameplayModule.ScratchScreenModule.Entities
{
    /// <summary>
    /// One cell's foil: a texture of its own that a finger rubs clear, and that clears itself once enough is gone. Only a
    /// picture - it says where it is being scratched and when it is open, and decides nothing.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class ScratchSurface : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [Tooltip("The brush's radius against the cell's smaller side.")]
        [SerializeField, Range(.05f, .5f)] private float _brush = .2f;
        [Tooltip("Share of the foil rubbed off that opens the cell whole.")]
        [SerializeField, Range(.1f, 1)] private float _openAt = .55f;

        /// <summary>Rubbed here, in screen space, with the camera that saw it.</summary>
        public Action<Vector2, Camera> Scratched;
        /// <summary>The cell opened: rubbed enough, or opened whole.</summary>
        public Action<ScratchSurface> Opened;

        public bool Interactable { get; set; }
        public bool IsOpen { get; private set; }

        private RawImage _image;
        private Texture2D _texture;
        private Color32[] _pixels;
        private int _size, _cleared;

        /// <summary>Lays a fresh foil from the pixels given, a square of the size given, side on side.</summary>
        public void Cover(Color32[] foil, int size)
        {
            if (_image == null) _image = GetComponent<RawImage>();
            if (_texture == null || _size != size)
            {
                if (_texture != null) Destroy(_texture);
                _texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                _size = size;
            }
            _pixels = (Color32[])foil.Clone();
            _texture.SetPixels32(_pixels);
            _texture.Apply();
            _image.texture = _texture;
            _image.color = Color.white;
            _image.enabled = true;
            _cleared = 0;
            IsOpen = false;
        }

        /// <summary>Takes the whole foil off at once.</summary>
        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (_image != null) _image.enabled = false;
            Opened?.Invoke(this);
        }

        public void OnPointerDown(PointerEventData eventData) => Rub(eventData);
        public void OnDrag(PointerEventData eventData) => Rub(eventData);

        private void Rub(PointerEventData eventData)
        {
            if (!Interactable || IsOpen || _pixels == null) return;
            Scratched?.Invoke(eventData.position, eventData.pressEventCamera);
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local)) return;
            var r = rect.rect;
            int cx = Mathf.RoundToInt((local.x - r.xMin) / r.width * _size);
            int cy = Mathf.RoundToInt((local.y - r.yMin) / r.height * _size);
            int radius = Mathf.Max(1, Mathf.RoundToInt(_brush * _size));
            bool changed = false;
            for (int y = Mathf.Max(0, cy - radius); y < Mathf.Min(_size, cy + radius); y++)
            for (int x = Mathf.Max(0, cx - radius); x < Mathf.Min(_size, cx + radius); x++)
            {
                int dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy > radius * radius) continue;
                int at = y * _size + x;
                if (_pixels[at].a == 0) continue;
                _pixels[at].a = 0;
                _cleared++;
                changed = true;
            }
            if (!changed) return;
            if (_cleared >= _openAt * _size * _size) { Open(); return; }
            _texture.SetPixels32(_pixels);
            _texture.Apply();
        }

        private void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
        }
    }
}
