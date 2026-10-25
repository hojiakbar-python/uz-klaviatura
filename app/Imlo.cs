// Eski lotin imlosidan 2027-yilgi alifboga o'tkazish qoidalari.
//   oʻ -> ö, gʻ -> ğ, sh -> ş, ch -> ç, harflar orasidagi apostrof -> ʼ (tutuq)
// Eski imlo s/h ni ajratish uchun allaqachon tutuq ishlatgan (Is'hoq, mus'haf),
// shuning uchun "sh" har doim bitta harf deb olinadi.
using System.Text;

namespace UzKlaviatura
{
    public static class Imlo
    {
        public const char Tutuq = 'ʼ';

        // Odamlar oʻ belgisi o'rnida yozadigan barcha variantlar.
        public static bool IsApostrophe(char c)
        {
            switch (c)
            {
                case '\'': case '`': case '´':
                case 'ʻ': case 'ʼ': case '‘': case '’':
                    return true;
            }
            return false;
        }

        // o/g/s/c + ikkinchi belgi -> yangi harf, aks holda '\0'.
        public static char Combine(char first, char second)
        {
            bool upper = char.IsUpper(first);
            char f = char.ToLowerInvariant(first);
            if (IsApostrophe(second))
            {
                if (f == 'o') return upper ? 'Ö' : 'ö';
                if (f == 'g') return upper ? 'Ğ' : 'ğ';
            }
            else if (second == 'h' || second == 'H')
            {
                if (f == 's') return upper ? 'Ş' : 'ş';
                if (f == 'c') return upper ? 'Ç' : 'ç';
            }
            return '\0';
        }

        public static string Convert(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                // Havolalar va e-pochtalar o'zgarmaydi.
                if (i == 0 || char.IsWhiteSpace(text[i - 1]))
                {
                    int end = i;
                    while (end < text.Length && !char.IsWhiteSpace(text[end])) end++;
                    string token = text.Substring(i, end - i);
                    if (token.Contains("://") || token.Contains("@") || token.StartsWith("www.", System.StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append(token);
                        i = end;
                        continue;
                    }
                }

                char c = text[i];
                char next = i + 1 < text.Length ? text[i + 1] : '\0';
                bool prevLetter = i > 0 && char.IsLetter(text[i - 1]);
                bool afterNextLetter = i + 2 < text.Length && char.IsLetter(text[i + 2]);
                char combined = Combine(c, next);

                if (combined != '\0')
                {
                    char f = char.ToLowerInvariant(c);
                    // o' faqat harfdan oldin (o'zbek); g' so'z oxirida ham bo'ladi (tog').
                    // Aks holda bu yopiluvchi qo'shtirnoq bo'lishi mumkin: 'Bobo'
                    bool ok = f == 'o' ? afterNextLetter
                            : f == 'g' ? (afterNextLetter || prevLetter)
                            : true;
                    if (ok)
                    {
                        sb.Append(combined);
                        i += 2;
                        continue;
                    }
                }

                if (IsApostrophe(c) && prevLetter && char.IsLetter(next))
                {
                    sb.Append(Tutuq);
                    i++;
                    continue;
                }

                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }
    }
}
