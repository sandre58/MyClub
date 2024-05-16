// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace MyClub.Teamup.Infrastructure.Packaging.Models
{
    [XmlRoot("sendedMails", Namespace = XmlConstants.MyClubNamespace)]
    public class SendedMailsPackage : List<SendedMailPackage>
    {
        public SendedMailsPackage() { }

        public SendedMailsPackage(IEnumerable<SendedMailPackage> enumerable) : base(enumerable) { }
    }
}
