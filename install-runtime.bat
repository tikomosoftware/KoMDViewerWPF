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
    powershell -Command "& { $url = 'https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-9.0-windows-x64-installer'; Start-Process $url }"
    echo       ブラウザが開きました。インストーラーをダウンロードして実行してください。
    echo       インストール完了後、Enterキーを押して続行してください。
    pause > nul
)

echo.

:: --- Windows App Runtime チェック ---
echo [2/2] Windows App Runtime を確認中...
powershell -Command "& { $pkg = Get-AppxPackage -Name 'Microsoft.WindowsAppRuntime.2*' -ErrorAction SilentlyContinue; if ($pkg) { Write-Host '       既にインストール済みです。スキップします。'; exit 0 } else { exit 1 } }"
if %errorlevel% == 0 (
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

:: なければ Web からダウンロード
echo       インストーラーをダウンロード中...
powershell -Command "& { try { $url = 'https://aka.ms/windowsappruntimeinstall-x64'; $out = '%TEMP%\WindowsAppRuntimeInstall.exe'; Invoke-WebRequest -Uri $url -OutFile $out -UseBasicParsing; Start-Process $out -Wait; Write-Host '       インストール完了。' } catch { Write-Host ('       ダウンロード失敗: ' + $_.Exception.Message) } }"

:runtime_done
echo.
echo ============================================
echo  インストール完了
echo ============================================
echo.
echo KoMDViewer.exe を起動できます。
echo.
pause
