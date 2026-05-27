# KoMDViewer WPF runtime checker
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "KoMDViewer WPF - Runtime Checker"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  KoMDViewer WPF - Runtime Checker" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$needsAction = $false

Write-Host "[1/2] Checking .NET 9 Desktop Runtime..." -ForegroundColor Yellow
$dotnetInstalled = $false
try {
    $runtimes = & dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft\.WindowsDesktop\.App 9\.") {
        $dotnetInstalled = $true
    }
} catch {
}

if ($dotnetInstalled) {
    Write-Host "      OK: .NET 9 Desktop Runtime is installed." -ForegroundColor Green
} else {
    $needsAction = $true
    Write-Host "      Missing: .NET 9 Desktop Runtime." -ForegroundColor Red
    Start-Process "https://dotnet.microsoft.com/download/dotnet/9.0"
    Write-Host "      Install '.NET Desktop Runtime 9.0.x' for Windows x64."
}

Write-Host ""
Write-Host "[2/2] Checking Microsoft Edge WebView2 Runtime..." -ForegroundColor Yellow
$webView2Installed = $false
foreach ($key in @(
    "HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
    "HKCU:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
)) {
    if (Test-Path $key) {
        $webView2Installed = $true
        break
    }
}

if ($webView2Installed) {
    Write-Host "      OK: WebView2 Runtime is installed." -ForegroundColor Green
} else {
    $needsAction = $true
    Write-Host "      Missing or not detected: WebView2 Runtime." -ForegroundColor Yellow
    Start-Process "https://developer.microsoft.com/microsoft-edge/webview2/"
    Write-Host "      Install the Evergreen WebView2 Runtime if the preview pane does not work."
}

Write-Host ""
if ($needsAction) {
    Write-Host "Please install the missing runtime(s), then start KoMDViewerWPF.exe." -ForegroundColor Yellow
} else {
    Write-Host "All required runtimes were found. You can start KoMDViewerWPF.exe." -ForegroundColor Green
}

Write-Host ""
Read-Host "Press Enter to exit"
