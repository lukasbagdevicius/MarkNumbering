@echo off
rem ============================================================
rem  Mark numeravimas pagal ilgi - diegimas i Revit 2026
rem  Nukopijuoja DLL ir .addin manifesta i Revit Addins aplanka.
rem ============================================================

set "TARGET=%APPDATA%\Autodesk\Revit\Addins\2026"

if not exist "%~dp0dist\MarkNumbering.dll" (
    echo KLAIDA: nerastas dist\MarkNumbering.dll
    echo Pirmiausia sukompiliuokite projekta arba patikrinkite, ar failas yra dist aplanke.
    pause
    exit /b 1
)

if not exist "%TARGET%" mkdir "%TARGET%"

copy /Y "%~dp0dist\MarkNumbering.dll" "%TARGET%\" >nul
if errorlevel 1 goto :error

copy /Y "%~dp0MarkNumbering.addin" "%TARGET%\" >nul
if errorlevel 1 goto :error

echo.
echo Idiegta sekmingai i:
echo   %TARGET%
echo.
echo Paleiskite Revit 2026. Pirma karta pasirinkite "Always Load".
echo Komanda rasite: Add-Ins ^> External Tools ^> Mark numeravimas pagal ilgi
echo.
pause
exit /b 0

:error
echo KLAIDA: nepavyko nukopijuoti failu. Uzdarykite Revit ir bandykite dar karta.
pause
exit /b 1
