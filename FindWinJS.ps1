<#
.SYNOPSIS
    查找 HTML 文件中引用了 "winjs" 的 link 或 script 标签行。

.DESCRIPTION
    递归搜索指定目录下的所有 .html / .htm 文件，输出包含 "winjs" 字符串（不区分大小写）
    的 <link href="..."> 或 <script src="..."> 标签所在的行。

.PARAMETER Path
    要搜索的文件夹路径。默认为当前目录。

.EXAMPLE
    .\Find-WinJSElements.ps1 -Path "C:\Projects\Web"

.EXAMPLE
    .\Find-WinJSElements.ps1
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$Path = "."
)

# 检查路径是否存在
if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
    Write-Error "指定的路径不存在或不是文件夹: $Path"
    exit 1
}

# 正则表达式：匹配包含 "winjs" 的 link href 或 script src 标签
# 使用 (?i) 表示不区分大小写
$pattern = '(?i)<(link[^>]*?href\s*=\s*["''][^"'']*winjs[^"'']*["''][^>]*?)>|(script[^>]*?src\s*=\s*["''][^"'']*winjs[^"'']*["''][^>]*?)>'

Write-Host "正在搜索文件夹: $((Resolve-Path -LiteralPath $Path).Path)" -ForegroundColor Cyan
Write-Host "匹配模式: 包含 'winjs' 的 <link href> 或 <script src> 标签`n"

# 获取所有 .html 和 .htm 文件（不区分大小写）
$files = Get-ChildItem -LiteralPath $Path -Recurse -Include "*.html","*.htm" -File

if ($files.Count -eq 0) {
    Write-Host "未找到 HTML 文件。" -ForegroundColor Yellow
    exit
}

$foundAny = $false

foreach ($file in $files) {
    # 使用 Select-String 按行搜索，-CaseSensitive:$false 已隐含在 (?i) 中
    $matches = Select-String -LiteralPath $file.FullName -Pattern $pattern -AllMatches

    if ($matches) {
        $foundAny = $true
        Write-Host "`n文件: $($file.FullName)" -ForegroundColor Green
        foreach ($match in $matches) {
            # 输出行号和行内容（去除首尾空白）
            $line = $match.Line.Trim()
            Write-Host "  行 $($match.LineNumber): $line"
        }
    }
}

if (-not $foundAny) {
    Write-Host "`n未找到包含 'winjs' 的 link/script 元素。" -ForegroundColor Yellow
} else {
    Write-Host "`n搜索完成。" -ForegroundColor Cyan
}