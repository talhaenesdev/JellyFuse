using System.Collections.Generic;
using Modules.LocalizationModule.Shared.Data.ValueObjects;

namespace Modules.LocalizationModule.Services
{
    /// <summary>
    /// The module's one counterpart. A screen's Mediator injects it to fill its texts, and redraws
    /// them when LocalizationSignals.Outgoing.LanguageChanged arrives through its Connector.
    /// </summary>
    public partial interface ILocalizationService
    {
        /// <summary>The code of the language in use: en, tr.</summary>
        string Language { get; }

        /// <summary>The languages the game offers, in the order CD_Localization lists them.</summary>
        IReadOnlyList<LanguageCVO> Languages { get; }

        /// <summary>The text for the key in the language in use; the source language's when it has none; the key itself when neither has.</summary>
        string Get(string key);

        /// <summary>Get, with {0}, {1}… filled in from the values.</summary>
        string Format(string key, params object[] values);

        /// <summary>Switches to the language with this code and keeps the choice. A code the game does not offer is ignored with a warning.</summary>
        void SetLanguage(string code);

        /// <summary>
        /// The steps a game binds in a sequence of its own, one file each beside this one:
        /// <c>ILocalizationService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}
