#Requires -RunAsAdministrator

Push-Location $PSScriptRoot
try {
    # 语言检测函数
    function Get-Language {
        $lang = [System.Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName
        if ($lang -eq "zh") { return "zh" }
        return "en"
    }

    $lang = Get-Language

    # 本地化消息
    $messages = @{
        en = @{
            title          = "Uninstall Desktop App Installer"
            confirm        = "Are you sure you want to uninstall Desktop App Installer? (y/n)"
            invalid        = "Invalid input. Please enter y or n."
            removing_start = "Removing Start Menu folder..."
            removing_reg   = "Removing registry entries..."
            removing_dir   = "Removing installation directory..."
            confirm_dir    = "Do you also want to delete the installation directory? (y/n)"
            complete       = "Uninstallation complete."
            error          = "Error: "
            cancelled      = "Uninstall cancelled."
        }
        zh = @{
            title          = "卸载 Desktop App Installer"
            confirm        = "确定要卸载 Desktop App Installer 吗？(y/n)"
            invalid        = "无效输入，请输入 y 或 n。"
            removing_start = "正在删除开始菜单文件夹..."
            removing_reg   = "正在删除注册表项..."
            removing_dir   = "正在删除安装目录..."
            confirm_dir    = "是否同时删除安装目录？(y/n)"
            complete       = "卸载完成。"
            error          = "错误："
            cancelled      = "卸载已取消。"
        }
    }

    $msg = $messages[$lang]

    if ($env:PROCESSOR_ARCHITECTURE.Trim() -ne "ARM") {
        throw "Error: Cannot continue installation because the computer's processor architecture is not ARM."
    }

    # 确认卸载（控制台交互）
    do {
        $response = Read-Host $msg.confirm
        $response = $response.ToLower()
        if ($response -eq 'y') {
            $confirmed = $true
            break
        } elseif ($response -eq 'n') {
            Write-Host $msg.cancelled
            exit 0
        } else {
            Write-Host $msg.invalid
        }
    } while ($true)

    # 定义路径
    $appStartMenuFolder = "Desktop App Installer"
    $appPublicStartMenuFolder = [System.IO.Path]::Combine($env:ProgramData, "Microsoft\Windows\Start Menu\Programs")
    $startitemfolder = [System.IO.Path]::Combine($appPublicStartMenuFolder, $appStartMenuFolder)
    $AppFolder = $PSScriptRoot

    # 删除开始菜单文件夹
    Write-Host $msg.removing_start
    if (Test-Path $startitemfolder) {
        Remove-Item -Path $startitemfolder -Recurse -Force -ErrorAction SilentlyContinue
        if (Test-Path $startitemfolder) {
            Write-Warning "$msg.error Could not remove $startitemfolder"
        } else {
            Write-Output "Removed $startitemfolder"
        }
    } else {
        Write-Output "Start Menu folder not found, skipping."
    }

    # 删除注册表项
    Write-Host $msg.removing_reg
    $reg = [Microsoft.Win32.Registry]::ClassesRoot

    $keysToDelete = @(
        "Microsoft.DesktopAppInstaller",
        ".appx",
        ".appxbundle"
    )
    foreach ($keyName in $keysToDelete) {
        try {
            if ($reg.OpenSubKey($keyName) -ne $null) {
                $reg.DeleteSubKeyTree($keyName)
                Write-Output "Removed registry key: HKCR\$keyName"
            } else {
                Write-Output "Registry key not found: HKCR\$keyName"
            }
        } catch {
            Write-Warning "$msg.error Could not remove HKCR\$keyName : $_"
        }
    }

    # 删除 Applications\AppInstaller.exe 下的 DefaultIcon
    $appPath = "Applications\AppInstaller.exe"
    try {
        $appKey = $reg.OpenSubKey($appPath, $true)
        if ($appKey) {
            $subKeyNames = $appKey.GetSubKeyNames()
            if ($subKeyNames -contains "DefaultIcon") {
                $appKey.DeleteSubKey("DefaultIcon")
                Write-Output "Removed registry key: HKCR\$appPath\DefaultIcon"
            }
            $appKey.Close()
        } else {
            Write-Output "Registry key not found: HKCR\$appPath"
        }
    } catch {
        Write-Warning "$msg.error Could not process HKCR\$appPath : $_"
    }

    # 删除安装目录
    Write-Host $msg.removing_dir
    do {
        $respDir = Read-Host $msg.confirm_dir
        $respDir = $respDir.ToLower()
        if ($respDir -eq 'y') {
            if (Test-Path $AppFolder) {
                Remove-Item -Path $AppFolder -Recurse -Force -ErrorAction SilentlyContinue
                if (Test-Path $AppFolder) {
                    Write-Warning "$msg.error Could not delete $AppFolder"
                } else {
                    Write-Output "Deleted installation directory."
                }
            } else {
                Write-Output "Installation directory not found."
            }
            break
        } elseif ($respDir -eq 'n') {
            Write-Output "Skipped deleting installation directory."
            break
        } else {
            Write-Host $msg.invalid
        }
    } while ($true)

    Write-Host $msg.complete
    Start-Sleep -Seconds 3
} catch {
    Write-Error "Uninstall failed: $_"
} finally {
    Pop-Location
}