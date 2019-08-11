using Talib.Indicators;
using Xunit;

namespace Talib.Tests
{
    public class RsiTests
    {
        private readonly double[] ascending_data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        private readonly double[] descending_data = { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

        [Fact]
        public void AverageGain_SimpleValues_Calculated()
        {
            //When
            var result = Rsi.AverageGain(ascending_data, period: 9);

            //Then
            Assert.Equal(1, result);
        }

        [Fact]
        public void AverageGain_PeriodBiggerOrEqualToDataLength_ReturnZero()
        {
            //When
            var result = Rsi.AverageGain(ascending_data, period: 10);

            //Then
            Assert.Equal(0, result);
        }

        [Fact]
        public void AverageLoss_PeriodBiggerOrEqualToDataLength_ReturnZero()
        {
            //When
            var result = Rsi.AverageLoss(ascending_data, period: 10);

            //Then
            Assert.Equal(0, result);
        }

        [Fact]
        public void AverageLoss_SimpleValues_Calculated()
        {
            //When
            var result = Rsi.AverageLoss(descending_data, period: 9);

            //Then
            Assert.Equal(1, result);
        }

        [Fact]
        public void RsiSingle_SimpleValues_Calculated()
        {
            //When
            var result = Rsi.RsiSingle(descending_data, period: 9);
            var result2 = Rsi.RsiSingle(ascending_data, period: 9);

            //Then
            Assert.Equal(0D, result);
            Assert.Equal(100D, result2);
        }

        [Fact]
        public void RsiSingle_PeriodBiggerOrEqualToDataLength_ReturnNull()
        {
            //When
            var result = Rsi.RsiSingle(descending_data, period: 10);

            //Then
            Assert.Null(result);
        }

        [Fact]
        public void Rsi_Simplevalues_Calculated()
        {
            //When
            var result = Rsi.RSI(ascending_data, period: 9);
            double?[] expected = {null,null,null,null,null,null,null,null,null,100};
            //Then
            Assert.Equal(expected, result);
        }
    }
}