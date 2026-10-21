// -----------------------------------------------------------------------
// <copyright file="TemplateCatalog.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Registry of inspired competition templates.
/// </summary>
public sealed class TemplateCatalog
{
    private readonly Dictionary<string, ICompetitionTemplate> _byId;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateCatalog"/> class.
    /// </summary>
    /// <param name="templates">Templates to register.</param>
    public TemplateCatalog(IEnumerable<ICompetitionTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);

        // Collection expression cannot carry StringComparer; OrdinalIgnoreCase is required for template ids.
#pragma warning disable IDE0028
        _byId = new Dictionary<string, ICompetitionTemplate>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028
        foreach (var template in templates)
        {
            ArgumentNullException.ThrowIfNull(template);
            if (!_byId.TryAdd(template.Id, template))
            {
                throw new InvalidOperationException($"Duplicate template id '{template.Id}'.");
            }
        }
    }

    /// <summary>Gets all registered templates.</summary>
    public IReadOnlyCollection<ICompetitionTemplate> All => _byId.Values;

    /// <summary>
    /// Resolves a template by id.
    /// </summary>
    /// <param name="id">Template id.</param>
    /// <returns>The template.</returns>
    public ICompetitionTemplate Get(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _byId.TryGetValue(id, out var template)
            ? template
            : throw new InvalidOperationException($"Unknown Development template '{id}'.");
    }
}
