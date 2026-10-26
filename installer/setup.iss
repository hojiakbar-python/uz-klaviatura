; Inno Setup 6 skripti: klaviatura tartiblari + "Yangi alifbo yordamçisi".
; build.ps1 orqali yig'iladi (avval build\ papkasi tayyorlanadi).

#define AppName "Özbek klaviaturasi (yangi alifbo)"
#define AppVersion "0.2.2"
#define AppExe "AlifboYordamchi.exe"
#define LayoutsKey "SYSTEM\CurrentControlSet\Control\Keyboard Layouts"

[Setup]
AppId={{6E3C1B2A-5F47-4C1D-9B8E-2A7D0F3C9E11}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=UzKlaviatura
DefaultDirName={autopf}\UzKlaviatura
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\dist
OutputBaseFilename=UzKlaviatura-{#AppVersion}-setup
SetupIconFile=..\build\icon.ico
UninstallDisplayIcon={app}\{#AppExe}
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=force
; Avtoishga tushirish HKCU'ga yoziladi. UAC odatda o'sha foydalanuvchining o'zini ko'taradi;
; boshqa administrator hisobi bilan o'rnatilsa, foydalanuvchi uni sozlamalar oynasidan yoqadi.
UsedUserAreasWarning=no

[Languages]
Name: "uz"; MessagesFile: "compiler:Default.isl,Uzbek.isl"

[Tasks]
; Ikkalasi ham standart holatda belgilangan.
Name: "desktopicon"; Description: "Iş stolida yorliq yaratiş"; GroupDescription: "Qöşimça:"
Name: "autostart"; Description: "Yordamçini Windows bilan birga işga tuşiriş"; GroupDescription: "Qöşimça:"

[Files]
Source: "..\build\layout\x64\KBDUZ27.DLL";    DestDir: "{sys}";      Flags: restartreplace uninsrestartdelete
Source: "..\build\layout\x64\KBDUZ27U.DLL";   DestDir: "{sys}";      Flags: restartreplace uninsrestartdelete
Source: "..\build\layout\wow64\KBDUZ27.DLL";  DestDir: "{syswow64}"; Flags: restartreplace uninsrestartdelete
Source: "..\build\layout\wow64\KBDUZ27U.DLL"; DestDir: "{syswow64}"; Flags: restartreplace uninsrestartdelete
Source: "..\build\app\{#AppExe}";             DestDir: "{app}";      Flags: ignoreversion
Source: "language.ps1";                       DestDir: "{app}";      Flags: ignoreversion

[Registry]
Root: HKLM; Subkey: "{#LayoutsKey}\a0000443"; ValueType: string; ValueName: "Layout File"; ValueData: "KBDUZ27.DLL"; Flags: uninsdeletekey
Root: HKLM; Subkey: "{#LayoutsKey}\a0000443"; ValueType: string; ValueName: "Layout Text"; ValueData: "Özbek (yangi alifbo)"
Root: HKLM; Subkey: "{#LayoutsKey}\a0000443"; ValueType: string; ValueName: "Layout Id";   ValueData: "0f27"
Root: HKLM; Subkey: "{#LayoutsKey}\a0010443"; ValueType: string; ValueName: "Layout File"; ValueData: "KBDUZ27U.DLL"; Flags: uninsdeletekey
Root: HKLM; Subkey: "{#LayoutsKey}\a0010443"; ValueType: string; ValueName: "Layout Text"; ValueData: "Özbek (yangi alifbo, US)"
Root: HKLM; Subkey: "{#LayoutsKey}\a0010443"; ValueType: string; ValueName: "Layout Id";   ValueData: "0f28"
; Avtoishga tushirish foydalanuvchiniki: sozlamalar oynasidan administratorsiz o'chiriladi.
; 0.1.x versiyalar HKLM'ga yozgan edi — o'sha qiymat olib tashlanadi.
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "UzKlaviatura"; Flags: deletevalue
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "UzKlaviatura"; ValueData: """{app}\{#AppExe}"" --autostart"; Tasks: autostart; Flags: uninsdeletevalue

[Icons]
; Yorliq har doim sozlamalar oynasini ochadi (dastur ishlab turgan bo'lsa ham).
Name: "{group}\Yangi alifbo yordamçisi"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\Yangi alifbo yordamçisi"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
; Til ro'yxati har bir foydalanuvchiniki, shuning uchun administrator emas, o'rnatgan odam nomidan.
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\language.ps1"" -Add"; Flags: runhidden runasoriginaluser waituntilterminated; StatusMsg: "Klaviatura til röyxatiga qöşilmoqda..."
Filename: "{app}\{#AppExe}"; Description: "Yangi alifbo yordamçisini işga tuşiriş"; Flags: postinstall nowait runasoriginaluser

[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/f /im {#AppExe}"; Flags: runhidden; RunOnceId: "KillApp"
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\language.ps1"" -Remove"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveLanguage"

[Code]
// Tray ilovasining oynasi yo'q, Restart Manager uni yopa olmaydi: yangilashdan oldin o'zimiz yopamiz.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/f /im {#AppExe}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
