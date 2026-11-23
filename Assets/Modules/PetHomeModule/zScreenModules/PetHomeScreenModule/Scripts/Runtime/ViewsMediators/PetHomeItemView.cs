using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.PetHomeModule.PetHomeScreenModule.ViewsMediators
{
    /// <summary>One item standing on the room's floor. It passes taps and drags to the screen and draws itself where told.</summary>
    [RequireComponent(typeof(Image))]
    public class PetHomeItemView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _image;

        private PetHomeScreenView _screen;
        private float _height;
        private bool _dragging;

        public string Id { get; private set; }
        /// <summary>Where it is drawn now, on the floor from 0,0 front left to 1,1 back right.</summary>
        public Vector2 Floor { get; set; }
        /// <summary>Where it last stood for good - a drop off the floor goes back there.</summary>
        public Vector2 Committed { get; set; }
        public bool Valid { get; set; } = true;

        public void Begin(PetHomeScreenView screen, string id)
        {
            _screen = screen;
            Id = id;
            if (_image == null) _image = GetComponent<Image>();
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, 0);
        }

        public void Show(Sprite sprite, float height)
        {
            _image.sprite = sprite;
            _image.preserveAspect = true;
            _height = height;
            if (!_dragging) Committed = Floor;
        }

        /// <summary>Drawn as tall as its share of the floor's height, at the scale its depth gives.</summary>
        public void Size(float floorHeight)
        {
            float h = floorHeight * _height;
            var sprite = _image.sprite;
            float aspect = sprite != null && sprite.rect.height > 0 ? sprite.rect.width / sprite.rect.height : 1;
            ((RectTransform)transform).sizeDelta = new Vector2(h * aspect, h);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging) _screen.ItemTapped(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = _screen.CanDrag;
            if (_dragging) Committed = Floor;
            else PassToParent(eventData, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragging) _screen.ItemDragged(this, eventData.position, eventData.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            _screen.ItemDropped(this);
        }

        private void PassToParent<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
        {
            if (transform.parent != null) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, handler);
        }
    }
}
