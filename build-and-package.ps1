# KoMDViewer リリースビルドスクリプト
# 2つのパッケージを作成: リリース版、Vector配布用
#
# 注意: dotnet publish では WinUI 3 の .xbf ファイルが出力されない既知問題があるため、
# dotnet build の出力をそのままパッケージングする方式を採用しています。

param(
    [string]$Version = "1.1",
    [switch]$Clean
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

Write-Host "KoMDViewer v$Version Build and Package Script" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""

# 変数定義
$ProjectFile = "KoMDViewer.csproj"
$DistDir = "dist"
$TempReleaseDir = "$DistDir\temp_release"
$TempVectorDir = "$DistDir\temp_vector"
$ReleaseZipFile = "$DistDir\KoMDViewer-v$Version-release.zip"
$VectorZipFile = "$DistDir\KoMDViewer-v$Version-vector.zip"
$BuildOutputDir = "bin\Release\net9.0-windows10.0.19041.0\win-x64"

# ZIP作成ヘルパー関数
function New-ZipFromDirectory {
    param(
        [string]$SourceDir,
        [string]$DestinationZip
    )
    $fullSource = (Resolve-Path $SourceDir).Path
    $fullDest = Join-Path (Get-Location).Path $DestinationZip
    if (Test-Path $fullDest) { Remove-Item $fullDest -Force }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($fullSource, $fullDest)
}

$BuildStartTime = Get-Date

if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean $ProjectFile -c Release 2>$null
    if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force -ErrorAction SilentlyContinue }
}

if (Test-Path $DistDir) {
    Remove-Item $DistDir -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# ========================================
# Step 1: ビルド
# ========================================
Write-Host "Step 1: Building..." -ForegroundColor Yellow
dotnet build $ProjectFile -c Release -r win-x64

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Build completed" -ForegroundColor Green
Write-Host ""

# ========================================
# Step 2: リリース版パッケージング
# ========================================
Write-Host "Step 2: Packaging Release..." -ForegroundColor Yellow
$releaseBuildSuccess = $false
try {
    New-Item -ItemType Directory -Path $TempReleaseDir -Force | Out-Null

    # build出力からコピー（.NETランタイムDLL本体とデバッグファイルのみ除外）
    Get-ChildItem -Path $BuildOutputDir -File | Where-Object {
        $_.Name -notmatch "^(coreclr|clr|hostfxr|hostpolicy|createdump|mscor|msquic)" -and
        $_.Name -notmatch "^System\." -and
        $_.Name -notmatch "^(Microsoft\.CSharp|Microsoft\.VisualBasic|Microsoft\.Win32\.Primitives|Microsoft\.Win32\.Registry|Microsoft\.DiaSymReader|Microsoft\.NETCore)" -and
        $_.Name -notmatch "^(netstandard|WindowsBase)\." -and
        $_.Extension -ne ".pdb"
    } | Copy-Item -Destination $TempReleaseDir

    # Microsoft.UI.Xaml.dll はシステムのWindowsAppRuntimeから読み込まれるため除外
    # （同梱するとシステムDLLと競合してクラッシュする）
    Remove-Item "$TempReleaseDir\Microsoft.UI.Xaml.dll" -ErrorAction SilentlyContinue

    # サブフォルダ（editor, Resources）をコピー
    Get-ChildItem -Path $BuildOutputDir -Directory | Copy-Item -Destination $TempReleaseDir -Recurse

    Copy-Item "README.md" $TempReleaseDir

    # install-runtime.bat / .ps1 を同梱
    foreach ($f in @("install-runtime.bat", "install-runtime.ps1")) {
        if (Test-Path $f) {
            Copy-Item $f $TempReleaseDir
            Write-Host "  ✓ $f included" -ForegroundColor DarkGreen
        }
    }

    Start-Sleep -Seconds 2

    New-ZipFromDirectory -SourceDir $TempReleaseDir -DestinationZip $ReleaseZipFile
    Write-Host "  ✓ Release package completed" -ForegroundColor Green
    $releaseBuildSuccess = $true
}
catch {
    Write-Host "  ✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# ========================================
# Step 3: Vector用パッケージ（リリース版 + Vector用README）
# ========================================
Write-Host "Step 3: Packaging Vector..." -ForegroundColor Yellow
$vectorBuildSuccess = $false
try {
    if ($releaseBuildSuccess) {
        New-Item -ItemType Directory -Path $TempVectorDir -Force | Out-Null
        Copy-Item "$TempReleaseDir\*" $TempVectorDir -Recurse

        if (Test-Path "$TempVectorDir\README.md") { Remove-Item "$TempVectorDir\README.md" -Force }
        Copy-Item "README_VECTOR.md" "$TempVectorDir\README.md"

        New-ZipFromDirectory -SourceDir $TempVectorDir -DestinationZip $VectorZipFile
        Write-Host "  ✓ Vector package completed" -ForegroundColor Green
        $vectorBuildSuccess = $true
    }
    else {
        throw "Release build failed, skipping Vector package."
    }
}
catch {
    Write-Host "  ✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Cleanup
Write-Host "Cleaning up..." -ForegroundColor Yellow
@($TempReleaseDir, $TempVectorDir) | ForEach-Object {
    if (Test-Path $_) { Remove-Item -Path $_ -Recurse -Force -ErrorAction SilentlyContinue }
}
Write-Host "Done" -ForegroundColor Green
Write-Host ""

# Summary
$BuildTimeSeconds = [math]::Round(((Get-Date) - $BuildStartTime).TotalSeconds, 1)

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host ""

if ($releaseBuildSuccess -and (Test-Path $ReleaseZipFile)) {
    $info = Get-Item $ReleaseZipFile
    $hash = (Get-FileHash $ReleaseZipFile -Algorithm SHA256).Hash
    Write-Host "📦 Release:" -ForegroundColor Cyan
    Write-Host "   $($info.Name)  $([math]::Round($info.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "   SHA256: $hash" -ForegroundColor Gray
    Write-Host ""
}

if ($vectorBuildSuccess -and (Test-Path $VectorZipFile)) {
    $info = Get-Item $VectorZipFile
    Write-Host "📦 Vector:" -ForegroundColor Cyan
    Write-Host "   $($info.Name)  $([math]::Round($info.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host ""
}

Write-Host "⏱ Build time: $BuildTimeSeconds seconds" -ForegroundColor White
Write-Host "📁 Output: $DistDir\" -ForegroundColor White
Write-Host ""
