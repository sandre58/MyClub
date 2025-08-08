// -----------------------------------------------------------------------
// <copyright file="SimpleArchitectureTest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.IO;
using System.Reflection;
using Xunit;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Simple architecture test demonstrating the concept without external dependencies.
/// This test validates basic architectural principles using reflection.
/// </summary>
public class SimpleArchitectureTest
{
    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure_Assemblies()
    {
        // Arrange
        var domainAssembly = Assembly.LoadFrom(@"..\..\..\..\src\Scorer\MyClub.Scorer.Domain\bin\Release\net10.0\MyClub.Scorer.Domain.dll");

        // Act
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        // Assert
        foreach (var reference in referencedAssemblies)
        {
            Assert.DoesNotContain("EntityFrameworkCore", reference.Name ?? string.Empty);
            Assert.DoesNotContain("Infrastructure", reference.Name ?? string.Empty);
            Assert.DoesNotContain("Application", reference.Name ?? string.Empty);
        }
    }

    [Fact]
    public void Architecture_Layers_Exist()
    {
        // This test verifies that our main architectural layers exist
        // Domain
        Assert.True(File.Exists(@"..\..\..\..\src\Scorer\MyClub.Scorer.Domain\MyClub.Scorer.Domain.csproj"));

        // Application
        Assert.True(File.Exists(@"..\..\..\..\src\Scorer\MyClub.Scorer.Application\MyClub.Scorer.Application.csproj"));

        // Infrastructure
        Assert.True(File.Exists(@"..\..\..\..\src\Scorer\MyClub.Scorer.Infrastructure.Persistence\MyClub.Scorer.Infrastructure.Persistence.csproj"));

        // Shared layers
        Assert.True(File.Exists(@"..\..\..\..\src\Shared\MyClub.Shared.Kernel\MyClub.Shared.Kernel.csproj"));
        Assert.True(File.Exists(@"..\..\..\..\src\Shared\MyClub.Shared.Domain\MyClub.Shared.Domain.csproj"));
        Assert.True(File.Exists(@"..\..\..\..\src\Shared\MyClub.Shared.Application\MyClub.Shared.Application.csproj"));
        Assert.True(File.Exists(@"..\..\..\..\src\Shared\MyClub.Shared.Infrastructure\MyClub.Shared.Infrastructure.csproj"));
    }
}
