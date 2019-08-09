using System;
using System.Linq;
using Talib.Indicators;
using Xunit;

namespace Talib.Tests {
    public class MaTests {

        private readonly double[] data = { 46.282, 45.614, 46.0328, 45.8931, 46.0826, 45.8433, 45.4245, 45.0955, 44.8264, 44.3278, 43.6124, 44.1497, 44.0902, 44.3389, 44.3189, 44.3289 };

        [Fact]
        public void SmaSingle_SimpleValues_calculated () {
            var ma = new MA ();

            var result = Math.Round (ma.SmaSingle (data, 10).Value, 4);

            Assert.Equal (44.4513D, result);
        }

        [Fact]
        public void SmaSingle_PeriodBiggerThanDataSize_ReturnNull () {
            var ma = new MA ();

            var result = ma.SmaSingle (data, 1000);

            Assert.Null (result);
        }

        [Fact]
        public void SmaSingle_PeriodZeroOrLess_ReturnNull () {
            var ma = new MA ();

            var result = ma.SmaSingle (data, 0);
            var result2 = ma.SmaSingle (data, -10);

            Assert.Null (result);
            Assert.Null (result2);
        }

        [Fact]
        public void Sma_SimpleValues_calculated () {
            var ma = new MA ();

            double[] result = ma.Sma (data, 3).Where (x => x != null).Select (x => Math.Round (x.Value, 4)).ToArray ();
            int nullCounts = ma.Sma (data, 3).Where (x => x == null).Count ();
            double[] expected = { 45.9763, 45.8466, 46.0028, 45.9397, 45.7835, 45.4544, 45.1155, 44.7499, 44.2555, 44.03, 43.9508, 44.1929, 44.2493, 44.3289 };
            Assert.Equal (expected, result);
            Assert.Equal (2, nullCounts);
        }

        [Fact]
        public void Sma_PeriodBiggerThanDataSize_ReturnNull () {
            var ma = new MA ();

            double?[] result = ma.Sma (data, 300);
            double?[] expected = { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };
            Assert.Equal (expected, result);
        }
    }
}