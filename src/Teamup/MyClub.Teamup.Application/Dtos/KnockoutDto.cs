// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Teamup.Application.Dtos
{
    public class KnockoutDto : RoundDto
    {
        public DateTime Date { get; set; }

        public bool IsPostponed { get; set; }

        public DateTime? PostponedDate { get; set; }
    }
}
