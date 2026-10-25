// SendInput'ga ketadigan INPUT tuzilmasi: o'lchami va harf kodi aynan Windows kutganidek bo'lishi kerak.
// Avval wScan char edi va ANSI'ga marshal qilinib, ş -> ų, ğ -> ŧ bo'lib chiqardi.
using System;
using System.Runtime.InteropServices;
using UzKlaviatura;

static class InputStructTests
{
    static int Main()
    {
        int failures = 0;
        int expectedSize = IntPtr.Size == 8 ? 40 : 28;
        int size = Marshal.SizeOf(typeof(KeyboardHook.INPUT));
        Console.WriteLine("{0} INPUT o'lchami ({1}-bit): {2}, kutilgan {3}", size == expectedSize ? "OK  " : "XATO", IntPtr.Size * 8, size, expectedSize);
        if (size != expectedSize) failures++;

        // union x64'da 8-baytdan, x86'da 4-baytdan boshlanadi; wScan undan 2 bayt keyin.
        int scanOffset = (IntPtr.Size == 8 ? 8 : 4) + 2;
        IntPtr mem = Marshal.AllocHGlobal(size);
        try
        {
            foreach (char c in "şŞğĞöÖçÇʼa")
            {
                for (int i = 0; i < size; i++) Marshal.WriteByte(mem, i, 0xFF);   // keraksiz baytlar
                Marshal.StructureToPtr(KeyboardHook.Key(0, c, 4), mem, false);
                int got = (ushort)Marshal.ReadInt16(mem, scanOffset);
                bool ok = got == c;
                if (!ok) failures++;
                Console.WriteLine("{0} {1} U+{2:X4} -> U+{3:X4}", ok ? "OK  " : "XATO", c, (int)c, got);
            }
        }
        finally { Marshal.FreeHGlobal(mem); }

        Console.WriteLine(failures == 0 ? "Hammasi to'g'ri." : failures + " ta xato.");
        return failures == 0 ? 0 : 1;
    }
}
