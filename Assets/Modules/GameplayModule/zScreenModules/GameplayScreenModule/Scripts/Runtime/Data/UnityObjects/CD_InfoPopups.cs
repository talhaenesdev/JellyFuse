using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.ValueObjects;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects
{
    /// <summary>The pages the info panel can show, each found by its Key.</summary>
    [CreateAssetMenu(fileName = "CD_InfoPopups", menuName = "Jelly Fuse/Info Popups")]
    public sealed class CD_InfoPopups : ScriptableObject
    {
        [Tooltip("The localization table the pages' texts are written to and read from in the editor. The game reads them by key from every table.")]
        public CD_LocalizationTable Table;
        public List<InfoPopupCVO> Pages = new();

        public InfoPopupCVO Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var page in Pages) if (page != null && page.Key == key) return page;
            return null;
        }
    }
}
