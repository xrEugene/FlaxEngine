# ==============================================================================
# Script: Export-FlaxMap.ps1
# Path:   C:\Flax\Engine\Fork\Source\Editor
# ==============================================================================

$rootDir = (Get-Location).Path
$mapDir = Join-Path $rootDir "_Map"
$oneDriveTarget = "C:\Users\eugen\OneDrive\Documents"
$finalMapTarget = Join-Path $oneDriveTarget "_Map"

Write-Host ">>> Starting source export at: $rootDir" -ForegroundColor Cyan

# 1. Create/Recreate local _Map directory
if (Test-Path $mapDir) {
    Remove-Item -Path $mapDir -Recurse -Force
}
New-Item -ItemType Directory -Path $mapDir -Force | Out-Null
Write-Host "[OK] Initialized local _Map folder." -ForegroundColor Green

# Helper function to aggregate files and write to target text file
function Aggregate-Files {
    param (
        [string]$OutputFile,
        $Files
    )

    if (-not $Files -or $Files.Count -eq 0) { return }

    $outPath = Join-Path $mapDir $OutputFile
    $sb = New-Object System.Text.StringBuilder

    foreach ($file in $Files) {
        $relativePath = $file.FullName.Substring($rootDir.Length).TrimStart('\', '/')
        [void]$sb.AppendLine("// " + ("=" * 70))
        [void]$sb.AppendLine("// FILE: " + $relativePath)
        [void]$sb.AppendLine("// " + ("=" * 70))
        [void]$sb.AppendLine()

        $content = [System.IO.File]::ReadAllText($file.FullName)
        [void]$sb.AppendLine($content)
        [void]$sb.AppendLine()
    }

    [System.IO.File]::WriteAllText($outPath, $sb.ToString(), [System.Text.Encoding]::UTF8)
    $msg = "  -> Generated: " + $OutputFile + " (" + $Files.Count + " files)"
    Write-Host $msg -ForegroundColor Gray
}

# ------------------------------------------------------------------------------
# 2. Perform Action A for Root Level files
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host ">>> Processing Root Directory..." -ForegroundColor Yellow
$rootFiles = @(Get-ChildItem -Path $rootDir -File -Filter "*.cs")
if ($rootFiles.Count -gt 0) {
    Aggregate-Files -OutputFile "Editor.txt" -Files $rootFiles
}

# ------------------------------------------------------------------------------
# 3. Perform Action A for R (Root-level folders) and SR (1-level down folders)
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host ">>> Processing Subdirectories (R and SR)..." -ForegroundColor Yellow

$rFolders = @(Get-ChildItem -Path $rootDir -Directory | Where-Object { $_.Name -ne "_Map" -and $_.Name -notlike ".*" })

foreach ($r in $rFolders) {
    # --- Action A for R level (direct files only) ---
    $rFiles = @(Get-ChildItem -Path $r.FullName -File -Filter "*.cs")
    if ($rFiles.Count -gt 0) {
        $fileName = $r.Name + ".txt"
        Aggregate-Files -OutputFile $fileName -Files $rFiles
    }

    # --- Action A for SR level (subfolders and all their nested descendants) ---
    $srFolders = @(Get-ChildItem -Path $r.FullName -Directory | Where-Object { $_.Name -notlike ".*" })
    foreach ($sr in $srFolders) {
        $srFiles = @(Get-ChildItem -Path $sr.FullName -File -Filter "*.cs" -Recurse)
        if ($srFiles.Count -gt 0) {
            $fileName = $r.Name + "." + $sr.Name + ".txt"
            Aggregate-Files -OutputFile $fileName -Files $srFiles
        }
    }
}

# ------------------------------------------------------------------------------
# 4. Copy _Map to OneDrive Documents
# ------------------------------------------------------------------------------
Write-Host ""
Write-Host ">>> Copying to OneDrive..." -ForegroundColor Yellow

if (Test-Path $finalMapTarget) {
    Write-Host ("  -> Target already exists. Removing: " + $finalMapTarget) -ForegroundColor DarkGray
    Remove-Item -Path $finalMapTarget -Recurse -Force
}

Copy-Item -Path $mapDir -Destination $oneDriveTarget -Recurse -Force
Write-Host ("[OK] Successfully copied _Map to: " + $finalMapTarget) -ForegroundColor Green
Write-Host ""
Write-Host ">>> Done!" -ForegroundColor Cyan