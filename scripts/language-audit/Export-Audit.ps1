[CmdletBinding()]
param([string]$Root)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
}
$results = Join-Path $Root '.artifacts/language-audit-20261007'
$destination = Join-Path $Root 'docs/audit/bytecode-only'
$snapshotDate = [DateTime]::Now.ToString('yyyyMMdd')
$inventory = Get-Content (Join-Path $results 'inventory.json') -Raw | ConvertFrom-Json
$observations = Get-Content (Join-Path $results 'observations.json') -Raw | ConvertFrom-Json
$samples = Get-Content (Join-Path $results 'samples.json') -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Path $destination -Force | Out-Null
# Подробные пути позволяют перепроверить каждую строку; наличие упоминаний не считается тестом.
$registryRows = @($inventory.Rows | ForEach-Object {
    [pscustomobject]@{
        Name=$_.Name; Id=$_.Id; Arity=$_.Arity; Origin='registry'
        Parameters=($_.Parameters | ForEach-Object {$_.Name + ':' + $_.Type}) -join ';'
        DeclaredReturn=$_.ReturnType; Binding=$_.Binding; OperandPolicy=$_.OperandPolicy; ResultPolicy=$_.ResultPolicy
        VmFiles=$_.Vm -join ';'; VmDispatch=$_.VmDispatch
        HelperMentions=$_.Helpers -join ';'; TestMentions=$_.Tests -join ';'; Samples=$_.Samples -join ';'; Manual=$_.Manual -join ';'
        Light=$_.Light; Dark=$_.Dark; Normal='UNVERIFIED_PER_OVERLOAD'; Boundary='UNVERIFIED_PER_OVERLOAD'
        Invalid='UNVERIFIED_PER_OVERLOAD'; WrongType='UNVERIFIED_PER_OVERLOAD'; Status=$_.Status
    }
})
$syntheticRows = foreach ($name in 'LBOUND','UBOUND') {
    $row = ($registryRows | Where-Object {$_.Name -eq $name} | Select-Object -First 1).PSObject.Copy()
    $row.Arity=2; $row.Origin='binder-synthetic'; $row.Parameters='array:ARRAY;dimension:INTEGER'
    $row.Status='PARTIAL: dimension-форма подтверждена сценарием bound-dimension, не является дескриптором registry'
    $row
}
@($registryRows) + @($syntheticRows) | Export-Csv (Join-Path $destination "LANGUAGE_AUDIT_FUNCTION_MATRIX_$snapshotDate.csv") -NoTypeInformation -Encoding utf8
$observations | Where-Object {$_.Id -notmatch '^property-'} | ConvertTo-Json -Depth 12 |
    Set-Content (Join-Path $destination "LANGUAGE_AUDIT_EVIDENCE_$snapshotDate.json") -Encoding utf8
$samples | ForEach-Object {
    $v=$null
    if ($_.Runs) { $v=$_.Runs[0] }
    [pscustomobject]@{
        Path=$_.Path; Compilation=$_.Compilation; CompileCodes=$_.Diagnostics.Code -join ';'; Bytecode=$_.Bytecode
        Executed=[bool]$_.Runs; VmStatus=$v.Status
        VmException=$v.Exception
        Reason=$_.Reason
    }
} | Export-Csv (Join-Path $destination "LANGUAGE_SAMPLES_$snapshotDate.csv") -NoTypeInformation -Encoding utf8
$matrix = Get-Content (Join-Path $results 'matrix.md') -Raw
$matrixPath = Join-Path $destination 'LANGUAGE_FUNCTION_MATRIX.md'
# Этот экспорт дописывается один раз на результат сессии, а не заменяет прошлую инвентаризацию.
if (!(Test-Path -LiteralPath $matrixPath) -or
    !(Select-String -LiteralPath $matrixPath -SimpleMatch '# Независимая матрица реестра — 2026-10-09' -Quiet)) {
    Add-Content -LiteralPath $matrixPath -Value "`n`n$matrix" -Encoding utf8
}
Write-Output 'Экспортированы матрица, исходные наблюдения и статусы примеров.'
