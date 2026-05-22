@echo off
chcp 65001 > nul
title KoMDViewer - ランタイムインストーラー

echo ============================================
echo  KoMDViewer - ランタイムインストーラー
echo ============================================
echo.
echo KoMDViewer の実行に必要なランタイムをインストールします。
echo.

:: --- [1/2] .NET 9.0 Desktop Runtime ---
echo [1/2] .NET 9.0 Desktop Runtime を確認中...
dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 9." > nul
if %errorlevel% == 0 (
    echo       OK: 既にインストール済みです。
) else (
    echo       未インストールです。ダウンロードページを開きます...
    start "" "https://dotnet.microsoft.com/download/dotnet/9.0"
    echo.
    echo       ブラウザが開きました。
    echo       .NET Desktop Runtime 9.0.x の Windows x64 インストーラーを
    echo       ダウンロードして実行してください。
    echo.
    echo       インストール完了後、Enterキーを押してください...
    pause > nul
)

echo.

:: --- [2/2] Windows App Runtime 2.x ---
echo [2/2] Windows App Runtime 2.x を確認中...
powershell -NoProfile -Command "exit $(if (Get-AppxPackage -Name 'Microsoft.WindowsAppRuntime.2*' -EA SilentlyContinue) {0} else {1})"
if %errorlevel% == 0 (
    echo       OK: 既にインストール済みです。
    goto :done
)

echo       未インストールです。インストーラーをダウンロード中...
echo.
powershell -NoProfile -Command "Invoke-WebRequest -Uri 'https://aka.ms/windowsappruntimeinstall-x64' -OutFile '%TEMP%\WARInstall.exe' -UseBasicParsing -MaximumRedirection 10"
if exist "%TEMP%\WARInstall.exe" (
    echo       ダウンロード完了。インストールを開始します...
    "%TEMP%\WARInstall.exe"
    del "%TEMP%\WARInstall.exe" 2>nul
) else (
    echo       ダウンロードに失敗しました。
    echo       以下のURLから手動でインストールしてください:
    echo       https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads
)

:done
echo.
echo ============================================
echo  完了しました。KoMDViewer.exe を起動できます。
echo ============================================
echo.
pause
