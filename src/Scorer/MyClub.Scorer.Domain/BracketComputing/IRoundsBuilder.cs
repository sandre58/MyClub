﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Domain.BracketComputing
{
    public interface IRoundsBuilder
    {
        IEnumerable<Round> Build(Knockout stage, IRoundsAlgorithm algorithm);
    }
}
