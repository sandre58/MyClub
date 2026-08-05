<#
.SYNOPSIS
  Creates a new repository from the Forge engineering baseline.

.PARAMETER Name
  Project / folder name (e.g. MyProject).

.PARAMETER Type
  Library (packable NuGet) or App (non-packable).

.PARAMETER Org
  GitHub org/user (default sandre58).

.PARAMETER TargetRoot
  Parent directory (default C:\Dev).

.PARAMETER RepositoryUrl
  Full GitHub URL. Defaults to https://github.com/{Org}/{Name}.

.PARAMETER ProjectDescription
  Short description for the generated README.

.PARAMETER CreateGitHub
  Create remote with gh and push.

.PARAMETER Verify
  Run dotnet restore/build/test after scaffold (default true). Pass -Verify:`$false to skip.

.PARAMETER OpenCursor
  Open the new folder in Cursor.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Name,

    [ValidateSet("Library", "App")]
    [string] $Type = "Library",

    [string] $Org = "sandre58",

    [string] $TargetRoot = "C:\Dev",

    [string] $RepositoryUrl = "",

    [string] $ProjectDescription = "",

    [switch] $CreateGitHub,

    [bool] $Verify = $true,

    [switch] $OpenCursor
)

$ErrorActionPreference = "Stop"

$forgeRoot = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $TargetRoot $Name

if ([string]::IsNullOrWhiteSpace($RepositoryUrl)) {
    $RepositoryUrl = "https://github.com/$Org/$Name"
}

if ([string]::IsNullOrWhiteSpace($ProjectDescription)) {
    if ($Type -eq "Library") {
        $ProjectDescription = "$Name is a .NET library."
    }
    else {
        $ProjectDescription = "$Name is a .NET application."
    }
}

$year = (Get-Date).Year.ToString()

if (Test-Path $destination) {
    throw "Destination already exists: $destination"
}

Write-Host "Creating $Type project '$Name' at $destination"

$excludeNames = @(
    ".git",
    "packages",
    "bin",
    "obj",
    "TestResults",
    "artifacts",
    "samples",
    "templates",
    "docs",
    "README.md",
    "CONTRIBUTING.md",
    "TEMPLATE.md",
    "forge.manifest.json",
    "Forge.slnx",
    "Forge.sln"
)

New-Item -ItemType Directory -Path $destination -Force | Out-Null

Get-ChildItem -Path $forgeRoot -Force | Where-Object {
    $excludeNames -notcontains $_.Name
} | ForEach-Object {
    $target = Join-Path $destination $_.Name
    Copy-Item -Path $_.FullName -Destination $target -Recurse -Force
}

function Replace-Tokens([string] $text) {
    return $text.
        Replace("{{ProjectName}}", $Name).
        Replace("{{RepositoryUrl}}", $RepositoryUrl).
        Replace("{{Year}}", $year).
        Replace("{{ProjectDescription}}", $ProjectDescription).
        Replace("{{Org}}", $Org)
}

function Write-TokenFile([string] $sourceRelative, [string] $destRelative) {
    $source = Join-Path $forgeRoot $sourceRelative
    $dest = Join-Path $destination $destRelative
    $dir = Split-Path -Parent $dest
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $content = Get-Content -Path $source -Raw -Encoding UTF8
    Set-Content -Path $dest -Value (Replace-Tokens $content) -Encoding UTF8 -NoNewline
}

# Project docs from templates (not kit README)
Write-TokenFile "templates\README.project.md" "README.md"
Write-TokenFile "templates\CONTRIBUTING.project.md" "CONTRIBUTING.md"
Write-TokenFile "templates\repo.props" "build\repo.props"
Write-TokenFile "templates\issue-config.yml" ".github\ISSUE_TEMPLATE\config.yml"

# CI by type
$ciSource = if ($Type -eq "Library") { "templates\ci.library.yml" } else { "templates\ci.app.yml" }
Copy-Item (Join-Path $forgeRoot $ciSource) (Join-Path $destination ".github\workflows\ci.yml") -Force

# Remove kit-only templates folder from child (keep decisions docs)
$childTemplates = Join-Path $destination "templates"
if (Test-Path $childTemplates) {
    Remove-Item -Recurse -Force $childTemplates
}

# Remove empty kit placeholders under src/tests before scaffold
$srcDir = Join-Path $destination "src"
$testsDir = Join-Path $destination "tests"
Get-ChildItem $srcDir -Force -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
Get-ChildItem $testsDir -Force -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $srcDir, $testsDir | Out-Null

Push-Location $destination
try {
    Write-Host "Scaffolding solution and projects..."
    dotnet new sln -n $Name --force
    if ($LASTEXITCODE -ne 0) { throw "dotnet new sln failed" }
    $sln = Get-ChildItem -Filter "$Name.sln*" | Select-Object -First 1
    if (-not $sln) { throw "Solution file was not created" }

    $projDir = Join-Path $srcDir $Name
    $testName = "$Name.Tests"
    $testDir = Join-Path $testsDir $testName

    if ($Type -eq "Library") {
        dotnet new classlib -n $Name -o $projDir -f net10.0 --force
        if ($LASTEXITCODE -ne 0) { throw "dotnet new classlib failed" }
    }
    else {
        dotnet new console -n $Name -o $projDir -f net10.0 --force
        if ($LASTEXITCODE -ne 0) { throw "dotnet new console failed" }
    }

    dotnet new xunit -n $testName -o $testDir -f net10.0 --force
    if ($LASTEXITCODE -ne 0) { throw "dotnet new xunit failed" }

    # Ensure packability for libraries
    $csproj = Join-Path $projDir "$Name.csproj"
    if ($Type -eq "Library") {
        $xml = Get-Content $csproj -Raw -Encoding UTF8
        if ($xml -notmatch "IsPackable") {
            $xml = $xml -replace "</PropertyGroup>", "    <IsPackable>true</IsPackable>`r`n  </PropertyGroup>"
            Set-Content -Path $csproj -Value $xml -Encoding UTF8
        }
        else {
            $xml = $xml -replace "<IsPackable>false</IsPackable>", "<IsPackable>true</IsPackable>"
            Set-Content -Path $csproj -Value $xml -Encoding UTF8
        }
    }

    # Replace xunit template csproj: dependencies.props already injects test packages via CPM.
    $testCsproj = Join-Path $testDir "$testName.csproj"
    $cleanTestCsproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\$Name\$Name.csproj" />
  </ItemGroup>
</Project>
"@
    Set-Content -Path $testCsproj -Value $cleanTestCsproj.Trim() -Encoding UTF8

    # Normalize the default unit test file (template relies on Xunit global usings we re-add above)
    $unitTest = Get-ChildItem $testDir -Filter "UnitTest*.cs" | Select-Object -First 1
    if ($unitTest) {
        $unitContent = @"
namespace $testName;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }
}
"@
        Set-Content -Path $unitTest.FullName -Value $unitContent.Trim() -Encoding UTF8
    }

    # Drop stale restore outputs from the template csproj before our clean file
    Remove-Item -Recurse -Force (Join-Path $testDir "obj") -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force (Join-Path $testDir "bin") -ErrorAction SilentlyContinue

    dotnet sln $sln.Name add $csproj | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet sln add project failed" }
    dotnet sln $sln.Name add $testCsproj | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet sln add test project failed" }

    if (Test-Path ".git") {
        Remove-Item -Recurse -Force ".git"
    }
    git init -b main | Out-Null
    git config commit.template .gitmessage
    git add .
    git commit -m "chore: bootstrap from Forge template" | Out-Null

    if ($Verify) {
        Write-Host "Verifying build..."
        dotnet restore $sln.Name
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }
        dotnet build $sln.Name --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
        $testOutput = & dotnet test $sln.Name --configuration Release --no-build --verbosity normal 2>&1 | Out-String
        Write-Host $testOutput
        if ($LASTEXITCODE -ne 0) { throw "dotnet test failed" }
        if ($testOutput -match "Aucun test n'est disponible" -or $testOutput -match "No test is available") {
            throw "dotnet test discovered 0 tests (xUnit adapter missing from output?)."
        }
    }

    if ($CreateGitHub) {
        if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
            throw "gh CLI not found. Install GitHub CLI or omit -CreateGitHub."
        }
        Write-Host "Creating GitHub repository $Org/$Name ..."
        gh repo create "$Org/$Name" --private --source=. --remote=origin --push
        if ($LASTEXITCODE -ne 0) { throw "gh repo create failed" }
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Created: $destination"
Write-Host "Type: $Type"
Write-Host "RepositoryUrl: $RepositoryUrl"

if ($OpenCursor) {
    if (Get-Command cursor -ErrorAction SilentlyContinue) {
        Push-Location $destination
        try { cursor . } finally { Pop-Location }
    }
    else {
        Write-Warning "cursor CLI not found; open $destination manually."
    }
}
