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
            title              = "Desktop App Installer Setup"
            confirm            = "Do you want to install Desktop App Installer? (y/n)"
            invalid            = "Invalid input. Please enter y or n."
            cancelled          = "Installation cancelled."
            arch_error         = "Error: Cannot continue installation because the computer's processor architecture is not ARM."
            creating_shortcuts = "Creating shortcuts in Start Screen..."
            registering        = "Registering..."
            complete           = "Installation complete!"
        }
        zh = @{
            title              = "Desktop App Installer 安装程序"
            confirm            = "是否安装 Desktop App Installer？(y/n)"
            invalid            = "无效输入，请输入 y 或 n。"
            cancelled          = "安装已取消。"
            arch_error         = "错误：无法继续安装，因为计算机的处理器架构不是 ARM。"
            creating_shortcuts = "正在创建开始屏幕快捷方式..."
            registering        = "正在注册..."
            complete           = "安装完成！"
        }
    }

    $msg = $messages[$lang]

    # 函数定义
    function Create-Shortcut {
        param(
            [string]$LnkPath,
            [string]$TargetPath,
            [string]$AppId
        )
        $toolShortcutPath = Join-Path $PSScriptRoot "shortcut.exe"
        if (-not (Test-Path $toolShortcutPath)) {
            throw "Error: cannot find file 'shortcut.exe' in folder '$PSScriptRoot'"
        }
        & $toolShortcutPath $LnkPath $TargetPath $AppId
        return $LASTEXITCODE
    }

    function Set-DesktopInit {
        param(
            [string]$IniPath,
            [string]$Section,
            [string]$Key,
            [string]$Value
        )
        $toolDesktopIniPath = Join-Path $PSScriptRoot "desktopini.exe"
        if (-not (Test-Path $toolDesktopIniPath)) {
            throw "Error: cannot find file 'desktopini.exe' in folder '$PSScriptRoot'"
        }
        & $toolDesktopIniPath $IniPath $Section $Key $Value
        return $LASTEXITCODE
    }

    # 检查处理器架构
    if ($env:PROCESSOR_ARCHITECTURE.Trim() -ne "ARM") {
        throw $msg.arch_error
    }

    # 确认安装
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

    # 安装目录和快捷方式目录
    $appStartMenuFolder = "Desktop App Installer"
    $appPublicStartMenuFolder = [System.IO.Path]::Combine($env:ProgramData, "Microsoft\Windows\Start Menu\Programs")
    $startitemfolder = [System.IO.Path]::Combine($appPublicStartMenuFolder, $appStartMenuFolder)
    $AppFolder = $PSScriptRoot

    Write-Output $msg.creating_shortcuts
    if (-not (Test-Path $startitemfolder)) {
        New-Item -ItemType Directory -Path $startitemfolder -Force | Out-Null
    }

    $shortcuts = @(
        @{
            LnkPath     = Join-Path $startitemfolder "App Installer.lnk"
            TargetPath  = Join-Path $AppFolder "appinstaller.exe"
            AppId       = "Microsoft.DesktopAppInstaller!App"
        },
        @{
            LnkPath     = Join-Path $startitemfolder "Settings.lnk"
            TargetPath  = Join-Path $AppFolder "settings.exe"
            AppId       = "WindowsModern.PracticalToolsProject!Settings"
        },
        @{
            LnkPath     = Join-Path $startitemfolder "Package Manager.lnk"
            TargetPath  = Join-Path $AppFolder "Manager.exe"
            AppId       = "WindowsModern.PracticalToolsProject!Manager"
        },
        @{
            LnkPath     = Join-Path $startitemfolder "Update.lnk"
            TargetPath  = Join-Path $AppFolder "Update.exe"
            AppId       = "WindowsModern.PracticalToolsProject!Update"
        },
        @{
            LnkPath     = Join-Path $startitemfolder "Package Reader.lnk"
            TargetPath  = Join-Path $AppFolder "Reader.exe"
            AppId       = "WindowsModern.PracticalToolsProject!Reader"
        }
    )

    foreach ($item in $shortcuts) {
        $exitCode = Create-Shortcut -LnkPath $item.LnkPath -TargetPath $item.TargetPath -AppId $item.AppId
    }

    # $desktopini = Join-Path $startitemfolder "desktop.ini"
    $desktopini = $startitemfolder
    Set-DesktopInit -IniPath $desktopini -Section ".ShellClassInfo" -Key "ConfirmFileOp" -Value 0
    Set-DesktopInit -IniPath $desktopini -Section "LocalizedFileNames" -Key "App Installer.lnk" -Value "@$AppFolder\appinstaller.exe,-300"
    Set-DesktopInit -IniPath $desktopini -Section "LocalizedFileNames" -Key "Settings.lnk" -Value "@$AppFolder\settings.exe,-200"
    Set-DesktopInit -IniPath $desktopini -Section "LocalizedFileNames" -Key "Update.lnk" -Value "@$AppFolder\reslib.dll,-103"
    Set-DesktopInit -IniPath $desktopini -Section "LocalizedFileNames" -Key "Package Manager.lnk" -Value "@$AppFolder\reslib.dll,-228"
    Set-DesktopInit -IniPath $desktopini -Section "LocalizedFileNames" -Key "Uninstall.lnk" -Value "@$AppFolder\reslib.dll,-131"
    Set-DesktopInit -IniPath $desktopini -Section ".ShellClassInfo" -Key "LocalizedResourceName" -Value "@$AppFolder\appinstaller.exe,-300"

    Write-Output $msg.registering
    $reg = [Microsoft.Win32.Registry]::ClassesRoot
    $key = $reg.CreateSubKey("Microsoft.DesktopAppInstaller")
    $key.SetValue("", "Windows Store App Package")
    $key.Close()
    $subKey = $reg.CreateSubKey("Microsoft.DesktopAppInstaller\Shell\Open\Command")
    $subKey.SetValue("", "`"$AppFolder\appinstaller.exe`" `"%1`"")
    $subKey.Close()
    $subKey = $reg.CreateSubKey("Microsoft.DesktopAppInstaller\DefaultIcon")
    $subKey.SetValue("", "$AppFolder\appinstaller.exe,2")
    $subKey.Close()
    $subKey = $reg.CreateSubKey("Applications\AppInstaller.exe\DefaultIcon")
    $subKey.SetValue("", "$AppFolder\appinstaller.exe,-136")
    $subKey.Close()
    $subKey = $reg.CreateSubKey(".appx")
    $subKey.SetValue("", "Microsoft.DesktopAppInstaller")
    $subKey.Close()
    $subKey = $reg.CreateSubKey(".appxbundle")
    $subKey.SetValue("", "Microsoft.DesktopAppInstaller")
    $subKey.Close()

    Write-Output ""
    Write-Output $msg.complete
    Start-Sleep -Seconds 5
} finally {
    Pop-Location
}