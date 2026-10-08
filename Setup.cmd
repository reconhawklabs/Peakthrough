@echo off
cd /d "%~dp0"
py -3 --version >nul 2>&1
if errorlevel 1 (
 echo Python 3 is not available through the Windows Python launcher.
 echo Install or repair Python until "py -3 --version" works in Command Prompt.
 pause
 exit /b 1
)
py -3 tools\player_setup.py %*
if errorlevel 1 echo Setup did not finish. Read the message above.
pause
