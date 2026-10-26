// Foydalanuvchi sozlamalari: HKCU\Software\UzKlaviatura.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UzKlaviatura
{
    public sealed class Settings
    {
        const string KeyPath = @"Software\UzKlaviatura";
        const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunValue = "UzKlaviatura";

        public bool Enabled = true;
        public bool OnlyUzbekLayout = true;
        public bool LearningMode = true;
        public bool SpellCheck = false;
        public Keys Hotkey = Keys.Control | Keys.Alt | Keys.Y;
        public List<string> ExcludedApps = new List<string>();   // "code.exe" kabi, kichik harflarda

        public static Settings Load()
        {
            var s = new Settings();
            using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
            {
                if (key == null) return s;
                s.Enabled = ReadBool(key, "Enabled", s.Enabled);
                s.OnlyUzbekLayout = ReadBool(key, "OnlyUzbekLayout", s.OnlyUzbekLayout);
                s.LearningMode = ReadBool(key, "LearningMode", s.LearningMode);
                s.SpellCheck = ReadBool(key, "SpellCheck", s.SpellCheck);
                object hotkey = key.GetValue("Hotkey");
                if (hotkey is int) s.Hotkey = (Keys)(int)hotkey;
                var apps = key.GetValue("ExcludedApps") as string[];
                if (apps != null) s.ExcludedApps = apps.Select(NormalizeApp).Where(a => a.Length > 0).Distinct().ToList();
            }
            return s;
        }

        public void Save()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
            {
                key.SetValue("Enabled", Enabled ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("OnlyUzbekLayout", OnlyUzbekLayout ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("LearningMode", LearningMode ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("SpellCheck", SpellCheck ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("Hotkey", (int)Hotkey, RegistryValueKind.DWord);
                key.SetValue("ExcludedApps", ExcludedApps.ToArray(), RegistryValueKind.MultiString);
            }
        }

        public static string NormalizeApp(string name)
        {
            name = (name ?? "").Trim().ToLowerInvariant();
            name = System.IO.Path.GetFileName(name);
            if (name.Length > 0 && !name.EndsWith(".exe")) name += ".exe";
            return name;
        }

        // Windows bilan birga ishga tushish (joriy foydalanuvchi uchun).
        public static bool AutoStart
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
                    return key != null && key.GetValue(RunValue) != null;
            }
            set
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (value) key.SetValue(RunValue, "\"" + Application.ExecutablePath + "\" --autostart");
                    else key.DeleteValue(RunValue, false);
                }
            }
        }

        public static string FormatHotkey(Keys keys)
        {
            var parts = new List<string>();
            if ((keys & Keys.Control) != 0) parts.Add("Ctrl");
            if ((keys & Keys.Alt) != 0) parts.Add("Alt");
            if ((keys & Keys.Shift) != 0) parts.Add("Shift");
            Keys code = keys & Keys.KeyCode;
            if (code != Keys.None) parts.Add(code >= Keys.D0 && code <= Keys.D9 ? ((char)code).ToString() : code.ToString());
            return string.Join("+", parts);
        }

        static bool ReadBool(RegistryKey key, string name, bool fallback)
        {
            object value = key.GetValue(name);
            return value is int ? (int)value != 0 : fallback;
        }
    }
}
