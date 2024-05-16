// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;
using MyClub.Teamup.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class TrainingAttendanceDto : EntityDto
    {
        public Guid PlayerId { get; set; }

        public Attendance Attendance { get; set; }

        public string? Reason { get; set; }

        public double? Rating { get; set; }

        public string? Comment { get; set; }
    }
}
