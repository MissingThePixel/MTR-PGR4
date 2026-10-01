param([Parameter(Mandatory=$true)][string]$GameDataRoot,
      [string]$SdkPrefix = (Join-Path $PSScriptRoot '..\sdk-install'), [int]$Jobs = 4)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$GameDataRoot = (Resolve-Path -LiteralPath $GameDataRoot).Path
$SdkPrefix = (Resolve-Path -LiteralPath $SdkPrefix).Path
foreach ($name in @('default.xex','gw.xex')) {
    if (!(Test-Path -LiteralPath (Join-Path $GameDataRoot $name))) { throw "Missing $name in the extracted game folder." }
}
$rexglue = Join-Path $SdkPrefix 'bin\rexglue.exe'
if (!(Test-Path -LiteralPath $rexglue)) { throw 'Patched SDK not installed; run scripts/setup-sdk.ps1 first.' }
$utf8 = New-Object Text.UTF8Encoding($false)
function Local-Manifest([string]$Source, [string]$Destination, [string]$Xex) {
    $content = [IO.File]::ReadAllText($Source)
    $gamePath = $GameDataRoot.Replace('\','/').Replace('"','\"')
    $xexPath = (Join-Path $GameDataRoot $Xex).Replace('\','/').Replace('"','\"')
    $content = $content -replace 'game_root = "[^"]*"', ('game_root = "'+$gamePath+'"')
    $content = $content -replace 'file_path = "[^"]*"', ('file_path = "'+$xexPath+'"')
    [IO.File]::WriteAllText($Destination,$content,$utf8)
}
$pgrManifest = Join-Path $root 'pgr4_local.toml'
$gwManifest = Join-Path $root 'geometry-wars\gw_local.toml'
Local-Manifest (Join-Path $root 'pgr4_recompiled_manifest.toml') $pgrManifest 'default.xex'
Local-Manifest (Join-Path $root 'geometry-wars\gw_recompiled_manifest.toml') $gwManifest 'gw.xex'
foreach ($manifest in @($pgrManifest,$gwManifest)) {
    & $rexglue codegen $manifest
    if ($LASTEXITCODE -ne 0) { throw "Code generation failed: $manifest" }
}
foreach ($project in @(@($root,'pgr4',$pgrManifest),@((Join-Path $root 'geometry-wars'),'geometry-wars',$gwManifest))) {
    $build = Join-Path $root ('out\'+$project[1])
    & cmake -S $project[0] -B $build -G Ninja -DCMAKE_CXX_COMPILER=clang-cl -DCMAKE_BUILD_TYPE=Release "-DCMAKE_PREFIX_PATH=$SdkPrefix" "-DMTR_GAME_MANIFEST=$($project[2])"
    if ($LASTEXITCODE -ne 0) { throw 'Game configuration failed.' }
    & cmake --build $build -j $Jobs
    if ($LASTEXITCODE -ne 0) { throw 'Game build failed.' }
}
& (Join-Path $root 'launcher\build-launcher.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }
Write-Host 'Built PGR4, Geometry Wars and MTR-PGR4 launcher. Use scripts/package-release.ps1 to create a player ZIP.'
