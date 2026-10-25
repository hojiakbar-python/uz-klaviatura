// Yozish paytida almashtirish mantiqi. Klaviaturadan mustaqil, shuning uchun test qilinadi.
// Har bir tugma uchun null (tugma o'z holicha o'tadi) yoki EditAction
// (tugma yutiladi: Backspaces marta o'chiriladi, keyin Text yoziladi) qaytaradi.
using System.Collections.Generic;

namespace UzKlaviatura
{
    public sealed class EditAction
    {
        public int Backspaces;
        public string Text;

        public EditAction(int backspaces, string text)
        {
            Backspaces = backspaces;
            Text = text;
        }
    }

    public sealed class TypingEngine
    {
        // Shift bilan bosilgan belgi yoki boshqa harf bo'lmagan belgi.
        public const char Other = '\u0001';

        readonly List<char> buffer = new List<char>();
        string revertText;   // ş dan keyin darhol Backspace bosilsa, "sh" qaytariladi

        public void Reset()
        {
            buffer.Clear();
            revertText = null;
        }

        public EditAction OnChar(char c)
        {
            revertText = null;
            char prev = buffer.Count > 0 ? buffer[buffer.Count - 1] : '\0';

            char combined = Imlo.Combine(prev, c);
            if (combined != '\0')
            {
                buffer[buffer.Count - 1] = combined;
                revertText = new string(new[] { prev, c });
                return new EditAction(1, combined.ToString());
            }

            // Harflar orasidagi apostrof tutuqqa aylanadi: ma'no -> maʼno.
            // Faqat keyingi harf kelganda bilamiz, shuning uchun apostrof qayta yoziladi.
            if (char.IsLetter(c) && Imlo.IsApostrophe(prev) && prev != Imlo.Tutuq
                && buffer.Count >= 2 && char.IsLetter(buffer[buffer.Count - 2]))
            {
                buffer[buffer.Count - 1] = Imlo.Tutuq;
                Push(c);
                return new EditAction(1, new string(new[] { Imlo.Tutuq, c }));
            }

            Push(c);
            return null;
        }

        public EditAction OnBackspace()
        {
            if (revertText != null)
            {
                string text = revertText;
                Reset();
                return new EditAction(1, text);
            }
            if (buffer.Count > 0) buffer.RemoveAt(buffer.Count - 1);
            return null;
        }

        void Push(char c)
        {
            buffer.Add(c);
            if (buffer.Count > 16) buffer.RemoveAt(0);
        }
    }
}
