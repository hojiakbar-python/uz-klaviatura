@echo off
rem Klaviatura DLL'larini yig'adi: 2 variant x (x64 + WOW64) -> ..\build\layout\
setlocal
set "VCVARS=C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvarsall.bat"
set "PATH=%PATH%;C:\Program Files (x86)\Microsoft Visual Studio\Installer"
set "SRC=%~dp0"
set "OUT=%~dp0..\build\layout"
if not exist "%OUT%\x64" mkdir "%OUT%\x64"
if not exist "%OUT%\wow64" mkdir "%OUT%\wow64"

set CFLAGS=/nologo /c /W4 /WX /O1 /GS- /Gy /Zl /Zp8 /TC
set LFLAGS=/nologo /DLL /NOENTRY /NODEFAULTLIB /SUBSYSTEM:NATIVE /RELEASE /OPT:REF /OPT:ICF ^
 /MERGE:.rdata=.data /MERGE:.text=.data /MERGE:.edata=.data /SECTION:.data,re /IGNORE:4254

call "%VCVARS%" x64 >nul || exit /b 1
call :build x64 KBDUZ27 "" || exit /b 1
call :build x64 KBDUZ27U "/DUZ_US_VARIANT" || exit /b 1

call "%VCVARS%" x64_x86 >nul || exit /b 1
call :build wow64 KBDUZ27 "/DBUILD_WOW6432" || exit /b 1
call :build wow64 KBDUZ27U "/DBUILD_WOW6432 /DUZ_US_VARIANT" || exit /b 1

echo Tayyor: %OUT%
exit /b 0

:build
set "OBJ=%OUT%\%1\%2.obj"
cl %CFLAGS% %~3 /Fo"%OBJ%" "%SRC%kbduz27.c" || exit /b 1
(echo LIBRARY %2& echo EXPORTS& echo     KbdLayerDescriptor @1) > "%OUT%\%1\%2.def"
link %LFLAGS% /DEF:"%OUT%\%1\%2.def" /OUT:"%OUT%\%1\%2.DLL" "%OBJ%" || exit /b 1
del "%OBJ%" "%OUT%\%1\%2.def" "%OUT%\%1\%2.lib" "%OUT%\%1\%2.exp" 2>nul
exit /b 0
