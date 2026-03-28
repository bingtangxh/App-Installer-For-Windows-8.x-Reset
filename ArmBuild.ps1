$ErrorActionPreference = "Stop"

$root    = Split-Path -Parent $MyInvocation.MyCommand.Path
$staging = Join-Path $root "_staging"
$outZip  = Join-Path $root "package.zip"

function Copy-Binaries-ToRoot($src)
{
    if (!(Test-Path $src)) { return }

    Get-ChildItem $src -File | Where-Object {
        $_.Extension -in ".exe", ".dll"
    } | ForEach-Object {
        Copy-Item $_.FullName $staging -Force
    }
}

Write-Host "Preparing staging..."
Remove-Item $staging -Recurse -Force -ErrorAction Ignore
New-Item $staging -ItemType Directory | Out-Null

# ① 先放 Release
Copy-Binaries-ToRoot (Join-Path $root "Release")

# ② 再放 ARM\Release（覆盖同名）
Copy-Binaries-ToRoot (Join-Path $root "ARM\Release")

# ③ shared 全部铺到根目录
$shared = Join-Path $root "shared"
if (Test-Path $shared) {
    Get-ChildItem $shared -Recurse | ForEach-Object {
        $rel = $_.FullName.Substring($shared.Length).TrimStart('\')
        $dest = Join-Path $staging $rel

        if ($_.PSIsContainer) {
            New-Item $dest -ItemType Directory -Force | Out-Null
        } else {
            Copy-Item $_.FullName $dest -Force
        }
    }
}

Write-Host "Creating ZIP..."
if (Test-Path $outZip) { Remove-Item $outZip -Force }

Compress-Archive `
    -Path (Join-Path $staging "*") `
    -DestinationPath $outZip `
    -CompressionLevel Fastest

Remove-Item $staging -Recurse -Force

Write-Host "Done: $outZip"