# KoMDViewer ランタイムインストーラー
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "KoMDViewer - ランタイムインストーラー"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  KoMDViewer - ランタイムインストーラー" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "KoMDViewer の実行に必要なランタイムをインストールします。"
Write-Host ""

# --- [1/2] .NET 9.0 Desktop Runtime ---
Write-Host "[1/2] .NET 9.0 Desktop Runtime を確認中..." -ForegroundColor Yellow
$dotnetInstalled = $false
try {
    $runtimes = & dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft\.WindowsDesktop\.App 9\.") {
        $dotnetInstalled = $true
    }
} catch {}

if ($dotnetInstalled) {
    Write-Host "      既にインストール済みです。スキップします。" -ForegroundColor Green
} else {
    Write-Host "      未インストールです。ダウンロードページを開きます..." -ForegroundColor Red
    Start-Process "https://dotnet.microsoft.com/download/dotnet/9.0"
    Write-Host "      ブラウザが開きました。「.NET Desktop Runtime 9.0.x」の"
    Write-Host "      Windows x64 インストーラーをダウンロードして実行してください。"
    Write-Host ""
    Read-Host "      インストール完了後、Enterキーを押してください"
}

Write-Host ""

# --- [2/2] Windows App Runtime 2.x ---
Write-Host "[2/2] Windows App Runtime 2.x を確認中..." -ForegroundColor Yellow
$waInstalled = $false
try {
    $pkg = Get-AppxPackage -Name "Microsoft.WindowsAppRuntime.2*" -ErrorAction SilentlyContinue
    if ($pkg) {
        $waInstalled = $true
        Write-Host "      インストール済みバージョン: $($pkg[0].Version)" -ForegroundColor Green
    }
} catch {}

if ($waInstalled) {
    Write-Host "      既にインストール済みです。スキップします。" -ForegroundColor Green
} else {
    Write-Host "      未インストールです。インストールを開始します..." -ForegroundColor Red
    Write-Host ""

    # 同梱インストーラーがあれば使う
    $bundled = Join-Path $PSScriptRoot "WindowsAppRuntimeInstall.exe"
    if (Test-Path $bundled) {
        Write-Host "      同梱インストーラーを実行中..."
        Start-Process $bundled -Wait
    } else {
        # Web からダウンロード
        Write-Host "      インストーラーをダウンロード中..."
        $tmpPath = Join-Path $env:TEMP "WindowsAppRuntimeInstall.exe"
        try {
            Invoke-WebRequest -Uri "https://aka.ms/windowsappruntimeinstall-x64" -OutFile $tmpPath -UseBasicParsing
            Write-Host "      ダウンロード完了。インストールを開始します..." -ForegroundColor Green
            Start-Process $tmpPath -Wait
            Remove-Item $tmpPath -Force -ErrorAction SilentlyContinue
        } catch {
            Write-Host "      ダウンロードに失敗しました: $_" -ForegroundColor Red
            Write-Host "      手動でインストールしてください:"
            Write-Host "      https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads"
            Read-Host "      Enterキーを押して終了"
            exit 1
        }
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  インストール完了" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "KoMDViewer.exe を起動できます。" -ForegroundColor Green
Write-Host ""
Read-Host "Enterキーを押して終了"
