param([string]$OutputDirectory = (Join-Path $PSScriptRoot '..\out\launcher'))
$ErrorActionPreference = 'Stop'
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $OutputDirectory 'MTR-PGR4.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ ('/out:' + $output) ('/win32icon:' + (Join-Path $PSScriptRoot 'app.ico')) ('/resource:' + (Join-Path $PSScriptRoot 'app-icon.png') + ',LauncherArtwork') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'Launcher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Launcher compilation failed.' }
Write-Host "Launcher built: $output"
