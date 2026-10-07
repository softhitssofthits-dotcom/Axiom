param(
    [string]$ProjectRoot = "$PSScriptRoot\..",
    # -Write: заменить секции Keywords/TypeName/BuiltIn в обоих xshd-файлах.
    # Без -Write скрипт только печатает сгенерированные секции.
    [switch]$Write
)

$ErrorActionPreference = "Stop"

$keywordsPath = Join-Path $ProjectRoot "src\Axiom.Core\AxiomLanguageKeywords.cs"
$xshdLightPath = Join-Path $ProjectRoot "src\Axiom.Ide\Editor\Highlighting\Axiom.xshd"
$xshdDarkPath = Join-Path $ProjectRoot "src\Axiom.Ide\Editor\Highlighting\Axiom.Dark.xshd"

$content = Get-Content $keywordsPath -Raw

$keywordMatch = [regex]::Match($content, 'Keywords\s*=\s*new\s*HashSet<string>.*?\{([^}]+)\}', [System.Text.RegularExpressions.RegexOptions]::Singleline)
$typeMatch = [regex]::Match($content, 'TypeNames\s*=\s*new\s*HashSet<string>.*?\{([^}]+)\}', [System.Text.RegularExpressions.RegexOptions]::Singleline)

if (-not $keywordMatch.Success -or -not $typeMatch.Success) {
    Write-Error "Could not parse AxiomLanguageKeywords.cs"
    exit 1
}

$keywords = ($keywordMatch.Groups[1].Value -replace '"', '' -replace '\s+', ' ').Trim().Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Sort-Object
$types = ($typeMatch.Groups[1].Value -replace '"', '' -replace '\s+', ' ').Trim().Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Sort-Object

# Источник builtin-имён — реестр BuiltinFunctions через `axiom metadata`:
# подсветка не может разойтись с реестром, если генерируется из него (аудит AXD-016).
$metadataJson = dotnet run --project (Join-Path $ProjectRoot "src\Axiom.Cli") -- metadata
if ($LASTEXITCODE -ne 0) {
    Write-Error "axiom metadata failed"
    exit 1
}

$metadata = $metadataJson | ConvertFrom-Json
$builtins = $metadata.builtins |
    ForEach-Object { $_.name.ToUpperInvariant() } |
    Sort-Object -Unique

$keywordXml = ($keywords | ForEach-Object { "            <Word>$_</Word>" }) -join "`n"
$typeXml = ($types | ForEach-Object { "            <Word>$_</Word>" }) -join "`n"
$builtinXml = ($builtins | ForEach-Object { "            <Word>$_</Word>" }) -join "`n"

Write-Host "Keywords ($($keywords.Count)):" -ForegroundColor Cyan
Write-Host "TypeNames ($($types.Count)):" -ForegroundColor Cyan
Write-Host "BuiltIns ($($builtins.Count)):" -ForegroundColor Cyan

function Update-Xshd {
    param([string]$Path, [string]$KeywordXml, [string]$TypeXml, [string]$BuiltinXml, [bool]$Apply)

    $xshd = Get-Content $Path -Raw
    $sections = @(
        @{ Pattern = '(?s)<Keywords color="Keyword">\s*(?:<Word>[^<]+</Word>\s*)+</Keywords>'; Name = 'Keyword'; Xml = $KeywordXml },
        @{ Pattern = '(?s)<Keywords color="TypeName">\s*(?:<Word>[^<]+</Word>\s*)+</Keywords>'; Name = 'TypeName'; Xml = $TypeXml },
        @{ Pattern = '(?s)<Keywords color="BuiltIn">\s*(?:<Word>[^<]+</Word>\s*)+</Keywords>'; Name = 'BuiltIn'; Xml = $BuiltinXml }
    )

    foreach ($section in $sections) {
        $match = [regex]::Match($xshd, $section.Pattern)
        if (-not $match.Success) {
            Write-Error "Section $($section.Name) not found in $Path"
            exit 1
        }

        $opening = $match.Value.Substring(0, $match.Value.IndexOf('>') + 1)
        $replacement = $opening + "`n" + $section.Xml + "`n        </Keywords>"
        if ($Apply) {
            $xshd = $xshd.Remove($match.Index, $match.Length).Insert($match.Index, $replacement)
        }
    }

    if ($Apply) {
        # Сохраняем перевод строки в стиле исходного файла.
        $hasCrlf = $xshd.Contains("`r`n")
        if (-not $hasCrlf) {
            $xshd = $xshd.Replace("`r`n", "`n")
        }

        [System.IO.File]::WriteAllText($Path, $xshd)
        Write-Host "Updated $Path" -ForegroundColor Green
    }
}

Update-Xshd -Path $xshdLightPath -KeywordXml $keywordXml -TypeXml $typeXml -BuiltinXml $builtinXml -Apply:$Write.IsPresent
Update-Xshd -Path $xshdDarkPath -KeywordXml $keywordXml -TypeXml $typeXml -BuiltinXml $builtinXml -Apply:$Write.IsPresent

if (-not $Write.IsPresent) {
    Write-Host "`nGenerated XSHD Keyword section:" -ForegroundColor Green
    Write-Host $keywordXml

    Write-Host "`nGenerated XSHD TypeName section:" -ForegroundColor Green
    Write-Host $typeXml

    Write-Host "`nGenerated XSHD BuiltIn section:" -ForegroundColor Green
    Write-Host $builtinXml

    Write-Host "`nTo update XSHD files, rerun with -Write." -ForegroundColor Yellow
}
