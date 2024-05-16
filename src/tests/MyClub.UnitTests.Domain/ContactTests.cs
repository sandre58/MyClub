// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain;
using MyNet.Utilities.Exceptions;
using Xunit;

namespace MyClub.UnitTests.Domain
{
    public class ContactTests
    {
        [Theory]
        [InlineData("dsdsdf.df")]
        [InlineData("@dfe")]
        [InlineData("jkjyuk")]
        [InlineData("gfegd@effef")]
        [InlineData("gfegd@effef.fr@fdf")]
        public void ThrowExceptionWhenEmailIsInvalid(string value)
            => Assert.Throws<InvalidEmailAddressException>(() => _ = new Email(value));

        [Theory]
        [InlineData("dfef@fdsdsdf.df")]
        [InlineData("fzefgrg.Frge@dfe.Fd")]
        public void NotThrowExceptionWhenEmailIsValid(string value)
            => Assert.Equal(value, new Email(value).Value);

        [Theory]
        [InlineData("fdgfggfg")]
        [InlineData("00fg005454")]
        public void ThrowExceptionWhenPhoneIsInvalid(string value)
    => Assert.Throws<InvalidPhoneException>(() => _ = new Phone(value));

        [Theory]
        [InlineData("0445878969")]
        [InlineData("04 45 87 89 69")]
        [InlineData("+334 45 87 89 69")]
        public void NotThrowExceptionWhenPhoneIsValid(string value)
            => Assert.Equal(value, new Phone(value).Value);
    }
}
