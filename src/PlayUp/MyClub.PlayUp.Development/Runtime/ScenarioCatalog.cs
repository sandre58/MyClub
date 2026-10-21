// -----------------------------------------------------------------------
// <copyright file="ScenarioCatalog.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Registry of Development Workspace scenarios.
/// </summary>
public sealed class ScenarioCatalog
{
    private readonly Dictionary<string, IScenario> _byId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioCatalog"/> class.
    /// </summary>
    /// <param name="scenarios">Scenarios to register.</param>
    public ScenarioCatalog(IEnumerable<IScenario> scenarios)
    {
        ArgumentNullException.ThrowIfNull(scenarios);

        // Collection expression cannot carry StringComparer; OrdinalIgnoreCase is required for scenario ids.
#pragma warning disable IDE0028
        _byId = new Dictionary<string, IScenario>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028
        foreach (var scenario in scenarios)
        {
            ArgumentNullException.ThrowIfNull(scenario);
            if (!_byId.TryAdd(scenario.Id, scenario))
            {
                throw new InvalidOperationException($"Duplicate scenario id '{scenario.Id}'.");
            }
        }
    }

    /// <summary>Gets all registered scenarios.</summary>
    public IReadOnlyCollection<IScenario> All => _byId.Values;

    /// <summary>
    /// Resolves a scenario by id.
    /// </summary>
    /// <param name="id">Scenario id.</param>
    /// <returns>The scenario.</returns>
    public IScenario Get(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _byId.TryGetValue(id, out var scenario)
            ? scenario
            : throw new InvalidOperationException($"Unknown Development Workspace scenario '{id}'.");
    }
}
