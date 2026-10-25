# Hammasini yig'adi: klaviatura DLL'lari -> testlar -> ilova -> o'rnatuvchi (dist\).
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$build = "$root\build"
New-Item -ItemType Directory -Force "$build\tests", "$build\app" | Out-Null

function Run([string]$what, [scriptblock]$cmd) {
    Write-Host "== $what" -ForegroundColor Cyan
    & $cmd
    if ($LASTEXITCODE -ne 0) { throw "${what}: xato (kod $LASTEXITCODE)" }
}

Run 'Klaviatura DLL' { cmd /c "`"$root\layout\build.cmd`"" }

Run 'Test: tartib (x64)' {
    & $csc /nologo /platform:x64 /out:"$build\tests\LayoutTest64.exe" "$root\tests\LayoutTest.cs"
    & "$build\tests\LayoutTest64.exe" "$build\layout\x64\KBDUZ27.DLL" main | Select-Object -Last 1
    & "$build\tests\LayoutTest64.exe" "$build\layout\x64\KBDUZ27U.DLL" us | Select-Object -Last 1
}
Run 'Test: tartib (WOW64)' {
    & $csc /nologo /platform:x86 /out:"$build\tests\LayoutTest32.exe" "$root\tests\LayoutTest.cs"
    & "$build\tests\LayoutTest32.exe" "$build\layout\wow64\KBDUZ27.DLL" main | Select-Object -Last 1
    & "$build\tests\LayoutTest32.exe" "$build\layout\wow64\KBDUZ27U.DLL" us | Select-Object -Last 1
}
Run 'Test: imlo qoidalari' {
    & $csc /nologo /out:"$build\tests\EngineTests.exe" "$root\tests\EngineTests.cs" "$root\app\Imlo.cs" "$root\app\TypingEngine.cs"
    & "$build\tests\EngineTests.exe" | Select-Object -Last 1
}

Run 'Test: SendInput tuzilmasi' {
    foreach ($plat in 'x64', 'x86') {
        & $csc /nologo /platform:$plat /out:"$build\tests\InputStructTests_$plat.exe" "$root\tests\InputStructTests.cs" `
            "$root\app\KeyboardHook.cs" "$root\app\TypingEngine.cs" "$root\app\Imlo.cs"
        & "$build\tests\InputStructTests_$plat.exe" | Select-Object -Last 1
        if ($LASTEXITCODE -ne 0) { return }
    }
}

Run 'Belgi' { & "$root\installer\make-icon.ps1" -Out "$build\icon.ico"; $global:LASTEXITCODE = 0 }

Run 'Ilova' {
    & $csc /nologo /target:winexe /platform:anycpu /optimize+ /win32icon:"$build\icon.ico" `
        /out:"$build\app\AlifboYordamchi.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll "$root\app\*.cs"
}

$iscc = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
          "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    Write-Warning "Inno Setup 6 topilmadi: o'rnatuvchi yig'ilmadi. O'rnatish: winget install JRSoftware.InnoSetup"
    exit 0
}
Run "O'rnatuvchi" { & $iscc /Q "$root\installer\setup.iss" }
Get-ChildItem "$root\dist\*.exe" | ForEach-Object { Write-Host "Tayyor: $($_.FullName)" -ForegroundColor Green }
