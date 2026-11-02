param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$out = Join-Path $root "publish"

if (Test-Path $out) { Remove-Item -Recurse -Force $out }

dotnet publish (Join-Path $root "AceHeadsetFix.csproj") `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained `
    /p:PublishSingleFile=true `
    /p:EnableCompressionInSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:DebugType=none `
    --output $out
if ($LASTEXITCODE -ne 0) { throw "publish failed" }

Write-Host ""
Write-Host "Published to $out"
Get-ChildItem $out | ForEach-Object { Write-Host ("  {0}  {1:N1} MB" -f $_.Name, ($_.Length / 1MB)) }
