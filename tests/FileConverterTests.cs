// FileConverter testlari: .docx (bo'laklarga bo'lingan matn, formatlash) va matn fayllari (kodlash).
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UzKlaviatura;

static class FileConverterTests
{
    static int failures;

    static void Check(string what, string expected, string actual)
    {
        bool ok = expected == actual;
        if (!ok) failures++;
        Console.WriteLine("{0} {1}: {2}{3}", ok ? "OK  " : "XATO", what, actual, ok ? "" : "   (kutilgan: " + expected + ")");
    }

    const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    static string Run(string text, bool bold)
    {
        return "<w:r>" + (bold ? "<w:rPr><w:b/></w:rPr>" : "") + "<w:t xml:space=\"preserve\">" + text + "</w:t></w:r>";
    }

    static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        string dir = Path.Combine(Path.GetTempPath(), "uzk-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            // 1) "O" qalin, "'zbekiston" oddiy: natijada "Ö" qalin bo'lagida qolishi kerak.
            string body =
                "<w:p>" + Run("O", true) + Run("'zbekiston ", false) + Run("shahar", true) + "</w:p>" +
                "<w:p>" + Run("ma'no va tog'", false) + "</w:p>" +
                "<w:p><w:hyperlink><w:r><w:instrText>HYPERLINK \"https://shop.uz\"</w:instrText></w:r>" + Run("chiroyli", false) + "</w:hyperlink></w:p>";
            string docXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"" + W + "\"><w:body>" + body + "</w:body></w:document>";
            string headerXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><w:hdr xmlns:w=\"" + W + "\">" + "<w:p>" + Run("Bosh sahifa", false) + "</w:p></w:hdr>";

            string docx = Path.Combine(dir, "hujjat.docx");
            using (var zip = ZipFile.Open(docx, ZipArchiveMode.Create))
            {
                Write(zip, "[Content_Types].xml", "<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"/>");
                Write(zip, "word/document.xml", docXml);
                Write(zip, "word/header1.xml", headerXml);
                Write(zip, "word/media/rasm.xml", "<x>sh</x>");   // matn qismi emas — tegilmaydi
            }

            string outPath = FileConverter.OutputPath(docx, false);
            Check("chiqish nomi", Path.Combine(dir, "hujjat (yangi alifbo).docx"), outPath);
            int changes = FileConverter.Convert(docx, outPath);
            Check("o'zgarishlar soni", "6", changes.ToString());   // Ö ş ʼ ğ ç + kolontitulda ş

            var doc = new XmlDocument();
            using (var zip = ZipFile.OpenRead(outPath))
            {
                using (var s = zip.GetEntry("word/document.xml").Open()) doc.Load(s);
                using (var r = new StreamReader(zip.GetEntry("word/media/rasm.xml").Open())) Check("boshqa qismlar", "<x>sh</x>", r.ReadToEnd());
                var hdr = new XmlDocument();
                using (var s = zip.GetEntry("word/header1.xml").Open()) hdr.Load(s);
                Check("kolontitul", "Boş sahifa", hdr.InnerText);
            }
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("w", W);
            XmlNodeList t = doc.SelectNodes("//w:t", ns);
            Check("1-bo'lak (qalin)", "Ö", t[0].InnerText);
            Check("2-bo'lak", "zbekiston ", t[1].InnerText);
            Check("3-bo'lak (qalin)", "şahar", t[2].InnerText);
            Check("2-paragraf", "maʼno va toğ", t[3].InnerText);
            Check("havola matni", "çiroyli", t[4].InnerText);
            Check("havola manzili", "HYPERLINK \"https://shop.uz\"", doc.SelectSingleNode("//w:instrText", ns).InnerText);
            Check("qalinlik saqlangan", "1", doc.SelectNodes("//w:r[w:rPr/w:b]/w:t[.='Ö']", ns).Count.ToString());
            Check("asl fayl o'zgarmagan", "True", File.ReadAllBytes(docx).Length > 0 && Contains(docx, "word/document.xml", "'zbekiston") ? "True" : "False");

            // 2) Matn fayllari: ANSI (1252, ‘ belgisi bilan) va UTF-8.
            string ansi = Path.Combine(dir, "eski.txt");
            File.WriteAllBytes(ansi, Encoding.GetEncoding(1252).GetBytes("o‘zbek tili, shahar"));
            FileConverter.Convert(ansi, FileConverter.OutputPath(ansi, false));
            Check("ANSI .txt", "özbek tili, şahar", File.ReadAllText(FileConverter.OutputPath(ansi, false), Encoding.UTF8));

            string utf = Path.Combine(dir, "matn.srt");
            File.WriteAllText(utf, "1\n00:00:01,000 --> 00:00:02,000\nTo'g'ri choy\n", new UTF8Encoding(false));
            FileConverter.Convert(utf, utf);   // asl faylni almashtirish
            Check("UTF-8 .srt (o'rniga)", "1\n00:00:01,000 --> 00:00:02,000\nTöğri çoy\n", File.ReadAllText(utf, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(dir, true);
        }

        Console.WriteLine(failures == 0 ? "Hammasi to'g'ri." : failures + " ta xato.");
        return failures == 0 ? 0 : 1;
    }

    static void Write(ZipArchive zip, string name, string content)
    {
        using (var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false))) w.Write(content);
    }

    static bool Contains(string zipPath, string entry, string text)
    {
        using (var zip = ZipFile.OpenRead(zipPath))
        using (var r = new StreamReader(zip.GetEntry(entry).Open()))
            return r.ReadToEnd().Contains(text);
    }
}
