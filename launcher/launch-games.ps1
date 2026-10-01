# Leave game switches in $args: Windows PowerShell -File otherwise tries to
# bind the game's --switches as named PowerShell parameters.
$GameArguments = [string[]]@($args)
$ErrorActionPreference = 'Stop'
if ($GameArguments.Count -gt 0 -and $GameArguments[0] -eq '--') {
    $GameArguments = @($GameArguments | Select-Object -Skip 1)
}
$startGeometryWars = $GameArguments -contains '--start-geometry-wars'
$GameArguments = @($GameArguments | Where-Object { $_ -ne '--start-geometry-wars' })

# Start-Process accepts a single Windows command line. Quote each argument using
# the CRT rules, including trailing backslashes and embedded quotes.
function Quote-Argument([string]$Value) {
    '"' + [regex]::Replace([regex]::Replace($Value, '(\\*)"', '$1$1\"'), '(\\+)$', '$1$1') + '"'
}
$gameRoot = $env:PGR4_GAME_ROOT
if (!$gameRoot) {
    $settingsFile = Join-Path $PSScriptRoot 'launcher-settings.json'
    if (Test-Path -LiteralPath $settingsFile) {
        $settings = Get-Content -LiteralPath $settingsFile -Raw | ConvertFrom-Json
        $gameRoot = $settings.GameDataRoot
    }
}
if (!$gameRoot) { $gameRoot = Join-Path $PSScriptRoot '..\..\PGR4' }
$gameRoot = [IO.Path]::GetFullPath($gameRoot)
if (!(Test-Path -LiteralPath (Join-Path $gameRoot 'default.xex')) -or !(Test-Path -LiteralPath (Join-Path $gameRoot 'Game') -PathType Container)) {
    Write-Host 'Game files not found. Open MTR-PGR4.exe and choose your extracted PGR4 folder.' -ForegroundColor Red
    exit 1
}
$sessionRoot = Join-Path $PSScriptRoot ('logs\geometry-wars\' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $sessionRoot -Force | Out-Null
$output = Join-Path $sessionRoot 'request.bin'
$inputFile = Join-Path $sessionRoot 'incoming.bin'
$previousOutput = $env:PGR4_HANDOFF_OUTPUT
$previousInput = $env:PGR4_HANDOFF_INPUT
$env:PGR4_HANDOFF_OUTPUT = $output
$env:PGR4_HANDOFF_INPUT = $null
$title = if ($startGeometryWars) { 2 } else { 1 }
$exitCode = 0
$sequence = 0
try {
    while ($true) {
        $sequence++
        if ($title -eq 1) {
            $exe = Join-Path $PSScriptRoot 'pgr4_recompiled.exe'
            $arguments = @('--game_data_root', $gameRoot, '--user_data_root', (Join-Path $PSScriptRoot 'userdata'),
                '--execute_unclipped_draw_vs_on_cpu', '--gpu_plugin=xenos', '--d3d12_present_vsync', '--xmp_music_gain', '0.35') + $GameArguments
            $workingDirectory = $PSScriptRoot
        } else {
            $workingDirectory = Join-Path $PSScriptRoot 'geometry-wars'
            $exe = Join-Path $workingDirectory 'gw_recompiled.exe'
            $arguments = @('--game_data_root', $gameRoot, '--user_data_root', (Join-Path $workingDirectory 'userdata'),
                '--execute_unclipped_draw_vs_on_cpu', '--gpu_plugin=xenos', '--d3d12_present_vsync',
                '--log_file', (Join-Path $sessionRoot "geometry-wars-$sequence.log"))
            # GW runs at 60 FPS itself. Only common display options carry over.
            foreach ($argument in $GameArguments) {
                if ($argument -match '^--(fullscreen|resolution_scale=\d+)$') { $arguments += $argument }
            }
        }
        if (!(Test-Path -LiteralPath $exe)) { throw "Game executable missing: $exe" }
        $commandLine = ($arguments | ForEach-Object { Quote-Argument $_ }) -join ' '
        $process = Start-Process -FilePath $exe -ArgumentList $commandLine -WorkingDirectory $workingDirectory -WindowStyle Hidden -PassThru
        $process.WaitForExit()
        $process.Refresh()
        $exitCode = $process.ExitCode
        if (!(Test-Path -LiteralPath $output)) { break }
        if ($exitCode -ne 0) { throw "Title exited with error $exitCode; handoff cancelled." }
        $bytes = [IO.File]::ReadAllBytes($output)
        if ($bytes.Length -lt 20 -or [BitConverter]::ToUInt32($bytes,0) -ne 0x48475250) { throw 'Invalid title handoff header.' }
        $next = [BitConverter]::ToUInt32($bytes,4)
        $present = [BitConverter]::ToUInt32($bytes,12)
        $length = [BitConverter]::ToUInt32($bytes,16)
        if ($next -notin @(1,2) -or $present -gt 1 -or $length -gt 3072 -or $bytes.Length -ne 20 + $length) { throw 'Invalid title handoff data.' }
        Move-Item -LiteralPath $output -Destination $inputFile -Force
        $env:PGR4_HANDOFF_INPUT = $inputFile
        $title = $next
        if ($title -eq 2) { Write-Host 'Launching Geometry Wars...' } else { Write-Host 'Returning to PGR4...' }
    }
} catch {
    Write-Host $_ -ForegroundColor Red
    $exitCode = 1
} finally {
    $env:PGR4_HANDOFF_OUTPUT = $previousOutput
    $env:PGR4_HANDOFF_INPUT = $previousInput
    # Keep game logs, discard only this session's transient launch data.
    foreach ($file in @($inputFile, $output)) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    if (!(Get-ChildItem -LiteralPath $sessionRoot)) { Remove-Item -LiteralPath $sessionRoot }
}
exit $exitCode
