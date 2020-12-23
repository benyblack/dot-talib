using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators
{
    public class RsiTests
    {
        private readonly double[] _ascendingData = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        private readonly double[] _descendingData = { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

        [Theory]
        [InlineData(2,1)]
        [InlineData(3,1)]
        [InlineData(5,1)]
        [InlineData(7,1)]
        [InlineData(8,1)]
        [InlineData(9, 1)]
        public void AverageGain_SimpleValues_Calculated(int period,double expected)
        {
            //When
            var result = Rsi.AverageGain(_ascendingData, period);

            //Then
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(2, 0)]
        [InlineData(3, 0)]
        [InlineData(5, 0)]
        [InlineData(7, 0)]
        [InlineData(8, 0)]
        [InlineData(9, 0)]
        public void AverageGain_Must_Return_Zero_On_Descending_Values(int period, double expected)
        {
            //When
            var result = Rsi.AverageGain(_descendingData, period);

            //Then
            Assert.Equal(expected, result);
        }

        [Fact]
        public void AverageGain_PeriodBiggerOrEqualToDataLength_ReturnZero()
        {
            //When
            var result = Rsi.AverageGain(_ascendingData, period: 10);

            //Then
            Assert.Equal(0, result);
        }



        [Fact]
        public void AverageLoss_PeriodBiggerOrEqualToDataLength_ReturnZero()
        {
            //When
            var result = Rsi.AverageLoss(_ascendingData, period: 10);

            //Then
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(3, 1)]
        [InlineData(5, 1)]
        [InlineData(7, 1)]
        [InlineData(8, 1)]
        [InlineData(9, 1)]
        public void AverageLoss_SimpleValues_Calculated(int period, double expected)
        {
            //When
            var result = Rsi.AverageLoss(_descendingData, period);

            //Then
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        public void RsiSingle_SimpleValues_Calculated(int period)
        {
            //When
            var result = Rsi.RsiSingle(_descendingData, period);
            var result2 = Rsi.RsiSingle(_ascendingData, period);

            //Then
            Assert.Equal(0D, result);
            Assert.Equal(100D, result2);
        }

        [Fact]
        public void RsiSingle_PeriodBiggerOrEqualToDataLength_ReturnNull()
        {
            //When
            var result = Rsi.RsiSingle(_descendingData, period: 10);

            //Then
            Assert.Null(result);
        }

        [Theory]
        [InlineData(9, null, null, null, null, null, null, null, null, null, 100D)]
        [InlineData(7, null, null, null, null, null, null, null, 100D, 100D, 100D)]
        [InlineData(6, null, null, null, null, null, null, 100D, 100D, 100D, 100D)]
        [InlineData(5, null, null, null, null, null, 100D, 100D, 100D, 100D, 100D)]
        [InlineData(4, null, null, null, null, 100D, 100D, 100D, 100D, 100D, 100D)]
        [InlineData(2, null, null, 100D, 100D, 100D, 100D, 100D, 100D, 100D, 100D)]
        public void Rsi_SimpleValues_Calculated(int period,params double?[] expected)
        {
            //When
            var result = Rsi.RSI(_ascendingData, period);
            //Then
            Assert.Equal(expected, result);
        }
    }
}