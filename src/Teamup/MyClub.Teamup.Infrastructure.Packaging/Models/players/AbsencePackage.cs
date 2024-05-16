﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    public class AbsencePackage : AuditablePackage
    {
        [XmlAttribute("type")]
        public int Type { get; set; }

        [XmlAttribute("label")]
        public string Label { get; set; } = string.Empty;

        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }
    }
}
