param(
    [string]$PackagesPath = (Join-Path $PSScriptRoot '..' 'artifacts' 'packages')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$packagesPath = [System.IO.Path]::GetFullPath($PackagesPath)
$packages = @(Get-ChildItem -Path $packagesPath -Filter '*.nupkg' -File)
if ($packages.Count -eq 0) {
    Write-Host "ERROR: no .nupkg files found in $packagesPath" -ForegroundColor Red
    exit 1
}

$errors = [System.Collections.Generic.List[string]]::new()
$requiredFiles = @('README.md', 'icon.png')
$requiredMetadata = @('id', 'version', 'description', 'license', 'readme', 'icon', 'tags')

foreach ($package in $packages) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
        if (-not $nuspecEntry) {
            $errors.Add("$($package.Name): .nuspec is missing")
            continue
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $metadata = $nuspec.package.metadata
        foreach ($field in $requiredMetadata) {
            $value = [string]$metadata.$field
            if ([string]::IsNullOrWhiteSpace($value)) {
                $errors.Add("$($package.Name): metadata '$field' is empty")
            }
        }

        foreach ($file in $requiredFiles) {
            $entry = $archive.Entries | Where-Object { $_.FullName -eq $file } | Select-Object -First 1
            if (-not $entry) {
                $errors.Add("$($package.Name): '$file' is missing from the package")
            }
        }

        $assembly = $archive.Entries | Where-Object { $_.FullName -like 'lib/net10.0/*.dll' } | Select-Object -First 1
        if (-not $assembly) {
            $errors.Add("$($package.Name): no lib/net10.0 assembly found")
        }

        $expectedId = [string]$metadata.id
        if (-not $package.Name.StartsWith($expectedId + '.', [StringComparison]::Ordinal)) {
            $errors.Add("$($package.Name): file name does not start with package id '$expectedId'")
        }

        $expectedVersion = [string]$metadata.version
        if (-not $package.Name.EndsWith(".$expectedVersion.nupkg", [StringComparison]::Ordinal)) {
            $errors.Add("$($package.Name): file name does not end with version '$expectedVersion'")
        }
    }
    finally {
        $archive.Dispose()
    }
}

if ($errors.Count -gt 0) {
    foreach ($message in $errors) {
        Write-Host "ERROR: $message" -ForegroundColor Red
    }
    exit 1
}

Write-Host "Verified $($packages.Count) package(s) in $packagesPath"
