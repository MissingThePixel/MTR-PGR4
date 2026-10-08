param([string]$PgrExecutable = (Join-Path $PSScriptRoot '..\out\pgr4\pgr4_recompiled.exe'),
      [string]$GwExecutable = (Join-Path $PSScriptRoot '..\out\geometry-wars\gw_recompiled.exe'),
      [string]$LauncherExecutable = (Join-Path $PSScriptRoot '..\out\launcher\MTR-PGR4.exe'),
      [string]$RuntimeDirectory = (Join-Path $PSScriptRoot '..\sdk-install\bin'),
      [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\dist'))
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$package = Join-Path $OutputDirectory 'MTR-PGR4-windows-x64'
if (Test-Path -LiteralPath $package) { throw "Output folder already exists: $package. Select an empty output directory." }
foreach ($file in @($PgrExecutable,$GwExecutable,$LauncherExecutable,(Join-Path $RuntimeDirectory 'rexruntime.dll'),(Join-Path $RuntimeDirectory 'rexgpu-xenos.dll'))) {
    if (!(Test-Path -LiteralPath $file)) { throw "Required binary missing: $file" }
}
$runtime = Join-Path $package 'runtime'
$gw = Join-Path $runtime 'geometry-wars'
foreach ($folder in @($package,$runtime,$gw,(Join-Path $package 'LICENSES'))) {
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
}
Copy-Item -LiteralPath $LauncherExecutable -Destination (Join-Path $package 'MTR-PGR4.exe')
Copy-Item -LiteralPath $PgrExecutable -Destination (Join-Path $runtime 'pgr4_recompiled.exe')
Copy-Item -LiteralPath $GwExecutable -Destination (Join-Path $gw 'gw_recompiled.exe')
foreach ($dll in @('rexruntime.dll','rexgpu-xenos.dll')) {
    Copy-Item -LiteralPath (Join-Path $RuntimeDirectory $dll) -Destination $runtime
    Copy-Item -LiteralPath (Join-Path $RuntimeDirectory $dll) -Destination $gw
}
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination $package
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $package 'LICENSE.txt')
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $root 'LICENSES') -File) {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $package 'LICENSES')
}
$files = Get-ChildItem -LiteralPath $package -Recurse -File
if ($files | Where-Object { $_.Extension -in @('.xex','.iso','.raw','.wav','.wma','.etl','.rdc','.dmp','.save','.ps1','.bat','.cmd') -or $_.Name -eq 'launcher-settings.json' }) {
    throw 'Excluded data detected in package.'
}
if ($files | Where-Object { $_.Extension -eq '.md' -and $_.Name -ne 'README.md' }) { throw 'Unexpected Markdown in player package.' }
$zip = Join-Path $OutputDirectory 'MTR-PGR4-windows-x64.zip'
Compress-Archive -LiteralPath $package -DestinationPath $zip -CompressionLevel Optimal
Write-Host "Release ZIP: $zip"
