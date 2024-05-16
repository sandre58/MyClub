// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities.Geography;
using Xunit;

namespace MyClub.UnitTests.Domain
{
    public class AddressTests
    {
        [Fact]
        public void AddressesAreEqualsWhenPropertiesAreEquals()
        {
            var address1 = new Address("Street 1", "00000", "City", Country.France);
            var address2 = new Address("Street 1", "00000", "City", Country.France);

            Assert.Equal(address1, address2);
        }

        [Fact]
        public void AddressesAreNotEqualsWhenPropertiesAreNotEquals()
        {
            var address1 = new Address("Street", "00000", "City", Country.France);
            var address2 = new Address("Street 1", "00000", "City", Country.France);

            Assert.NotEqual(address1, address2);
        }
    }
}
