// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MyClub.Teamup.Application.Dtos
{
    public class GroupStageDto : RoundDto
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public List<GroupDto>? Groups { get; set; }
    }
}
