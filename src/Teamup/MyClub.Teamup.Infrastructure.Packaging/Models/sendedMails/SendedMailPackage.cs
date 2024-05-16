// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Serialization;
using MyClub.CrossCutting.Packaging.Models;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlType("sendedMail")]
    public class SendedMailPackage : AuditablePackage
    {
        [XmlAttribute("subject")]
        public string Subject { get; set; } = string.Empty;

        [XmlElement("body", IsNullable = true)]
        public string? Body { get; set; }

        [XmlIgnore]
        public bool BodySpecified => !string.IsNullOrEmpty(Body);

        [XmlAttribute("sendACopy")]
        public bool SendACopy { get; set; }

        [XmlAttribute("date")]
        public DateTime Date { get; set; }

        [XmlAttribute("state")]
        public int State { get; set; }

        [XmlAttribute("toAddresses")]
        public string ToAddresses { get; set; } = string.Empty;
    }
}
