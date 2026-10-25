// Imlo.Convert va TypingEngine uchun testlar.
// TypingEngine matn muharririga taqlid qilib sinaladi: '\b' = Backspace.
using System;
using System.Text;
using UzKlaviatura;

static class EngineTests
{
    static int failures;

    static void Check(string kind, string input, string expected, string actual)
    {
        bool ok = expected == actual;
        if (!ok) failures++;
        Console.WriteLine("{0} {1,-6} {2,-28} -> {3}{4}", ok ? "OK  " : "XATO", kind,
            input.Replace("\b", "<BS>"), actual, ok ? "" : "   (kutilgan: " + expected + ")");
    }

    static string Type(string keys)
    {
        var engine = new TypingEngine();
        var doc = new StringBuilder();
        foreach (char k in keys)
        {
            EditAction a = k == '\b' ? engine.OnBackspace() : engine.OnChar(k);
            if (a == null)
            {
                if (k == '\b') { if (doc.Length > 0) doc.Length--; }
                else doc.Append(k);
                continue;
            }
            doc.Length = Math.Max(0, doc.Length - a.Backspaces);
            doc.Append(a.Text);
        }
        return doc.ToString();
    }

    static void Both(string input, string expected)
    {
        Check("matn", input, expected, Imlo.Convert(input));
        Check("yozuv", input, expected, Type(input));
    }

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // web/imlo.js bilan umumiy holatlar.
        foreach (string line in System.IO.File.ReadAllLines(args[0], Encoding.UTF8))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            string[] parts = line.Split('\t');
            string input = parts[0].Replace("\\n", "\n"), expected = parts[1].Replace("\\n", "\n");
            Check("tsv", input, expected, Imlo.Convert(input));
        }

        Both("salom", "salom");
        Both("shahar", "şahar");
        Both("Shahar", "Şahar");
        Both("SHAHAR", "ŞAHAR");
        Both("chiroyli", "çiroyli");
        Both("Chirchiq", "Çirçiq");
        Both("o'zbek", "özbek");
        Both("O'zbekiston", "Özbekiston");
        Both("O'ZBEKISTON", "ÖZBEKISTON");
        Both("o`zbek", "özbek");
        Both("oʻzbek", "özbek");
        Both("o‘zbek", "özbek");
        Both("g'oya", "ğoya");
        Both("to'g'ri", "töğri");
        Both("bo'lmoq", "bölmoq");
        Both("ma'no", "maʼno");
        Both("Is'hoq", "Isʼhoq");
        Both("ming", "ming");
        Both("kishi ishonch bilan", "kişi işonç bilan");
        Both("qishloq xo'jaligi", "qişloq xöjaligi");

        // Faqat matn konvertori: kontekstga qarab qaror qiladi.
        Check("matn", "tog'", "toğ", Imlo.Convert("tog'"));
        Check("matn", "'Salom, Bobo'", "'Salom, Bobo'", Imlo.Convert("'Salom, Bobo'"));
        Check("matn", "'shahar' dedi", "'şahar' dedi", Imlo.Convert("'shahar' dedi"));
        Check("matn", "https://shop.uz/chat", "https://shop.uz/chat", Imlo.Convert("https://shop.uz/chat"));
        Check("matn", "info@shahar.uz manzil", "info@shahar.uz manzil", Imlo.Convert("info@shahar.uz manzil"));
        Check("matn", "bog'\nsho'rva", "boğ\nşörva", Imlo.Convert("bog'\nsho'rva"));

        // Faqat yozuv: Backspace bilan bekor qilish va oddiy o'chirish.
        Check("yozuv", "sh\b", "sh", Type("sh\b"));
        Check("yozuv", "sh\bop", "shop", Type("sh\bop"));
        Check("yozuv", "sh\b\b", "s", Type("sh\b\b"));
        Check("yozuv", "o'\bzbek", "o'zbek", Type("o'\bzbek"));
        Check("yozuv", "shu\b\bh", "h", Type("shu\b\bh"));
        Check("yozuv", "sa\bh", "ş", Type("sa\bh"));
        Check("yozuv", "tog'", "toğ", Type("tog'"));
        Check("yozuv", "shhh", "şhh", Type("shhh"));
        Check("yozuv", "'salom' dedi", "'salom' dedi", Type("'salom' dedi"));

        Console.WriteLine(failures == 0 ? "Hammasi to'g'ri." : failures + " ta xato.");
        return failures == 0 ? 0 : 1;
    }
}
