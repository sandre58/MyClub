<#
.SYNOPSIS
  Builds and tests the Forge kit smoke sample.
#>
[CmdletBinding()]
param(
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$forgeRoot = Split-Path -Parent $PSScriptRoot
Set-Location $forgeRoot

$solution = Join-Path $forgeRoot "Forge.slnx"
if (-not (Test-Path $solution)) {
    throw "Forge.slnx not found at $solution"
}

Write-Host "Restoring $solution"
dotnet restore $solution
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }

Write-Host "Building $solution ($Configuration)"
dotnet build $solution --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

Write-Host "Testing $solution ($Configuration)"
$testOutput = & dotnet test $solution --configuration $Configuration --no-build --verbosity normal 2>&1 | Out-String
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet test failed" }

# Normalize NBSP / narrow spaces used by localized dotnet CLI
$normalized = $testOutput -replace '[\u00A0\u202F\u2007]', ' '

if ($normalized -match "Aucun test n'est disponible" -or $normalized -match "No test is available") {
    throw "Verify-Forge: test host found no tests (adapter/discovery failure)."
}

$ok = $false
if ($normalized -match 'Nombre total de tests\s*:\s*(\d+)' -or $normalized -match 'Total tests:\s*(\d+)') {
    $ok = [int]$Matches[1] -ge 1
}
elseif ($normalized -match 'r[eé]ussite\s*:\s*(\d+)' -or $normalized -match 'Passed:\s*(\d+)') {
    $ok = [int]$Matches[1] -ge 1
}
elseif ($normalized -match 'Discovered:\s+\S+\s+\((\d+)\s+test') {
    $ok = [int]$Matches[1] -ge 1
}

if (-not $ok) {
    throw "Verify-Forge: expected at least 1 passing/discovered test in output."
}

Write-Host "Verify-Forge: OK"
