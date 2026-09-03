// -----------------------------------------------------------------------
// <copyright file="ConfigureDrawInputs.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: configure inputs on a Draft Draw.
/// </summary>
public static class ConfigureDrawInputs
{
    /// <summary>
    /// Configures draw inputs (resets resolution to NotResolved).
    /// </summary>
    /// <param name="stage">Owning stage.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="inputs">Concrete inputs.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Stage stage, DrawId drawId, DrawInputs inputs, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(clock);

        var draw = stage.GetDraw(drawId);
        if (draw.Status != DrawStatus.Draft)
        {
            throw new ApplicationFailureException(
                $"Draw '{drawId}' must be Draft to configure inputs (status is '{draw.Status}').",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        inputs.EnsureCompatibleWith(draw.Kind);
        stage.ConfigureDrawInputs(drawId, inputs);
    }
}
