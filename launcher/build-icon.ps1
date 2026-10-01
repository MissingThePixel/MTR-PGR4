# Encode the supplied artwork into standard Windows icon sizes. Preserve its
# aspect ratio and center it in the same square footprint as the launcher art.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$source = [Drawing.Image]::FromFile((Join-Path $PSScriptRoot 'app-icon.png'))
$sizes = @(16,24,32,48,64,128,256)
$frames = @()
try {
    foreach ($size in $sizes) {
        $bitmap = New-Object Drawing.Bitmap($size,$size)
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        $graphics.Clear([Drawing.Color]::White)
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $ratio = [Math]::Min($size / $source.Width, $size / $source.Height)
        $width = $source.Width * $ratio; $height = $source.Height * $ratio
        $graphics.DrawImage($source,[Drawing.RectangleF]::new(($size-$width)/2,($size-$height)/2,$width,$height))
        $stream = New-Object IO.MemoryStream
        $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
        $frames += ,$stream.ToArray()
        $stream.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
    }
    $file = [IO.File]::Create((Join-Path $PSScriptRoot 'app.ico'))
    $writer = New-Object IO.BinaryWriter($file)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($index=0; $index -lt $sizes.Count; $index++) {
            $size = $sizes[$index]; $dimension = if ($size -eq 256) {0} else {$size}
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
            $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$frames[$index].Length); $writer.Write([uint32]$offset)
            $offset += $frames[$index].Length
        }
        foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
    } finally { $writer.Dispose(); $file.Dispose() }
} finally { $source.Dispose() }
