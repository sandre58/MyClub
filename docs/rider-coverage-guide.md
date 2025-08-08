# Code Coverage Guide - Rider

## 🚀 Initial Setup

### 1. Configure Rider
1. **Settings** (`Ctrl+Alt+S`) → **Unit Testing** → **VSTest**
2. Check ✅ **Use settings file**
3. Select `MyClub.runsettings`

### 2. Configured Files
- ✅ `MyClub.runsettings` - Coverage configuration
- ✅ `MyClub.sln.DotSettings` - Rider preferences

## 🎯 Usage

### Method 1: Rider Interface
```
Run → Cover Unit Tests (Ctrl+Shift+F11)
```

### Method 2: Terminal
```bash
dotnet test --collect:"XPlat Code Coverage" --settings MyClub.runsettings
```

### Method 3: Right-click
- On a test project → **Cover Unit Tests**
- On a test class → **Cover Unit Tests in Class**
- On a method → **Cover Unit Test**

## 📊 Visualization

### Coverage Window
1. **View** → **Tool Windows** → **Coverage**
2. Real-time metrics by assembly/namespace/class

### In Editor
- 🟢 **Green**: Covered lines
- 🔴 **Red**: Uncovered lines  
- 🟡 **Yellow**: Partially covered branches

### Reports
1. Right-click in **Coverage** → **Export Coverage Report**
2. Formats: HTML, XML, JSON

## 🎯 Target Metrics

| Component | Target | Current |
|-----------|--------|---------|
| **Domain** | 90%+ | To check |
| **Application** | 80%+ | ~0% |
| **Infrastructure** | 70%+ | To check |
| **Shared** | 80%+ | To check |

## 🔧 Useful Commands

### Quick tests with coverage
```bash
# Tests for a specific project
dotnet test tests/MyClub.Scorer.Domain.Tests --collect:"XPlat Code Coverage" --settings MyClub.runsettings

# Tests with filter
dotnet test --filter "Category=Unit" --collect:"XPlat Code Coverage" --settings MyClub.runsettings
```

### Report generation
```bash
# With ReportGenerator (if installed)
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html

# Open the report
start ./coverage-report/index.html
```

## ❌ Configured Exclusions

### Excluded Assemblies
- ✅ `*Tests*.dll` - Test projects
- ✅ `*Migrations*.dll` - EF Core migrations

### Excluded Files
- ✅ `*.Designer.cs` - Generated files
- ✅ `*ModelSnapshot.cs` - EF snapshots
- ✅ `Program.cs` / `Startup.cs` - Entry points

### Excluded Attributes
- ✅ `[ExcludeFromCodeCoverage]`
- ✅ `[GeneratedCode]`
- ✅ `[CompilerGenerated]`

## 🎯 Next Steps

1. **Analyze** areas with 0% coverage
2. **Create** missing unit tests
3. **Target** 80%+ coverage on business code
4. **Integrate** into CI/CD with quality gates
