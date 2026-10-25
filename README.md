# UzKlaviatura

2027-yilgi özbek alifbosi (Ö, Ğ, Ş, Ç) uçun Windows klaviaturasi va yordamçi dastur.

- **Klaviatura tartiblari** (`layout/`): «Özbek (yangi alifbo)» — `[ ] ; \` → ö ğ ş ç;
  «Özbek (yangi alifbo, US)» — US tartibi özgarmaydi. Ikkalasida AltGr+o/g/s/c → ö/ğ/ş/ç, AltGr+' → tutuq.
- **Yangi alifbo yordamçisi** (`app/`): eski odat bilan yozilgan `sh`, `ch`, `o'`, `g'` ni
  yozish paytida özi almaştiradi. Tray belgisidan boşqariladi.
- **Örnatuvçi** (`installer/`): Inno Setup 6.

## Yigʻiş

Kerak: Visual Studio (C++ komponenti), Inno Setup 6. .NET Framework 4.8 Windows'da bor.

```powershell
.\build.ps1
```

Natija: `dist\UzKlaviatura-<versiya>-setup.exe`. Skript avval barça testlarni işga tuşiradi.
