// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.Enums;

namespace MyClub.Teamup.Domain.Factories.Extensions
{
    public static class DefaultValueExtensions
    {
        public static string GetDefaultCondition(this InjuryType type) => type switch
        {
            InjuryType.Other => string.Empty,
            InjuryType.Head => MyClubEnumsResources.InjuryHead,
            InjuryType.Neck => MyClubEnumsResources.InjuryNeck,
            InjuryType.Torso => MyClubEnumsResources.InjuryTorso,
            InjuryType.Back => MyClubEnumsResources.InjuryBack,
            InjuryType.LeftElbow => MyClubEnumsResources.InjuryLeftElbow,
            InjuryType.RightElbow => MyClubEnumsResources.InjuryRightElbow,
            InjuryType.LeftWrist => MyClubEnumsResources.InjuryLeftWrist,
            InjuryType.RightWrist => MyClubEnumsResources.InjuryRightWrist,
            InjuryType.LeftShoulder => MyClubEnumsResources.InjuryLeftShoulder,
            InjuryType.RightShoulder => MyClubEnumsResources.InjuryRightShoulder,
            InjuryType.LeftHand => MyClubEnumsResources.InjuryLeftHand,
            InjuryType.RightHand => MyClubEnumsResources.InjuryRightHand,
            InjuryType.LeftThigh => MyClubEnumsResources.InjuryLeftThigh,
            InjuryType.RightThigh => MyClubEnumsResources.InjuryRightThigh,
            InjuryType.LeftKnee => MyClubEnumsResources.InjuryLeftKnee,
            InjuryType.RightKnee => MyClubEnumsResources.InjuryRightKnee,
            InjuryType.LeftAnkle => MyClubEnumsResources.InjuryLeftAnkle,
            InjuryType.RightAnkle => MyClubEnumsResources.InjuryRightAnkle,
            InjuryType.LeftFoot => MyClubEnumsResources.InjuryLeftFoot,
            InjuryType.RightFoot => MyClubEnumsResources.InjuryRightFoot,
            InjuryType.LeftShin => MyClubEnumsResources.InjuryLeftShin,
            InjuryType.RightShin => MyClubEnumsResources.InjuryRightShin,
            InjuryType.Stomach => MyClubEnumsResources.InjuryStomach,
            InjuryType.LeftArm => MyClubEnumsResources.InjuryLeftArm,
            InjuryType.RightArm => MyClubEnumsResources.InjuryRightArm,
            InjuryType.LeftCalf => MyClubEnumsResources.InjuryLeftCalf,
            InjuryType.RightCalf => MyClubEnumsResources.InjuryRightCalf,
            InjuryType.Adductors => MyClubEnumsResources.InjuryAdductors,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        public static string GetDefaultLabel(this AbsenceType type) => type switch
        {
            AbsenceType.Other => MyClubResources.Other,
            AbsenceType.InHolidays => MyClubResources.InHolidays,
            AbsenceType.InSelection => MyClubResources.InSelection,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
