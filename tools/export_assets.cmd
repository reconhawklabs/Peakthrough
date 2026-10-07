@echo off
if "%~2"=="" (
 echo Usage: export_assets.cmd "Minecraft client jar" "asset cache folder"
 exit /b 1
)
java "%~dp0ExportAssets.java" "%~1" "%~2"
