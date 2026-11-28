// -----------------------------------------------------------------------
// <copyright file="MatchResultStaticFactoryScanner.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyClub.PlayUp.Application.Tests.Architecture;

/// <summary>
/// Finds product-code references to <see cref="MyClub.PlayUp.Domain.Matches.MatchResult"/> static factories
/// and <c>ConfrontationTieEligibility.CreateProof</c> (invocation, method group, <c>nameof</c>).
/// </summary>
internal static class MatchResultStaticFactoryScanner
{
    private static readonly string[] ProductRoots =
    [
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Application"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Host"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Development"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Domain"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Infrastructure"),
    ];

    private static readonly Dictionary<string, HashSet<string>> AllowedPathsByTarget = new(StringComparer.Ordinal)
    {
        ["MatchResult.CreateConfrontationTiePenaltyShootout"] =
        [
            "src/PlayUp/MyClub.PlayUp.Domain/Matches/MatchResult.cs",
            "src/PlayUp/MyClub.PlayUp.Application/Matches/ConfrontationTiePenaltyShootoutEligibility.cs",
        ],
        ["MatchResult.Rehydrate"] =
        [
            "src/PlayUp/MyClub.PlayUp.Domain/Matches/MatchResult.cs",
            "src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Converters/MatchResultJson.cs",
            "src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Converters/MatchResultJsonConverter.cs",
        ],
        ["ConfrontationTieEligibility.CreateProof"] =
        [
            "src/PlayUp/MyClub.PlayUp.Domain/Matches/ConfrontationTieEligibility.cs",
            "src/PlayUp/MyClub.PlayUp.Application/Matches/ConfrontationTiePenaltyShootoutEligibility.cs",
        ],
    };

    public static IReadOnlyList<string> DiscoverMatchResultStaticFactoryNames()
    {
        var matchResultType = typeof(MyClub.PlayUp.Domain.Matches.MatchResult);
        return matchResultType
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(method => method.ReturnType == matchResultType)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
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
                if (relative.Contains("/Migrations/", StringComparison.Ordinal))
                {
                    continue;
                }

                violations.AddRange(FindViolations(File.ReadAllText(path), relative));
            }
        }

        return violations;
    }

    public static IReadOnlyList<string> FindViolations(string source, string relativePath)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: relativePath);
        var root = tree.GetCompilationUnitRoot();
        var violations = new List<string>();

        foreach (var reference in EnumerateReferences(root))
        {
            if (IsMethodDefinition(reference.Node))
            {
                continue;
            }

            var targetKey = $"{reference.TypeName}.{reference.MethodName}";
            if (!AllowedPathsByTarget.TryGetValue(targetKey, out var allowed))
            {
                continue;
            }

            if (allowed.Contains(relativePath, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var line = tree.GetLineSpan(reference.Node.Span).StartLinePosition.Line + 1;
            var kind = reference.Kind;
            violations.Add($"{relativePath}:{line} {targetKey} ({kind})");
        }

        return violations;
    }

    private static IEnumerable<(SyntaxNode Node, string TypeName, string MethodName, string Kind)> EnumerateReferences(
        SyntaxNode root)
    {
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.Text: var typeName },
                    Name.Identifier.Text: var methodName
                })
            {
                if (IsTrackedTarget(typeName, methodName))
                {
                    yield return (invocation, typeName, methodName, "invoke");
                }
            }
        }

        foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (memberAccess.Expression is not IdentifierNameSyntax { Identifier.Text: var typeName })
            {
                continue;
            }

            var methodName = memberAccess.Name.Identifier.Text;
            if (!IsTrackedTarget(typeName, methodName))
            {
                continue;
            }

            if (memberAccess.Parent is InvocationExpressionSyntax invocation && invocation.Expression == memberAccess)
            {
                continue;
            }

            yield return (memberAccess, typeName, methodName, "method-group-or-member");
        }

        foreach (var nameofInvocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (nameofInvocation.Expression is not IdentifierNameSyntax { Identifier.Text: "nameof" })
            {
                continue;
            }

            if (nameofInvocation.ArgumentList.Arguments.Count != 1)
            {
                continue;
            }

            if (nameofInvocation.ArgumentList.Arguments[0].Expression is not MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.Text: var typeName },
                    Name.Identifier.Text: var methodName
                })
            {
                continue;
            }

            if (!IsTrackedTarget(typeName, methodName))
            {
                continue;
            }

            yield return (nameofInvocation, typeName, methodName, "nameof");
        }
    }

    private static bool IsTrackedTarget(string typeName, string methodName) =>
        (typeName == "MatchResult" && (methodName == "CreateConfrontationTiePenaltyShootout" || methodName == "Rehydrate"))
        || (typeName == "ConfrontationTieEligibility" && methodName == "CreateProof");

    private static bool IsMethodDefinition(SyntaxNode node)
    {
        var methodDecl = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDecl is null)
        {
            return false;
        }

        var typeDecl = methodDecl.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeDecl is null)
        {
            return false;
        }

        if (node is MemberAccessExpressionSyntax memberAccess
            && methodDecl.Identifier.Text == memberAccess.Name.Identifier.Text
            && typeDecl.Identifier.Text == (memberAccess.Expression as IdentifierNameSyntax)?.Identifier.Text)
        {
            return true;
        }

        return false;
    }
}
