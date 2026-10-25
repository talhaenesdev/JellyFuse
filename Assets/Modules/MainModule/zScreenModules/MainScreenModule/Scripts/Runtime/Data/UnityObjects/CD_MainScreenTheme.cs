using UnityEngine;

namespace Modules.MainModule.MainScreenModule.Data.UnityObjects
{
    [CreateAssetMenu(fileName = "CD_MainScreenTheme", menuName = "Main/Main Screen Theme")]
    public sealed class CD_MainScreenTheme : ScriptableObject
    {
        [Header("Primary Actions")]
        public Color PlayButton = Color.white;
        public Color ShopBuyButton = new(0.34f, 0.82f, 0.18f, 1f);
        public Color ButtonText = Color.white;

        [Header("Utility Buttons")]
        public Color UtilityButton = Color.white;

        [Header("Navigation")]
        public Color NavigationNormal = Color.white;
        public Color NavigationSelected = Color.white;
        public Color NavigationTextNormal = new(0.24f, 0.12f, 0.52f, 1f);
        public Color NavigationTextSelected = Color.white;
        public Color NavigationSelector = Color.white;
    }
}
