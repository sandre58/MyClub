// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Teamup.Application.Messages
{
    public class TeamsOrderValidationMessage(bool isValid)
    {
        public bool IsValid { get; } = isValid;
    }
}
