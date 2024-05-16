// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Teamup.Application.Dtos
{
    public class TrainingSessionMultipleDto : TrainingSessionDto
    {
        public bool UpdateTheme { get; set; }

        public bool UpdatePlace { get; set; }
    }
}
