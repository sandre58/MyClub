// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Domain.Enums;

namespace MyClub.Teamup.Domain.Extensions
{
    public static class AbsenceExtensions
    {
        public static Attendance ToAttendance(this AbsenceType absence)
            => absence switch
            {
                AbsenceType.InSelection => Attendance.InSelection,
                AbsenceType.InHolidays => Attendance.InHolidays,
                _ => Attendance.Apology,
            };
    }
}
