@echo off
chcp 65001 > nul
echo ============================================
echo  KoMDViewer - ランタイムインストーラー
echo ============================================
echo.
echo KoMDViewer の実行に必要なランタイムをインストールします。
echo.

:: --- .NET 9.0 Desktop Runtime チェック ---
echo [1/2] .NET 9.0 Desktop Runtime を確認中...
dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 9." > nul
if %errorlevel% == 0 (
    echo       既にインストール済みです。スキップします。
) else (
    echo       未インストールです。ダウンロードしてインストールします...
    echo.
    powershell -Command "Start-Process 'https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-9.0-windows-x64-installer'"
    echo       ブラウザが開きました。インストーラーをダウンロードして実行してください。
    echo       インストール完了後、Enterキーを押して続行してください。
    pause > nul
)

echo.

:: --- Windows App Runtime 2.x チェック ---
echo [2/2] Windows App Runtime 2.x を確認中...

:: PowerShell でチェック結果をファイルに書き出す方式（exitcode問題を回避）
set TMPCHECK=%TEMP%\waruntime_check.txt
powershell -Command "if (Get-AppxPackage -Name 'Microsoft.WindowsAppRuntime.2*' -ErrorAction SilentlyContinue) { 'FOUND' | Out-File '%TMPCHECK%' } else { 'NOTFOUND' | Out-File '%TMPCHECK%' }"

set /p WACHECK=<%TMPCHECK%
del "%TMPCHECK%" 2>nul

echo       検出結果: %WACHECK%

if "%WACHECK%"=="FOUND" (
    echo       既にインストール済みです。スキップします。
    goto runtime_done
)

echo       未インストールです。インストールを開始します...
echo.

:: 同梱の WindowsAppRuntimeInstall.exe があれば使う
if exist "%~dp0WindowsAppRuntimeInstall.exe" (
    echo       同梱インストーラーを実行中...
    "%~dp0WindowsAppRuntimeInstall.exe"
    goto runtime_done
)

:: なければ Web からダウンロードしてインストール
echo       インストーラーをダウンロード中...
set TMPINST=%TEMP%\WindowsAppRuntimeInstall.exe
powershell -Command "Invoke-WebRequest -Uri 'https://aka.ms/windowsappruntimeinstall-x64' -OutFile '%TMPINST%' -UseBasicParsing"
if exist "%TMPINST%" (
    echo       ダウンロード完了。インストールを開始します...
    "%TMPINST%"
    del "%TMPINST%" 2>nul
) else (
    echo       ダウンロードに失敗しました。
    echo       手動でインストールしてください:
    echo       https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads
    pause
    goto end
)

:runtime_done
echo.
echo ============================================
echo  インストール完了
echo ============================================
echo.
echo KoMDViewer.exe を起動できます。
echo.

:end
pause
