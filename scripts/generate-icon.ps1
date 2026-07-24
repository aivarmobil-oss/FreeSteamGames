Add-Type -AssemblyName System.Drawing

function New-RoundedRectPath {
    param([float]$X, [float]$Y, [float]$Width, [float]$Height, [float]$Radius)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $Radius * 2
    $path.AddArc($X, $Y, $d, $d, 180, 90)
    $path.AddArc($X + $Width - $d, $Y, $d, $d, 270, 90)
    $path.AddArc($X + $Width - $d, $Y + $Height - $d, $d, $d, 0, 90)
    $path.AddArc($X, $Y + $Height - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-LogoBitmap {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap $Size, $Size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 256.0

    $bgColor = [System.Drawing.Color]::FromArgb(255, 0x14, 0x17, 0x1C)
    $bgBrush = New-Object System.Drawing.SolidBrush $bgColor
    $bgPath = New-RoundedRectPath 0 0 $Size $Size (56 * $scale)
    $g.FillPath($bgBrush, $bgPath)

    $mint = [System.Drawing.Color]::FromArgb(255, 0x3E, 0xD5, 0x98)
    $mintBrush = New-Object System.Drawing.SolidBrush $mint

    $boxPath = New-RoundedRectPath (48*$scale) (96*$scale) (160*$scale) (112*$scale) (14*$scale)
    $g.FillPath($mintBrush, $boxPath)

    $lidPath = New-RoundedRectPath (38*$scale) (78*$scale) (180*$scale) (34*$scale) (10*$scale)
    $g.FillPath($mintBrush, $lidPath)

    $g.FillRectangle($bgBrush, (118*$scale), (78*$scale), (20*$scale), (130*$scale))
    $g.FillRectangle($bgBrush, (48*$scale), (140*$scale), (160*$scale), (20*$scale))

    $bowPath = New-RoundedRectPath (104*$scale) (48*$scale) (48*$scale) (32*$scale) (14*$scale)
    $g.FillPath($mintBrush, $bowPath)

    $g.Dispose()
    return $bmp
}

function Write-IcoFile {
    param([string]$Path, [int[]]$Sizes)

    $images = @()
    foreach ($s in $Sizes) {
        $bmp = New-LogoBitmap -Size $s
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $images += ,@{ Size = $s; Bytes = $ms.ToArray() }
        $bmp.Dispose()
    }

    $fs = New-Object System.IO.FileStream $Path, ([System.IO.FileMode]::Create)
    $bw = New-Object System.IO.BinaryWriter $fs

    # ICONDIR
    $bw.Write([UInt16]0)      # reserved
    $bw.Write([UInt16]1)      # type = icon
    $bw.Write([UInt16]$images.Count)

    $offset = 6 + (16 * $images.Count)
    foreach ($img in $images) {
        $sizeByte = if ($img.Size -ge 256) { 0 } else { $img.Size }
        $bw.Write([byte]$sizeByte)   # width
        $bw.Write([byte]$sizeByte)   # height
        $bw.Write([byte]0)           # color palette
        $bw.Write([byte]0)           # reserved
        $bw.Write([UInt16]1)         # color planes
        $bw.Write([UInt16]32)        # bits per pixel
        $bw.Write([UInt32]$img.Bytes.Length)
        $bw.Write([UInt32]$offset)
        $offset += $img.Bytes.Length
    }

    foreach ($img in $images) {
        $bw.Write($img.Bytes)
    }

    $bw.Flush()
    $fs.Close()
}

Write-IcoFile -Path "C:\Users\Red_Dragon\Projects\FreeSteamGames\Assets\tray.ico" -Sizes @(16,32,48,256)
Write-Output "Multi-size icon written."
