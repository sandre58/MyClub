// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using MyNet.Wpf.Helpers;
using MyNet.Utilities;
using MyClub.Teamup.Domain.Enums;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Helpers
{
    internal static class UiHelper
    {
        public static IEnumerable<(int start, int width, Color color)> HolidaysToSections(IEnumerable<DateTime> dates, IEnumerable<HolidaysViewModel> holidays)
        {
            var sections = new List<(int start, int width, Color color)>();

            if (dates.Any())
            {
                var orderedDates = dates.OrderBy(x => x).ToList();
                var minDate = orderedDates[0];
                var maxDate = orderedDates[orderedDates.Count - 1];
                var validHolidays = holidays.Where(x => x.StartDate.InRange(minDate, maxDate) || x.EndDate.InRange(minDate, maxDate)).ToList();
                foreach (var holiday in validHolidays)
                {
                    var start = orderedDates.Find(x => x >= holiday.StartDate.Date);
                    var end = orderedDates.LastOrDefault(x => x <= holiday.EndDate.Date);

                    var startIndex = orderedDates.IndexOf(start);
                    var endIndex = orderedDates.IndexOf(end);

                    sections.Add((startIndex > -1 ? startIndex : 0, endIndex > -1 ? endIndex - startIndex : orderedDates.Count - 1, holiday.Color));
                }
            }

            return sections;
        }

        public static SolidColorBrush GetBrushFromAttendance(Attendance attendance)
        {
            var resourceName = attendance switch
            {
                Attendance.Unknown => "MyNet.Brushes.None",
                Attendance.Present => "MyNet.Brushes.Positive",
                Attendance.Absent => "MyNet.Brushes.Negative",
                Attendance.Apology => "MyNet.Brushes.Warning",
                Attendance.Injured => "Teamup.Brushes.Attendance.Injured",
                Attendance.InHolidays => "Teamup.Brushes.Attendance.InHolidays",
                Attendance.InSelection => "Teamup.Brushes.Attendance.InSelection",
                Attendance.Resting => "Teamup.Brushes.Attendance.Resting",
                _ => throw new ArgumentOutOfRangeException(nameof(attendance)),
            };

            return WpfHelper.GetResource<SolidColorBrush>(resourceName);
        }

        public static SolidColorBrush GetBrushFromRating(double rating)
        {
            var resourceName = rating switch
            {
                var n when n is >= 0 and < 2 => "Teamup.Brushes.Rating.0To2",
                var n when n is >= 2 and < 3.5 => "Teamup.Brushes.Rating.2To35",
                var n when n is >= 3.5 and < 4.5 => "Teamup.Brushes.Rating.35To45",
                var n when n is >= 4.5 and < 6 => "Teamup.Brushes.Rating.45To6",
                var n when n is >= 6 and < 8 => "Teamup.Brushes.Rating.6To8",
                var n when n >= 8 => "Teamup.Brushes.Rating.8To10",
                _ => throw new ArgumentOutOfRangeException(nameof(rating)),
            };

            return WpfHelper.GetResource<SolidColorBrush>(resourceName);
        }
    }
}
