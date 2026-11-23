using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.PetHomeModule.PetHomeScreenModule.ViewsMediators
{
    /// <summary>One card in the pet home shop: the item, its price until it is the player's, then a badge.</summary>
    public class PetHomeCardView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _price;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private GameObject _owned;
        [SerializeField] private GameObject _inUse;

        public Action<string> Clicked;
        private string _id;

        private void OnEnable() => _button.onClick.AddListener(OnClicked);
        private void OnDisable() => _button.onClick.RemoveListener(OnClicked);

        public void Show(string id, Sprite sprite, int price, bool owned, bool inUse)
        {
            _id = id;
            _icon.sprite = sprite;
            _icon.preserveAspect = true;
            bool free = price <= 0;
            _price.SetActive(!owned && !free);
            _priceText.text = price.ToString("N0");
            _owned.SetActive((owned || free) && !inUse);
            _inUse.SetActive(inUse);
            _button.interactable = !inUse;
        }

        private void OnClicked() => Clicked?.Invoke(_id);
    }
}
