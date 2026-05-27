[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Target = "Axiom.sln",

    [Parameter()]
    [string]$ResultsName,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AdditionalArguments
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = Join-Path $repoRoot ".artifacts"
$testResultsRoot = Join-Path $artifactsRoot "test-results"

$targetLeaf = Split-Path -Leaf $Target
$targetStem = [System.IO.Path]::GetFileNameWithoutExtension($targetLeaf)
if ([string]::IsNullOrWhiteSpace($targetStem)) {
    $targetStem = "dotnet-test"
}

$resultsLeaf = if ([string]::IsNullOrWhiteSpace($ResultsName)) { $targetStem } else { $ResultsName }
$resultsDirectory = Join-Path $testResultsRoot $resultsLeaf

New-Item -ItemType Directory -Force -Path $resultsDirectory | Out-Null

$arguments = @(
    "test"
    $Target
    "--results-directory"
    $resultsDirectory
)

if ($AdditionalArguments) {
    $arguments += $AdditionalArguments
}

Write-Host "Запуск: dotnet $($arguments -join ' ')"
& dotnet @arguments
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    exit $exitCode
}
