// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.Extensions
{
    public static class InjuryExtensions
    {
        public static InjuryCategory ToDefaultCategory(this InjuryType type) => type switch
        {
            InjuryType.Other => InjuryCategory.Other,
            InjuryType.Head => InjuryCategory.Trauma,
            InjuryType.Neck => InjuryCategory.Trauma,
            InjuryType.Torso => InjuryCategory.Fracture,
            InjuryType.Back => InjuryCategory.Trauma,
            InjuryType.LeftElbow => InjuryCategory.Fracture,
            InjuryType.RightElbow => InjuryCategory.Fracture,
            InjuryType.LeftWrist => InjuryCategory.Ligament,
            InjuryType.RightWrist => InjuryCategory.Ligament,
            InjuryType.LeftShoulder => InjuryCategory.Ligament,
            InjuryType.RightShoulder => InjuryCategory.Ligament,
            InjuryType.LeftHand => InjuryCategory.Trauma,
            InjuryType.RightHand => InjuryCategory.Trauma,
            InjuryType.LeftThigh => InjuryCategory.Muscular,
            InjuryType.RightThigh => InjuryCategory.Muscular,
            InjuryType.LeftKnee => InjuryCategory.Ligament,
            InjuryType.RightKnee => InjuryCategory.Ligament,
            InjuryType.LeftAnkle => InjuryCategory.Ligament,
            InjuryType.RightAnkle => InjuryCategory.Ligament,
            InjuryType.LeftFoot => InjuryCategory.Fracture,
            InjuryType.RightFoot => InjuryCategory.Fracture,
            InjuryType.LeftShin => InjuryCategory.Fracture,
            InjuryType.RightShin => InjuryCategory.Fracture,
            InjuryType.Stomach => InjuryCategory.Sickness,
            InjuryType.LeftArm => InjuryCategory.Fracture,
            InjuryType.RightArm => InjuryCategory.Fracture,
            InjuryType.LeftCalf => InjuryCategory.Muscular,
            InjuryType.RightCalf => InjuryCategory.Muscular,
            InjuryType.Adductors => InjuryCategory.Muscular,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}
