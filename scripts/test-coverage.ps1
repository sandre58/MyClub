Write-Host "🧪 Running tests with coverage..."
dotnet test --no-build --collect:"XPlat Code Coverage"

Write-Host "📊 Generating HTML report..."
reportgenerator -reports:**/coverage*.xml -targetdir:coverage-report -reporttypes:Html

Write-Host "✅ Done! Open coverage-report/index.html"