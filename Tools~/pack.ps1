param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$RepositoryRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot "validate.ps1")
if ($LASTEXITCODE -ne 0) {
    throw "Repository validation failed."
}

Add-Type -AssemblyName System.Formats.Tar
$Package = Get-Content -LiteralPath (Join-Path $RepositoryRoot "package.json") -Raw | ConvertFrom-Json
if ($Package.name -ne "com.abc.unity" -or $Package.version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Package identity or release version is invalid."
}

$OutputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
if ($OutputRoot.StartsWith($RepositoryRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $OutputRoot -eq $RepositoryRoot) {
    throw "Release artifacts must be placed outside the source repository."
}

$IncludedPaths = @(
    ".editorconfig", ".gitattributes", "abc.unity.asmdef", "AGENTS.md", "CHANGELOG.md",
    "LICENSE", "package.json", "README.md", "Scripts", "Tests", "Samples~", "Documentation~", "Tools~"
)
$Files = [System.Collections.Generic.SortedDictionary[string, string]]::new([StringComparer]::Ordinal)
foreach ($RelativePath in $IncludedPaths) {
    $SourcePath = Join-Path $RepositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $SourcePath)) {
        throw "Missing release input: $RelativePath"
    }

    $SourceItems = @(Get-Item -LiteralPath $SourcePath)
    if (Test-Path -LiteralPath $SourcePath -PathType Container) {
        $SourceItems += Get-ChildItem -LiteralPath $SourcePath -Recurse -Force
    }
    if (Test-Path -LiteralPath ($SourcePath + ".meta")) {
        $SourceItems += Get-Item -LiteralPath ($SourcePath + ".meta")
    }

    foreach ($Item in $SourceItems) {
        if (($Item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Release inputs cannot contain links: $($Item.FullName)"
        }
        if ($Item.PSIsContainer) {
            continue
        }

        $EntryName = [System.IO.Path]::GetRelativePath($RepositoryRoot, $Item.FullName).Replace('\', '/')
        $Files[$EntryName] = $Item.FullName
    }
}

[System.IO.Directory]::CreateDirectory($OutputRoot) | Out-Null
$ArchiveName = "$($Package.name)-$($Package.version).tgz"
$ArchivePath = Join-Path $OutputRoot $ArchiveName
$ReceiptPath = Join-Path $OutputRoot "$($Package.name)-$($Package.version).manifest.json"
if ((Test-Path -LiteralPath $ArchivePath) -or (Test-Path -LiteralPath $ReceiptPath)) {
    throw "Release artifacts already exist. Choose a new output directory."
}

$FileRecords = [System.Collections.Generic.List[object]]::new()
$ArchiveStream = [System.IO.File]::Open($ArchivePath, [System.IO.FileMode]::CreateNew)
try {
    $Gzip = [System.IO.Compression.GZipStream]::new($ArchiveStream, [System.IO.Compression.CompressionLevel]::Optimal, $true)
    try {
        $Writer = [System.Formats.Tar.TarWriter]::new($Gzip, [System.Formats.Tar.TarEntryFormat]::Ustar, $true)
        try {
            foreach ($Pair in $Files.GetEnumerator()) {
                $Entry = [System.Formats.Tar.UstarTarEntry]::new([System.Formats.Tar.TarEntryType]::RegularFile, "package/$($Pair.Key)")
                $Entry.ModificationTime = [DateTimeOffset]::UnixEpoch
                $Entry.Mode = [System.IO.UnixFileMode]::UserRead -bor [System.IO.UnixFileMode]::UserWrite -bor
                    [System.IO.UnixFileMode]::GroupRead -bor [System.IO.UnixFileMode]::OtherRead
                $InputStream = [System.IO.File]::OpenRead($Pair.Value)
                try {
                    $Hash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($InputStream)).ToLowerInvariant()
                    $InputStream.Position = 0
                    $Entry.DataStream = $InputStream
                    $Writer.WriteEntry($Entry)
                    $FileRecords.Add([ordered]@{ path = $Pair.Key; sha256 = $Hash })
                }
                finally {
                    $InputStream.Dispose()
                }
            }
        }
        finally {
            $Writer.Dispose()
        }
    }
    finally {
        $Gzip.Dispose()
    }
}
finally {
    $ArchiveStream.Dispose()
}

$Receipt = [ordered]@{
    name = $Package.name
    version = $Package.version
    unity = $Package.unity
    archive = $ArchiveName
    sha256 = (Get-FileHash -LiteralPath $ArchivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    files = $FileRecords
}
$ReceiptJson = $Receipt | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText($ReceiptPath, $ReceiptJson + "`n", [System.Text.UTF8Encoding]::new($false))
Write-Host "Release package: $ArchivePath"
Write-Host "SHA-256: $($Receipt.sha256)"
Write-Host "Receipt: $ReceiptPath"
