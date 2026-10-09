[CmdletBinding()]
param([string]$Root = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = 'Stop'
$results = Join-Path $Root '.artifacts/language-audit-20261007'
$destination = Join-Path $Root 'docs/audit'
$inventory = Get-Content (Join-Path $results 'inventory.json') -Raw | ConvertFrom-Json
$observations = Get-Content (Join-Path $results 'observations.json') -Raw | ConvertFrom-Json
$samples = Get-Content (Join-Path $results 'samples.json') -Raw | ConvertFrom-Json
# Подробные пути позволяют перепроверить каждую строку; наличие упоминаний не считается тестом.
$registryRows = @($inventory.Rows | ForEach-Object {
    [pscustomobject]@{
        Name=$_.Name; Id=$_.Id; Arity=$_.Arity; Origin='registry'
        Parameters=($_.Parameters | ForEach-Object {$_.Name + ':' + $_.Type}) -join ';'
        DeclaredReturn=$_.ReturnType; Binding=$_.Binding; OperandPolicy=$_.OperandPolicy; ResultPolicy=$_.ResultPolicy
        InterpreterFiles=$_.Interpreter -join ';'; VmFiles=$_.Vm -join ';'; VmDispatch=$_.VmDispatch
        HelperMentions=$_.Helpers -join ';'; TestMentions=$_.Tests -join ';'; Samples=$_.Samples -join ';'; Manual=$_.Manual -join ';'
        Light=$_.Light; Dark=$_.Dark; Normal='UNVERIFIED_PER_OVERLOAD'; Boundary='UNVERIFIED_PER_OVERLOAD'
        Invalid='UNVERIFIED_PER_OVERLOAD'; WrongType='UNVERIFIED_PER_OVERLOAD'; Parity='UNVERIFIED_PER_OVERLOAD'; Status=$_.Status
    }
})
$syntheticRows = foreach ($name in 'LBOUND','UBOUND') {
    $row = ($registryRows | Where-Object {$_.Name -eq $name} | Select-Object -First 1).PSObject.Copy()
    $row.Arity=2; $row.Origin='binder-synthetic'; $row.Parameters='array:ARRAY;dimension:INTEGER'
    $row.Status='PARTIAL: dimension-форма подтверждена сценарием bound-dimension, не является дескриптором registry'
    $row
}
@($registryRows) + @($syntheticRows) | Export-Csv (Join-Path $destination 'LANGUAGE_FUNCTION_MATRIX_20261007.csv') -NoTypeInformation -Encoding utf8
$observations | Where-Object {$_.Id -notmatch '^property-'} | ConvertTo-Json -Depth 12 |
    Set-Content (Join-Path $destination 'LANGUAGE_AUDIT_EVIDENCE_20261007.json') -Encoding utf8
$samples | ForEach-Object {
    $i=$null; $v=$null
    if ($_.Runs) { $i=$_.Runs[0]; $v=$_.Runs[1] }
    [pscustomobject]@{
        Path=$_.Path; Compilation=$_.Compilation; CompileCodes=$_.Diagnostics.Code -join ';'; Bytecode=$_.Bytecode
        Executed=[bool]$_.Runs; InterpreterStatus=$i.Status; VmStatus=$v.Status
        InterpreterException=$i.Exception; VmException=$v.Exception
        OutputParity=if($_.Runs){($i.Output -join "`n") -ceq ($v.Output -join "`n")}else{'UNVERIFIED'}
        DiagnosticCodeParity=if($_.Runs){($i.Diagnostics.Code -join ';') -ceq ($v.Diagnostics.Code -join ';')}else{'UNVERIFIED'}
        Reason=$_.Reason
    }
} | Export-Csv (Join-Path $destination 'LANGUAGE_SAMPLES_20261007.csv') -NoTypeInformation -Encoding utf8
$matrix = Get-Content (Join-Path $results 'matrix.md') -Raw
$matrixPath = Join-Path $destination 'LANGUAGE_FUNCTION_MATRIX.md'
# Этот экспорт дописывается один раз на результат сессии, а не заменяет прошлую инвентаризацию.
if (!(Select-String -LiteralPath $matrixPath -SimpleMatch '# Независимая матрица реестра — 2026-10-07' -Quiet)) {
    Add-Content -LiteralPath $matrixPath -Value "`n`n$matrix" -Encoding utf8
}
Write-Output 'Экспортированы матрица, исходные наблюдения и статусы примеров.'
