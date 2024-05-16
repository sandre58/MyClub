// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;
using MyClub.Teamup.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class AbsenceDto : EntityDto
    {
        public AbsenceType Type { get; set; }

        public string? Label { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public Guid? PlayerId { get; set; }
    }
}
