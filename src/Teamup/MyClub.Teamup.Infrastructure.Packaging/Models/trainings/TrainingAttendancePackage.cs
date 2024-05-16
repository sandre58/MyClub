// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("attendance")]
    public class TrainingAttendancePackage : EntityPackage
    {
        [XmlAttribute("playerId")]
        public Guid PlayerId { get; set; }

        [XmlAttribute("attendance")]
        public int Attendance { get; set; }

        [XmlElement("reason", IsNullable = true)]
        public string? Reason { get; set; }

        [XmlIgnore]
        public bool ReasonSpecified => !string.IsNullOrEmpty(Reason);

        [XmlElement("rating", IsNullable = true)]
        public double? Rating { get; set; }

        [XmlIgnore]
        public bool RatingSpecified => Rating.HasValue;

        [XmlElement("comment", IsNullable = true)]
        public string? Comment { get; set; }

        [XmlIgnore]
        public bool CommentSpecified => !string.IsNullOrEmpty(Comment);
    }
}
