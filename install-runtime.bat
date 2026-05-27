@echo off
chcp 65001 > nul
title KoMDViewer WPF - Runtime Checker

echo ============================================
echo  KoMDViewer WPF - Runtime Checker
echo ============================================
echo.

echo [1/2] Checking .NET 9 Desktop Runtime...
dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 9." > nul
if %errorlevel% == 0 (
    echo       OK: .NET 9 Desktop Runtime is installed.
) else (
    echo       Missing: .NET 9 Desktop Runtime.
    start "" "https://dotnet.microsoft.com/download/dotnet/9.0"
    echo       Install ".NET Desktop Runtime 9.0.x" for Windows x64.
)

echo.
echo [2/2] Checking Microsoft Edge WebView2 Runtime...
powershell -NoProfile -ExecutionPolicy Bypass -Command "exit $(if ((Test-Path 'HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}') -or (Test-Path 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}') -or (Test-Path 'HKCU:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}')) {0} else {1})"
if %errorlevel% == 0 (
    echo       OK: WebView2 Runtime is installed.
) else (
    echo       Missing or not detected: WebView2 Runtime.
    start "" "https://developer.microsoft.com/microsoft-edge/webview2/"
    echo       Install the Evergreen WebView2 Runtime if the preview pane does not work.
)

echo.
echo Done. Start KoMDViewerWPF.exe after installing any missing runtime.
echo.
pause
