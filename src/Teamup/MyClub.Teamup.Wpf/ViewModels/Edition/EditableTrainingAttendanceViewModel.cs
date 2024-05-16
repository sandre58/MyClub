// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using MyNet.UI.Commands;
using MyNet.Utilities.Helpers;
using MyNet.Utilities;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Translatables;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableTrainingAttendanceViewModel : EditableObject, IIdentifiable<Guid>
    {
        public Guid? Id { get; private set; }

        Guid IIdentifiable<Guid>.Id => Id ?? Guid.NewGuid();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public PlayerViewModel Player { get; }

        [IsRequired]
        [Display(Name = nameof(Attendance), ResourceType = typeof(MyClubResources))]
        public Attendance Attendance { get; set; }

        [Display(Name = nameof(Reason), ResourceType = typeof(MyClubResources))]
        public string? Reason { get; set; }

        [Display(Name = nameof(Rating), ResourceType = typeof(MyClubResources))]
        public AcceptableValue<double> Rating { get; set; } = new AcceptableValue<double>(TrainingAttendance.AcceptableRangeRating);

        [Display(Name = nameof(Comment), ResourceType = typeof(MyClubResources))]
        public string? Comment { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public IEnumerable<double> AllowedRatings { get; private set; } = EnumerableHelper.Range(TrainingAttendance.AcceptableRangeRating.MinOrDefault(), TrainingAttendance.AcceptableRangeRating.MaxOrDefault(), 0.5);

        public ICommand UpRatingCommand { get; set; }

        public ICommand DownRatingCommand { get; set; }


        public EditableTrainingAttendanceViewModel(PlayerViewModel player, Attendance attendance, string? comment = null, string? reason = null, double? rating = null)
        {
            Player = player;
            Attendance = attendance;
            Comment = comment;
            Reason = reason;
            Rating.Value = rating;

            DownRatingCommand = CommandsManager.Create<object>(DownRating, CanDownRating);
            UpRatingCommand = CommandsManager.Create<object>(UpRating, CanUpRating);
        }

        #region Rating

        private void DownRating(object? obj)
        {
            if (obj is not ComboBox cb)
            {
                return;
            }

            _ = double.TryParse(cb.Text, out var value);

            cb.Text = AllowedRatings.LastOrDefault(x => x < value).ToString(CultureInfo.CurrentCulture);
        }

        private bool CanDownRating(object? obj) => !(obj is ComboBox cb && double.TryParse(cb.Text, out var value)) || value > AllowedRatings.FirstOrDefault();

        private void UpRating(object? obj)
        {
            if (obj is not ComboBox cb)
            {
                return;
            }

            _ = double.TryParse(cb.Text, out var value);

            cb.Text = AllowedRatings.FirstOrDefault(x => x > value).ToString(CultureInfo.CurrentCulture);
        }

        private bool CanUpRating(object? obj) => !(obj is ComboBox cb && double.TryParse(cb.Text, out var value)) || value < AllowedRatings.LastOrDefault();

        #endregion
    }
}
