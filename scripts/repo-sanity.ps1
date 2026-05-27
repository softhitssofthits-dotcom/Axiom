[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Split-Path -Parent $PSScriptRoot

$ignoredRoots = @(
    (Join-Path $repoRoot ".artifacts"),
    (Join-Path $repoRoot ".git"),
    (Join-Path $repoRoot ".codex"),
    (Join-Path $repoRoot ".playwright-mcp"),
    (Join-Path $repoRoot ".vs")
)

$forbiddenDirectoryNames = @(
    "bin",
    "obj",
    "TestResults",
    ".dotnet-home",
    ".dotnet-cli",
    ".tmp-build"
)

function Test-IsUnderIgnoredRoot {
    param(
        [string]$Path
    )

    foreach ($ignoredRoot in $ignoredRoots) {
        if ($Path.StartsWith($ignoredRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

$violations = Get-ChildItem -Path $repoRoot -Recurse -Directory -Force |
    Where-Object {
        -not (Test-IsUnderIgnoredRoot -Path $_.FullName) -and
        $forbiddenDirectoryNames -contains $_.Name
    } |
    Sort-Object FullName

if ($violations.Count -gt 0) {
    Write-Host "Найдены временные каталоги вне .artifacts:"
    $violations | ForEach-Object { Write-Host $_.FullName }
    exit 1
}

Write-Host "Sanity-check пройден: временные каталоги вне .artifacts не найдены."
