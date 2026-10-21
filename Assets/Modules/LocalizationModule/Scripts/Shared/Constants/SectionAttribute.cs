using UnityEngine;
namespace Modules.LocalizationModule.Shared.Constants
{
    /// <summary>
    /// A heading above a field in the inspector, like [Header], but drawn in the language the
    /// developer picked. The title is written in English and is its own key in an Editor table.
    /// </summary>
    public sealed class SectionAttribute : PropertyAttribute
    {
        public string Title { get; }
        public SectionAttribute(string title) { Title = title; }
    }
}
