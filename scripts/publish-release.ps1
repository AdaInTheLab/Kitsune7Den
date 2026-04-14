# Kitsune7Den release build helper
#
# Builds a single-file, self-contained Release exe and generates a SHA-256
# checksum alongside it. Run from the repo root:
#
#   .\scripts\publish-release.ps1
#
# Outputs:
#   publish\Kitsune7Den.exe
#   publish\Kitsune7Den.exe.sha256

[CmdletBinding()]
param(
    [string]$OutputDir = "publish"
)

$ErrorActionPreference = "Stop"

# Kill any running instance so the build can overwrite the exe
Get-Process Kitsune7Den -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Clean any previous publish output
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}

Write-Host "Building Release exe..." -ForegroundColor Cyan
dotnet publish src/Kitsune7Den `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir

$exePath = Join-Path $OutputDir "Kitsune7Den.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "Build did not produce $exePath"
    exit 1
}

# Generate SHA-256 alongside the exe
Write-Host "Generating SHA-256..." -ForegroundColor Cyan
$hash = (Get-FileHash $exePath -Algorithm SHA256).Hash.ToLower()
$shaFile = "$exePath.sha256"
"$hash  Kitsune7Den.exe" | Out-File $shaFile -Encoding ASCII -NoNewline
Add-Content $shaFile "`n"

# Report
$size = (Get-Item $exePath).Length
$sizeMB = [math]::Round($size / 1MB, 1)
Write-Host ""
Write-Host "Build complete" -ForegroundColor Green
Write-Host "  $exePath ($sizeMB MB)"
Write-Host "  $shaFile"
Write-Host "  SHA256: $hash"
