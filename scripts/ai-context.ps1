[CmdletBinding()]
param(
    [Parameter()]
    [string[]]$Topic,

    [Parameter()]
    [int]$ContextLines = 2,

    [Parameter()]
    [int]$MatchesPerFile = 3,

    [Parameter()]
    [switch]$StdoutOnly,

    [Parameter()]
    [string]$OutFile = ".artifacts/ai-context/session-brief.md"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$projectFiles = [ordered]@{
    "PROJECT" = (Join-Path $repoRoot "PROJECT.md")
    "README" = (Join-Path $repoRoot "README.md")
    "SPECIFICATION3" = (Join-Path $repoRoot "docs/SPECIFICATION3.md")
    "ARCHITECTURE" = (Join-Path $repoRoot "docs/ARCHITECTURE.md")
    "PLANS4" = (Join-Path $repoRoot "docs/PLANS4.md")
    "WORKLOG5" = (Join-Path $repoRoot "docs/WORKLOG5.md")
    "BYTECODE_COVERAGE" = (Join-Path $repoRoot "docs/BYTECODE_COVERAGE.md")
    "BYTECODE_PERF_MATRIX" = (Join-Path $repoRoot "docs/BYTECODE_PERF_MATRIX.md")
    "VERSIONS" = (Join-Path $repoRoot "versions.json")
    "VERSIONING" = (Join-Path $repoRoot "docs/VERSIONING.md")
    "CHANGELOG" = (Join-Path $repoRoot "docs/CHANGELOG.md")
}

function Get-FileText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Get-Content -Raw -Encoding UTF8 $Path
}

function Get-LastMarkdownSection {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,

        [Parameter(Mandatory = $true)]
        [string]$HeadingPrefixRegex
    )

    $lines = $Text -split "`r?`n"
    $indices = New-Object System.Collections.Generic.List[int]

    for ($index = 0; $index -lt $lines.Length; $index++) {
        if ($lines[$index] -match $HeadingPrefixRegex) {
            $indices.Add($index)
        }
    }

    if ($indices.Count -eq 0) {
        return $null
    }

    $start = $indices[$indices.Count - 1]
    $end = $lines.Length - 1

    for ($index = $start + 1; $index -lt $lines.Length; $index++) {
        if ($lines[$index] -match '^##\s+') {
            $end = $index - 1
            break
        }
    }

    (($lines[$start..$end] -join "`n").Trim())
}

function Get-IndentedBlockAfterLabel {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Section,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $lines = $Section -split "`r?`n"

    for ($index = 0; $index -lt $lines.Length; $index++) {
        if ($lines[$index].Trim() -ne $Label) {
            continue
        }

        $result = New-Object System.Collections.Generic.List[string]

        for ($cursor = $index + 1; $cursor -lt $lines.Length; $cursor++) {
            $line = $lines[$cursor]

            if ([string]::IsNullOrWhiteSpace($line)) {
                if ($result.Count -gt 0) {
                    break
                }

                continue
            }

            if ($line -match '^##\s+') {
                break
            }

            if ($line -match '^[A-Za-zА-Яа-я0-9].*:$' -and $result.Count -gt 0) {
                break
            }

            if ($line -match '^\s*-\s+' -or $line -match '^\s*\d+\.\s+') {
                $result.Add($line.TrimEnd())
                continue
            }

            if ($result.Count -gt 0) {
                break
            }
        }

        if ($result.Count -gt 0) {
            return (($result.ToArray()) -join "`n").Trim()
        }
    }

    $null
}

function Get-LineAfterLabel {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Section,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $lines = $Section -split "`r?`n"

    for ($index = 0; $index -lt $lines.Length; $index++) {
        if ($lines[$index].Trim() -ne $Label) {
            continue
        }

        for ($cursor = $index + 1; $cursor -lt $lines.Length; $cursor++) {
            $candidate = $lines[$cursor].Trim()
            if ($candidate.Length -gt 0) {
                return $candidate
            }
        }
    }

    $null
}

function Get-FirstMatch {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,

        [Parameter(Mandatory = $true)]
        [string]$Pattern
    )

    $match = [regex]::Match($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if ($match.Success) {
        return $match.Groups[1].Value.Trim()
    }

    $null
}

function Get-MarkdownHeadingFirstLine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,

        [Parameter(Mandatory = $true)]
        [string]$Heading
    )

    $lines = $Text -split "`r?`n"

    for ($index = 0; $index -lt $lines.Length; $index++) {
        if ($lines[$index].Trim() -ne $Heading) {
            continue
        }

        for ($cursor = $index + 1; $cursor -lt $lines.Length; $cursor++) {
            $candidate = $lines[$cursor].Trim()
            if ($candidate.Length -eq 0) {
                continue
            }

            if ($candidate -match '^##\s+') {
                break
            }

            return $candidate
        }
    }

    $null
}

function Get-SafeRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $normalizedRoot = ($repoRoot.TrimEnd('\') + '\')
    if ($Path.StartsWith($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($normalizedRoot.Length).Replace('\', '/')
    }

    return $Path.Replace('\', '/')
}

function Add-Block {
    param(
        [Parameter(Mandatory = $true)]
        [System.Text.StringBuilder]$Builder,

        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    foreach ($line in ($Text -split "`r?`n")) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        [void]$Builder.AppendLine($line.TrimEnd())
    }
}

$projectText = Get-FileText -Path $projectFiles.PROJECT
$readmeText = Get-FileText -Path $projectFiles.README
$plansText = Get-FileText -Path $projectFiles.PLANS4
$worklogText = Get-FileText -Path $projectFiles.WORKLOG5

$planSection = Get-LastMarkdownSection -Text $plansText -HeadingPrefixRegex '^##\s+Обновление плана'
if (-not $planSection) {
    $planSection = Get-LastMarkdownSection -Text $plansText -HeadingPrefixRegex '^##\s+Следующий точный шаг'
}
$worklogSection = Get-LastMarkdownSection -Text $worklogText -HeadingPrefixRegex '^##\s+Session\s+\d+\s+-'

$projectName = Get-MarkdownHeadingFirstLine -Text $projectText -Heading '## Project name'
$projectSummary = Get-MarkdownHeadingFirstLine -Text $projectText -Heading '## One-sentence summary'
if (-not $projectName) { $projectName = 'Axiom IDE' }
if (-not $projectSummary) { $projectSummary = Get-MarkdownHeadingFirstLine -Text $projectText -Heading '## Назначение продукта' }

$readmeStatusBlock = Get-FirstMatch -Text $readmeText -Pattern '^## Текущий статус\s*\r?\n(.+?)\r?\n## '
$readmeStatus = if ($readmeStatusBlock) { (($readmeStatusBlock -split "`r?`n" | Select-Object -First 1) -join '') } else { $null }

$planTitle = "Не найдено"
if ($planSection) {
    $planTitle = ($planSection -split "`r?`n")[0].Trim()
}

$worklogTitle = "Не найдено"
if ($worklogSection) {
    $worklogTitle = ($worklogSection -split "`r?`n")[0].Trim()
}

$nextStep = $null
if ($worklogSection) {
    $nextStep = Get-LineAfterLabel -Section $worklogSection -Label 'Следующий точный шаг:'
}
if (-not $nextStep -and $planSection) {
    $nextStep = Get-LineAfterLabel -Section $planSection -Label 'Следующий точный шаг:'
}
if ($nextStep) {
    $nextStep = ($nextStep -replace '^\-\s*', '').Trim()
}

$worklogDoneBlock = $null
if ($worklogSection) {
    $worklogDoneBlock = Get-IndentedBlockAfterLabel -Section $worklogSection -Label 'Что сделано:'
}

$planDoneBlock = $null
if ($planSection) {
    $planDoneBlock = Get-IndentedBlockAfterLabel -Section $planSection -Label 'Сделано:'
}

$repoFileCount = (& rg --files | Measure-Object).Count
$manualFileCount = (& rg --files docs/manual | Measure-Object).Count
$sampleFileCount = (& rg --files samples | Measure-Object).Count
$testFileCount = (& rg --files tests | Measure-Object).Count

$builder = [System.Text.StringBuilder]::new()
[void]$builder.AppendLine("# Краткая сводка Axiom")
[void]$builder.AppendLine()
[void]$builder.AppendLine("Сгенерировано: $(Get-Date -Format 'yyyy-MM-dd HH:mm')")
[void]$builder.AppendLine()
[void]$builder.AppendLine("## Проект")
[void]$builder.AppendLine("- Проект: $projectName")
[void]$builder.AppendLine("- Кратко: $projectSummary")
if ($readmeStatus) {
    [void]$builder.AppendLine("- Текущее состояние по README: $readmeStatus")
}
[void]$builder.AppendLine()
[void]$builder.AppendLine("## Текущее официальное состояние")
[void]$builder.AppendLine("- Последнее обновление плана: $planTitle")
[void]$builder.AppendLine("- Последняя запись журнала: $worklogTitle")
if ($nextStep) {
    [void]$builder.AppendLine("- Следующий шаг: $nextStep")
}
[void]$builder.AppendLine()
[void]$builder.AppendLine("## Что уже сделано рядом с текущим шагом")
if ($worklogDoneBlock) {
    Add-Block -Builder $builder -Text $worklogDoneBlock
}
elseif ($planDoneBlock) {
    Add-Block -Builder $builder -Text $planDoneBlock
}
else {
    [void]$builder.AppendLine("- Явный краткий блок рядом с последним шагом не найден.")
}
[void]$builder.AppendLine()
[void]$builder.AppendLine("## Горячие документы")
[void]$builder.AppendLine('- `PROJECT.md`')
[void]$builder.AppendLine('- `README.md`')
[void]$builder.AppendLine('- `docs/SPECIFICATION3.md`')
[void]$builder.AppendLine('- `docs/ARCHITECTURE.md`')
[void]$builder.AppendLine('- `docs/PLANS4.md`')
[void]$builder.AppendLine('- `docs/WORKLOG5.md`')
[void]$builder.AppendLine('- `docs/plans/UNFINISHED_WORK.md`')
[void]$builder.AppendLine('- `docs/BYTECODE_COVERAGE.md`')
[void]$builder.AppendLine('- `docs/BYTECODE_PERF_MATRIX.md`')
[void]$builder.AppendLine()
[void]$builder.AppendLine("## Репозиторий в цифрах")
[void]$builder.AppendLine("- Всего файлов: $repoFileCount")
[void]$builder.AppendLine("- Файлов manual: $manualFileCount")
[void]$builder.AppendLine("- Файлов samples: $sampleFileCount")
[void]$builder.AppendLine("- Файлов tests: $testFileCount")

if ($Topic -and $Topic.Count -gt 0) {
    $searchFiles = @(
        $projectFiles.README
        $projectFiles.SPECIFICATION3
        $projectFiles.ARCHITECTURE
        $projectFiles.PLANS4
        $projectFiles.WORKLOG5
        (Join-Path $repoRoot "docs/agent/CURRENT_STATE.md")
        (Join-Path $repoRoot "docs/agent/ARCHITECTURE_MAP.md")
    ) | Where-Object { Test-Path $_ }

    [void]$builder.AppendLine()
    [void]$builder.AppendLine("## Тематические фрагменты")
    [void]$builder.AppendLine("- Тема: $($Topic -join ', ')")

    foreach ($file in $searchFiles) {
        $matches = Select-String -Path $file -Pattern $Topic -SimpleMatch -Context $ContextLines, $ContextLines -Encoding UTF8 |
            Select-Object -First $MatchesPerFile

        if (-not $matches) {
            continue
        }

        [void]$builder.AppendLine()
        [void]$builder.AppendLine(('### `{0}`' -f (Get-SafeRelativePath -Path $file)))

        foreach ($match in $matches) {
            [void]$builder.AppendLine("- Строка $($match.LineNumber):")
            [void]$builder.AppendLine('```text')

            $contextBlock = @()
            $contextBlock += $match.Context.PreContext
            $contextBlock += $match.Line
            $contextBlock += $match.Context.PostContext
            [void]$builder.AppendLine((($contextBlock -join [Environment]::NewLine).Trim()))

            [void]$builder.AppendLine('```')
        }
    }
}

$outputText = $builder.ToString().TrimEnd() + [Environment]::NewLine

if (-not $StdoutOnly) {
    $resolvedOutFile = $OutFile
    if (-not [System.IO.Path]::IsPathRooted($OutFile)) {
        $resolvedOutFile = Join-Path $repoRoot $OutFile
    }

    $outDirectory = Split-Path -Parent $resolvedOutFile
    if (-not (Test-Path $outDirectory)) {
        New-Item -ItemType Directory -Force -Path $outDirectory | Out-Null
    }

    Set-Content -Path $resolvedOutFile -Value $outputText -Encoding UTF8
}

Write-Output $outputText



