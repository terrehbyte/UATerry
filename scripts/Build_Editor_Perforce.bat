REM
REM Place this in the same directory as the project's .uproject file
REM

@echo off

REM Move to the directory of this script (to allow launchging from P4V)
cd %~dp0

uaterry build --perforce

pause
