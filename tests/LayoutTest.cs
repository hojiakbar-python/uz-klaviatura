// Klaviatura DLL'ini o'rnatmasdan tekshiradi: KbdLayerDescriptor jadvallarini o'qiydi.
// Ishlatish: LayoutTest.exe <dll> <variant: main|us>
// x64 DLL 64-bitli jarayonda, WOW64 DLL 32-bitli jarayonda tekshiriladi;
// ikkalasida ham jadvaldagi ko'rsatkichlar 8 baytli.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

static class LayoutTest
{
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32", CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr mod, string name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate long DescriptorFn();

    static int failures;

    static IntPtr Ptr(IntPtr baseAddr, int offset)
    {
        long v = Marshal.ReadInt64(baseAddr, offset);
        return IntPtr.Size == 4 ? new IntPtr((int)(v & 0xFFFFFFFF)) : new IntPtr(v);
    }

    static void Check(string what, object expected, object actual)
    {
        bool ok = expected.Equals(actual);
        if (!ok) failures++;
        Console.WriteLine("{0} {1}: kutilgan {2}, olingan {3}", ok ? "OK  " : "XATO", what, Show(expected), Show(actual));
    }

    static string Show(object o)
    {
        if (o is char) return string.Format("'{0}' U+{1:X4}", o, (int)(char)o);
        return o.ToString();
    }

    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string dll = args[0];
        bool us = args.Length > 1 && args[1] == "us";
        Console.WriteLine("{0} ({1}-bit jarayon)", dll, IntPtr.Size * 8);

        IntPtr mod = LoadLibrary(dll);
        if (mod == IntPtr.Zero) { Console.WriteLine("XATO: yuklanmadi, kod " + Marshal.GetLastWin32Error()); return 1; }
        IntPtr fn = GetProcAddress(mod, "KbdLayerDescriptor");
        if (fn == IntPtr.Zero) { Console.WriteLine("XATO: KbdLayerDescriptor topilmadi"); return 1; }

        long raw = ((DescriptorFn)Marshal.GetDelegateForFunctionPointer(fn, typeof(DescriptorFn)))();
        IntPtr tables = IntPtr.Size == 4 ? new IntPtr((int)(raw & 0xFFFFFFFF)) : new IntPtr(raw);

        // KBDTABLES
        IntPtr mods = Ptr(tables, 0);
        IntPtr wcharTables = Ptr(tables, 8);
        IntPtr vscToVk = Ptr(tables, 48);
        int maxVsc = Marshal.ReadByte(tables, 56);
        int localeFlags = Marshal.ReadInt32(tables, 80);
        int kbdType = Marshal.ReadInt32(tables, 96);

        Check("skan-kodlar soni", 0x80, maxVsc);
        Check("fLocaleFlags", 0x00010001, localeFlags);
        Check("klaviatura turi", 4, kbdType);
        Check("skan 0x10 -> Q", (int)'Q', (int)(ushort)Marshal.ReadInt16(vscToVk, 0x10 * 2));
        Check("skan 0x1A -> OEM_4", 0xDB, (int)(ushort)Marshal.ReadInt16(vscToVk, 0x1A * 2));
        Check("skan 0x36 -> RShift|EXT", 0x1A1, (int)(ushort)Marshal.ReadInt16(vscToVk, 0x36 * 2));

        // MODIFIERS: AltGr (Ctrl+Alt = 6) -> 4-ustun
        Check("wMaxModBits", 7, (int)Marshal.ReadInt16(mods, 8));
        Check("AltGr ustuni", 4, (int)Marshal.ReadByte(mods, 10 + 6));
        Check("Shift+AltGr ustuni", 5, (int)Marshal.ReadByte(mods, 10 + 7));

        // VK -> belgilar
        var map = new Dictionary<int, char[]>();
        for (int t = 0; ; t++)
        {
            IntPtr entry = IntPtr.Add(wcharTables, t * 16);
            IntPtr rows = Ptr(entry, 0);
            if (rows == IntPtr.Zero) break;
            int nMod = Marshal.ReadByte(entry, 8);
            int cb = Marshal.ReadByte(entry, 9);
            Check("jadval " + t + " qator o'lchami", 2 + 2 * nMod, cb);
            for (IntPtr row = rows; Marshal.ReadByte(row) != 0; row = IntPtr.Add(row, cb))
            {
                int vk = Marshal.ReadByte(row);
                var chars = new char[nMod];
                for (int i = 0; i < nMod; i++) chars[i] = (char)Marshal.ReadInt16(row, 2 + 2 * i);
                if (!map.ContainsKey(vk)) map[vk] = chars;
            }
        }

        Func<int, int, char> ch = (vk, col) => map.ContainsKey(vk) && col < map[vk].Length ? map[vk][col] : '￿';
        const int OEM_1 = 0xBA, OEM_4 = 0xDB, OEM_5 = 0xDC, OEM_6 = 0xDD, OEM_7 = 0xDE, COMMA = 0xBC;

        Check("a", 'a', ch('A', 0));
        Check("Shift+a", 'A', ch('A', 1));
        Check("AltGr+o", 'ö', ch('O', 4));
        Check("Shift+AltGr+o", 'Ö', ch('O', 5));
        Check("AltGr+g", 'ğ', ch('G', 4));
        Check("AltGr+s", 'ş', ch('S', 4));
        Check("AltGr+c", 'ç', ch('C', 4));
        Check("AltGr+'", 'ʼ', ch(OEM_7, 4));
        Check("AltGr+,", '«', ch(COMMA, 4));
        Check("numpad 5", '5', ch(0x65, 0));

        if (us)
        {
            Check("[", '[', ch(OEM_4, 0));
            Check(";", ';', ch(OEM_1, 0));
            Check("Shift+;", ':', ch(OEM_1, 1));
        }
        else
        {
            Check("[ -> ö", 'ö', ch(OEM_4, 0));
            Check("Shift+[ -> Ö", 'Ö', ch(OEM_4, 1));
            Check("] -> ğ", 'ğ', ch(OEM_6, 0));
            Check("; -> ş", 'ş', ch(OEM_1, 0));
            Check("\\ -> ç", 'ç', ch(OEM_5, 0));
            Check("AltGr+[ -> [", '[', ch(OEM_4, 4));
            Check("Shift+AltGr+; -> :", ':', ch(OEM_1, 5));
        }

        Console.WriteLine(failures == 0 ? "Hammasi to'g'ri." : failures + " ta xato.");
        return failures == 0 ? 0 : 1;
    }
}
