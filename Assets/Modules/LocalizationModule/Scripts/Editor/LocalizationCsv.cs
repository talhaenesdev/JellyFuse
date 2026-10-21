#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
namespace Modules.LocalizationModule.Editor
{
    /// <summary>
    /// One table as CSV: a Key column, then one column per language code. Fields are quoted the way
    /// spreadsheets write them, so texts with commas, quotes and line breaks survive Google Sheets
    /// and Excel both ways. Files are UTF-8 with a byte order mark, which Excel needs to read Turkish.
    /// </summary>
    internal sealed class LocalizationCsv
    {
        public const string KEY = "Key";

        public string Export(CD_LocalizationTable table, IReadOnlyList<string> codes)
        {
            var csv = new StringBuilder();
            Row(csv, Header(codes));
            foreach (var text in table.Texts)
            {
                var row = new List<string> { text.Key };
                foreach (var code in codes) row.Add(text.In(code) ?? "");
                Row(csv, row);
            }
            return csv.ToString();
        }

        /// <summary>Writes every cell of the CSV into the table; returns how many texts it set, or an error.</summary>
        public int Import(CD_LocalizationTable table, string content, out string error)
        {
            error = null;
            if (content.Length > 0 && content[0] == '﻿') content = content.Substring(1);
            var rows = Parse(content);
            if (rows.Count == 0 || rows[0].Count == 0 || rows[0][0].Trim() != KEY)
            {
                error = "The first row must start with a Key column, then one column per language code.";
                return 0;
            }
            var codes = rows[0];
            int set = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0])) continue;
                var text = table.FindOrAdd(row[0]);
                for (int c = 1; c < codes.Count && c < row.Count; c++)
                {
                    if (string.IsNullOrWhiteSpace(codes[c])) continue;
                    text.Set(codes[c].Trim(), row[c]);
                    set++;
                }
            }
            return set;
        }

        private static List<string> Header(IReadOnlyList<string> codes)
        {
            var header = new List<string> { KEY };
            header.AddRange(codes);
            return header;
        }
        private static void Row(StringBuilder csv, List<string> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0) csv.Append(',');
                string cell = cells[i] ?? "";
                bool quote = cell.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
                csv.Append(quote ? "\"" + cell.Replace("\"", "\"\"") + "\"" : cell);
            }
            csv.Append("\r\n");
        }
        private static List<List<string>> Parse(string content)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < content.Length && content[i + 1] == '"') { cell.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else cell.Append(c);
                    continue;
                }
                if (c == '"') quoted = true;
                else if (c == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                    row.Add(cell.ToString()); cell.Clear();
                    rows.Add(row); row = new List<string>();
                }
                else cell.Append(c);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }
    }
}
#endif
