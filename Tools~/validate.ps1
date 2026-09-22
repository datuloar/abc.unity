param(
    [string]$UnityEditorPath,
    [string]$UnityProjectPath
)

$ErrorActionPreference = "Stop"
$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Failures = [System.Collections.Generic.List[string]]::new()
$SourceRoots = @("Scripts", "Tests", "Samples~")
$SourceFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()

foreach ($SourceRoot in $SourceRoots) {
    $AbsoluteRoot = Join-Path $RepositoryRoot $SourceRoot
    if (-not (Test-Path -LiteralPath $AbsoluteRoot)) {
        $Failures.Add("Missing source root: $SourceRoot")
        continue
    }

    foreach ($File in Get-ChildItem -LiteralPath $AbsoluteRoot -Recurse -Filter "*.cs" -File) {
        $SourceFiles.Add($File)
    }
}

foreach ($File in $SourceFiles) {
    $RelativePath = [System.IO.Path]::GetRelativePath($RepositoryRoot, $File.FullName)
    $Content = [System.IO.File]::ReadAllText($File.FullName)
    $LineCount = ($Content -split "`r?`n").Count

    if ($LineCount -gt 400) {
        $Failures.Add("Source exceeds 400 lines: $RelativePath ($LineCount)")
    }
    if ($Content -match "(?m)^\s*//|/\*|\*/") {
        $Failures.Add("Source comment found: $RelativePath")
    }
    if ($Content -match "(?i)\bTODO\b|\bFIXME\b|\bHACK\b") {
        $Failures.Add("Deferred-work marker found: $RelativePath")
    }

    if ($RelativePath -match '^Scripts[\\/]Editor[\\/]' -and
        $Content -match '\b(OnGUI|OnInspectorGUI|IMGUIContainer|ReorderableList|GUILayout|EditorGUILayout)\b|\bEditorGUI\.') {
        $Failures.Add("Legacy editor UI found: $RelativePath")
    }

    foreach ($Match in [regex]::Matches($Content, "(?m)^\s*namespace\s+([^\s{]+)")) {
        if (-not $Match.Groups[1].Value.StartsWith("Abc.Unity", [System.StringComparison]::Ordinal)) {
            $Failures.Add("Unexpected namespace in $RelativePath`: $($Match.Groups[1].Value)")
        }
    }
}

$JsonFiles = Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -File | Where-Object {
    $_.Extension -eq ".json" -or $_.Extension -eq ".asmdef"
} | Where-Object {
    [System.IO.Path]::GetRelativePath($RepositoryRoot, $_.FullName) -notmatch "(^|[\\/])(Library|Temp)[\\/]"
}

foreach ($File in $JsonFiles) {
    try {
        [System.IO.File]::ReadAllText($File.FullName) | ConvertFrom-Json | Out-Null
    }
    catch {
        $RelativePath = [System.IO.Path]::GetRelativePath($RepositoryRoot, $File.FullName)
        $Failures.Add("Invalid JSON: $RelativePath")
    }
}

foreach ($SourceRoot in $SourceRoots) {
    $AbsoluteRoot = Join-Path $RepositoryRoot $SourceRoot
    if (-not (Test-Path -LiteralPath $AbsoluteRoot)) {
        continue
    }

    foreach ($Asset in Get-ChildItem -LiteralPath $AbsoluteRoot -Recurse -Force | Where-Object { $_.Name -notlike "*.meta" }) {
        if (-not (Test-Path -LiteralPath ($Asset.FullName + ".meta"))) {
            $RelativePath = [System.IO.Path]::GetRelativePath($RepositoryRoot, $Asset.FullName)
            $Failures.Add("Missing meta file: $RelativePath")
        }
    }
}

$Guids = @{}
foreach ($MetaFile in Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -Filter "*.meta" -File | Where-Object {
    [System.IO.Path]::GetRelativePath($RepositoryRoot, $_.FullName) -notmatch "(^|[\\/])(Library|Temp)[\\/]"
}) {
    $Match = [regex]::Match([System.IO.File]::ReadAllText($MetaFile.FullName), "(?m)^guid:\s*([0-9a-f]{32})\s*$")
    if (-not $Match.Success) {
        $Failures.Add("Invalid Unity GUID: $($MetaFile.FullName)")
        continue
    }

    $Guid = $Match.Groups[1].Value
    if ($Guids.ContainsKey($Guid)) {
        $FirstPath = [System.IO.Path]::GetRelativePath($RepositoryRoot, $Guids[$Guid])
        $SecondPath = [System.IO.Path]::GetRelativePath($RepositoryRoot, $MetaFile.FullName)
        $Failures.Add("Duplicate GUID $Guid`: $FirstPath and $SecondPath")
    }
    else {
        $Guids[$Guid] = $MetaFile.FullName
    }
}

$PackagePath = Join-Path $RepositoryRoot "package.json"
if ($JsonFiles.Count -eq 0 -or $Guids.Count -eq 0) {
    $Failures.Add("Package metadata validation cannot be empty.")
}
$Package = Get-Content -LiteralPath $PackagePath -Raw | ConvertFrom-Json
if ($Package.name -ne "com.abc.unity" -or $Package.version -notmatch '^\d+\.\d+\.\d+$') {
    $Failures.Add("Package identity or release version is invalid.")
}
foreach ($Sample in $Package.samples) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot $Sample.path) -PathType Container)) {
        $Failures.Add("Missing declared sample: $($Sample.path)")
    }
}

$MarkdownFiles = Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -Filter "*.md" -File
foreach ($MarkdownFile in $MarkdownFiles) {
    $Markdown = Get-Content -LiteralPath $MarkdownFile.FullName -Raw
    foreach ($Link in [regex]::Matches($Markdown, '\]\(([^)]+)\)|<img[^>]+src="([^"]+)"')) {
        $Target = if ($Link.Groups[1].Success) { $Link.Groups[1].Value } else { $Link.Groups[2].Value }
        if ($Target -match '^(https?://|mailto:|#)') {
            continue
        }
        $Target = [Uri]::UnescapeDataString(($Target -split '#', 2)[0])
        if (-not (Test-Path -LiteralPath (Join-Path $MarkdownFile.DirectoryName $Target))) {
            $Failures.Add("Broken documentation link in $($MarkdownFile.Name): $Target")
        }
    }
}

if (Test-Path -LiteralPath (Join-Path $RepositoryRoot ".git")) {
    Push-Location $RepositoryRoot
    try {
        $GitOutput = & git diff --check 2>&1
        if ($LASTEXITCODE -ne 0) {
            $Failures.Add("git diff --check failed: $($GitOutput -join ' ')")
        }
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "Git diff check skipped: installed package has no checkout metadata."
}

if ([string]::IsNullOrWhiteSpace($UnityEditorPath) -xor [string]::IsNullOrWhiteSpace($UnityProjectPath)) {
    $Failures.Add("UnityEditorPath and UnityProjectPath must be supplied together.")
}

if (-not [string]::IsNullOrWhiteSpace($UnityEditorPath) -and -not [string]::IsNullOrWhiteSpace($UnityProjectPath)) {
    $ResultPath = Join-Path $UnityProjectPath "TestResults-abc.xml"
    $LogPath = Join-Path $UnityProjectPath "Logs/abc-validation.log"
    New-Item -ItemType Directory -Path (Split-Path -Parent $LogPath) -Force | Out-Null
    $UnityArguments = @(
        "-batchmode",
        "-nographics",
        "-projectPath",
        "`"$UnityProjectPath`"",
        "-runTests",
        "-testPlatform",
        "EditMode",
        "-testResults",
        "`"$ResultPath`"",
        "-logFile",
        "`"$LogPath`""
    )
    $TestStartedAt = [DateTime]::UtcNow
    $UnityProcess = Start-Process -FilePath $UnityEditorPath -ArgumentList $UnityArguments -WindowStyle Hidden -Wait -PassThru
    if ($UnityProcess.ExitCode -ne 0) {
        $Failures.Add("Unity EditMode tests failed with exit code $($UnityProcess.ExitCode). Log: $LogPath")
    }
    if (-not (Test-Path -LiteralPath $ResultPath) -or (Get-Item -LiteralPath $ResultPath).LastWriteTimeUtc -lt $TestStartedAt) {
        $Failures.Add("Unity did not produce a fresh test report. Log: $LogPath")
    }
    else {
        try {
            [xml]$TestReport = Get-Content -LiteralPath $ResultPath -Raw
            $TestRun = $TestReport.'test-run'
            if ($TestRun.result -ne "Passed" -or [int]$TestRun.total -le 0 -or [int]$TestRun.failed -ne 0) {
                $Failures.Add("Unity test report does not contain a successful non-empty run: $ResultPath")
            }
        }
        catch {
            $Failures.Add("Unity test report is invalid: $ResultPath")
        }
    }
}

if ($Failures.Count -gt 0) {
    foreach ($Failure in $Failures) {
        [Console]::Error.WriteLine($Failure)
    }
    exit 1
}

Write-Host "ABC validation passed: $($SourceFiles.Count) C# files, $($JsonFiles.Count) JSON files, $($Guids.Count) unique Unity GUIDs."
