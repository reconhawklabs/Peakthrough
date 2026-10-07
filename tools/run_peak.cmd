@echo off
if "%~1"=="" (
 echo Usage: run_peak.cmd "PEAK game folder"
 exit /b 1
)
call "%~1\BepInEx\plugins\Peakthrough\data\server\server_start.cmd" --owner peak-wrapper
if errorlevel 1 exit /b 1
start "" "steam://rungameid/3527290"
