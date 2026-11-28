// -----------------------------------------------------------------------
// <copyright file="MatchResultStaticFactoryScanner.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyClub.PlayUp.Application.Tests.Architecture;

/// <summary>
/// Semantically scans all <c>src/</c> C# for calls to confrontation tie <see cref="MyClub.PlayUp.Domain.Matches.MatchResult"/> factories.
/// </summary>
/// <remarks>
/// Reflection (<c>GetMethod</c> / <c>Invoke</c> / <c>Activator</c>) bypasses this guard; that limitation is accepted and documented on the architecture tests.
/// </remarks>
internal static class MatchResultStaticFactoryScanner
{
    private const string CreateMethod = "CreateConfrontationTiePenaltyShootout";
    private const string RehydrateMethod = "Rehydrate";

    private static readonly Dictionary<string, HashSet<string>> AllowedPathsByMethod = new(StringComparer.Ordinal)
    {
        [CreateMethod] =
        [
            "src/PlayUp/MyClub.PlayUp.Domain/Matches/MatchResult.cs",
            "src/PlayUp/MyClub.PlayUp.Application/Matches/ConfrontationTiePenaltyShootoutEligibility.cs",
        ],
        [RehydrateMethod] =
        [
            "src/PlayUp/MyClub.PlayUp.Domain/Matches/MatchResult.cs",
            "src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Converters/MatchResultJson.cs",
            "src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Converters/MatchResultJsonConverter.cs",
        ],
    };

    public static IReadOnlyList<string> DiscoverPublicMatchResultStaticFactoryNames() =>
        typeof(MyClub.PlayUp.Domain.Matches.MatchResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.ReturnType == typeof(MyClub.PlayUp.Domain.Matches.MatchResult))
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<string> ScanRepository(string repoRoot)
    {
        var srcRoot = Path.Combine(repoRoot, "src");
        if (!Directory.Exists(srcRoot))
        {
            return [];
        }

        var sourcePaths = Directory
            .EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("/Migrations/", StringComparison.Ordinal))
            .ToList();

        var trees = sourcePaths
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
            .ToImmutableArray();

        var references = BuildMetadataReferences(repoRoot);
        var compilation = CSharpCompilation.Create(
            assemblyName: "ProductSemanticScan",
            syntaxTrees: trees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var violations = new List<string>();
        foreach (var tree in trees)
        {
            var relative = Path.GetRelativePath(repoRoot, tree.FilePath).Replace('\\', '/');
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            violations.AddRange(FindViolations(model, tree.GetRoot(), relative));
        }

        return violations;
    }

    public static IReadOnlyList<string> FindViolations(
        string source,
        string relativePath,
        IReadOnlyList<MetadataReference>? extraReferences = null)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: relativePath);
        var references = extraReferences is { Count: > 0 }
            ? extraReferences
            : BuildMetadataReferences(FindRepositoryRootFromCaller());
        var compilation = CSharpCompilation.Create(
            assemblyName: "MyClub.PlayUp.Infrastructure",
            syntaxTrees: [tree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
        return FindViolations(model, tree.GetRoot(), relativePath).ToList();
    }

    private static IEnumerable<string> FindViolations(
        SemanticModel model,
        SyntaxNode? root,
        string relativePath)
    {
        if (root is null)
        {
            yield break;
        }

        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in root.DescendantNodesAndSelf())
        {
            if (IsMethodDefinitionSite(model, node))
            {
                continue;
            }

            var symbol = ResolveMethodSymbol(model, node);
            if (symbol is null)
            {
                continue;
            }

            var methodName = symbol.Name;
            if (!AllowedPathsByMethod.ContainsKey(methodName))
            {
                continue;
            }

            if (AllowedPathsByMethod[methodName].Contains(relativePath, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var line = node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
            var kind = node.Kind().ToString();
            var message = $"{relativePath}:{line} MatchResult.{methodName} ({kind})";
            if (!reported.Add(message))
            {
                continue;
            }

            yield return message;
        }
    }

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

    private static IMethodSymbol? ResolveMethodSymbol(SemanticModel model, SyntaxNode node) =>
        node switch
        {
            InvocationExpressionSyntax invocation => ResolveFromSymbolInfo(model.GetSymbolInfo(invocation)),
            MemberAccessExpressionSyntax memberAccess => ResolveFromSymbolInfo(model.GetSymbolInfo(memberAccess)),
            IdentifierNameSyntax identifier => ResolveFromSymbolInfo(model.GetSymbolInfo(identifier)),
            _ => null,
        };

    private static IMethodSymbol? ResolveFromSymbolInfo(SymbolInfo info)
    {
        if (info.Symbol is IMethodSymbol direct && IsTrackedFactory(direct))
        {
            return direct;
        }

        return info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(IsTrackedFactory);
    }

    private static bool IsTrackedFactory(IMethodSymbol symbol) =>
        symbol.ContainingType.Name == "MatchResult"
        && symbol.ContainingNamespace?.ToDisplayString() == "MyClub.PlayUp.Domain.Matches"
        && (symbol.Name == CreateMethod || symbol.Name == RehydrateMethod);

    private static bool IsMethodDefinitionSite(SemanticModel model, SyntaxNode node)
    {
        var methodDecl = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDecl is null)
        {
            return false;
        }

        if (methodDecl.Identifier.Text != CreateMethod && methodDecl.Identifier.Text != RehydrateMethod)
        {
            return false;
        }

        if (model.GetDeclaredSymbol(methodDecl) is not IMethodSymbol methodSymbol || !IsTrackedFactory(methodSymbol))
        {
            return false;
        }

        return methodDecl.DescendantNodesAndSelf().Contains(node);
    }

    private static ImmutableArray<MetadataReference> BuildMetadataReferences(string repoRoot)
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MyClub.PlayUp.Domain.Matches.MatchResult).Assembly.Location),
        };

        foreach (var referenceName in typeof(MyClub.PlayUp.Domain.Matches.MatchResult).Assembly.GetReferencedAssemblies())
        {
            try
            {
                var loaded = Assembly.Load(referenceName);
                if (!string.IsNullOrEmpty(loaded.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(loaded.Location));
                }
            }
            catch (FileNotFoundException)
            {
                // Optional dependency not on disk in test context.
            }
        }

        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (!string.IsNullOrEmpty(trustedAssemblies))
        {
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (File.Exists(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        var srcRoot = Path.Combine(repoRoot, "src");
        if (Directory.Exists(srcRoot))
        {
            foreach (var assemblyPath in Directory.EnumerateFiles(srcRoot, "*.dll", SearchOption.AllDirectories))
            {
                if (assemblyPath.Contains("/bin/", StringComparison.Ordinal)
                    && assemblyPath.Contains("/Release/", StringComparison.Ordinal))
                {
                    references.Add(MetadataReference.CreateFromFile(assemblyPath));
                }
            }
        }

        return references.ToImmutableArray();
    }

    private static string FindRepositoryRootFromCaller()
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
