// Fayllarni yangi alifboga ommaviy o'tkazish: .txt/.md/.csv/.srt va .docx.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace UzKlaviatura
{
    public static class FileConverter
    {
        public static readonly string[] TextExtensions = { ".txt", ".md", ".csv", ".srt" };
        const string WordNs = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        public static bool IsSupported(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".docx" || TextExtensions.Contains(ext);
        }

        // "hisobot.docx" -> "hisobot (yangi alifbo).docx"
        public static string OutputPath(string path, bool overwrite)
        {
            if (overwrite) return path;
            return Path.Combine(Path.GetDirectoryName(path),
                Path.GetFileNameWithoutExtension(path) + " (yangi alifbo)" + Path.GetExtension(path));
        }

        // O'zgarishlar sonini qaytaradi.
        public static int Convert(string path, string outputPath)
        {
            return Path.GetExtension(path).ToLowerInvariant() == ".docx"
                ? ConvertDocx(path, outputPath)
                : ConvertText(path, outputPath);
        }

        static int ConvertText(string path, string outputPath)
        {
            string text = ReadText(File.ReadAllBytes(path));
            int[] map;
            int changes;
            string converted = Imlo.Convert(text, out map, out changes);
            File.WriteAllText(outputPath, converted, new UTF8Encoding(true));   // yangi harflar uchun Unicode shart
            return changes;
        }

        // BOM bo'lsa — unga qarab; bo'lmasa UTF-8, u ham bo'lmasa Windows'ning ANSI kodlashi.
        static string ReadText(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            try { return new UTF8Encoding(false, true).GetString(bytes); }
            catch (DecoderFallbackException) { return Encoding.Default.GetString(bytes); }
        }

        static int ConvertDocx(string path, string outputPath)
        {
            if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
                File.Copy(path, outputPath, true);

            int changes = 0;
            using (var zip = ZipFile.Open(outputPath, ZipArchiveMode.Update))
            {
                var parts = zip.Entries.Where(e => IsTextPart(e.FullName)).Select(e => e.FullName).ToList();
                foreach (string name in parts)
                {
                    ZipArchiveEntry entry = zip.GetEntry(name);
                    var doc = new XmlDocument { PreserveWhitespace = true };
                    using (var s = entry.Open()) doc.Load(s);

                    int partChanges = ConvertWordXml(doc);
                    if (partChanges == 0) continue;
                    changes += partChanges;

                    entry.Delete();
                    ZipArchiveEntry fresh = zip.CreateEntry(name, CompressionLevel.Optimal);
                    using (var s = fresh.Open())
                    using (var w = XmlWriter.Create(s, new XmlWriterSettings { Encoding = new UTF8Encoding(false) }))
                        doc.Save(w);
                }
            }
            return changes;
        }

        static bool IsTextPart(string name)
        {
            name = name.ToLowerInvariant();
            if (!name.StartsWith("word/") || !name.EndsWith(".xml") || name.Contains("/_rels/")) return false;
            string file = Path.GetFileName(name);
            return file == "document.xml" || file.StartsWith("header") || file.StartsWith("footer")
                || file == "footnotes.xml" || file == "endnotes.xml" || file == "comments.xml";
        }

        // Har bir paragrafdagi <w:t> bo'laklari birlashtirilib o'tkaziladi: "o" bir bo'lakda,
        // "'" boshqasida bo'lsa ham "ö" chiqadi. Natija bo'laklarga asl o'rniga qarab qaytariladi,
        // shuning uchun formatlash (qalin, rang...) saqlanadi.
        public static int ConvertWordXml(XmlDocument doc)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("w", WordNs);
            int changes = 0;

            foreach (XmlElement p in doc.SelectNodes("//w:p", ns))
            {
                var runs = new List<XmlElement>();
                foreach (XmlElement t in p.SelectNodes(".//w:t", ns))
                    if (NearestParagraph(t) == p) runs.Add(t);   // ichki jadval/matn qutisi o'z paragrafida
                if (runs.Count == 0) continue;

                var starts = new int[runs.Count + 1];
                var all = new StringBuilder();
                for (int k = 0; k < runs.Count; k++)
                {
                    starts[k] = all.Length;
                    all.Append(runs[k].InnerText);
                }
                starts[runs.Count] = all.Length;

                int[] map;
                int c;
                string converted = Imlo.Convert(all.ToString(), out map, out c);
                if (c == 0) continue;
                changes += c;

                var pieces = new StringBuilder[runs.Count];
                for (int k = 0; k < runs.Count; k++) pieces[k] = new StringBuilder();
                int run = 0;
                for (int j = 0; j < converted.Length; j++)
                {
                    while (run < runs.Count - 1 && map[j] >= starts[run + 1]) run++;
                    pieces[run].Append(converted[j]);
                }

                for (int k = 0; k < runs.Count; k++)
                {
                    string text = pieces[k].ToString();
                    if (text == runs[k].InnerText) continue;
                    runs[k].InnerText = text;
                    if (text.Length > 0 && (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[text.Length - 1])))
                        runs[k].SetAttribute("space", "http://www.w3.org/XML/1998/namespace", "preserve");
                }
            }
            return changes;
        }

        static XmlNode NearestParagraph(XmlNode node)
        {
            for (XmlNode n = node.ParentNode; n != null; n = n.ParentNode)
                if (n.LocalName == "p" && n.NamespaceURI == WordNs) return n;
            return null;
        }
    }
}
