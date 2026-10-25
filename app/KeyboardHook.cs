// Past darajali klaviatura ilmog'i: bosilgan tugmalarni TypingEngine'ga beradi
// va kerak bo'lsa tugmani yutib, o'rniga SendInput bilan yangi harfni yozadi.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UzKlaviatura
{
    public sealed class KeyboardHook : IDisposable
    {
        const int WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14;
        const int WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
        const int WM_LBUTTONDOWN = 0x201, WM_RBUTTONDOWN = 0x204, WM_MBUTTONDOWN = 0x207;
        const uint LLKHF_INJECTED = 0x10;
        const uint KEYEVENTF_KEYUP = 0x2, KEYEVENTF_UNICODE = 0x4;
        const uint MAPVK_VK_TO_CHAR = 2;
        const int VK_BACK = 0x08, VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12, VK_CAPITAL = 0x14;
        const int VK_LWIN = 0x5B, VK_RWIN = 0x5C;
        const int LANG_UZ_LATN = 0x0443;
        static readonly IntPtr Marker = new IntPtr(0x55A1F00D);

        public bool Enabled = true;
        public bool OnlyUzbekLayout = true;
        public HashSet<string> ExcludedApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Oxirgi marta yozilgan dastur ("notepad.exe"): tray menyusidagi "şu dasturda öçiriş" uchun.
        public string LastAppName { get; private set; }

        // Almashtirilgan yangi harf ("ş", "ʼa" ...) — örganiş rejimi uchun.
        public event Action<string> Converted;

        static readonly HashSet<string> ShellApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "explorer.exe", "shellhost.exe", "searchhost.exe", "startmenuexperiencehost.exe", "shellexperiencehost.exe" };

        readonly TypingEngine engine = new TypingEngine();
        readonly HookProc keyboardProc, mouseProc;   // GC yig'ib olmasligi uchun maydonda
        readonly HashSet<uint> swallowedKeys = new HashSet<uint>();
        readonly bool selfElevated;
        readonly int selfPid = Process.GetCurrentProcess().Id;
        IntPtr keyboardHook, mouseHook, lastForeground;
        bool foregroundBlocked;
        string foregroundApp = "";

        public KeyboardHook()
        {
            keyboardProc = OnKeyboard;
            mouseProc = OnMouse;
            selfElevated = IsElevated(Process.GetCurrentProcess().Handle);
            IntPtr module = GetModuleHandle(null);
            keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, module, 0);
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, module, 0);
            if (keyboardHook == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        public void Dispose()
        {
            if (keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(keyboardHook);
            if (mouseHook != IntPtr.Zero) UnhookWindowsHookEx(mouseHook);
            keyboardHook = mouseHook = IntPtr.Zero;
        }

        IntPtr OnMouse(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int msg = wParam.ToInt32();
            if (nCode >= 0 && (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN))
                engine.Reset();   // kursor boshqa joyga o'tgan bo'lishi mumkin
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        IntPtr OnKeyboard(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var k = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                int msg = wParam.ToInt32();
                if ((k.flags & LLKHF_INJECTED) == 0)
                {
                    if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                    {
                        if (HandleKeyDown(k.vkCode))
                        {
                            swallowedKeys.Add(k.vkCode);
                            return new IntPtr(1);
                        }
                    }
                    else if ((msg == WM_KEYUP || msg == WM_SYSKEYUP) && swallowedKeys.Remove(k.vkCode))
                    {
                        return new IntPtr(1);
                    }
                }
            }
            return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }

        bool HandleKeyDown(uint vk)
        {
            if (IsModifier(vk)) return false;
            if (!Enabled) { engine.Reset(); return false; }

            IntPtr fg = GetForegroundWindow();
            uint pid;
            uint thread = GetWindowThreadProcessId(fg, out pid);
            if (fg != lastForeground)
            {
                lastForeground = fg;
                engine.Reset();
                // Administrator oynasiga SendInput yetib bormaydi (UIPI): tugmani yutsak, yo'qolib qoladi.
                foregroundBlocked = !selfElevated && IsProcessElevated(pid);
                foregroundApp = ProcessName(pid);
                if (pid != selfPid && foregroundApp.Length > 0 && !ShellApps.Contains(foregroundApp))
                    LastAppName = foregroundApp;
            }
            if (foregroundBlocked || ExcludedApps.Contains(foregroundApp)) return false;

            if (Down(VK_CONTROL) || Down(VK_MENU) || Down(VK_LWIN) || Down(VK_RWIN))
            {
                engine.Reset();
                return false;
            }

            IntPtr hkl = GetKeyboardLayout(thread);
            if (OnlyUzbekLayout && (hkl.ToInt64() & 0xFFFF) != LANG_UZ_LATN)
            {
                engine.Reset();
                return false;
            }

            if (vk == VK_BACK) return Apply(engine.OnBackspace());

            // MapVirtualKeyEx o'lik tugma holatini buzmaydi (ToUnicodeEx'dan farqli).
            uint mapped = MapVirtualKeyEx(vk, MAPVK_VK_TO_CHAR, hkl);
            if (mapped == 0 || (mapped & 0x80000000) != 0)
            {
                engine.Reset();   // strelkalar, F-tugmalar, o'lik tugmalar
                return false;
            }

            char c = (char)(mapped & 0xFFFF);
            bool shift = Down(VK_SHIFT);
            if (char.IsLetter(c))
            {
                bool upper = shift ^ ((GetKeyState(VK_CAPITAL) & 1) != 0);
                c = upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
            }
            else if (shift)
            {
                c = TypingEngine.Other;
            }
            return Apply(engine.OnChar(c));
        }

        bool Apply(EditAction action)
        {
            if (action == null) return false;
            // Bekor qilish ("sh" qaytishi) yangi harf bilan boshlanmaydi, shuning uchun sanalmaydi.
            if ("şçöğʼŞÇÖĞ".IndexOf(action.Text[0]) >= 0)
            {
                var handler = Converted;
                if (handler != null) handler(action.Text);
            }
            var inputs = new List<INPUT>();
            for (int i = 0; i < action.Backspaces; i++)
            {
                inputs.Add(Key(VK_BACK, '\0', 0));
                inputs.Add(Key(VK_BACK, '\0', KEYEVENTF_KEYUP));
            }
            foreach (char ch in action.Text)
            {
                inputs.Add(Key(0, ch, KEYEVENTF_UNICODE));
                inputs.Add(Key(0, ch, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP));
            }
            INPUT[] arr = inputs.ToArray();
            SendInput((uint)arr.Length, arr, Marshal.SizeOf(typeof(INPUT)));
            return true;
        }

        internal static INPUT Key(int vk, char scan, uint flags)
        {
            var input = new INPUT { type = 1 };
            input.u.ki = new KEYBDINPUT { wVk = (ushort)vk, wScan = scan, dwFlags = flags, dwExtraInfo = Marker };
            return input;
        }

        static bool IsModifier(uint vk)
        {
            return (vk >= 0x10 && vk <= 0x12) || (vk >= 0xA0 && vk <= 0xA5)
                || vk == VK_CAPITAL || vk == VK_LWIN || vk == VK_RWIN || vk == 0x90 || vk == 0x91;
        }

        static bool Down(int vk) { return (GetAsyncKeyState(vk) & 0x8000) != 0; }

        static string ProcessName(uint pid)
        {
            try { return Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant() + ".exe"; }
            catch (ArgumentException) { return ""; }
            catch (InvalidOperationException) { return ""; }
        }

        static bool IsProcessElevated(uint pid)
        {
            const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
            IntPtr process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (process == IntPtr.Zero) return true;   // ochib bo'lmasa, xavfsiz tomonni tanlaymiz
            try { return IsElevated(process); }
            finally { CloseHandle(process); }
        }

        static bool IsElevated(IntPtr process)
        {
            const uint TOKEN_QUERY = 0x8;
            const int TokenElevation = 20;
            IntPtr token;
            if (!OpenProcessToken(process, TOKEN_QUERY, out token)) return true;
            try
            {
                int elevation, size;
                if (!GetTokenInformation(token, TokenElevation, out elevation, 4, out size)) return true;
                return elevation != 0;
            }
            finally { CloseHandle(token); }
        }

        // --- WinAPI -----------------------------------------------------------

        delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }

        // wScan ushort bo'lishi shart: char maydon standart holatda ANSI baytga marshal qilinadi
        // va ş "eng yaqin" s ga aylanib, ų (U+0173) bo'lib chiqadi.
        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT { public uint type; public InputUnion u; }

        [DllImport("user32", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, HookProc fn, IntPtr hMod, uint threadId);
        [DllImport("user32")] static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32")] static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32")] static extern uint SendInput(uint count, INPUT[] inputs, int size);
        [DllImport("user32")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
        [DllImport("user32")] static extern IntPtr GetKeyboardLayout(uint thread);
        [DllImport("user32", CharSet = CharSet.Unicode)] static extern uint MapVirtualKeyEx(uint code, uint mapType, IntPtr hkl);
        [DllImport("user32")] static extern short GetAsyncKeyState(int vk);
        [DllImport("user32")] static extern short GetKeyState(int vk);
        [DllImport("kernel32", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32")] static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
        [DllImport("kernel32")] static extern bool CloseHandle(IntPtr handle);
        [DllImport("advapi32")] static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
        [DllImport("advapi32")] static extern bool GetTokenInformation(IntPtr token, int cls, out int info, int len, out int retLen);
    }
}
