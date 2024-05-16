// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;

namespace MyClub.Teamup.Application.Dtos
{
    public class GroupDto : EntityDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public int Order { get; set; }

        public IEnumerable<Guid>? TeamIds { get; set; }

        public IDictionary<Guid, int>? Penalties { get; set; }
    }
}
