@echo off
cd /d "%~dp0"
py -3 tools\player_setup.py %*
if errorlevel 1 echo Setup did not finish. Read the message above.
pause
