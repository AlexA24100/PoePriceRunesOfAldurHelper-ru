@echo off
chcp 65001 >nul
cd /d "%~dp0"

set "EXE=%~dp0PoePriceRunesOfAldurHelper-ru\bin\Release\net10.0-windows10.0.19041.0\PoePriceRunesOfAldurHelper-ru.exe"

if not exist "%EXE%" (
    echo Сборка проекта...
    dotnet build "%~dp0PoePriceRunesOfAldurHelper-ru" -c Release
    if errorlevel 1 (
        echo Ошибка сборки. Установите .NET 10 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
        pause
        exit /b 1
    )
)

start "" "%EXE%"
