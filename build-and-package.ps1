# KoMDViewer デュアルリリースビルドスクリプト
# 3つのビルドを作成: フレームワーク依存版（軽量）、自己完結型版、Vector用

param(
    [string]$Version = "1.0",
    [switch]$Clean
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

Write-Host "KoMDViewer v$Version Dual Build and Package Script" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""

# 変数定義
$ProjectFile = "KoMDViewer.csproj"
$DistDir = "dist"
$TempFrameworkDir = "$DistDir\temp_framework"
$TempStandaloneDir = "$DistDir\temp_standalone"
$FrameworkZipFile = "$DistDir\KoMDViewer-v$Version-framework-dependent-release.zip"
$StandaloneZipFile = "$DistDir\KoMDViewer-v$Version-standalone-release.zip"
$VectorZipFile = "$DistDir\KoMDViewer-v$Version-vector.zip"
$TempVectorDir = "$DistDir\temp_vector"

# ZIP作成ヘルパー関数（ファイルロック回避）
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

# ビルド開始時刻を記録
$BuildStartTime = Get-Date

if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean
    if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
    if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
}

# Create distribution folder
if (Test-Path $DistDir) { 
    Remove-Item $DistDir -Recurse -Force 
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# ========================================
# フレームワーク依存ビルド（軽量版）
# ========================================
Write-Host "Building Framework-Dependent (Lightweight)..." -ForegroundColor Yellow
$frameworkBuildSuccess = $false
try {
    New-Item -ItemType Directory -Path $TempFrameworkDir -Force | Out-Null
    
    Write-Host "  Publishing..." -ForegroundColor Gray
    dotnet publish $ProjectFile `
        -c Release `
        -r win-x64 `
        --self-contained false `
        -o $TempFrameworkDir
    
    if ($LASTEXITCODE -eq 0) {
        # Copy README
        Copy-Item "README.md" $TempFrameworkDir

        # publish後のファイルロック解放を待つ
        Start-Sleep -Seconds 2
        
        # Create ZIP
        New-ZipFromDirectory -SourceDir $TempFrameworkDir -DestinationZip $FrameworkZipFile
        Write-Host "  ✓ Framework-dependent build completed" -ForegroundColor Green
        $frameworkBuildSuccess = $true
    }
    else {
        throw "Build failed!"
    }
}
catch {
    Write-Host "  ✗ Framework-dependent build failed: $($_.Exception.Message)" -ForegroundColor Red
}

# ========================================
# 自己完結型ビルド
# ========================================
Write-Host ""
Write-Host "Building Self-Contained..." -ForegroundColor Yellow
$standaloneBuildSuccess = $false
try {
    New-Item -ItemType Directory -Path $TempStandaloneDir -Force | Out-Null
    
    Write-Host "  Publishing..." -ForegroundColor Gray
    dotnet publish $ProjectFile `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -o $TempStandaloneDir
    
    if ($LASTEXITCODE -eq 0) {
        # Copy README
        Copy-Item "README.md" $TempStandaloneDir

        # publish後のファイルロック解放を待つ
        Start-Sleep -Seconds 2
        
        # Create ZIP
        New-ZipFromDirectory -SourceDir $TempStandaloneDir -DestinationZip $StandaloneZipFile
        Write-Host "  ✓ Self-contained build completed" -ForegroundColor Green
        $standaloneBuildSuccess = $true
    }
    else {
        throw "Publish failed!"
    }
}
catch {
    Write-Host "  ✗ Self-contained build failed: $($_.Exception.Message)" -ForegroundColor Red
}

# 両方のビルドが失敗した場合はエラー終了
if (-not $frameworkBuildSuccess -and -not $standaloneBuildSuccess) {
    Write-Host "Both builds failed!" -ForegroundColor Red
    exit 1
}

# ========================================
# Vector用パッケージ（自己完結型 + Vector用README）
# ========================================
Write-Host ""
Write-Host "Building Vector Package..." -ForegroundColor Yellow
$vectorBuildSuccess = $false
try {
    if ($standaloneBuildSuccess) {
        New-Item -ItemType Directory -Path $TempVectorDir -Force | Out-Null
        
        # 自己完結型フォルダの内容をコピー
        Copy-Item "$TempStandaloneDir\*" $TempVectorDir -Recurse
        
        # README.mdをVector用に差し替え
        if (Test-Path "$TempVectorDir\README.md") { Remove-Item "$TempVectorDir\README.md" -Force }
        Copy-Item "README_VECTOR.md" "$TempVectorDir\README.md"
        
        # Create ZIP
        New-ZipFromDirectory -SourceDir $TempVectorDir -DestinationZip $VectorZipFile
        Write-Host "  ✓ Vector package completed" -ForegroundColor Green
        $vectorBuildSuccess = $true
    }
    else {
        throw "Standalone build failed, skipping Vector package."
    }
}
catch {
    Write-Host "  ✗ Vector package failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Cleanup temporary directories
Write-Host ""
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
if (Test-Path $TempFrameworkDir) {
    Remove-Item -Path $TempFrameworkDir -Recurse -Force
}
if (Test-Path $TempStandaloneDir) {
    Remove-Item -Path $TempStandaloneDir -Recurse -Force
}
if (Test-Path $TempVectorDir) {
    Remove-Item -Path $TempVectorDir -Recurse -Force
}
Write-Host "Cleanup completed" -ForegroundColor Green
Write-Host ""

# ビルド結果のサマリー表示
$BuildEndTime = Get-Date
$BuildDuration = $BuildEndTime - $BuildStartTime
$BuildTimeSeconds = [math]::Round($BuildDuration.TotalSeconds, 1)

Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host ""

# フレームワーク依存ビルドの情報
if ($frameworkBuildSuccess -and (Test-Path $FrameworkZipFile)) {
    $frameworkZipInfo = Get-Item $FrameworkZipFile
    $frameworkZipHash = Get-FileHash $FrameworkZipFile -Algorithm SHA256
    
    Write-Host "📦 Framework-Dependent Build (Lightweight):" -ForegroundColor Cyan
    Write-Host "   File: $($frameworkZipInfo.Name)" -ForegroundColor White
    Write-Host "   Size: $([math]::Round($frameworkZipInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "   SHA256: $($frameworkZipHash.Hash)" -ForegroundColor Gray
    Write-Host "   ⚠ Requires .NET 9.0 Desktop Runtime" -ForegroundColor Yellow
    Write-Host ""
}

# 自己完結型ビルドの情報
if ($standaloneBuildSuccess -and (Test-Path $StandaloneZipFile)) {
    $standaloneZipInfo = Get-Item $StandaloneZipFile
    $standaloneZipHash = Get-FileHash $StandaloneZipFile -Algorithm SHA256
    
    Write-Host "📦 Self-Contained Build:" -ForegroundColor Cyan
    Write-Host "   File: $($standaloneZipInfo.Name)" -ForegroundColor White
    Write-Host "   Size: $([math]::Round($standaloneZipInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "   SHA256: $($standaloneZipHash.Hash)" -ForegroundColor Gray
    Write-Host "   ✓ No .NET Runtime installation required" -ForegroundColor Green
    Write-Host ""
}

# Vectorパッケージの情報
if ($vectorBuildSuccess -and (Test-Path $VectorZipFile)) {
    $vectorZipInfo = Get-Item $VectorZipFile
    
    Write-Host "📦 Vector Package (Self-Contained + Vector README):" -ForegroundColor Cyan
    Write-Host "   File: $($vectorZipInfo.Name)" -ForegroundColor White
    Write-Host "   Size: $([math]::Round($vectorZipInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "   ✓ Ready for Vector distribution" -ForegroundColor Green
    Write-Host ""
}

Write-Host "⏱ Total build time: $BuildTimeSeconds seconds" -ForegroundColor White
Write-Host "Package is located at: $DistDir\" -ForegroundColor White
Write-Host ""
