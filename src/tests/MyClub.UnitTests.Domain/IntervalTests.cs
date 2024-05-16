// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Sequences;
using Xunit;

namespace MyClub.UnitTests.Domain
{
    public class IntervalTests
    {
        private class IntervalFake(int start, int end) : Interval<int>(start, end)
        {
        }

        [Fact]
        public void PropertiesAreValidWhenIntervalIsCreated()
        {
            var item = new IntervalFake(1, 10);

            Assert.Equal(1, item.Start);
            Assert.Equal(10, item.End);
        }

        [Theory]
        [InlineData(1, 1, 10)]
        [InlineData(5, 1, 10)]
        [InlineData(10, 1, 10)]
        public void ContainsReturnTrueWhenValueIsBetweenStartAndEnd(int value, int start, int end)
        {
            var item = new IntervalFake(start, end);

            Assert.True(item.Contains(value));
        }

        [Theory]
        [InlineData(-1, 1, 10)]
        [InlineData(0, 1, 10)]
        [InlineData(11, 1, 10)]
        public void ContainsReturnFalseWhenValueIsNotBetweenStartAndEnd(int value, int start, int end)
        {
            var item = new IntervalFake(start, end);

            Assert.False(item.Contains(value));
        }

        [Fact]
        public void ThrowExceptionWhenStartIsAfterEnd()
            => Assert.Throws<IsNotLowerOrEqualsThanException>(() => new IntervalFake(10, 5));
    }
}
