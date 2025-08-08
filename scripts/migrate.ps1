# Migration helper script
# Usage: .\scripts\migrate.ps1 -Application Scorer -Provider SqlServer -Environment Development -Action "add" -Name "InitSchema"
#        .\scripts\migrate.ps1 -Application Scorer -Provider SqlServer -Environment Development -Action "remove"

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Scorer", "Teamup")]
    [string]$Application,

    [Parameter(Mandatory=$true)]
    [ValidateSet("SqlServer", "Sqlite")]
    [string]$Provider,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Development", "Production", "Staging")]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("add", "remove", "update", "script")]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [string]$Name,
    
    [Parameter(Mandatory=$false)]
    [string]$Output
)

# Set environment
$env:DOTNET_ENVIRONMENT = $Environment

# Determine project path
$projectPath = "src\$Application\MyClub.$Application.Infrastructure.Migrations.$Provider"

Write-Host "Application: $Application" -ForegroundColor Green
Write-Host "Using provider: $Provider" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Green
Write-Host "Project path: $projectPath" -ForegroundColor Green

# Change to project directory
Push-Location $projectPath

try {
    switch ($Action) {
        "add" {
            if (-not $Name) {
                throw "Migration name is required for 'add' action"
            }
            dotnet ef migrations add $Name
        }
        "remove" {
            dotnet ef migrations remove
        }
        "update" {
            dotnet ef database update
        }
        "script" {
            if ($Output) {
                dotnet ef migrations script --output $Output
            } else {
                dotnet ef migrations script
            }
        }
    }
} finally {
    Pop-Location
}