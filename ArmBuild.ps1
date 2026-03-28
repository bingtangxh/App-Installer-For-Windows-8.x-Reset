# ArmBuild.ps1
$ErrorActionPreference = "Stop"

$root    = Split-Path -Parent $MyInvocation.MyCommand.Path
$staging = Join-Path $root "_staging"
$outZip  = Join-Path $root "ArmPackage.zip"
$packageFolderName = "Desktop App Installer"
$destRoot = Join-Path $staging $packageFolderName

function Copy-Binaries-ToDest($src, $dest)
{
    if (!(Test-Path $src)) { return }

    Get-ChildItem $src -File | Where-Object {
        $_.Extension -in ".exe", ".dll"
    } | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $dest -Force
    }
}

Write-Host "Preparing staging..."
# 清理旧的 staging
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item $destRoot -ItemType Directory | Out-Null

# ① 先放 Release
Copy-Binaries-ToDest (Join-Path $root "Release") $destRoot

# ② 再放 ARM\Release（覆盖同名）
Copy-Binaries-ToDest (Join-Path $root "ARM\Release") $destRoot

# ③ shared 全部铺到 Desktop App Installer 里
$shared = Join-Path $root "shared"
if (Test-Path $shared) {
    Get-ChildItem $shared -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Substring($shared.Length).TrimStart('\')
        $destinationPath = Join-Path $destRoot $relativePath

        if ($_.PSIsContainer) {
            if (!(Test-Path $destinationPath)) {
                New-Item -Path $destinationPath -ItemType Directory | Out-Null
            }
        } else {
            $parentDir = Split-Path $destinationPath -Parent
            if (!(Test-Path $parentDir)) {
                New-Item -Path $parentDir -ItemType Directory | Out-Null
            }
            Copy-Item -Path $_.FullName -Destination $destinationPath -Force
        }
    }
}

Write-Host "Creating ZIP..."
if (Test-Path $outZip) { Remove-Item $outZip -Force }

# 压缩 _staging 下的 Desktop App Installer 文件夹
Compress-Archive -Path $destRoot -DestinationPath $outZip -CompressionLevel Optimal

# 清理 staging
Remove-Item $staging -Recurse -Force

Write-Host "Done: $outZip"