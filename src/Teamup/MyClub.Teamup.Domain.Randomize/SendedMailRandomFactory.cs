// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Resources;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.CrossCutting.Localization;

namespace MyClub.Teamup.Domain.Randomize
{
    public static class SendedMailRandomFactory
    {

        public static IEnumerable<SendedMail> RandomSendedMails(IList<string>? addresses, DateTime? minDate = null, int min = 2, int max = 10)
            => Enumerable.Range(1, RandomGenerator.Int(min, max)).Select(_ => Random((addresses is not null && addresses.Any()) ? RandomGenerator.ListItems(addresses) : null, minDate));

        public static SendedMail Random(IEnumerable<string>? addresses = null, DateTime? minDate = null)
        {
            var item = new SendedMail(RandomGenerator.Date(minDate ?? DateTime.Today.BeginningOfYear(), DateTime.Today), SentenceGenerator.Sentence(3, 6), SentenceGenerator.Paragraph(15, 50, 2, 10), RandomGenerator.Bool())
            {
                State = RandomGenerator.Enum<SendingState>()
            };

            var toAddresses = addresses;
            item.ToAddresses.AddRange(toAddresses);

            item.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return item;
        }
    }
}
