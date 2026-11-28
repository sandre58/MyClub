// -----------------------------------------------------------------------
// <copyright file="StoredTieFormatReadScanner.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyClub.PlayUp.Application.Tests.Architecture;

/// <summary>
/// Scans C# source for stored <c>Round.TieFormat</c> / <c>StageRegulation.TieFormat</c> member reads.
/// </summary>
internal static class StoredTieFormatReadScanner
{
    private static readonly string[] AllowListedFileSuffixes =
    [
        "src/PlayUp/MyClub.PlayUp.Domain/Rules/TieFormat/TieFormat.cs",
        "src/PlayUp/MyClub.PlayUp.Domain/Stages/Round.cs",
        "src/PlayUp/MyClub.PlayUp.Domain/Rules/StageRegulation.cs",
        "src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Configurations/RoundConfiguration.cs",
    ];

    private static readonly (string Type, string Method)[] AllowListedMembers =
    [
        ("Stage", "ResolveEffectiveTieFormat"),
        ("Stage", "ResolveEffectiveTieFormatForKey"),
        ("Stage", "ResolveEffectiveDefault"),
        ("TieFormat", "ResolveEffective"),
        ("TieFormat", "OrDefaultOneLeg"),
        ("StructureViewAssembler", "BuildStageHubSummary"),
        ("StructureViewAssembler", "StageNeedsTieFormatAction"),
        ("StructureViewAssembler", "BuildConfrontationSegments"),
        ("ApplyStageTieFormatEdits", "Execute"),
    ];

    public static IReadOnlyList<string> FindViolations(string source, string relativePath = "snippet.cs")
    {
        if (ShouldSkipFile(relativePath))
        {
            return [];
        }

        if (AllowListedFileSuffixes.Any(suffix =>
                relativePath.Equals(suffix, StringComparison.OrdinalIgnoreCase)))
        {
            return [];
        }

        var tree = CSharpSyntaxTree.ParseText(source, path: relativePath);
        var compilationUnit = tree.GetCompilationUnitRoot();
        var violations = new List<string>();

        foreach (var node in EnumerateTieFormatPropertyReads(compilationUnit))
        {
            if (node is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "TieFormat" })
            {
                continue;
            }

            if (IsInsideNameOf(node) || IsInsideTypePropertyDeclaration(node))
            {
                continue;
            }

            var (typeName, methodName) = GetEnclosingContext(node);
            if (IsAllowListedMember(typeName, methodName))
            {
                continue;
            }

            var line = tree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
            violations.Add($"{relativePath}:{line} ({typeName}.{methodName})");
        }

        return violations;
    }

    public static IReadOnlyList<string> ScanRepository(string repoRoot)
    {
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
                violations.AddRange(FindViolations(File.ReadAllText(path), relative));
            }
        }

        return violations;
    }

    private static readonly string[] ProductRoots =
    [
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Application"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Host"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Development"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Domain"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Infrastructure"),
    ];

    private static IEnumerable<SyntaxNode> EnumerateTieFormatPropertyReads(SyntaxNode root)
    {
        foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (memberAccess.Name.Identifier.Text == "TieFormat")
            {
                yield return memberAccess;
            }
        }

        foreach (var binding in root.DescendantNodes().OfType<MemberBindingExpressionSyntax>())
        {
            if (binding.Name.Identifier.Text == "TieFormat")
            {
                yield return binding;
            }
        }
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

    private static bool IsAllowListedMember(string? typeName, string? methodName)
    {
        if (typeName is null || methodName is null)
        {
            return false;
        }

        return AllowListedMembers.Any(pair =>
            string.Equals(pair.Type, typeName, StringComparison.Ordinal)
            && string.Equals(pair.Method, methodName, StringComparison.Ordinal));
    }
}
