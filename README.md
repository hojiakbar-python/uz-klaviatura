# UzKlaviatura

2027-yilgi özbek alifbosi (Ö, Ğ, Ş, Ç) uçun Windows klaviaturasi va yordamçi dastur.

- **Klaviatura tartiblari** (`layout/`): «Özbek (yangi alifbo)» — `[ ] ; \` → ö ğ ş ç;
  «Özbek (yangi alifbo, US)» — US tartibi özgarmaydi. Ikkalasida AltGr+o/g/s/c → ö/ğ/ş/ç, AltGr+' → tutuq.
- **Yangi alifbo yordamçisi** (`app/`): eski odat bilan yozilgan `sh`, `ch`, `o'`, `g'` ni
  yozish paytida özi almaştiradi. Tray belgisidan va sozlamalar oynasidan boşqariladi:
  - avto-almaştiriş, «faqat özbek tartibida», örganiş rejimi — har biri yoqiladi/öçiriladi;
  - tezkor tugma (standart Ctrl+Alt+Y);
  - istisno dasturlar röyxati (öyinlar, kod muharrirlari...);
  - fayllarni ommaviy ögiriş: .docx (formatlaş saqlanadi), .txt, .md, .csv, .srt;
  - statistika va maslahatlar (örganiş rejimi).
- **Veb-sayt** (`web/`): onlayn konvertor, alifbo, klaviatura sxemasi. Server kerak emas —
  `build\site\` papkasini GitHub Pages'ga yuklaş kifoya.
- **Örnatuvçi** (`installer/`): Inno Setup 6.

Imlo qoidalari ikki joyda bor (`app/Imlo.cs` va `web/imlo.js`) va ikkalasi `tests/imlo-cases.tsv`
bilan tekşiriladi. Tutuq — U+02BC (ʼ), OʻRQ-1184-son Qonundagi alifbo jadvali böyiça.

## Yigʻiş

Kerak: Visual Studio (C++ komponenti), Inno Setup 6. .NET Framework 4.8 Windows'da bor.

```powershell
.\build.ps1
```

Natija: `dist\UzKlaviatura-<versiya>-setup.exe`. Skript avval barça testlarni işga tuşiradi.
