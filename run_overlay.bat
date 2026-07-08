@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo [INFO] Current directory: %CD%
echo [INFO] Looking for executable...

set "EXE_RELEASE=%~dp0PoePriceRunesOfAldurHelper-ru.exe"
set "EXE_DEV=%~dp0PoePriceRunesOfAldurHelper-ru\bin\Release\net10.0-windows10.0.19041.0\PoePriceRunesOfAldurHelper-ru.exe"

if exist "%EXE_RELEASE%" (
    echo [INFO] Found: %EXE_RELEASE%
    echo [INFO] Launching...
    start "" "%EXE_RELEASE%"
    exit /b 0
)

if exist "%EXE_DEV%" (
    echo [INFO] Found: %EXE_DEV%
    echo [INFO] Launching...
    start "" "%EXE_DEV%"
    exit /b 0
)

echo [WARN] Executable not found. Starting build...
dotnet build "%~dp0PoePriceRunesOfAldurHelper-ru" -c Release

if errorlevel 1 (
    echo [ERROR] Build failed.
    echo [ERROR] Install .NET 10 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
    pause
    exit /b 1
)

if exist "%EXE_DEV%" (
    echo [INFO] Launching built executable...
    start "" "%EXE_DEV%"
) else (
    echo [ERROR] Executable still not found after build.
    pause
)
