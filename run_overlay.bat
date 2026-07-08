@echo off
chcp 65001 >nul
cd /d "%~dp0"

set "EXE_RELEASE=%~dp0PoePriceRunesOfAldurHelper-ru.exe"
set "EXE_DEV=%~dp0PoePriceRunesOfAldurHelper-ru\bin\Release\net10.0-windows10.0.19041.0\PoePriceRunesOfAldurHelper-ru.exe"

if exist "%EXE_RELEASE%" (
    start "" "%EXE_RELEASE%"
    exit /b 0
)

if exist "%EXE_DEV%" (
    start "" "%EXE_DEV%"
    exit /b 0
)

echo Building project...
dotnet build "%~dp0PoePriceRunesOfAldurHelper-ru" -c Release
if errorlevel 1 (
    echo Build failed. Install .NET 10 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
    pause
    exit /b 1
)

if exist "%EXE_DEV%" (
    start "" "%EXE_DEV%"
)
