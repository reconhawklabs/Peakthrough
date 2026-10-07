@echo off
if "%~1"=="" (
 echo Usage: deploy.cmd "PEAK game folder"
 exit /b 1
)
tasklist /FI "IMAGENAME eq PEAK.exe" /NH | find /I "PEAK.exe" >nul
if not errorlevel 1 (
 echo Close PEAK first.
 exit /b 1
)
dotnet build "%~dp0src\PeakCreativeMode.Plugin" -c Release -t:Rebuild -p:DebugType=None -p:DebugSymbols=false "-p:PeakDir=%~1"
if errorlevel 1 exit /b 1
if not exist "%~1\BepInEx\core\BepInEx.dll" exit /b 1
if not exist "%~1\BepInEx\plugins\Peakthrough" mkdir "%~1\BepInEx\plugins\Peakthrough"
copy /y "%~dp0src\PeakCreativeMode.Plugin\bin\Release\netstandard2.1\Peakthrough.dll" "%~1\BepInEx\plugins\Peakthrough\"
copy /y "%~dp0src\PeakCreativeMode.Plugin\bin\Release\netstandard2.1\PeakCreativeMode.Core.dll" "%~1\BepInEx\plugins\Peakthrough\"

for %%N in (PeakCreativeMode.Plugin.dll PeakCreativeMode.Core.dll) do if exist "%~1\BepInEx\plugins\PeakCreativeMode\%%N" move /y "%~1\BepInEx\plugins\PeakCreativeMode\%%N" "%~1\BepInEx\plugins\PeakCreativeMode\%%N.previous"
