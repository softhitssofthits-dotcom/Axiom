[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Group = "list",

    [string]$Filter = "",
    [string]$From = "",
    [string]$Out = ""
)

# Единая точка запуска проверок Axiom по логическим группам.
# Полный каталог групп и правила выбора — docs/TESTING.md.
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

$dotnetGroups = [ordered]@{
    "lexer"      = @("tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj")
    "syntax"     = @("tests/Axiom.Syntax.Tests/Axiom.Syntax.Tests.csproj")
    "parser"     = @("tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj")
    "semantics"  = @("tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj")
    "bytecode"   = @("tests/Axiom.Bytecode.Tests/Axiom.Bytecode.Tests.csproj")
    "execution"  = @("tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj")
    "runtime"    = @("tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj")
    "cli"        = @("tests/Axiom.Cli.Tests/Axiom.Cli.Tests.csproj")
    "ide-backend" = @("tests/Axiom.Ide.Backend.Tests/Axiom.Ide.Backend.Tests.csproj")
    "language"   = @(
        "tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj"
        "tests/Axiom.Syntax.Tests/Axiom.Syntax.Tests.csproj"
        "tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj"
        "tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj"
        "tests/Axiom.Bytecode.Tests/Axiom.Bytecode.Tests.csproj"
        "tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj"
        "tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj"
    )
    "dotnet-all" = @("Axiom.sln")
}

$unitGroups = Get-Content "ide/tests/groups.json" -Raw | ConvertFrom-Json
$e2eGroups = Get-Content "ide/e2e/groups.json" -Raw | ConvertFrom-Json

function Show-Groups {
    Write-Host "Группы .NET:"
    $dotnetGroups.Keys | ForEach-Object { Write-Host "  $_" }
    Write-Host "Группы Node (ide/tests):"
    $unitGroups.PSObject.Properties.Name | ForEach-Object { Write-Host "  $_" }
    Write-Host "Группы Electron (ide/e2e):"
    $e2eGroups.PSObject.Properties.Name | ForEach-Object { Write-Host "  $_" }
    Write-Host "Составные:"
        Write-Host "  ide        — ide-backend + ide-unit-all + e2e-all"
    Write-Host "  all        — dotnet-all + ide-unit-all + e2e-all"
}

function Invoke-Dotnet([string[]]$Projects) {
    foreach ($project in $Projects) {
        Write-Host "`n=== dotnet test $project ===" -ForegroundColor Cyan
        & dotnet test $project --nologo | Out-Host
        $code = $LASTEXITCODE
        if ($code -ne 0) { return $code }
    }
    return 0
}

function Invoke-Unit([string[]]$Files) {
    if (-not $Files -or $Files.Count -eq 0) {
        Write-Host "Нет файлов в группе." -ForegroundColor Yellow
        return 0
    }
    $paths = $Files | ForEach-Object { Join-Path "ide/tests" $_ }
    Write-Host "`n=== node --test ($($paths.Count) файлов) ===" -ForegroundColor Cyan
    & node --test @paths | Out-Host
    return $LASTEXITCODE
}

function Invoke-E2E([string]$Name) {
    $arguments = @("ide/e2e/runner.js", "--group", $Name)
    if ($From) { $arguments += @("--from", $From) }
    if ($Out) { $arguments += @("--out", $Out) }
    if ($Filter) { $arguments += $Filter }
    Write-Host "`n=== Electron E2E [$Name] ===" -ForegroundColor Cyan
    & node @arguments | Out-Host
    return $LASTEXITCODE
}

$failures = @()

try {
    switch ($Group) {
        "list" { Show-Groups; exit 0 }

        "all" {
            $failures += Invoke-Dotnet $dotnetGroups["dotnet-all"]
            $failures += Invoke-Unit $unitGroups."ide-unit-all"
            $failures += Invoke-E2E "e2e-all"
        }
        "ide" {
            $failures += Invoke-Dotnet $dotnetGroups["ide-backend"]
            $failures += Invoke-Unit $unitGroups."ide-unit-all"
            $failures += Invoke-E2E "e2e-all"
        }
        default {
            if ($dotnetGroups.Contains($Group)) {
                $failures += Invoke-Dotnet $dotnetGroups[$Group]
            }
            elseif ($unitGroups.PSObject.Properties.Name -contains $Group) {
                $failures += Invoke-Unit $unitGroups.$Group
            }
            elseif ($e2eGroups.PSObject.Properties.Name -contains $Group) {
                $failures += Invoke-E2E $Group
            }
            else {
                Write-Error "Неизвестная группа: $Group. Запустите '.\scripts\test.ps1 list'."
                exit 2
            }
        }
    }
}
finally {
    Pop-Location
}

if ($failures | Where-Object { $_ -ne 0 }) {
    Write-Host "`nПроверки группы '$Group' завершились с ошибкой." -ForegroundColor Red
    exit 1
}
Write-Host "`nГруппа '$Group': успешно." -ForegroundColor Green
exit 0
