// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows.Data;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Wpf.Converters
{
    internal sealed class InjuryTypeToCanvasPositionConverter(InjuryTypeToCanvasPositionConverter.CanvasPosition canvasPosition) : IMultiValueConverter, IValueConverter
    {
        public enum CanvasPosition
        {
            Left,

            Top
        }

        private readonly CanvasPosition _position = canvasPosition;

        public static InjuryTypeToCanvasPositionConverter Left { get; } = new(CanvasPosition.Left);

        public static InjuryTypeToCanvasPositionConverter Top { get; } = new(CanvasPosition.Top);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var injuryType = values.Length > 0 ? (InjuryType)values[0] : InjuryType.Other;
            var isFemale = values.Length > 1 && (bool)values[1];
            (double left, double top) male = default;
            (double left, double top) female = default;

            switch (injuryType)
            {
                case InjuryType.Other:
                    male = (45, 2);
                    female = (45, 2);
                    break;

                case InjuryType.Head:
                    male = (21, 2);
                    female = (21, 2);
                    break;

                case InjuryType.Neck:
                    male = (73, 10);
                    female = (71.2, 10);
                    break;

                case InjuryType.Torso:
                    male = (21, 20);
                    female = (21, 20);
                    break;

                case InjuryType.Back:
                    male = (73, 34);
                    female = (71.2, 34);
                    break;

                case InjuryType.LeftElbow:
                    male = (54, 32);
                    female = (58, 31);
                    break;

                case InjuryType.RightElbow:
                    male = (92, 32);
                    female = (86, 31);
                    break;

                case InjuryType.LeftWrist:
                    male = (52, 45);
                    female = (53, 43);
                    break;

                case InjuryType.RightWrist:
                    male = (94, 45);
                    female = (91, 43);
                    break;

                case InjuryType.LeftShoulder:
                    male = (59, 16);
                    female = (60, 15);
                    break;

                case InjuryType.RightShoulder:
                    male = (87, 16);
                    female = (86, 15);
                    break;

                case InjuryType.LeftHand:
                    male = (52, 52);
                    female = (51, 49);
                    break;

                case InjuryType.RightHand:
                    male = (95, 52);
                    female = (92, 49);
                    break;

                case InjuryType.LeftThigh:
                    male = (65, 59);
                    female = (65, 55);
                    break;

                case InjuryType.RightThigh:
                    male = (81, 59);
                    female = (77, 55);
                    break;

                case InjuryType.LeftKnee:
                    male = (64, 68);
                    female = (66, 64);
                    break;

                case InjuryType.RightKnee:
                    male = (83, 68);
                    female = (77, 64);
                    break;

                case InjuryType.LeftAnkle:
                    male = (64, 88);
                    female = (67, 88);
                    break;

                case InjuryType.RightAnkle:
                    male = (82, 88);
                    female = (75, 88);
                    break;

                case InjuryType.LeftFoot:
                    male = (60, 92);
                    female = (66, 93);
                    break;

                case InjuryType.RightFoot:
                    male = (86, 92);
                    female = (76, 93);
                    break;

                case InjuryType.LeftShin:
                    male = (31, 80);
                    female = (27, 77);
                    break;

                case InjuryType.RightShin:
                    male = (18, 80);
                    female = (16, 77);
                    break;

                case InjuryType.Stomach:
                    male = (21, 37);
                    female = (21, 36);
                    break;

                case InjuryType.LeftArm:
                    male = (55, 25);
                    female = (58.5, 25);
                    break;

                case InjuryType.RightArm:
                    male = (91, 25);
                    female = (85, 25);
                    break;

                case InjuryType.LeftCalf:
                    male = (63, 80);
                    female = (66, 77);
                    break;

                case InjuryType.RightCalf:
                    male = (83, 80);
                    female = (77, 77);
                    break;

                case InjuryType.Adductors:
                    male = (21, 50);
                    female = (21, 45);
                    break;
                default:
                    break;
            }

            return isFemale ? _position == CanvasPosition.Left ? female.left - 4 : female.top : (object)(_position == CanvasPosition.Left ? male.left - 6 : male.top);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Convert([value], targetType, parameter, culture);

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
