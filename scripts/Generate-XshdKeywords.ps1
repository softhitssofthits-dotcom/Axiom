param(
    [string]$ProjectRoot = "$PSScriptRoot\.."
)

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

$keywordXml = ($keywords | ForEach-Object { "            <Word>$_</Word>" }) -join "`n"
$typeXml = ($types | ForEach-Object { "            <Word>$_</Word>" }) -join "`n"

Write-Host "Keywords ($($keywords.Count)):" -ForegroundColor Cyan
$keywords | ForEach-Object { Write-Host "  $_" }

Write-Host "`nTypes ($($types.Count)):" -ForegroundColor Cyan
$types | ForEach-Object { Write-Host "  $_" }

Write-Host "`nGenerated XSHD Keyword section:" -ForegroundColor Green
Write-Host $keywordXml

Write-Host "`nGenerated XSHD TypeName section:" -ForegroundColor Green
Write-Host $typeXml

Write-Host "`nTo update XSHD files, replace the <Keywords> and <Keywords color=`"TypeName`"> sections." -ForegroundColor Yellow
