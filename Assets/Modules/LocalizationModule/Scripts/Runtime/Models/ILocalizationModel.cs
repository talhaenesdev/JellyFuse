using System.Collections.Generic;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.Shared.Data.ValueObjects;

namespace Modules.LocalizationModule.Models
{
    public interface ILocalizationModel
    {
        /// <summary>The code of the language in use; empty until the module is initialized.</summary>
        string Language { get; }
        IReadOnlyList<LanguageCVO> Languages { get; }
        CD_Localization Config { get; }
        void Load(CD_Localization config);
        /// <summary>Whether the language is one the game offers.</summary>
        bool Offers(string code);
        /// <summary>Switches to the language and builds its lookup. The caller has checked it is offered.</summary>
        void SetLanguage(string code);
        /// <summary>The text for the key: in the language in use, else in the source language, else the key.</summary>
        string Get(string key);
    }
}
