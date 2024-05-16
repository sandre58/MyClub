// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class ProjectMetadataDto
    {
        public string? Name { get; set; }

        public string? Color { get; set; }

        public byte[]? Image { get; set; }

        public Guid? MainTeamId { get; set; }

        public ClubDto? Club { get; set; }

        public SeasonDto? Season { get; set; }

        public Category? Category { get; set; }

        public TimeSpan TrainingDuration { get; set; }

        public TimeSpan TrainingStartTime { get; set; }
    }
}
