// Global tezkor tugma (standart: Ctrl+Alt+Y) — yordamchini yoqish/o'chirish.
// Ctrl+Alt AltGr bilan bir xil, lekin bizning tartibda AltGr+Y bo'sh, shuning uchun to'qnashmaydi.
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UzKlaviatura
{
    public sealed class HotkeyManager : NativeWindow, IDisposable
    {
        const int WM_HOTKEY = 0x0312;
        const int Id = 0x5A27;
        const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4, MOD_NOREPEAT = 0x4000;

        public event Action Pressed;
        bool registered;

        public HotkeyManager() { CreateHandle(new CreateParams()); }

        // false: tugma boshqa dastur tomonidan band.
        public bool Register(Keys keys)
        {
            Unregister();
            Keys code = keys & Keys.KeyCode;
            if (code == Keys.None) return true;   // o'chirilgan
            uint mods = MOD_NOREPEAT;
            if ((keys & Keys.Control) != 0) mods |= MOD_CONTROL;
            if ((keys & Keys.Alt) != 0) mods |= MOD_ALT;
            if ((keys & Keys.Shift) != 0) mods |= MOD_SHIFT;
            registered = RegisterHotKey(Handle, Id, mods, (uint)code);
            return registered;
        }

        public void Unregister()
        {
            if (registered) UnregisterHotKey(Handle, Id);
            registered = false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == Id)
            {
                var handler = Pressed;
                if (handler != null) handler();
                return;
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            Unregister();
            DestroyHandle();
        }

        [DllImport("user32", SetLastError = true)] static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);
        [DllImport("user32")] static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    }
}
