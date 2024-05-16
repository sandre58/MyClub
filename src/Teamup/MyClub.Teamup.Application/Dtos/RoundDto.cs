// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyClub.Teamup.Domain.CompetitionAggregate;

namespace MyClub.Teamup.Application.Dtos
{
    public class RoundDto : EntityDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public Guid? ParentId { get; set; }

        public IEnumerable<Guid>? TeamIds { get; set; }

        public CompetitionRules? Rules { get; set; }
    }
}
