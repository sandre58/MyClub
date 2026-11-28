// -----------------------------------------------------------------------
// <copyright file="StoredTieFormatPropertyReadArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Architecture;

/// <summary>
/// A'9-N1 — stored <c>Round.TieFormat</c> / <c>StageRegulation.TieFormat</c> reads only in whitelisted resolvers,
/// persistence, DTO mapping, and tie-format editing.
/// </summary>
public sealed class StoredTieFormatPropertyReadArchitectureTests
{
    private static readonly string[] ProductRoots =
    [
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Application"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Host"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Development"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Domain"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Infrastructure"),
    ];

    private static readonly string[] AllowListedFileSuffixes =
    [
        "src/PlayUp/MyClub.PlayUp.Domain/Rules/TieFormat/TieFormat.cs",
        "src/PlayUp/MyClub.PlayUp.Domain/Stages/Round.cs",
        "src/PlayUp/MyClub.PlayUp.Domain/Rules/StageRegulation.cs",
        "src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Configurations/RoundConfiguration.cs",
    ];

    private static readonly string[] AllowListedTypeNames =
    [
        "StructureViewAssembler",
        "ApplyStageTieFormatEdits",
    ];

    private static readonly (string Type, string Method)[] AllowListedMethods =
    [
        ("Stage", "ResolveEffectiveTieFormat"),
        ("Stage", "ResolveEffectiveTieFormatForKey"),
        ("Stage", "ResolveEffectiveDefault"),
        ("TieFormat", "ResolveEffective"),
        ("TieFormat", "OrDefaultOneLeg"),
    ];

    [Fact]
    public void Product_code_reads_stored_TieFormat_only_in_whitelisted_members()
    {
        var repoRoot = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var root in ProductRoots)
        {
            var directory = Path.Combine(repoRoot, root);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                if (ShouldSkipFile(relative))
                {
                    continue;
                }

                var source = File.ReadAllText(path);
                var tree = CSharpSyntaxTree.ParseText(source, path: relative);
                var compilationUnit = tree.GetCompilationUnitRoot();

                foreach (var memberAccess in compilationUnit
                             .DescendantNodes()
                             .OfType<MemberAccessExpressionSyntax>())
                {
                    if (memberAccess.Name.Identifier.Text != "TieFormat")
                    {
                        continue;
                    }

                    if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "TieFormat" })
                    {
                        continue;
                    }

                    if (IsInsideNameOf(memberAccess))
                    {
                        continue;
                    }

                    if (IsInsideTypePropertyDeclaration(memberAccess))
                    {
                        continue;
                    }

                    var (typeName, methodName) = GetEnclosingContext(memberAccess);
                    if (IsAllowListed(relative, typeName, methodName))
                    {
                        continue;
                    }

                    var line = tree.GetLineSpan(memberAccess.Span).StartLinePosition.Line + 1;
                    violations.Add($"{relative}:{line} ({typeName}.{methodName})");
                }
            }
        }

        violations.Should().BeEmpty(
            "read stored TieFormat via Stage.ResolveEffectiveTieFormat / Stage.ResolveEffectiveDefault / "
            + "Structure DTO assemblers, or extend the explicit whitelist with justification");
    }

    private static bool IsInsideTypePropertyDeclaration(SyntaxNode node) =>
        node.Ancestors().OfType<PropertyDeclarationSyntax>()
            .Any(property => property.Identifier.Text == "TieFormat");

    private static bool IsInsideNameOf(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is InvocationExpressionSyntax invocation
                && invocation.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" })
            {
                return true;
            }
        }

        return false;
    }

    private static (string? TypeName, string? MethodName) GetEnclosingContext(SyntaxNode node)
    {
        var type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        var method = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        return (type?.Identifier.Text, method?.Identifier.Text);
    }

    private static bool ShouldSkipFile(string relative) =>
        relative.Contains("/Migrations/", StringComparison.Ordinal)
        || relative.Contains("/Persistence/Converters/", StringComparison.Ordinal);

    private static bool IsAllowListed(string relative, string? typeName, string? methodName)
    {
        if (AllowListedFileSuffixes.Any(suffix =>
                relative.Equals(suffix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (typeName is not null
            && AllowListedTypeNames.Any(allowed => string.Equals(typeName, allowed, StringComparison.Ordinal)))
        {
            return true;
        }

        if (typeName is not null && methodName is not null)
        {
            return AllowListedMethods.Any(pair =>
                string.Equals(pair.Type, typeName, StringComparison.Ordinal)
                && string.Equals(pair.Method, methodName, StringComparison.Ordinal));
        }

        return false;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MyClub.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
