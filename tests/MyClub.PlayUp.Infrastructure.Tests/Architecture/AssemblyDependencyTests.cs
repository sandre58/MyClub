// -----------------------------------------------------------------------
// <copyright file="AssemblyDependencyTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Architecture;

public sealed class AssemblyDependencyTests
{
    [Fact]
    public void Domain_assembly_does_not_reference_ef_npgsql_or_aspnet() =>
        ReferencedAssemblyNames(typeof(CompetitionId)).Should().NotContain(static name => IsForbiddenDependency(name));

    [Fact]
    public void Application_assembly_does_not_reference_ef_npgsql_or_aspnet() =>
        ReferencedAssemblyNames(typeof(IUnitOfWork)).Should().NotContain(static name => IsForbiddenDependency(name));

    private static IEnumerable<string> ReferencedAssemblyNames(Type type) =>
        type.Assembly.GetReferencedAssemblies().Select(static assembly => assembly.Name!);

    private static bool IsForbiddenDependency(string name) =>
        name.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase);
}
