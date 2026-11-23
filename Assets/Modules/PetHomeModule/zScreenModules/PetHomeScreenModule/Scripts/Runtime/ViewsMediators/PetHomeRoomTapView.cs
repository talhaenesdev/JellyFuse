using UnityEngine;
using UnityEngine.EventSystems;

namespace Modules.PetHomeModule.PetHomeScreenModule.ViewsMediators
{
    /// <summary>The room's empty floor and walls: a tap that was not the end of a slide is passed to the screen.</summary>
    public class PetHomeRoomTapView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private PetHomeScreenView _screen;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging) _screen.RoomTapped(eventData.position, eventData.pressEventCamera);
        }
    }
}
