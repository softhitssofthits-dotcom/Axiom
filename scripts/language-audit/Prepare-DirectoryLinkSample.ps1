param([string]$RepositoryRoot = (Join-Path $PSScriptRoot '../..'))
$ErrorActionPreference = 'Stop'
$repositoryPath = [IO.Path]::GetFullPath($RepositoryRoot)
$artifactsPath = [IO.Path]::GetFullPath((Join-Path $repositoryPath '.artifacts'))
$fixturePath = [IO.Path]::GetFullPath((Join-Path $artifactsPath 'unicode-directory-links'))
if (-not $fixturePath.StartsWith($artifactsPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Проверочный каталог вышел за границы .artifacts.'
}
$sourcePath = Join-Path $fixturePath 'source'
$outsidePath = Join-Path $fixturePath 'outside'
New-Item -ItemType Directory -Force -Path $sourcePath, $outsidePath | Out-Null
[IO.File]::WriteAllBytes((Join-Path $sourcePath 'own.dat'), [byte[]]@(1, 2, 3))
[IO.File]::WriteAllBytes((Join-Path $outsidePath 'foreign.dat'), [byte[]](1..11))
foreach ($pair in @(@('linked', $outsidePath), @('loop', $sourcePath))) {
    $linkPath = Join-Path $sourcePath $pair[0]
    if (Test-Path -LiteralPath $linkPath) {
        $link = Get-Item -LiteralPath $linkPath -Force
        if (-not ($link.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Проверочный путь уже существует и не является ссылкой: $linkPath"
        }
        if ([IO.Path]::GetFullPath($link.Target) -ne [IO.Path]::GetFullPath($pair[1])) {
            throw "Проверочная ссылка указывает на другой каталог: $linkPath"
        }
    } else {
        New-Item -ItemType Junction -Path $linkPath -Target $pair[1] | Out-Null
    }
}
Write-Output "Подготовлены обычный файл на 3 байта, внешняя ссылка и циклическая ссылка: $fixturePath"
Write-Output 'Запустите samples/smoke/directory_link_boundaries.axm из CLI или IDE.'
