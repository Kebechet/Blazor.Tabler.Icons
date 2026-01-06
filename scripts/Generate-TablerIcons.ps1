<#
.SYNOPSIS
    Downloads the latest Tabler Icons webfont and generates C# enum, constants, and extension methods.

.DESCRIPTION
    This script:
    1. Downloads the latest @tabler/icons-webfont npm package
    2. Extracts icon names from both outline and filled CSS files
    3. Generates TablerIcon enum (with Filled suffix for filled icons)
    4. Generates TablerIconConstants class
    5. Generates TablerIconExtensions class with switch expression

.PARAMETER OutputPath
    The output directory for generated C# files. Defaults to ../src/Blazor.Tabler.Icons

.EXAMPLE
    .\Generate-TablerIcons.ps1
#>

param(
    [string]$OutputPath = "$PSScriptRoot\..\src\Blazor.Tabler.Icons"
)

$ErrorActionPreference = "Stop"

$tempDir = Join-Path $env:TEMP "tabler-icons-$(Get-Date -Format 'yyyyMMddHHmmss')"
$tarballUrl = "https://registry.npmjs.org/@tabler/icons-webfont/-/icons-webfont-3.36.1.tgz"

function ConvertTo-PascalCase {
    param([string]$text)

    $parts = $text -split '-'
    $result = ""
    foreach ($part in $parts) {
        if ($part.Length -gt 0) {
            $result += $part.Substring(0,1).ToUpper() + $part.Substring(1)
        }
    }

    if ($result -match '^\d') {
        $result = "_$result"
    }

    return $result
}

function Expand-TarGz {
    param(
        [string]$ArchivePath,
        [string]$DestinationPath
    )

    Add-Type -AssemblyName System.IO.Compression

    $gzipStream = [System.IO.File]::OpenRead($ArchivePath)
    $decompressedStream = New-Object System.IO.Compression.GZipStream($gzipStream, [System.IO.Compression.CompressionMode]::Decompress)

    $tarPath = Join-Path $DestinationPath "package.tar"
    $tarFileStream = [System.IO.File]::Create($tarPath)
    $decompressedStream.CopyTo($tarFileStream)
    $tarFileStream.Close()
    $decompressedStream.Close()
    $gzipStream.Close()

    $tarBytes = [System.IO.File]::ReadAllBytes($tarPath)
    $position = 0

    while ($position -lt $tarBytes.Length) {
        $header = $tarBytes[$position..($position + 511)]

        $allZero = $true
        foreach ($b in $header) {
            if ($b -ne 0) { $allZero = $false; break }
        }
        if ($allZero) { break }

        $nameBytes = $header[0..99]
        $name = [System.Text.Encoding]::ASCII.GetString($nameBytes).Trim([char]0).Trim()

        $sizeOctal = [System.Text.Encoding]::ASCII.GetString($header[124..135]).Trim([char]0).Trim()
        $size = 0
        if ($sizeOctal -ne "") {
            $size = [Convert]::ToInt64($sizeOctal, 8)
        }

        $typeFlag = [char]$header[156]

        $position += 512

        if ($name -ne "" -and $typeFlag -ne '5' -and $size -gt 0) {
            $filePath = Join-Path $DestinationPath $name
            $fileDir = Split-Path $filePath -Parent
            if (-not (Test-Path $fileDir)) {
                New-Item -ItemType Directory -Path $fileDir -Force | Out-Null
            }

            $fileBytes = $tarBytes[$position..($position + $size - 1)]
            [System.IO.File]::WriteAllBytes($filePath, $fileBytes)
        }

        $blocks = [Math]::Ceiling($size / 512)
        $position += $blocks * 512
    }

    Remove-Item $tarPath -Force
}

function Parse-IconsCss {
    param(
        [string]$CssPath,
        [string]$Suffix = ""
    )

    $cssContent = Get-Content $CssPath -Raw
    $icons = @{}
    $pattern = '\.ti-([a-z0-9-]+):before\s*\{\s*content:\s*"\\([a-fA-F0-9]+)"'
    $matches = [regex]::Matches($cssContent, $pattern)

    foreach ($match in $matches) {
        $iconName = $match.Groups[1].Value
        $unicode = $match.Groups[2].Value

        if ($iconName -ne "ti") {
            $key = $iconName + $Suffix
            $icons[$key] = @{
                OriginalName = $iconName
                Unicode = $unicode
                IsFilled = ($Suffix -eq "-filled")
            }
        }
    }

    return $icons
}

try {
    Write-Host "Creating temp directory: $tempDir"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    $tarballPath = Join-Path $tempDir "icons-webfont.tgz"
    Write-Host "Downloading Tabler Icons webfont from $tarballUrl..."
    Invoke-WebRequest -Uri $tarballUrl -OutFile $tarballPath

    Write-Host "Extracting archive..."
    Expand-TarGz -ArchivePath $tarballPath -DestinationPath $tempDir

    # Parse outline icons
    $outlineCssFile = Get-ChildItem -Path $tempDir -Recurse -Filter "tabler-icons.css" | Where-Object { $_.Name -eq "tabler-icons.css" } | Select-Object -First 1
    if (-not $outlineCssFile) {
        throw "Could not find tabler-icons.css in the extracted package"
    }

    Write-Host "Parsing outline CSS file: $($outlineCssFile.FullName)"
    $outlineIcons = Parse-IconsCss -CssPath $outlineCssFile.FullName
    Write-Host "Found $($outlineIcons.Count) outline icons"

    # Parse filled icons
    $filledCssFile = Get-ChildItem -Path $tempDir -Recurse -Filter "tabler-icons-filled.css" | Select-Object -First 1
    if (-not $filledCssFile) {
        throw "Could not find tabler-icons-filled.css in the extracted package"
    }

    Write-Host "Parsing filled CSS file: $($filledCssFile.FullName)"
    $filledIcons = Parse-IconsCss -CssPath $filledCssFile.FullName -Suffix "-filled"
    Write-Host "Found $($filledIcons.Count) filled icons"

    # Merge all icons
    $allIcons = @{}
    foreach ($key in $outlineIcons.Keys) {
        $allIcons[$key] = $outlineIcons[$key]
    }
    foreach ($key in $filledIcons.Keys) {
        $allIcons[$key] = $filledIcons[$key]
    }

    $totalCount = $allIcons.Count
    Write-Host "Total icons: $totalCount"

    if ($totalCount -eq 0) {
        throw "No icons found in CSS files"
    }

    $sortedIcons = $allIcons.GetEnumerator() | Sort-Object Name

    Write-Host "Generating C# files..."

    # Generate enum
    $enumBuilder = [System.Text.StringBuilder]::new()
    [void]$enumBuilder.AppendLine("namespace Blazor.Tabler.Icons;")
    [void]$enumBuilder.AppendLine()
    [void]$enumBuilder.AppendLine("/// <summary>")
    [void]$enumBuilder.AppendLine("/// Enumeration of all available Tabler icons (outline and filled variants).")
    [void]$enumBuilder.AppendLine("/// </summary>")
    [void]$enumBuilder.AppendLine("public enum TablerIconType")
    [void]$enumBuilder.AppendLine("{")

    $first = $true
    foreach ($icon in $sortedIcons) {
        if (-not $first) {
            [void]$enumBuilder.AppendLine(",")
        }
        $pascalName = ConvertTo-PascalCase $icon.Name
        [void]$enumBuilder.Append("    $pascalName")
        $first = $false
    }
    [void]$enumBuilder.AppendLine()
    [void]$enumBuilder.AppendLine("}")

    # Generate constants
    $constantsBuilder = [System.Text.StringBuilder]::new()
    [void]$constantsBuilder.AppendLine("namespace Blazor.Tabler.Icons;")
    [void]$constantsBuilder.AppendLine()
    [void]$constantsBuilder.AppendLine("/// <summary>")
    [void]$constantsBuilder.AppendLine("/// CSS class constants for Tabler icons.")
    [void]$constantsBuilder.AppendLine("/// </summary>")
    [void]$constantsBuilder.AppendLine("public static class TablerIconConstants")
    [void]$constantsBuilder.AppendLine("{")

    foreach ($icon in $sortedIcons) {
        $pascalName = ConvertTo-PascalCase $icon.Name
        $originalName = $icon.Value.OriginalName
        $isFilled = $icon.Value.IsFilled
        if ($isFilled) {
            [void]$constantsBuilder.AppendLine("    public const string $pascalName = `"tif tif-$originalName`";")
        } else {
            [void]$constantsBuilder.AppendLine("    public const string $pascalName = `"ti ti-$originalName`";")
        }
    }
    [void]$constantsBuilder.AppendLine("}")

    # Generate extension methods
    $extensionBuilder = [System.Text.StringBuilder]::new()
    [void]$extensionBuilder.AppendLine("namespace Blazor.Tabler.Icons;")
    [void]$extensionBuilder.AppendLine()
    [void]$extensionBuilder.AppendLine("/// <summary>")
    [void]$extensionBuilder.AppendLine("/// Extension methods for <see cref=`"TablerIconType`"/>.")
    [void]$extensionBuilder.AppendLine("/// </summary>")
    [void]$extensionBuilder.AppendLine("public static class TablerIconTypeExtensions")
    [void]$extensionBuilder.AppendLine("{")
    [void]$extensionBuilder.AppendLine("    /// <summary>")
    [void]$extensionBuilder.AppendLine("    /// Converts the icon to its CSS class string.")
    [void]$extensionBuilder.AppendLine("    /// </summary>")
    [void]$extensionBuilder.AppendLine("    public static string ToCssClass(this TablerIconType icon) => icon switch")
    [void]$extensionBuilder.AppendLine("    {")

    foreach ($icon in $sortedIcons) {
        $pascalName = ConvertTo-PascalCase $icon.Name
        [void]$extensionBuilder.AppendLine("        TablerIconType.$pascalName => TablerIconConstants.$pascalName,")
    }
    [void]$extensionBuilder.AppendLine("        _ => throw new System.ArgumentOutOfRangeException(nameof(icon), icon, `"Unknown icon`")")
    [void]$extensionBuilder.AppendLine("    };")
    [void]$extensionBuilder.AppendLine("}")

    $outputDir = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
    if (-not $outputDir) {
        $outputDir = $OutputPath
    }

    Write-Host "Writing files to $outputDir..."

    $enumPath = Join-Path $outputDir "TablerIconType.cs"
    $constantsPath = Join-Path $outputDir "TablerIconConstants.cs"
    $extensionPath = Join-Path $outputDir "TablerIconTypeExtensions.cs"

    $enumBuilder.ToString() | Out-File -FilePath $enumPath -Encoding UTF8 -NoNewline
    $constantsBuilder.ToString() | Out-File -FilePath $constantsPath -Encoding UTF8 -NoNewline
    $extensionBuilder.ToString() | Out-File -FilePath $extensionPath -Encoding UTF8 -NoNewline

    $wwwrootDir = Join-Path $outputDir "wwwroot"
    $fontsDir = Join-Path $wwwrootDir "fonts"
    New-Item -ItemType Directory -Path $fontsDir -Force | Out-Null

    # Copy and process outline CSS
    $minCssSource = Get-ChildItem -Path $tempDir -Recurse -Filter "tabler-icons.min.css" | Where-Object { $_.Name -eq "tabler-icons.min.css" } | Select-Object -First 1
    if ($minCssSource) {
        $minCssContent = Get-Content $minCssSource.FullName -Raw
        $minCssContent = $minCssContent -replace '\.\./fonts/', 'fonts/'
        $minCssContent = $minCssContent -replace ',url\("[^"]+\.woff\?[^"]*"\)\s*format\("woff"\)', ''
        $minCssContent = $minCssContent -replace ',url\("[^"]+\.ttf[^"]*"\)\s*format\("truetype"\)', ''
        $minCssContent | Out-File -FilePath (Join-Path $wwwrootDir "tabler-icons.min.css") -Encoding UTF8 -NoNewline
        Write-Host "  - Copied tabler-icons.min.css"
    }

    # Copy and process filled CSS
    $filledMinCssSource = Get-ChildItem -Path $tempDir -Recurse -Filter "tabler-icons-filled.min.css" | Select-Object -First 1
    if ($filledMinCssSource) {
        $filledMinCssContent = Get-Content $filledMinCssSource.FullName -Raw
        $filledMinCssContent = $filledMinCssContent -replace '\.\./fonts/', 'fonts/'
        $filledMinCssContent = $filledMinCssContent -replace ',url\("[^"]+\.woff\?[^"]*"\)\s*format\("woff"\)', ''
        $filledMinCssContent = $filledMinCssContent -replace ',url\("[^"]+\.ttf[^"]*"\)\s*format\("truetype"\)', ''
        # Change .ti to .tif for filled icons (base class and icon class selectors only)
        $filledMinCssContent = $filledMinCssContent -replace '\.ti\{', '.tif{'
        $filledMinCssContent = $filledMinCssContent -replace '\.ti-([a-z0-9-]+):before', '.tif-$1:before'
        $filledMinCssContent | Out-File -FilePath (Join-Path $wwwrootDir "tabler-icons-filled.min.css") -Encoding UTF8 -NoNewline
        Write-Host "  - Copied tabler-icons-filled.min.css"
    }

    $fontFiles = Get-ChildItem -Path $tempDir -Recurse -Include "tabler-icons.woff2", "tabler-icons-filled.woff2"
    foreach ($fontFile in $fontFiles) {
        Copy-Item $fontFile.FullName -Destination $fontsDir -Force
        Write-Host "  - Copied $($fontFile.Name)"
    }

    Write-Host ""
    Write-Host "Generated files:"
    Write-Host "  - $enumPath"
    Write-Host "  - $constantsPath"
    Write-Host "  - $extensionPath"
    Write-Host ""
    Write-Host "Done! Generated $totalCount icons ($($outlineIcons.Count) outline + $($filledIcons.Count) filled)."
}
finally {
    if (Test-Path $tempDir) {
        Write-Host "Cleaning up temp directory..."
        Remove-Item -Path $tempDir -Recurse -Force
    }
}
