// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MyClub.Teamup.Application.Dtos
{
    public class LeagueDto : CompetitionDto
    {
        public IDictionary<Guid, int>? Penalties { get; set; }
    }
}
