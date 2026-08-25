// -----------------------------------------------------------------------
// <copyright file="ScenarioContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Development.Datasets;

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Persistence-agnostic execution context for a single scenario or template run.
/// </summary>
public sealed class ScenarioContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioContext"/> class.
    /// </summary>
    public ScenarioContext(
        string scenarioId,
        int seed,
        SeedProgress progress,
        ICompetitionRepository competitions,
        IStageRepository stages,
        IMatchRepository matches,
        IUnitOfWork unitOfWork,
        ControlledClock clock,
        DeterministicIdFactory ids,
        DeterministicEntropy entropy,
        DatasetCatalog datasets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ScenarioId = scenarioId;
        Seed = seed;
        Progress = progress;
        Competitions = competitions ?? throw new ArgumentNullException(nameof(competitions));
        Stages = stages ?? throw new ArgumentNullException(nameof(stages));
        Matches = matches ?? throw new ArgumentNullException(nameof(matches));
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        Entropy = entropy ?? throw new ArgumentNullException(nameof(entropy));
        Datasets = datasets ?? throw new ArgumentNullException(nameof(datasets));
    }

    /// <summary>Gets the scenario or template identity.</summary>
    public string ScenarioId { get; }

    /// <summary>Gets the workspace seed.</summary>
    public int Seed { get; }

    /// <summary>Gets the requested seed progress.</summary>
    public SeedProgress Progress { get; }

    /// <summary>Gets the competition port.</summary>
    public ICompetitionRepository Competitions { get; }

    /// <summary>Gets the stage port.</summary>
    public IStageRepository Stages { get; }

    /// <summary>Gets the match port.</summary>
    public IMatchRepository Matches { get; }

    /// <summary>Gets the unit of work.</summary>
    public IUnitOfWork UnitOfWork { get; }

    /// <summary>Gets the controlled clock for this run.</summary>
    public ControlledClock Clock { get; }

    /// <summary>Gets the deterministic id factory.</summary>
    public DeterministicIdFactory Ids { get; }

    /// <summary>Gets the deterministic entropy source.</summary>
    public DeterministicEntropy Entropy { get; }

    /// <summary>Gets JSON team datasets.</summary>
    public DatasetCatalog Datasets { get; }
}
