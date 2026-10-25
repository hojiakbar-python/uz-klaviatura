// Örganiş rejimi: yordamchi necha marta almashtirganini sanaydi va vaqti-vaqti bilan
// yangi harfni to'g'ridan-to'g'ri qaysi tugma bilan yozish mumkinligini eslatadi.
using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace UzKlaviatura
{
    public sealed class LearningCoach
    {
        const string KeyPath = @"Software\UzKlaviatura\Stats";
        static readonly string[] Letters = { "ş", "ç", "ö", "ğ", "ʼ" };
        static readonly TimeSpan TipInterval = TimeSpan.FromMinutes(15);
        const int TipThreshold = 20;   // shu harf shuncha marta almashtirilgach maslahat

        public readonly Dictionary<string, int> Today = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Total = new Dictionary<string, int>();
        readonly Dictionary<string, int> sinceTip = new Dictionary<string, int>();
        string todayDate;
        DateTime lastTip = DateTime.MinValue;
        int unsaved;

        // (sarlavha, matn) — maslahat ko'rsatish uchun
        public event Action<string, string> Tip;

        public LearningCoach()
        {
            foreach (string l in Letters) { Today[l] = 0; Total[l] = 0; sinceTip[l] = 0; }
            Load();
        }

        public static string Old(string letter)
        {
            switch (letter)
            {
                case "ş": return "sh";
                case "ç": return "ch";
                case "ö": return "o'";
                case "ğ": return "g'";
                default: return "'";
            }
        }

        public static string Shortcut(string letter)
        {
            switch (letter)
            {
                case "ş": return "; tugmasi yoki AltGr+S";
                case "ç": return "\\ tugmasi yoki AltGr+C";
                case "ö": return "[ tugmasi yoki AltGr+O";
                case "ğ": return "] tugmasi yoki AltGr+G";
                default: return "AltGr+'";
            }
        }

        public void OnConverted(string replacement, bool showTips)
        {
            if (string.IsNullOrEmpty(replacement)) return;
            string letter = replacement.Substring(0, 1).ToLowerInvariant();
            if (!Total.ContainsKey(letter)) return;

            RollDate();
            Today[letter]++;
            Total[letter]++;
            sinceTip[letter]++;
            if (++unsaved >= 25) Save();

            if (!showTips || sinceTip[letter] < TipThreshold || DateTime.Now - lastTip < TipInterval) return;
            lastTip = DateTime.Now;
            sinceTip[letter] = 0;
            var handler = Tip;
            if (handler != null)
                handler("Örganiş rejimi",
                    string.Format("Bugun {0} marta «{1}» yozdingiz. «{2}» harfini bitta tugma bilan yozing: {3}.",
                        Today[letter], Old(letter), letter, Shortcut(letter)));
        }

        public void Reset()
        {
            foreach (string l in Letters) { Today[l] = 0; Total[l] = 0; sinceTip[l] = 0; }
            Save();
        }

        void RollDate()
        {
            string now = DateTime.Today.ToString("yyyy-MM-dd");
            if (now == todayDate) return;
            todayDate = now;
            foreach (string l in Letters) Today[l] = 0;
        }

        void Load()
        {
            todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
            {
                if (key == null) return;
                bool sameDay = (key.GetValue("Date") as string) == todayDate;
                foreach (string l in Letters)
                {
                    object total = key.GetValue("total_" + Old(l)), today = key.GetValue("today_" + Old(l));
                    if (total is int) Total[l] = (int)total;
                    if (sameDay && today is int) Today[l] = (int)today;
                }
            }
        }

        public void Save()
        {
            unsaved = 0;
            using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
            {
                key.SetValue("Date", todayDate);
                foreach (string l in Letters)
                {
                    key.SetValue("total_" + Old(l), Total[l], RegistryValueKind.DWord);
                    key.SetValue("today_" + Old(l), Today[l], RegistryValueKind.DWord);
                }
            }
        }
    }
}
