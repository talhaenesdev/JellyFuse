using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Leaderboard
{
    /// <summary>
    /// Keeps names that every player sees clean. A name is folded before it is checked - lower case, Turkish letters
    /// to plain ones, look-alike digits and symbols to letters, repeats squeezed - so "Ş1k" and "s.i.k" read alike.
    /// Long words are refused wherever they appear; short ones only as a whole word, so "Kamil" or "Sikke" pass.
    /// Kept short on purpose: a list catches the obvious, not everything.
    /// </summary>
    public static class NameFilter
    {
        public const string Fallback = "Oyuncu";

        private static readonly string[] Anywhere =
        {
            // Turkish
            "orospu", "orosbu", "kahpe", "yarrak", "yarak", "siktir", "sikerim", "sikeyim", "sikik", "sikis", "amcik",
            "aminako", "pezevenk", "gavat", "kavat", "ibne", "puust", "yavsak", "serefsiz", "gerizekali", "pic kurusu",
            "picler", "dalyarak", "tassak", "tasak", "got veren", "gotveren", "godos", "kaltak", "fahise", "sokarim", "anani",
            "bacini", "avradini",
            // English
            "fuck", "shit", "bitch", "cunt", "dick", "pussy", "whore", "slut", "nigger", "nigga", "faggot", "fagot",
            "asshole", "bastard", "retard", "porn", "penis", "vagina", "hitler",
        };

        // Short words, and ones hidden inside ordinary names (Nazife, grape, Aminah, Hamka): refused only on their own.
        private static readonly HashSet<string> WholeWord = new()
        {
            "am", "amk", "amina", "sik", "got", "pic", "oc", "aq", "mk", "fuk", "rape", "nazi", "ass", "fag", "sex", "seks",
            "cum", "tits", "kill",
        };

        /// <summary>The name as it is, or <see cref="Fallback"/> when it holds a word on the lists.</summary>
        public static string Clean(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name ?? string.Empty;
            string folded = Fold(name);
            string joined = folded.Replace(" ", string.Empty);
            foreach (var word in Anywhere)
            {
                string bad = Fold(word);
                if (folded.Contains(bad) || joined.Contains(bad.Replace(" ", string.Empty))) return Fallback;
            }
            foreach (var token in folded.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (WholeWord.Contains(token)) return Fallback;
            // A word spelt out letter by letter ("s.i.k") is whole once the gaps go.
            if (WholeWord.Contains(joined)) return Fallback;
            return name;
        }

        /// <summary>Lower case, plain letters, look-alikes mapped, every other mark a space, runs of a letter squeezed to one.</summary>
        private static string Fold(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char raw in text.ToLower(new CultureInfo("tr-TR")))
            {
                char c = raw switch
                {
                    'ç' => 'c', 'ğ' => 'g', 'ı' => 'i', 'ö' => 'o', 'ş' => 's', 'ü' => 'u', 'â' => 'a', 'î' => 'i', 'û' => 'u',
                    '0' => 'o', '1' => 'i', '!' => 'i', '|' => 'i', '3' => 'e', '4' => 'a', '@' => 'a', '5' => 's', '$' => 's',
                    '7' => 't', '8' => 'b', '9' => 'g',
                    _ => raw,
                };
                if (c < 'a' || c > 'z') c = ' ';
                if (sb.Length > 0 && sb[^1] == c) continue;
                sb.Append(c);
            }
            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
